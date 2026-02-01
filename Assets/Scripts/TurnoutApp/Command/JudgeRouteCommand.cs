using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class JudgeRouteCommand : AbstractCommand
{
    private int correctCount;

    protected override void OnExecute()
    {
        correctCount = 0;
        var keys = this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Keys;

        foreach (var key in keys)
        {
            if (this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines[key].TurnoutState ==
                this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[key].TurnoutState)
            {
                correctCount++;
            }
        }

        if (correctCount == keys.Count)
        {
            this.GetModel<TurnoutModel>().isCorrectRoute.Value = true;
        }
    }
}