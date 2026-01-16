using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;

public class TurnoutMachineController : MonoBehaviour, IController
{
    public TurnoutID turnoutID;
    public TurnoutState currentLocalTurnoutState;
    public TurnoutState targetLocalTurnoutState;
    public XRSocketInteractor xRSocketInteractor;


    public XRKnob xRKnob;
    public XRGrabInteractable xRGrabInteractable;
    private float currentValue;
    private float deathFloat = 0.05f;
    private Rigidbody handlerRb;

    public RotationOnlySync rotationOnlySync;
    public TurnoutMoveTByKnob turnoutMoveTByKnob;

    //UI
    public TeleportationAnchor teleportationAnchor;
    public Button cutPowerButton;
    public Transform butterflyKey;
    private readonly string cutPowerTag = "CutPower";
    private bool isPowerCut;
    private Animator animator;

    //Modle
    private TurnoutModel turnoutModel;

    public void Start()
    {
        xRSocketInteractor = GetComponentInChildren<XRSocketInteractor>();
        rotationOnlySync = GetComponentInChildren<RotationOnlySync>();
        turnoutMoveTByKnob = GetComponentInChildren<TurnoutMoveTByKnob>();

        if (xRSocketInteractor != null)
        {
            xRSocketInteractor.selectEntered.AddListener(GetInteractables);
        }

        animator = GetComponent<Animator>();
        cutPowerButton.onClick.AddListener(CutPower);

        //QFramework
        turnoutModel = this.GetModel<TurnoutModel>();
    }

    private void GetInteractables(SelectEnterEventArgs args)
    {
        if (args.interactableObject != null)
        {
            rotationOnlySync.SetTarget(args.interactableObject.transform.GetChild(0));
            turnoutMoveTByKnob.knob = args.interactableObject.transform.GetComponentInParent<XRKnob>();
            rotationOnlySync.transform.GetComponent<MeshRenderer>().enabled = true;
            xRKnob = args.interactableObject.transform.GetComponent<XRKnob>();
            if (xRKnob != null)
            {
                // currentValue = xRKnob.value;
                xRKnob.onValueChange.AddListener(EnableGrab);
            }

            xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
            if (xRKnob != null && xRGrabInteractable != null)
            {
                handlerRb = xRGrabInteractable.transform.GetComponent<Rigidbody>();
                DisableGrab(xRGrabInteractable);
            }
        }
    }

    private void DisableGrab(XRGrabInteractable xRGrabInteractable)
    {
        xRGrabInteractable.enabled = false;
        handlerRb.useGravity = false;
        xRGrabInteractable.transform.position = xRSocketInteractor.transform.position;
        xRGrabInteractable.transform.rotation = xRSocketInteractor.transform.rotation;
        xRKnob.enabled = true;
    }

    private void EnableGrab(float value)
    {
        if (currentValue <= 0 + deathFloat)
        {
            if (value >= 1 - deathFloat)
            {
                xRKnob.enabled = false;
                xRGrabInteractable.enabled = true;
                handlerRb.useGravity = true;
                currentValue = 1;
                rotationOnlySync.RemoveTarget();
                rotationOnlySync.transform.GetComponent<MeshRenderer>().enabled = false;
                currentLocalTurnoutState = TurnoutState.Reverse;
                turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].TurnoutState = currentLocalTurnoutState;
            }
        }

        if (currentValue >= 1 - deathFloat)
        {
            if (value <= 0 + deathFloat)
            {
                xRKnob.enabled = false;
                xRGrabInteractable.enabled = true;
                handlerRb.useGravity = true;
                currentValue = 0;
                rotationOnlySync.RemoveTarget();
                rotationOnlySync.transform.GetComponent<MeshRenderer>().enabled = false;
                currentLocalTurnoutState = TurnoutState.Normal;
                turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].TurnoutState = currentLocalTurnoutState;
            }
        }
    }

    private void CutPower()
    {
        if (!isPowerCut)
        {
            cutPowerButton.gameObject.SetActive(false);
            butterflyKey.gameObject.SetActive(true);
            animator.SetTrigger(cutPowerTag);
        }
    }

    public void HideButterflyKey()
    {
        butterflyKey.gameObject.SetActive(false);
        isPowerCut = true;
        turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].IsPowerCut = isPowerCut;
    }

    private void Update()
    {
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}