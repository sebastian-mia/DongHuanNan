using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutUIcontroller : MonoBehaviour, IController
{
    public Button cutPowerButton;
    public TurnoutID turnoutID;
    private bool isHideUI;

    void Start()
    {
        this.GetModel<TurnoutModel>().currentRoute.Register(value =>
        {
            if (value.TurnoutMachines[turnoutID].IsPowerCut)
            {
                isHideUI = value.TurnoutMachines[turnoutID].IsPowerCut;
            }
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isHideUI)
        {
            ShowPowerCutUI();
        }
    }

    private void ShowPowerCutUI()
    {
        if (cutPowerButton != null)
        {
            cutPowerButton.gameObject.SetActive(true);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}