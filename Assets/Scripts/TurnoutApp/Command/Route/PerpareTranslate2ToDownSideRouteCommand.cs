using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareTranslate2ToDownSideRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute.SetValueWithoutEvent(new Route(TurnoutID.Turnout04, true));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout01,
            new TurnoutMachine(TurnoutID.Turnout01, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout03,
            new TurnoutMachine(TurnoutID.Turnout04, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout04,
            new TurnoutMachine(TurnoutID.Turnout04, LockedState.Nothing, false, TurnoutState.Reverse));
    }
}