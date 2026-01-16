using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class TurnoutHandlerController : MonoBehaviour, IController
{
    
    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}