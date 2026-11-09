using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class MovingTarget : MonoBehaviour
{
    private const float FallDuration = 0.26f;
    private const float RiseDuration = 0.42f;

    [SerializeField] private Collider hitCollider;
    [SerializeField, Min(0f)] private float travel = 0.38f;
    [SerializeField, Min(0f)] private float cyclesPerSecond = 0.32f;
    [SerializeField, Min(0f)] private float downTime = 2.25f;

    internal event Action<MovingTarget> KnockedDown;
    internal float SpeedMultiplier { get; set; } = 1f;

    private Rigidbody body;
    private Vector3 home;
    private Quaternion upright;
    private float phase;
    private float recoveryElapsed = -1f;
    private Arrow embeddedArrow;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        home = body.position;
        upright = body.rotation;
    }

    internal void SetPhase(float radians)
    {
        phase = radians;
    }

    internal void KnockDown(Arrow arrow)
    {
        if (recoveryElapsed >= 0f)
            return;

        embeddedArrow = arrow;
        hitCollider.enabled = false;
        recoveryElapsed = 0f;
        KnockedDown?.Invoke(this);
    }

    private void FixedUpdate()
    {
        float tilt = 0f;
        if (recoveryElapsed < 0f)
        {
            phase += Time.fixedDeltaTime * cyclesPerSecond * SpeedMultiplier * 2f * Mathf.PI;
        }
        else
        {
            recoveryElapsed += Time.fixedDeltaTime;
            float fall = Mathf.Clamp01(recoveryElapsed / FallDuration);
            float rise = Mathf.Clamp01((recoveryElapsed - FallDuration - downTime) / RiseDuration);
            tilt = -87f * fall * fall * (1f - Mathf.SmoothStep(0f, 1f, rise));

            if (rise >= 1f)
            {
                if (embeddedArrow != null)
                    Destroy(embeddedArrow.gameObject);

                embeddedArrow = null;
                recoveryElapsed = -1f;
                hitCollider.enabled = true;
            }
        }

        var offset = Vector3.forward * (Mathf.Sin(phase) * travel);
        body.Move(home + offset, upright * Quaternion.AngleAxis(tilt, Vector3.forward));
    }
}
