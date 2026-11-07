using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor))]
public class InteractorEventDispatcher : MonoBehaviour
{
    public UnityEvent<XRBaseInteractable> OnSelectedEnter;

    void Awake()
    {
        var interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
        interactor.selectEntered.AddListener(args => OnSelectedEnter.Invoke(args.interactableObject as XRBaseInteractable));
    }
}
