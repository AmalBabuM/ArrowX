using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PullInteraction : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    private Bow bow;
    public float PullAmount { get; private set; } = 0.0f;
    public Transform start, end;
    private IXRSelectInteractor pullingInteractor;

    [Header("Polish")]
    public LineRenderer stringLine;
    [ColorUsage(true,true)]
    public Color stringNormalCol, stringPulledCol;
    public ParticleSystem lineParticle;
    public bool showHandsOnPull = true;
    public GameObject rightHand, leftHand;

    protected override void Awake()
    {
        base.Awake();
        bow = GetComponentInParent<Bow>();
    }
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        pullingInteractor = args.interactorObject;
        base.OnSelectEntered(args);
        if (pullingInteractor is XRBaseInputInteractor inputInteractor)
            inputInteractor.SendHapticImpulse(.5f, .05f);
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        pullingInteractor = null;
        PullAmount = 0f;

        //polish
        stringLine.material.SetColor("_EmissionColor", stringNormalCol);
        lineParticle.Play();
    }
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (pullingInteractor != null)
            {
                Vector3 pullPosition = pullingInteractor.transform.position;
                PullAmount = CalculatePull(pullPosition);

                //haptic
                if (PullAmount > .3f && pullingInteractor is XRBaseInputInteractor inputInteractor)
                    inputInteractor.SendHapticImpulse(PullAmount / 5f, .05f);

                //polish
                stringLine.material.SetColor("_EmissionColor", 
                    Color.Lerp(stringNormalCol, stringPulledCol, PullAmount));
            }
        }
    }
    private float CalculatePull(Vector3 pullPosition)
    {
        Vector3 pullDirection = pullPosition - start.position;
        Vector3 targetDirection = end.position - start.position;
        float maxLength = targetDirection.magnitude;

        targetDirection.Normalize();
        float pullValue = Vector3.Dot(pullDirection, targetDirection) / maxLength;
        return Mathf.Clamp(pullValue, 0, 1);
    }

    public void ShowHand(bool show)
    {
        if (show)
        {
            var controller = bow.firstInteractorSelecting?.transform.GetComponentInParent<XRController>();
            if (showHandsOnPull && controller != null)
            {
                leftHand.SetActive(controller.controllerNode == XRNode.RightHand);
                rightHand.SetActive(controller.controllerNode == XRNode.LeftHand);
            }
        }
        else
        {
            if (showHandsOnPull)
            {
                leftHand.SetActive(false);
                rightHand.SetActive(false);
            }
        }
    }


}
