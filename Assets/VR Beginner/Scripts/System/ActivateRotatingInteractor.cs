using System;
using UnityEngine;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor))]
public class ActivateRotatingInteractor : MonoBehaviour
{
    public DialInteractable DialToActivate;

    UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor m_Interactor;
    void Start()
    {
        m_Interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
        m_Interactor.onSelectEntered.AddListener(Activated);
    }

    void Activated(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        DialToActivate.RotatingRigidbody = interactable.GetComponentInChildren<Rigidbody>();
        DialToActivate.gameObject.SetActive(true);

        interactable.interactionLayerMask = 0;
    }
}
