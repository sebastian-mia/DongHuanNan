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
        if (args.interactableObject == null) return;
        if (turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].LockedState != LockedState.Nothing) return;

        // 🔧 1. 立即绑定事件（Grab 还没关，实例稳定）
        xRGrabInteractable = args.interactableObject.transform.GetComponent<XRGrabInteractable>();
        xRKnob = args.interactableObject.transform.GetComponent<XRKnob>();
        if (xRKnob != null)
        {
            xRKnob.onValueChange.RemoveListener(EnableGrab); // 防重复
            xRKnob.onValueChange.AddListener(EnableGrab); // 立即绑
        }

        // 🔧 2. 1帧后再关 Grab、定位、开 Knob
        StartCoroutine(DisableGrabNextFrame(args.interactableObject));
    }

    private IEnumerator DisableGrabNextFrame(IXRInteractable interactable)
    {
        yield return null; // 等LateUpdate事件落地

        if (interactable is XRGrabInteractable grab)
        {
            grab.enabled = false; // 关闭抓取
            handlerRb = grab.GetComponent<Rigidbody>();
            handlerRb.useGravity = false;
            grab.transform.position = handlerSocketInteractor.transform.position;
            grab.transform.rotation = handlerSocketInteractor.transform.rotation;

            // 3. 开 Knob（事件已绑好，无需再绑）
            if (xRKnob) xRKnob.enabled = true;

            // 4. 其余绑定
            rotationOnlySync.SetTarget(grab.transform.GetChild(0));
            rotationOnlySync.GetComponent<MeshRenderer>().enabled = true;
            turnoutMoveTByKnob.knob = xRKnob;
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
        bool hitTop = currentValue <= 0 + deathFloat && value >= 1 - deathFloat;
        bool hitBot = currentValue >= 1 - deathFloat && value <= 0 + deathFloat;

        if (hitTop || hitBot)
        {
            // 🔧 强制让当前Socket释放物品（立即触发OnRelease）
            var hostSocket = xRGrabInteractable.transform.parent.GetComponent<XRSocketInteractor>();
            if (hostSocket && hostSocket.selectTarget == xRGrabInteractable)
                hostSocket.interactionManager.SelectExit(hostSocket, xRGrabInteractable);

            // 其余旧逻辑不变
            xRKnob.enabled = false;
            xRGrabInteractable.enabled = true;
            handlerRb.useGravity = true;
            currentValue = hitTop ? 1 : 0;
            rotationOnlySync.RemoveTarget();
            rotationOnlySync.GetComponent<MeshRenderer>().enabled = false;
            currentLocalTurnoutState = hitTop ? TurnoutState.Reverse : TurnoutState.Normal;
            turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].TurnoutState = currentLocalTurnoutState;
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