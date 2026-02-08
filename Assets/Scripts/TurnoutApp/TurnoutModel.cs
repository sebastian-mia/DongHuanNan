using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class TurnoutModel : AbstractModel
{
    public int routeCount;
    public BindableProperty<Route> currentRoute = new BindableProperty<Route>();
    public BindableProperty<Route> targetRoute = new BindableProperty<Route>();
    public List<NodeValue> nodeValues = new List<NodeValue>();
    public bool isCorrectRoute;

    protected override void OnInit()
    {
        routeCount = 10;
        currentRoute.SetValueWithoutEvent(new Route(TurnoutID.Turnout01, RedLightPosition.下行));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout01,
            new TurnoutMachine(TurnoutID.Turnout01, LockedState.Nothing, false, TurnoutState.Normal));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.Nothing, false, TurnoutState.Normal));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout03,
            new TurnoutMachine(TurnoutID.Turnout03, LockedState.Nothing, false, TurnoutState.Reverse));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout04,
            new TurnoutMachine(TurnoutID.Turnout04, LockedState.Nothing, false, TurnoutState.Normal));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout05,
            new TurnoutMachine(TurnoutID.Turnout05, LockedState.Nothing, false, TurnoutState.Reverse));
        currentRoute.Value.turnoutMachines.Add(TurnoutID.Turnout06,
            new TurnoutMachine(TurnoutID.Turnout06, LockedState.Nothing, false, TurnoutState.Normal));
    }
}