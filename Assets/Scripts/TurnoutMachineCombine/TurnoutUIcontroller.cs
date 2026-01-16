using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutUIcontroller : MonoBehaviour
{
    public Button cutPowerButton;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ShowPowerCutUI();
        }
    }

    private void ShowPowerCutUI()
    {
        if (cutPowerButton != null)
        {
            cutPowerButton.gameObject.SetActive(true);
        }
    }
}