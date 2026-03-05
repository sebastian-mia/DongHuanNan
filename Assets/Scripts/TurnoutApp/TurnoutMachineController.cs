using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;

public class TurnoutMachineController : MonoBehaviour, IController
{
    public TurnoutID turnoutID;

    // public TurnoutState currentLocalTurnoutState;
    // public TurnoutState targetLocalTurnoutState;
    private XRSocketInteractor handlerSocketInteractor;
    private XRSocketInteractor turnoutLockNormalSocketInteractor;
    private XRSocketInteractor turnoutLockReverseSocketInteractor;
    private XRSocketInteractor turnoutCutPowerSocketInteractor;
    private readonly string turnoutMachineSocketTag = "TurnoutMachineSocket";
    private readonly string turnoutCutPowerSocketTag = "CutPowerSocket";


    private XRKnob xRKnob;
    private MeshRenderer xRKnobMeshRenderer;
    private BoxCollider[] xRKnobColliders;
    private XRGrabInteractable xRGrabInteractable;
    private WaistSlotItem waistSlotItem;
    private float currentValue;
    private float deathFloat = 0.05f;
    private Rigidbody handlerRb;

    [Header("如果道岔初始是反位需要勾选")] [SerializeField]
    public bool isStartReverse;

    private RotationOnlySync rotationOnlySync;
    private TurnoutMoveTByKnob turnoutMoveTByKnob;

    //UI
    public Transform butterflyKey;
    private readonly string cutPowerTag = "CutPower";
    private Animator animator;

    //LockTurnout
    private readonly string lockTurnoutTag = "LockTurnout";
    private readonly string turnoutNormalSideTag = "NormalSide";
    private readonly string turnoutReverseSideTag = "ReverseSide";

    private AudioManager audioManager;
    private int playTurnoutMachineSoundIndex;


    //Modle
    private TurnoutModel turnoutModel;

    public void Start()
    {
        audioManager = this.GetUtility<AudioManager>();
        xRKnob = GetComponentInChildren<XRKnob>();
        rotationOnlySync = GetComponentInChildren<RotationOnlySync>();
        turnoutMoveTByKnob = GetComponentInChildren<TurnoutMoveTByKnob>();
        if (xRKnob != null)
        {
            xRKnobMeshRenderer = xRKnob.transform.GetChild(0).GetComponent<MeshRenderer>();
            xRKnobColliders = xRKnob.transform.GetChild(0).GetComponents<BoxCollider>();
            rotationOnlySync.SetTarget(xRKnob.transform.GetChild(0));
            turnoutMoveTByKnob.knob = xRKnob;
            // xRKnob.onValueChange.RemoveListener(EnableHandler); // 防止重复
            xRKnob.onValueChange.AddListener(EnableHandler);
        }

        var xRSocketInteractors = GetComponentsInChildren<XRSocketInteractor>();
        for (int i = 0; i < xRSocketInteractors.Length; i++)
        {
            if (xRSocketInteractors[i].CompareTag(turnoutMachineSocketTag))
            {
                handlerSocketInteractor = xRSocketInteractors[i];
            }

            if (xRSocketInteractors[i].CompareTag(turnoutCutPowerSocketTag))
            {
                turnoutCutPowerSocketInteractor = xRSocketInteractors[i];
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
            turnoutLockReverseSocketInteractor.selectExited.AddListener(UnLockTurnout);
        }

        if (turnoutCutPowerSocketInteractor != null)
        {
            turnoutCutPowerSocketInteractor.selectEntered.AddListener(TurnoutMachineCutPower);
        }

        animator = GetComponent<Animator>();

        //QFramework
        turnoutModel = this.GetModel<TurnoutModel>();
    }

    private void TurnoutMachineCutPower(SelectEnterEventArgs args)
    {
        turnoutCutPowerSocketInteractor.enabled = false;
        PlayCutPowerAnimator();
        this.SendCommand<CutPowerCommand>(new CutPowerCommand(turnoutID));
    }

    private void GetHandlerInteractables(SelectEnterEventArgs args)
    {
        if (args.interactableObject == null) return;
        if (turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState != LockedState.未加锁) return;

        xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
        waistSlotItem = args.interactableObject.transform.GetComponent<WaistSlotItem>();
        if (xRGrabInteractable != null && waistSlotItem != null)
        {
            DisableHandler(xRGrabInteractable, waistSlotItem);
        }

        rotationOnlySync.GetComponent<MeshRenderer>().enabled = true;
    }

    public void LockTurnout(SelectEnterEventArgs args)
    {
        audioManager.PlayLockSound();
        var animator = args.interactableObject.transform.GetComponent<Animator>();
        if (animator == null) return;

        animator.SetBool(lockTurnoutTag, true);
        if (args.interactorObject.transform.CompareTag(turnoutNormalSideTag))
            this.SendCommand<ToggleTurnoutLockToNormalCommand>(new ToggleTurnoutLockToNormalCommand(turnoutID));
        else
            this.SendCommand<ToggleTurnoutLockToReverseCommand>(new ToggleTurnoutLockToReverseCommand(turnoutID));
        Debug.Log("TurnoutLock:" +
                  this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState);
    }

    private void UnLockTurnout(SelectExitEventArgs args)
    {
        var animator = args.interactableObject.transform.GetComponent<Animator>();
        if (animator == null) return;

        animator.SetBool(lockTurnoutTag, false);
        this.SendCommand<ToggleTurnoutLockToNothingCommand>(new ToggleTurnoutLockToNothingCommand(turnoutID));
        Debug.Log("TurnoutLock:" +
                  this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState);
    }

    private void DisableHandler(XRGrabInteractable xRGrabInteractable, WaistSlotItem waistSlotItem)
    {
        audioManager.PlayHandlerSound();
        handlerRb = xRGrabInteractable.GetComponent<Rigidbody>();
        xRGrabInteractable.GetComponent<MeshRenderer>().enabled = false;
        var xRGrabInteractableBoxcolliders = xRGrabInteractable.GetComponents<BoxCollider>();
        for (int i = 0; i < xRGrabInteractableBoxcolliders.Length; i++)
        {
            xRGrabInteractableBoxcolliders[i].enabled = false;
        }

        handlerRb.useGravity = false;
        waistSlotItem.enabled = false;
        xRGrabInteractable.enabled = false;

        xRKnobMeshRenderer.enabled = true;
        for (int i = 0; i < xRKnobColliders.Length; i++)
        {
            xRKnobColliders[i].enabled = true;
        }

        xRKnob.enabled = true;
    }

    private void EnableHandler(float value)
    {
        if (xRKnob.isSelected)
        {
            audioManager.PlayturnoutMachineWorkingSound();
        }

        if (currentValue <= 0 + deathFloat)
        {
            if (value >= 1 - deathFloat)
            {
                audioManager.PlayturnoutMachineEndingSound();
                currentValue = 1;
                if (isStartReverse)
                {
                    this.SendCommand<ToggleTurnoutToNormalCommand>(new ToggleTurnoutToNormalCommand(turnoutID));
                }
                else
                {
                    this.SendCommand<ToggleTurnoutToReverseCommand>(new ToggleTurnoutToReverseCommand(turnoutID));
                }


                ToggleHandler();
            }
        }

        if (currentValue >= 1 - deathFloat)
        {
            if (value <= 0 + deathFloat)
            {
                audioManager.PlayturnoutMachineEndingSound();
                currentValue = 0;
                if (isStartReverse)
                {
                    this.SendCommand<ToggleTurnoutToReverseCommand>(new ToggleTurnoutToReverseCommand(turnoutID));
                }
                else
                {
                    this.SendCommand<ToggleTurnoutToNormalCommand>(new ToggleTurnoutToNormalCommand(turnoutID));
                }

                ToggleHandler();
            }
        }
    }

    private void ToggleHandler()
    {
        xRKnobMeshRenderer.enabled = false;
        for (int i = 0; i < xRKnobColliders.Length; i++)
        {
            xRKnobColliders[i].enabled = false;
        }


        xRKnob.enabled = false;
        rotationOnlySync.transform.GetComponent<MeshRenderer>().enabled = false;

        xRGrabInteractable.GetComponent<MeshRenderer>().enabled = true;
        var xRGrabInteractableBoxcolliders = xRGrabInteractable.GetComponents<BoxCollider>();
        for (int i = 0; i < xRGrabInteractableBoxcolliders.Length; i++)
        {
            xRGrabInteractableBoxcolliders[i].enabled = true;
        }

        handlerRb.useGravity = true;
        waistSlotItem.enabled = false;
        xRGrabInteractable.enabled = true;
    }

    private void PlayCutPowerAnimator()
    {
        butterflyKey.gameObject.SetActive(true);
        animator.SetTrigger(cutPowerTag);
    }

    public void PlayCutPowerSound()
    {
        audioManager.PlayturnoutMachineCutPowerSound();
    }

    public void HideButterflyKey()
    {
        butterflyKey.gameObject.SetActive(false);
    }


    private void Update()
    {
    }

    private void OnDestroy()
    {
        xRKnob.onValueChange.RemoveListener(EnableHandler);
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}