using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;


public class LocomotionModePanel : MonoBehaviour, IController
{
    public TMP_Dropdown LocomotionModeDropdown;
    public Button returnButton;
    public Button correntButton;
    private List<TMP_Dropdown.OptionData> LocomotionModes = new List<TMP_Dropdown.OptionData>();
    private readonly int stationOptionIndex;
    private LocomotionModeController locomotionModeController;
    private LocomotionMode currentLocomotionMode = LocomotionMode.挥动;

    private EnumUtils enumUtils;

    void Start()
    {
        enumUtils = this.GetUtility<EnumUtils>();
        locomotionModeController = FindObjectOfType<LocomotionModeController>();
        locomotionModeController.SetLocomotionMode(LocomotionMode.挥动);
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
        for (int i = 0; i < enumUtils.GetCount<LocomotionMode>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = enumUtils.GetValues<LocomotionMode>()[i].ToString();
            LocomotionModes.Add(option);
        }

        LocomotionModeDropdown.AddOptions(LocomotionModes);
        locomotionModeController.SetLocomotionMode(currentLocomotionMode);
    }


    private void ShowStationOptions(int setting)
    {
        LocomotionModes.Clear();
        LocomotionModeDropdown.options.Clear();
        for (int i = 0; i < enumUtils.GetCount<LocomotionMode>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = enumUtils.GetValues<LocomotionMode>()[i].ToString();
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

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}