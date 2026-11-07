using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor
{

    [Header("Sounds")]
    public AudioClip bowGrabClip;
    public AudioClip arrowGrabClip;

    public void ForceInteract(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        OnSelectEnter(interactable);
    }

    public void ForceDeinteract(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        OnSelectExit(interactable);
    }

    public void HandDetection(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        if(interactable is Arrow arrow)
        {
            arrow.sphereCollider.enabled = false;
            HandSounds(arrowGrabClip, 3.5f, 3,.8f, 7);
        }

        if (interactable is Bow bow)
        {
            HandSounds(bowGrabClip, 2.5f, 2.5f,.8f, -3);
        }


    }


    void HandSounds(AudioClip clip, float minPitch, float maxPitch,float volume, int id)
    {
        SFXPlayer.Instance.PlaySFX(clip, transform.position, new SFXPlayer.PlayParameters()
        {
            Pitch = Random.Range(minPitch, maxPitch),
            Volume = volume,
            SourceID = id
        });
    }
}
