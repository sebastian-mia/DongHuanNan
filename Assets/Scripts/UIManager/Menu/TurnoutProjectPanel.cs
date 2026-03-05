using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TurnoutProjectPanel : MonoBehaviour, IController
{
    public Button startTurnoutProjectButton;
    public TMP_InputField lineInputField;
    public TMP_InputField stationInputField;
    public TMP_Dropdown lineDropdown;
    public TMP_Dropdown stationDropdown;
    public Button returnButton;
    private List<TMP_Dropdown.OptionData> lineOptions = new List<TMP_Dropdown.OptionData>();
    private List<TMP_Dropdown.OptionData> stationOptions = new List<TMP_Dropdown.OptionData>();
    private readonly int stationOptionIndex;

    private EnumUtils enumUtils;
    private TurnoutDatabaseManager databaseManager;
    private int buttonIndex;

    void Start()
    {
        enumUtils = this.GetUtility<EnumUtils>();
        databaseManager = this.GetUtility<TurnoutDatabaseManager>();
        InitLineAndStationOptions();
        if (buttonIndex < 1)
        {
            lineDropdown.onValueChanged.AddListener(e =>
            {
                lineInputField.text = lineDropdown.options[lineDropdown.value].text;
                ShowStationOptions(e);
            });
            stationDropdown.onValueChanged.AddListener(e =>
            {
                stationInputField.text = stationDropdown.options[stationDropdown.value].text;
            });
            buttonIndex++;
        }
    }

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }

    private void InitLineAndStationOptions()
    {
        for (int i = 0; i < enumUtils.GetCount<LineName>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = enumUtils.GetValues<LineName>()[i].ToString();
            lineOptions.Add(option);
        }

        lineDropdown.AddOptions(lineOptions);
        lineInputField.text = lineDropdown.options[0].text;

        for (int i = 0; i < enumUtils.GetCount<Line1StationName>(); i++)
        {
            var stationOption = new TMP_Dropdown.OptionData();
            stationOption.text = enumUtils.GetValues<Line1StationName>()[i].ToString();
            stationOptions.Add(stationOption);
        }

        stationDropdown.AddOptions(stationOptions);
        stationInputField.text = stationDropdown.options[0].text;
    }


    private void ShowStationOptions(int line)
    {
        stationOptions.Clear();
        stationDropdown.options.Clear();
        if (line == (int)LineName.一号线)
        {
            for (int i = 0; i < enumUtils.GetCount<Line1StationName>(); i++)
            {
                var stationOption = new TMP_Dropdown.OptionData();
                stationOption.text = enumUtils.GetValues<Line1StationName>()[i].ToString();
                stationOptions.Add(stationOption);
            }
        }

        stationDropdown.AddOptions(stationOptions);
        stationDropdown.RefreshShownValue();
        stationInputField.text = enumUtils.GetValues<Line1StationName>()[stationDropdown.value].ToString();
        this.SendCommand<SetCurrentStationNameCommand>(
            new SetCurrentStationNameCommand(databaseManager.GetStationId(stationInputField.text).Value));
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}