using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class JudgeRouteCommand : AbstractCommand
{
    private int correctCount;
    private RedLightPosition redLightPosition;

    public JudgeRouteCommand(RedLightPosition redLightPosition)
    {
        this.redLightPosition = redLightPosition;
    }

    protected override void OnExecute()
    {
        var turnoutModel = this.GetModel<TurnoutModel>();
        turnoutModel.currentRoute.Value.redLightPosition = redLightPosition;
        correctCount = 0;
        var keys = turnoutModel.targetRoute.Value.turnoutMachines.Keys;
        var nodeRedLightValue = new NodeValue(RouteNodeTag.红闪灯.ToString(),
            turnoutModel.targetRoute.Value.redLightPosition.ToString(),
            turnoutModel.currentRoute.Value.redLightPosition.ToString(),
            turnoutModel.currentRoute.Value.redLightPosition
                .Equals(turnoutModel.targetRoute.Value.redLightPosition));
        turnoutModel.nodeValues.Add(nodeRedLightValue);
        foreach (var key in keys)
        {
            if (turnoutModel.targetRoute.Value.turnoutMachines[key].TurnoutState ==
                turnoutModel.currentRoute.Value.turnoutMachines[key].TurnoutState)
            {
                correctCount++;
            }

            var nodeTurnoutValue = new NodeValue("T0120" + key.ToString().Substring(key.ToString().Length-2) + "(" + RouteNodeTag.道岔 + ")",
                turnoutModel.targetRoute.Value.turnoutMachines[key].TurnoutState.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].TurnoutState.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].TurnoutState
                    .Equals(turnoutModel.targetRoute.Value.turnoutMachines[key].TurnoutState));
            turnoutModel.nodeValues.Add(nodeTurnoutValue);
            var nodeTurnoutLockValue = new NodeValue("T0120" + key.ToString().Substring(key.ToString().Length-2) + "(" + RouteNodeTag.钩锁器 + ")",
                turnoutModel.targetRoute.Value.turnoutMachines[key].LockedState.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].LockedState.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].LockedState
                    .Equals(turnoutModel.targetRoute.Value.turnoutMachines[key].LockedState));
            turnoutModel.nodeValues.Add(nodeTurnoutLockValue);
            var cutPowerValue = new NodeValue("T0120" + key.ToString().Substring(key.ToString().Length-2) + "(" + RouteNodeTag.断电 + ")",
                turnoutModel.targetRoute.Value.turnoutMachines[key].IsPowerCut.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].IsPowerCut.ToString(),
                turnoutModel.currentRoute.Value.turnoutMachines[key].IsPowerCut
                    .Equals(turnoutModel.targetRoute.Value.turnoutMachines[key].IsPowerCut));
            turnoutModel.nodeValues.Add(cutPowerValue);
        }

        Debug.Log("nodeValues:" + turnoutModel.nodeValues.Count);

        // Debug.Log("currentRoute" + this.GetModel<TurnoutModel>().currentRoute.Value.redLightPosition);
        // Debug.Log("targetRoute" + this.GetModel<TurnoutModel>().targetRoute.Value.redLightPosition);
        if (turnoutModel.currentRoute.Value.redLightPosition
            != turnoutModel.targetRoute.Value.redLightPosition)
        {
            turnoutModel.isCorrectRoute = false;
            this.SendEvent<ShowScorePanelEvent>();
            return;
        }

        if (correctCount == keys.Count)
        {
            turnoutModel.isCorrectRoute = true;
            this.SendEvent<ShowScorePanelEvent>();
            return;
        }

        this.GetModel<TurnoutModel>().isCorrectRoute = false;
        this.SendEvent<ShowScorePanelEvent>();
    }
}