using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class Notch : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
{
    private Bow bow;
    private PullInteraction pullInteraction;
    private Arrow currentArrow;

    [Header("Sound")]
    public AudioClip attatchClip;
    protected override void Awake()
    {
        base.Awake();
        bow = GetComponentInParent<Bow>();
        pullInteraction = GetComponent<PullInteraction>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        pullInteraction.selectExited.AddListener(TryToReleaseArrow);
        bow.lastSelectExited.AddListener(OnBowReleased);
    }

    protected override void OnDisable()
    {
        pullInteraction.selectExited.RemoveListener(TryToReleaseArrow);
        bow.lastSelectExited.RemoveListener(OnBowReleased);
        base.OnDisable();
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        if (bow.isSelected && !hasSelection && !pullInteraction.isSelected &&
            args.interactableObject is Arrow arrow && arrow.firstInteractorSelecting is HandInteractor hand)
        {
            args.manager.SelectExit((IXRSelectInteractor)hand, arrow);
            args.manager.SelectEnter((IXRSelectInteractor)hand, pullInteraction);
            if (hand.IsSelecting(pullInteraction))
            {
                args.manager.SelectEnter((IXRSelectInteractor)this, arrow);
                if (IsSelecting(arrow))
                {
                    NotchSounds(attatchClip, 3, 3.3f, 5);
                    return;
                }

                args.manager.SelectCancel((IXRSelectInteractor)hand, pullInteraction);
            }
            args.manager.SelectEnter((IXRSelectInteractor)hand, arrow);
        }
    }

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return bow.isSelected && interactable is Arrow && base.CanHover(interactable);
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return bow.isSelected && pullInteraction.isSelected &&
            interactable is Arrow && base.CanSelect(interactable);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        currentArrow = args.interactableObject as Arrow;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        currentArrow = null;
    }

    private void TryToReleaseArrow(SelectExitEventArgs args)
    {
        ReleaseArrow(args.manager, bow.isSelected && !args.isCanceled);
    }

    private void OnBowReleased(SelectExitEventArgs args)
    {
        args.manager.CancelInteractableSelection((IXRSelectInteractable)pullInteraction);
        ReleaseArrow(args.manager, false);
    }

    private void ReleaseArrow(XRInteractionManager manager, bool fire)
    {
        var arrow = currentArrow;
        if (arrow == null)
            return;

        manager.SelectExit((IXRSelectInteractor)this, arrow);
        if (fire)
            arrow.Release(pullInteraction.PullAmount);
        else
            arrow.DropFromBow();
    }

    void NotchSounds(AudioClip clip, float minPitch, float maxPitch, int id)
    {
        SFXPlayer.Instance.PlaySFX(clip, transform.position, new SFXPlayer.PlayParameters()
        {
            Pitch = Random.Range(minPitch, maxPitch),
            Volume = 1.0f,
            SourceID = id
        });
    }

}
