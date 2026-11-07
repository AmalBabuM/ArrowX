using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Supplies a one-frame select input to the teleport ray when the thumbstick is released.
/// </summary>
public class XRReleaseController : XRController
{
    bool m_Selected;

    protected override void OnEnable()
    {
        enableInputActions = true;
        base.OnEnable();
    }

    protected override void UpdateInput(XRControllerState controllerState)
    {
        controllerState.selectInteractionState.SetFrameState(m_Selected);
        m_Selected = false;
    }

    public void Select()
    {
        m_Selected = true;
    }
}
