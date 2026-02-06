using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TurnoutMachineUIController : MonoBehaviour
{
    public TurnoutID turnoutID;
    private XRSocketInteractor handlerSocketInteractor;
    private TurnoutMachineUI turnoutMachineUI;
    private readonly string turnoutMachineSocketTag = "TurnoutMachineSocket";

    void Start()
    {
        var xRSocketInteractors = GetComponentsInChildren<XRSocketInteractor>();
        for (int i = 0; i < xRSocketInteractors.Length; i++)
        {
            if (xRSocketInteractors[i].CompareTag(turnoutMachineSocketTag))
            {
                handlerSocketInteractor = xRSocketInteractors[i];
            }
        }

        if (handlerSocketInteractor == null)
        {
            handlerSocketInteractor.selectEntered.AddListener(ShowUseHandlerTag);
            handlerSocketInteractor.selectExited.AddListener(ShowRotateTag);
        }

        turnoutMachineUI = UIManager.Instance.ShowUI(ChooseUIByID()).GetComponent<TurnoutMachineUI>();
        turnoutMachineUI.OnShow();
    }

    private string ChooseUIByID()
    {
        switch (turnoutID)
        {
            case TurnoutID.Turnout01:
                return "Turnout01Panel";
            case TurnoutID.Turnout02:
                return "Turnout02Panel";
            case TurnoutID.Turnout03:
                return "Turnout03Panel";
            case TurnoutID.Turnout04:
                return "Turnout04Panel";
            case TurnoutID.Turnout05:
                return "Turnout05Panel";
            case TurnoutID.Turnout06:
                return "Turnout06Panel";
        }

        return null;
    }

    private void ShowUseHandlerTag(SelectEnterEventArgs args)
    {
        turnoutMachineUI.handlerTag.text = "插入手摇把";
    }

    private void ShowRotateTag(SelectExitEventArgs args)
    {
        turnoutMachineUI.handlerTag.text = "转动";
    }
}