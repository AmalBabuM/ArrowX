using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Notch), typeof(PullInteraction))]
public sealed class ArrowTrajectoryPreview : MonoBehaviour
{
    [SerializeField] private Material trajectoryMaterial;

    private const int SampleCount = 151;
    private const int RingSegments = 32;
    private const float RingRadius = 0.045f;
    private readonly Vector3[] samples = new Vector3[SampleCount];
    private readonly Vector3[] ringPoints = new Vector3[RingSegments];
    private Notch notch;
    private PullInteraction pull;
    private Bow bow;
    private LineRenderer arc;
    private LineRenderer marker;

    private void Awake()
    {
        notch = GetComponent<Notch>();
        pull = GetComponent<PullInteraction>();
        bow = GetComponentInParent<Bow>();

        arc = CreateLine("Trajectory Arc", 0.009f);
        arc.widthCurve = AnimationCurve.Linear(0f, 0.35f, 1f, 1f);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.2f, 0.92f, 0.86f), 0f), new GradientColorKey(new Color(0.68f, 1f, 0.95f), 1f) },
            new[] { new GradientAlphaKey(0.35f, 0f), new GradientAlphaKey(0.9f, 1f) });
        arc.colorGradient = gradient;

        marker = CreateLine("Trajectory Impact", 0.007f);
        marker.loop = true;
        marker.startColor = marker.endColor = new Color(0.72f, 1f, 0.94f);
        marker.positionCount = RingSegments;
    }

    private LineRenderer CreateLine(string name, float width)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(transform, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = trajectoryMaterial;
        line.widthMultiplier = width;
        line.useWorldSpace = true;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;
        return line;
    }

    private void LateUpdate()
    {
        var arrow = notch.CurrentArrow;
        if (arrow == null || !bow.isSelected || !pull.isSelected || pull.PullAmount <= 0.01f ||
            (!TriggerHeld(pull.firstInteractorSelecting) && !TriggerHeld(bow.firstInteractorSelecting)))
        {
            HidePreview();
            return;
        }

        arc.positionCount = arrow.GetTrajectory(pull.PullAmount, samples, out var impact);
        arc.SetPositions(samples);
        arc.enabled = true;

        marker.enabled = impact.collider != null;
        if (marker.enabled)
            DrawImpactMarker(impact);
    }

    private static bool TriggerHeld(IXRSelectInteractor interactor)
    {
        return interactor is XRBaseInputInteractor input && input.logicalActivateState.isPerformed;
    }

    private void DrawImpactMarker(RaycastHit hit)
    {
        var normal = hit.normal;
        var reference = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right;
        var tangent = Vector3.Cross(normal, reference).normalized;
        var bitangent = Vector3.Cross(normal, tangent);
        var center = hit.point + normal * 0.003f;
        for (int i = 0; i < ringPoints.Length; i++)
        {
            var angle = i * (2f * Mathf.PI / RingSegments);
            ringPoints[i] = center + RingRadius * (Mathf.Cos(angle) * tangent + Mathf.Sin(angle) * bitangent);
        }
        marker.SetPositions(ringPoints);
    }

    private void HidePreview()
    {
        arc.enabled = false;
        marker.enabled = false;
    }

    private void OnDisable()
    {
        HidePreview();
    }
}
