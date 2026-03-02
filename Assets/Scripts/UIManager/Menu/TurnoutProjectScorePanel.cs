using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutProjectScorePanel : MonoBehaviour, IController
{
    private int projectId;
    public TMP_Dropdown stationDropdown;
    public TMP_Dropdown lineDropdown;
    private Transform content;
    public Button searchButton;
    private TurnoutDatabaseManager turnoutDatabaseManager;
    public string valuePanelName = "UserValuePanel";
    public Button returnPerUIButton;
    private TimeFormatter timeFormatter;
    private List<TMP_Dropdown.OptionData> lineOptions = new List<TMP_Dropdown.OptionData>();
    private List<TMP_Dropdown.OptionData> stationOptions = new List<TMP_Dropdown.OptionData>();

    private void OnEnable()
    {
        turnoutDatabaseManager = this.GetUtility<TurnoutDatabaseManager>();
        //
        InitLineAndStationOptions();
        lineDropdown.onValueChanged.AddListener(e => { ShowStationOptions(e); });
        //

        timeFormatter = this.GetUtility<TimeFormatter>();
        var projectId = turnoutDatabaseManager.GetProjectId(ProjectName.人工准备进路.ToString());
        if (projectId.HasValue)
        {
            this.projectId = projectId.Value;
        }
        else
        {
            Debug.Log("projectId为空");
        }


        if (searchButton != null)
        {
            searchButton.onClick.AddListener(SearchPlayerValue);
        }

        if (returnPerUIButton != null)
        {
            returnPerUIButton.onClick.AddListener(ReturnPrePanel);
        }

        content = transform.GetComponentInChildren<ScrollRect>().content;
    }

    private void InitLineAndStationOptions()
    {
        var lines = turnoutDatabaseManager.GetAllLines();
        for (int i = 0; i < lines.Count; i++)
        {
            var option = new TMP_Dropdown.OptionData();
            option.text = lines[i].LineName;
            lineOptions.Add(option);
        }

        lineDropdown.AddOptions(lineOptions);
        var lineId = turnoutDatabaseManager.GetLineId(lines[0].LineName);
        var stations = new List<StationData>();
        if (lineId.HasValue)
        {
            stations =
                turnoutDatabaseManager.GetStationsByLine(lineId.Value);
        }
        else
        {
            Debug.Log("lineId为空");
        }


        for (int i = 0; i < stations.Count; i++)
        {
            var stationOption = new TMP_Dropdown.OptionData();
            stationOption.text = stations[i].StationName;
            stationOptions.Add(stationOption);
        }

        stationDropdown.AddOptions(stationOptions);
    }

    private void ShowStationOptions(int line)
    {
        stationOptions.Clear();
        stationDropdown.options.Clear();
        var stations =
            turnoutDatabaseManager.GetStationsByLine(line);
        for (int i = 0; i < stations.Count; i++)
        {
            var stationOption = new TMP_Dropdown.OptionData();
            stationOption.text = stations[i].StationName;
            stationOptions.Add(stationOption);
        }


        stationDropdown.AddOptions(stationOptions);
        stationDropdown.RefreshShownValue();
    }

    private void SearchPlayerValue()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        var playerValues = turnoutDatabaseManager.GetSubProjectList(projectId, stationDropdown.value);
        for (var i = 0; i < playerValues.Count; i++)
        {
            var playerValue = playerValues[i];
            Debug.Log("playerValue:" + playerValue.SubProjectName);
            var valuePanel = UIManager.Instance.ShowUI(valuePanelName);
            Debug.Log("valuePanel:" + valuePanel.name);
            valuePanel.transform.SetParent(content);
            CreateItem(valuePanel.transform);
            valuePanel.GetComponent<UserValuePanel>().id.text = playerValue.Id.ToString();
            valuePanel.GetComponent<UserValuePanel>().userId.text = playerValue.UserId;
            valuePanel.GetComponent<UserValuePanel>().time.text = playerValue.Time;
            valuePanel.GetComponent<UserValuePanel>().subProjectName = playerValue.SubProjectName;
        }
    }

    private void CreateItem(Transform valuePanel)
    {
        // 关键：设置正确的局部坐标（UI 元素必须设置）
        RectTransform rectTransform = valuePanel.GetComponent<RectTransform>();

        // 重置锚点和坐标（避免位置错乱）
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private void ReturnPrePanel()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        UIManager.Instance.PopUI();
    }


    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}