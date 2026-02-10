using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;


public class LocomotionModePanel : MonoBehaviour
{
    public TMP_Dropdown LocomotionModeDropdown;
    public Button returnButton;
    public Button correntButton;
    private List<TMP_Dropdown.OptionData> LocomotionModes = new List<TMP_Dropdown.OptionData>();
    private readonly int stationOptionIndex;
    private LocomotionModeController locomotionModeController;
    private LocomotionMode currentLocomotionMode = LocomotionMode.挥动;

    void Start()
    {
        locomotionModeController = FindObjectOfType<LocomotionModeController>();
        locomotionModeController.SetTeleportInputEnabled(false);
        if (correntButton != null)
        {
            correntButton.onClick.AddListener(() =>
            {
                SetLocomotionMode();
                ReturnPrePanel();
            });
        }

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnPrePanel);
        }

        InitLineAndStationOptions();
        LocomotionModeDropdown.onValueChanged.AddListener(e => { ShowStationOptions(e); });
    }

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }

    private void InitLineAndStationOptions()
    {
        for (int i = 0; i < EnumUtils.GetCount<LocomotionMode>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = EnumUtils.GetValues<LocomotionMode>()[i].ToString();
            LocomotionModes.Add(option);
        }

        LocomotionModeDropdown.AddOptions(LocomotionModes);
        locomotionModeController.SetLocomotionMode(currentLocomotionMode);
    }


    private void ShowStationOptions(int setting)
    {
        LocomotionModes.Clear();
        LocomotionModeDropdown.options.Clear();
        for (int i = 0; i < EnumUtils.GetCount<LocomotionMode>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = EnumUtils.GetValues<LocomotionMode>()[i].ToString();
            LocomotionModes.Add(option);
        }

        if (setting == (int)LocomotionMode.挥动)
        {
            currentLocomotionMode = global::LocomotionMode.挥动;
        }

        if (setting == (int)LocomotionMode.传送)
        {
            currentLocomotionMode = global::LocomotionMode.传送;
        }

        LocomotionModeDropdown.AddOptions(LocomotionModes);
        LocomotionModeDropdown.RefreshShownValue();
    }

    public void SetLocomotionMode()
    {
        locomotionModeController.SetLocomotionMode(currentLocomotionMode);
    }

    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }
}