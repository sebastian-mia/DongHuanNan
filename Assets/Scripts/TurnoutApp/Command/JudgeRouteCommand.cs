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

            var nodeRedLightValue = new NodeValue("T0120" + key + "(" + RouteNodeTag.红闪灯 + ")",
                this.GetModel<TurnoutModel>().targetRoute.Value.redLightPosition.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.redLightPosition.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.redLightPosition
                    .Equals(this.GetModel<TurnoutModel>().targetRoute.Value.redLightPosition));
            this.GetModel<TurnoutModel>().nodeValues.Add(nodeRedLightValue);
            var nodeTurnoutValue = new NodeValue("T0120" + key + "(" + RouteNodeTag.道岔 + ")",
                this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines[key].TurnoutState.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[key].TurnoutState.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[key].TurnoutState
                    .Equals(this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines[key].TurnoutState));
            this.GetModel<TurnoutModel>().nodeValues.Add(nodeTurnoutValue);
            var nodeTurnoutLockValue = new NodeValue("T0120" + key + "(" + RouteNodeTag.钩锁器 + ")",
                this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines[key].LockedState.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[key].LockedState.ToString(),
                this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[key].LockedState
                    .Equals(this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines[key].LockedState));
            this.GetModel<TurnoutModel>().nodeValues.Add(nodeTurnoutLockValue);
        }

        Debug.Log("nodeValues:" + this.GetModel<TurnoutModel>().nodeValues.Count);

        // Debug.Log("currentRoute" + this.GetModel<TurnoutModel>().currentRoute.Value.redLightPosition);
        // Debug.Log("targetRoute" + this.GetModel<TurnoutModel>().targetRoute.Value.redLightPosition);
        if (this.GetModel<TurnoutModel>().currentRoute.Value.redLightPosition
            != this.GetModel<TurnoutModel>().targetRoute.Value.redLightPosition)
        {
            this.GetModel<TurnoutModel>().isCorrectRoute = false;
            this.SendEvent<ShowScorePanelEvent>();
            return;
        }

        if (correctCount == keys.Count)
        {
            this.GetModel<TurnoutModel>().isCorrectRoute = true;
            this.SendEvent<ShowScorePanelEvent>();
            return;
        }

        this.GetModel<TurnoutModel>().isCorrectRoute = false;
        this.SendEvent<ShowScorePanelEvent>();
    }
}