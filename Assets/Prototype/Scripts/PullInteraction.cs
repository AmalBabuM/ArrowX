using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PullInteraction : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    private const float PullHandRevealPadding = 0.04f;

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
    [SerializeField, Min(0f)] private float hidePullHandDistance = 0.20f;

    private Renderer[] rightHandRenderers, leftHandRenderers;
    private GameObject pullHand;
    private Renderer[] pullHandRenderers;
    private Camera headsetCamera;
    private bool pullHandHidden;

    protected override void Awake()
    {
        base.Awake();
        bow = GetComponentInParent<Bow>();
        rightHandRenderers = rightHand?.GetComponentsInChildren<Renderer>(true);
        leftHandRenderers = leftHand?.GetComponentsInChildren<Renderer>(true);
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return bow.isSelected && base.IsSelectableBy(interactor);
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
        ShowHand(false);

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
        if (pullHand != null)
        {
            SetPullHandHidden(false);
            pullHand.SetActive(false);
            pullHand = null;
            pullHandRenderers = null;
        }

        if (!show || !showHandsOnPull)
            return;

        var bowController = bow.firstInteractorSelecting?.transform.GetComponentInParent<XRController>();
        pullHand = bowController?.controllerNode switch
        {
            XRNode.RightHand => leftHand,
            XRNode.LeftHand => rightHand,
            _ => null
        };
        if (pullHand == null)
            return;

        pullHandRenderers = pullHand == rightHand ? rightHandRenderers : leftHandRenderers;
        headsetCamera = Camera.main;
        pullHand.SetActive(true);
        UpdatePullHandVisibility();
    }

    private void LateUpdate()
    {
        if (pullHand != null)
            UpdatePullHandVisibility();
    }

    private void UpdatePullHandVisibility()
    {
        float distance = hidePullHandDistance + (pullHandHidden ? PullHandRevealPadding : 0f);
        bool hide = hidePullHandDistance > 0f && headsetCamera != null &&
            (headsetCamera.transform.position - pullHand.transform.position).sqrMagnitude < distance * distance;
        SetPullHandHidden(hide);
    }

    private void SetPullHandHidden(bool hide)
    {
        if (pullHandHidden == hide)
            return;

        pullHandHidden = hide;
        foreach (var handRenderer in pullHandRenderers)
            handRenderer.forceRenderingOff = hide;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ShowHand(false);
    }
}
