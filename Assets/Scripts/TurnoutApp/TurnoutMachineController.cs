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
    // 🔥 新增：音效节流控制
    private float lastSoundValue = -1f;      // 上次播放音效时的值
    private float lastValueChangeTime = 0f;  // 上次值变化的时间
    private const float SOUND_THRESHOLD = 0.005f;     // 值变化阈值
    private const float CHANGE_TIMEOUT = 0.1f;      // 变化超时时间（秒）


    //Modle
    private TurnoutModel turnoutModel;
    private bool isWaitingForHandlerReturn = false;

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

        // Lock 状态下，不做任何处理，手摇把自然吸附，玩家可 Grab，松手后回槽
        if (turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState != LockedState.未加锁)
        {
            // 保存引用
            xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
            waistSlotItem = args.interactableObject.transform.GetComponent<WaistSlotItem>();

            // 不调用 DisableHandler，手摇把保持可见可交互
            return;
        }

        // 正常流程
        xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
        waistSlotItem = args.interactableObject.transform.GetComponent<WaistSlotItem>();
        if (xRGrabInteractable != null && waistSlotItem != null)
        {
            DisableHandler(xRGrabInteractable, waistSlotItem);
        }

        rotationOnlySync.GetComponent<MeshRenderer>().enabled = true;
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
        xRGrabInteractable.enabled = true;
        waistSlotItem.enabled = true; // 启用，让回家逻辑正常工作
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

        // 🔥 禁用 Socket
        if (handlerSocketInteractor != null)
        {
            handlerSocketInteractor.enabled = false;
        }

        Debug.Log("TurnoutLock:" +
                  this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState);
    }

    private void UnLockTurnout(SelectExitEventArgs args)
    {
        var animator = args.interactableObject.transform.GetComponent<Animator>();
        if (animator == null) return;

        animator.SetBool(lockTurnoutTag, false);
        this.SendCommand<ToggleTurnoutLockToNothingCommand>(new ToggleTurnoutLockToNothingCommand(turnoutID));

        // 🔥 启用 Socket
        if (handlerSocketInteractor != null)
        {
            handlerSocketInteractor.enabled = true;
        }

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
            // 🔥 判断 value 是否在持续变化（0.1秒内是否有变化）
            bool isValueChanging = Time.time - lastValueChangeTime < CHANGE_TIMEOUT;

            // 更新变化时间戳
            if (Mathf.Abs(value - lastSoundValue) > 0.001f) // 忽略微小浮点误差
            {
                lastValueChangeTime = Time.time;
            }

            // 只有在持续变化且累积变化超过阈值时才播放
            if (isValueChanging && Mathf.Abs(value - lastSoundValue) >= SOUND_THRESHOLD)
            {
                audioManager.PlayturnoutMachineWorkingSound();
                lastSoundValue = value;
            }
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