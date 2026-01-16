using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnoutMachine
{
    private TurnoutID turnoutID;
    private LockedState lockedState;
    private bool isPowerCut;
    private TurnoutState turnoutState;

    public TurnoutMachine(TurnoutID turnoutID, LockedState lockedState, bool isPowerCut, TurnoutState turnoutState)
    {
        this.turnoutID = turnoutID;
        this.lockedState = lockedState;
        this.isPowerCut = isPowerCut;
        this.turnoutState = turnoutState;
    }

    public TurnoutID TurnoutID
    {
        get => turnoutID;
        set => turnoutID = value;
    }

    public LockedState LockedState
    {
        get => lockedState;
        set => lockedState = value;
    }

    public bool IsPowerCut
    {
        get => isPowerCut;
        set => isPowerCut = value;
    }

    public TurnoutState TurnoutState
    {
        get => turnoutState;
        set => turnoutState = value;
    }
}