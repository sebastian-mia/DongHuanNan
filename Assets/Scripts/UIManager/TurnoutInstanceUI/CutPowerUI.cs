using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class CutPowerUI : UIBase, IController
{
    public TurnoutID turnoutID;
    private Button cutPowerButton;

    void Awake()
    {
        cutPowerButton = GetComponentInChildren<Button>();
    }

    public override int GetParamHash()
    {
        return (int)turnoutID;
    }

    public override void Refresh(int paramHash)
    {
        turnoutID = (TurnoutID)paramHash;
    }

    public override void OnShow()
    {
        base.OnShow();
        if (cutPowerButton != null)
        {
            cutPowerButton.onClick.AddListener(cutPower);
        }
    }

    public override void OnHide()
    {
        base.OnHide();
        if (cutPowerButton != null)
        {
            cutPowerButton.onClick.RemoveListener(cutPower);
        }
    }

    private void cutPower()
    {
        this.SendCommand<CutPowerCommand>(new CutPowerCommand(turnoutID));
        if (this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].IsPowerCut == IsPowerCut.已断电)
        {
            OnHide();
        }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}