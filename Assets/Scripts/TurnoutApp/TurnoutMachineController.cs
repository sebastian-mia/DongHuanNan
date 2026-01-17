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
    public XRSocketInteractor handlerSocketInteractor;
    public XRSocketInteractor turnoutLockNormalSocketInteractor;
    public XRSocketInteractor turnoutLockReverseSocketInteractor;
    private readonly string turnoutMachineHandlerTag = "TurnoutMachineHandler";


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

    //LockTurnout
    private readonly string lockTurnoutTag = "LockTurnout";
    private readonly string turnoutNormalSideTag = "NormalSide";
    private readonly string turnoutReverseSideTag = "ReverseSide";


    //Modle
    private TurnoutModel turnoutModel;

    public void Start()
    {
        var xRSocketInteractors = GetComponentsInChildren<XRSocketInteractor>();
        for (int i = 0; i < xRSocketInteractors.Length; i++)
        {
            if (xRSocketInteractors[i].CompareTag(turnoutMachineHandlerTag))
            {
                handlerSocketInteractor = xRSocketInteractors[i];
            }

            if (xRSocketInteractors[i].CompareTag(turnoutNormalSideTag))
            {
                turnoutLockNormalSocketInteractor = xRSocketInteractors[i];
            }

            if (xRSocketInteractors[i].CompareTag(turnoutReverseSideTag))
            {
                turnoutLockReverseSocketInteractor = xRSocketInteractors[i];
            }
        }

        rotationOnlySync = GetComponentInChildren<RotationOnlySync>();
        turnoutMoveTByKnob = GetComponentInChildren<TurnoutMoveTByKnob>();

        if (handlerSocketInteractor != null)
        {
            handlerSocketInteractor.selectEntered.AddListener(GetHandlerInteractables);
        }

        if (turnoutLockNormalSocketInteractor != null)
        {
            turnoutLockNormalSocketInteractor.selectEntered.AddListener(LockTurnout);
            turnoutLockNormalSocketInteractor.selectExited.AddListener(UnLockTurnout);
        }

        if (turnoutLockReverseSocketInteractor != null)
        {
            turnoutLockReverseSocketInteractor.selectEntered.AddListener(LockTurnout);
        }

        animator = GetComponent<Animator>();
        cutPowerButton.onClick.AddListener(CutPower);

        //QFramework
        turnoutModel = this.GetModel<TurnoutModel>();
    }

    private void GetHandlerInteractables(SelectEnterEventArgs args)
    {
        if (args.interactableObject != null && turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState ==
            LockedState.Nothing)
        {
            rotationOnlySync.SetTarget(args.interactableObject.transform.GetChild(0));
            turnoutMoveTByKnob.knob = args.interactableObject.transform.GetComponentInParent<XRKnob>();
            rotationOnlySync.transform.GetComponent<MeshRenderer>().enabled = true;
            xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
            if (xRKnob != null && xRGrabInteractable != null)
            {
                handlerRb = xRGrabInteractable.transform.GetComponent<Rigidbody>();
                DisableGrab(xRGrabInteractable);
            }

            xRKnob = args.interactableObject.transform.GetComponent<XRKnob>();
            if (xRKnob != null)
            {
                // currentValue = xRKnob.value;
                xRKnob.onValueChange.AddListener(EnableGrab);
            }
        }
    }

    public void LockTurnout(SelectEnterEventArgs args)
    {
        var animator = args.interactableObject.transform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool(lockTurnoutTag, true);
            if (args.interactorObject.transform.CompareTag(turnoutNormalSideTag))
                turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.Normal;
            else
                turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.Reverse;
        }
    }

    private void UnLockTurnout(SelectExitEventArgs args)
    {
        var animator = args.interactableObject.transform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool(lockTurnoutTag, false);
            turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.Nothing;
        }
    }

    private void DisableGrab(XRGrabInteractable xRGrabInteractable)
    {
        xRGrabInteractable.enabled = false;
        handlerRb.useGravity = false;
        xRGrabInteractable.transform.position = handlerSocketInteractor.transform.position;
        xRGrabInteractable.transform.rotation = handlerSocketInteractor.transform.rotation;
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
        if (turnoutID == TurnoutID.Turnout01)
        {
            Debug.Log("TurnoutLock:" + turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}