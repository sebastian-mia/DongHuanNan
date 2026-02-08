using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route
{
    private TurnoutID startTurnoutID;
    public Dictionary<TurnoutID, TurnoutMachine> turnoutMachines = new Dictionary<TurnoutID, TurnoutMachine>();
    public RedLightPosition redLightPosition;

    public Route(TurnoutID startTurnoutID, RedLightPosition redLightPosition)
    {
        this.startTurnoutID = startTurnoutID;
        this.redLightPosition = redLightPosition;
    }

    public TurnoutID StartTurnoutID
    {
        get => startTurnoutID;
        set => startTurnoutID = value;
    }

    public Dictionary<TurnoutID, TurnoutMachine> TurnoutMachines
    {
        get => turnoutMachines;
        set => turnoutMachines = value;
    }

    public RedLightPosition HasRedLight
    {
        get => redLightPosition;
        set => redLightPosition = value;
    }
}