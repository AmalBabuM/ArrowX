using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class PrefabSpawner : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
{
    [SerializeField] GameObject prefab = default;
    public Arrow currentArrow;
    private bool needsArrow = true;

    [Header("Sound")]
    public AudioClip grabClip;

    private void LateUpdate()
    {
        if (!needsArrow)
            return;

        if (currentArrow == null)
        {
            var arrowPrefab = prefab.GetComponent<Arrow>();
            var socket = GetAttachTransform(arrowPrefab);
            var offset = arrowPrefab.attachTransform != null ? arrowPrefab.attachTransform.localPosition : Vector3.zero;
            currentArrow = Instantiate(arrowPrefab, socket.position - socket.rotation * offset, socket.rotation);
        }

        interactionManager.SelectEnter((IXRSelectInteractor)this, currentArrow);
        needsArrow = !IsSelecting(currentArrow);
    }

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return ReferenceEquals(interactable, currentArrow) && base.CanHover(interactable);
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return ReferenceEquals(interactable, currentArrow) && base.CanSelect(interactable);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (!ReferenceEquals(args.interactableObject, currentArrow))
            return;

        if (!args.isCanceled)
            currentArrow = null;
        needsArrow = true;
    }
}
