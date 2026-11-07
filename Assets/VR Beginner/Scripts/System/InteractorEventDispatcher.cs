using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor))]
public class InteractorEventDispatcher : MonoBehaviour
{
    public XRInteractorEvent OnSelectedEnter;

    void Awake()
    {
        var interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
        interactor.onSelectEntered.AddListener(interactable => { OnSelectedEnter.Invoke(interactable); } );
    }
}
