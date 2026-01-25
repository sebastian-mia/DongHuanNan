using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutUIcontroller : MonoBehaviour, IController
{
    public TurnoutID turnoutID;
    private CutPowerUI cutPowerUi;
    private TurnoutModel turnoutModel;

    void Start()
    {
        turnoutModel = this.GetModel<TurnoutModel>();
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !turnoutModel.currentRoute.Value.turnoutMachines[turnoutID].IsPowerCut)
        {
            cutPowerUi = UIManager.Instance.ShowUI(ChooseUIByID()).GetComponent<CutPowerUI>();
            cutPowerUi.OnShow();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cutPowerUi.OnHide();
        }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}