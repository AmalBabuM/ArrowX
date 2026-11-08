using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Bow : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    public Transform notch;
    private PullInteraction pullInteraction;
    private LineRenderer lineRenderer;

    [Header("Sound")]
    public AudioClip grabClip;

    [Header("Quiver")]
    public Transform quiver;
    public Vector2 quiverOffset;

    protected override void Awake()
    {
        base.Awake();
        pullInteraction = GetComponentInChildren<PullInteraction>();
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            if (isSelected)
                UpdateBow(pullInteraction.PullAmount);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (!isSelected)
            UpdateBow(0f);
    }

    private void UpdateBow(float value)
    {
        Vector3 linePosition = Vector3.forward * Mathf.Lerp(-0.25f, -0.5f, value);
        notch.localPosition = linePosition;
        lineRenderer.SetPosition(1, linePosition);
    }

    public void OffsetQuiver(SelectEnterEventArgs args)
    {
        var controller = args.interactorObject.transform.GetComponentInParent<XRController>();
        if (controller != null)
        {
            bool right = controller.controllerNode == XRNode.RightHand;
            quiver.localPosition = new Vector3(quiverOffset.x * (right ? -1 : 1), quiverOffset.y, quiver.localPosition.z);
        }
    }
}
