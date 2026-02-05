using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TurnoutProjectPanel : MonoBehaviour
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

    void Start()
    {
        InitLineAndStationOptions();
        lineDropdown.onValueChanged.AddListener(e =>
        {
            lineInputField.text = lineDropdown.options[lineDropdown.value].text;
            ShowStationOptions(e);
        });
        stationDropdown.onValueChanged.AddListener(e =>
        {
            stationInputField.text = stationDropdown.options[stationDropdown.value].text;
        });
    }

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }

    private void InitLineAndStationOptions()
    {
        for (int i = 0; i < EnumUtils.GetCount<LineName>(); i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = EnumUtils.GetValues<LineName>()[i].ToString();
            lineOptions.Add(option);
        }

        lineDropdown.AddOptions(lineOptions);
        lineInputField.text = lineDropdown.options[0].text;

        for (int i = 0; i < EnumUtils.GetCount<Line1StationName>(); i++)
        {
            var stationOption = new TMP_Dropdown.OptionData();
            stationOption.text = EnumUtils.GetValues<Line1StationName>()[i].ToString();
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
            for (int i = 0; i < EnumUtils.GetCount<Line1StationName>(); i++)
            {
                var stationOption = new TMP_Dropdown.OptionData();
                stationOption.text = EnumUtils.GetValues<Line1StationName>()[i].ToString();
                stationOptions.Add(stationOption);
            }
        }

        stationDropdown.AddOptions(stationOptions);
        stationDropdown.RefreshShownValue();
        stationInputField.text = stationDropdown.options[0].text;
    }
}