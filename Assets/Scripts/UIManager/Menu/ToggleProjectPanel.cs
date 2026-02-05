using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleProjectPanel : MonoBehaviour
{
    public Button turnoutProjectButton;
    public Button fasMachineProjectButton;
    public Button fireExtinguisherProjectButton;
    public Button pSDProjectButton;
    public Button returnButton;
    
    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }
}