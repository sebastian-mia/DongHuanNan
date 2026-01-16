using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class TurnoutController : MonoBehaviour, IController
{
    public void Start()
    {
        
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}