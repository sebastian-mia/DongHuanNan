using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class CutPowerCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public CutPowerCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].IsPowerCut = IsPowerCut.已断电;
        var e = new CutPowerEvent { turnoutID = turnoutID };
        this.SendEvent<CutPowerEvent>(e);
    }
}