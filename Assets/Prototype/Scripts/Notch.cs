using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class Notch : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
{
    private PullInteraction pullInteraction;
    private Arrow currentArrow;

    [Header("Sound")]
    public AudioClip attatchClip;
    protected override void Awake()
    {
        base.Awake();
        pullInteraction = GetComponent<PullInteraction>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        pullInteraction.selectExited.AddListener(TryToReleaseArrow);
    }

    protected override void OnDisable()
    {
        pullInteraction.selectExited.RemoveListener(TryToReleaseArrow);
        base.OnDisable();
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        if (!hasSelection && !pullInteraction.isSelected && args.interactableObject is Arrow arrow &&
            arrow.firstInteractorSelecting is HandInteractor hand)
        {
            args.manager.SelectExit((IXRSelectInteractor)hand, arrow);
            args.manager.SelectEnter((IXRSelectInteractor)this, arrow);
            args.manager.SelectEnter((IXRSelectInteractor)hand, pullInteraction);

            NotchSounds(attatchClip, 3, 3.3f, 5);
        }
    }

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return interactable is Arrow && base.CanHover(interactable);
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return interactable is Arrow && base.CanSelect(interactable);
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
        if (currentArrow == null)
            return;

        var arrow = currentArrow;
        args.manager.SelectExit((IXRSelectInteractor)this, arrow);
        if (!args.isCanceled)
            arrow.Release(pullInteraction.PullAmount);
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
