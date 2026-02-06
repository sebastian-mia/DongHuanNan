using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutMachineUI : UIBase
{
    public TurnoutID turnoutID;
    public TMP_Text handlerTag;
    public TMP_Text cutPowerTag;

    public override int GetParamHash()
    {
        return (int)turnoutID;
    }

    public override void Refresh(int paramHash)
    {
        turnoutID = (TurnoutID)paramHash;
    }
}