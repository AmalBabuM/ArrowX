using UnityEngine;


using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Subclass of the classic Socket Interactor from the Interaction toolkit that will only accept object with the right
/// SocketTarget 
/// </summary>
public class XRExclusiveSocketInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
{
    public string AcceptedType;

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return base.CanSelect(interactable) && MatchesSocketType(interactable);
    }

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return base.CanHover(interactable) && MatchesSocketType(interactable);
    }

    private bool MatchesSocketType(IXRInteractable interactable)
    {
        return interactable.transform.TryGetComponent(out SocketTarget target) &&
            target.SocketType == AcceptedType;
    }
}
