using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class MovingTarget : MonoBehaviour
{
    private enum Pose { Down, Rising, Standing, Falling }

    private const float RiseDuration = 0.42f;
    private const float FallDuration = 0.26f;
    private const float FloorTilt = -87f;
    internal const int BullseyePoints = 10;

    [SerializeField] private MeshCollider hitCollider;

    internal event Action<int> Hit;
    internal event Action Lowered;

    private Rigidbody body;
    private Quaternion upright;
    private Bounds faceBounds;
    private int depthAxis;
    private Pose pose;
    private float elapsed;
    private float standingTime;
    private float uprightUntil;
    private float fallStartTilt;
    private float currentTilt;
    private Arrow embeddedArrow;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        upright = body.rotation;
        faceBounds = hitCollider.sharedMesh.bounds;
        var extents = Vector3.Scale(faceBounds.extents, hitCollider.transform.lossyScale);
        depthAxis = Mathf.Abs(extents.x) <= Mathf.Abs(extents.y) ? 0 : 1;
        if (Mathf.Abs(extents.z) < Mathf.Abs(extents[depthAxis]))
            depthAxis = 2;

        hitCollider.enabled = false;
    }

    internal void RiseAt(Vector3 position, float window)
    {
        var rotation = upright * Quaternion.AngleAxis(FloorTilt, Vector3.forward);
        transform.SetPositionAndRotation(position, rotation);
        body.position = position;
        body.rotation = rotation;
        pose = Pose.Rising;
        elapsed = 0f;
        currentTilt = FloorTilt;
        standingTime = window;
        gameObject.SetActive(true);
    }

    internal bool TryKnockDown(Arrow arrow, RaycastHit impact)
    {
        if (pose != Pose.Standing || Time.fixedTime >= uprightUntil || impact.collider != hitCollider)
            return false;

        int points = ScoreImpact(impact.point);
        embeddedArrow = arrow;
        Lower();
        Hit?.Invoke(points);
        return true;
    }

    internal void Lower()
    {
        if (pose != Pose.Rising && pose != Pose.Standing)
            return;

        hitCollider.enabled = false;
        pose = Pose.Falling;
        elapsed = 0f;
        fallStartTilt = currentTilt;
    }

    private void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        switch (pose)
        {
            case Pose.Rising:
                if (elapsed < RiseDuration)
                {
                    MoveToTilt(Mathf.SmoothStep(FloorTilt, 0f, elapsed / RiseDuration));
                }
                else
                {
                    MoveToTilt(0f);
                    uprightUntil = Time.fixedTime + standingTime;
                    pose = Pose.Standing;
                    hitCollider.enabled = true;
                }
                break;
            case Pose.Standing:
                if (Time.fixedTime >= uprightUntil)
                    Lower();
                break;
            case Pose.Falling:
                float t = Mathf.Clamp01(elapsed / FallDuration);
                MoveToTilt(Mathf.Lerp(fallStartTilt, FloorTilt, t * t));
                if (t >= 1f)
                {
                    gameObject.SetActive(false);
                    Lowered?.Invoke();
                }
                break;
        }
    }

    private void MoveToTilt(float tilt)
    {
        currentTilt = tilt;
        body.MoveRotation(upright * Quaternion.AngleAxis(tilt, Vector3.forward));
    }

    private int ScoreImpact(Vector3 point)
    {
        var offset = hitCollider.transform.InverseTransformPoint(point) - faceBounds.center;
        offset[depthAxis] = 0f;
        var extents = faceBounds.extents;
        extents[depthAxis] = 1f;
        var normalized = new Vector3(offset.x / extents.x, offset.y / extents.y, offset.z / extents.z);
        // The cutoffs follow the rings printed on the target face.
        return normalized.magnitude switch
        {
            <= 0.105f => BullseyePoints,
            <= 0.25f => 9,
            <= 0.41f => 8,
            <= 0.6f => 7,
            <= 0.82f => 6,
            _ => 5
        };
    }

    private void OnDisable()
    {
        pose = Pose.Down;
        hitCollider.enabled = false;
        if (embeddedArrow != null)
            Destroy(embeddedArrow.gameObject);
        embeddedArrow = null;
    }
}
