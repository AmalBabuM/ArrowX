using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Arrow : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    private struct Flight
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 tipOffset;
        public Vector3 Tip => position + rotation * tipOffset;
    }

    public float speed = 1000f;
    public Transform tip;
    private bool inAir;
    private Flight flight;
    private RaycastHit[] flightHits = new RaycastHit[8];
    private Rigidbody rb;
    public Collider sphereCollider;

    [Header("Particles")]
    public ParticleSystem trailParticle;
    public ParticleSystem hitParticle;
    public TrailRenderer trailRenderer;

    [Header("Sound")]
    public AudioClip launchClip;
    public AudioClip hitClip;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!inAir)
            return;

        if (StepFlight(ref flight, out var hit) && ResolveImpact(hit, out var attachment))
        {
            var impactPosition = flight.position + hit.point - flight.Tip;
            rb.interpolation = RigidbodyInterpolation.None;
            transform.SetPositionAndRotation(impactPosition, flight.rotation);
            if (attachment != null)
                transform.SetParent(attachment);
            Stop();
            return;
        }

        rb.Move(flight.position, flight.rotation);
    }

    private bool ResolveImpact(RaycastHit hit, out Transform attachment)
    {
        var body = hit.rigidbody;
        attachment = body != null ? hit.transform : null;
        if (body == null)
            return true;

        if (body.TryGetComponent<MovingTarget>(out var target))
            return target.TryKnockDown(this, hit);

        if (body.TryGetComponent<Lantern>(out var lantern))
            lantern.TurnOn();

        if (body.TryGetComponent<Potion>(out var potion))
        {
            potion.BreakPotion();
            return false;
        }

        body.AddForce(flight.velocity, ForceMode.Impulse);
        return true;
    }
    private void Stop()
    {
        inAir = false;
        SetPhysics(false);

        ArrowParticles(false);
        ArrowSounds(hitClip, 1.5f, 2, .8f, -2);
    }

    public void Release(float value)
    {
        inAir = true;
        throwOnDetach = false;
        SetPhysics(false);
        foreach (var arrowCollider in colliders)
            arrowCollider.enabled = false;
        interactionLayers = 0;
        flight = BeginFlight(value);

        ArrowParticles(true);
        ArrowSounds(launchClip, 4.2f + (.6f*value), 4.4f + (.6f*value),Mathf.Max(.7f,value), -1);
    }

    public void DropFromBow()
    {
        sphereCollider.enabled = true;
        SetPhysics(true);
    }

    private void SetPhysics(bool usePhysics)
    {
        rb.useGravity = usePhysics;
        rb.isKinematic = !usePhysics;
    }

    private Flight BeginFlight(float power)
    {
        var rotation = transform.rotation;
        return new Flight
        {
            position = transform.position,
            rotation = rotation,
            velocity = transform.forward * (power * speed / rb.mass),
            tipOffset = Quaternion.Inverse(rotation) * (tip.position - transform.position)
        };
    }

    internal int GetTrajectory(float power, Vector3[] points, out RaycastHit impact)
    {
        var prediction = BeginFlight(power);
        points[0] = tip.position;
        for (int i = 1; i < points.Length; i++)
        {
            if (StepFlight(ref prediction, out impact))
            {
                points[i] = impact.point;
                return i + 1;
            }
            points[i] = prediction.Tip;
        }

        impact = default;
        return points.Length;
    }

    private bool StepFlight(ref Flight state, out RaycastHit impact)
    {
        var previousTip = state.Tip;
        var step = Time.fixedDeltaTime;
        var gravity = Physics.gravity;
        state.position += (state.velocity + gravity * (step * 0.5f)) * step;
        state.velocity += gravity * step;
        if (state.velocity.sqrMagnitude > 0.000001f)
            state.rotation = Quaternion.LookRotation(state.velocity, state.rotation * Vector3.up);

        var segment = state.Tip - previousTip;
        var distance = segment.magnitude;
        impact = default;
        if (distance <= 0f)
            return false;

        int hitCount;
        while ((hitCount = Physics.RaycastNonAlloc(previousTip, segment / distance, flightHits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == flightHits.Length)
        {
            System.Array.Resize(ref flightHits, flightHits.Length * 2);
        }

        var nearest = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            var candidate = flightHits[i];
            if (candidate.rigidbody != rb && candidate.distance < nearest)
            {
                nearest = candidate.distance;
                impact = candidate;
            }
        }

        return impact.collider != null;
    }

    public void ArrowHaptic(HoverEnterEventArgs args)
    {
        if (args.interactorObject is HandInteractor hand)
            hand.SendHapticImpulse(.7f, .05f);
    }

    void ArrowParticles(bool release)
    {
        if (release)
        {
            trailParticle.Play();
            trailRenderer.emitting = true;
        }
        else
        {
            trailParticle.Stop(); 
            hitParticle.Play();
            trailRenderer.emitting = false;
        }
    }

    void ArrowSounds(AudioClip clip, float minPitch, float maxPitch,float volume, int id)
    {
        SFXPlayer.Instance.PlaySFX(clip, transform.position, new SFXPlayer.PlayParameters()
        {
            Pitch = Random.Range(minPitch, maxPitch),
            Volume = volume,
            SourceID = id
        });
    }

}
