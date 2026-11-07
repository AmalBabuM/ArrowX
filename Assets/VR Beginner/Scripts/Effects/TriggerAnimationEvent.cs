using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;


[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor))]
public class TriggerAnimationEvent : MonoBehaviour
{
    public string TriggerName;

    int m_TriggerID;
    
    void Start()
    {
        m_TriggerID = Animator.StringToHash(TriggerName);
        var interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
        interactor.onSelectEntered.AddListener(TriggerAnim);
    }

    public void TriggerAnim(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        var animator = interactable.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.SetTrigger(TriggerName);
        }

        interactable.interactionLayerMask &= ~(1<<LayerMask.NameToLayer("Hands"));
    }
}
