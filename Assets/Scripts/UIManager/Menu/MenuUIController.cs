using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    // public Button startGameBtn;
    // public Button settingBtn;

    //UIPanelName
    private readonly string StartMenuPanelTag = "StartMenuPanel";
    private readonly string ToggleProjectPanelTag = "ToggleProjectPanel";
    private readonly string TurnoutProjectPanelTag = "TurnoutProjectPanel";
    private readonly string LocomotionModePanelTag = "LocomotionModePanel";

    //Panel
    private MenuPanel startMenuPanel;
    private ToggleProjectPanel toggleProjectPanel;
    private TurnoutProjectPanel turnoutProjectPanel;
    private LocomotionModePanel locomotionModePanel;

    //Index
    private int turnoutProjectButtonIndex;
    private int startTurnoutProjectButtonIndex;

    void Start()
    {
        // startGameBtn.onClick.AddListener(StartGame);
        startMenuPanel = UIManager.Instance.PushUI(StartMenuPanelTag).GetComponent<MenuPanel>();
        if (startMenuPanel != null)
        {
            startMenuPanel.startGameButton.onClick.AddListener(ShowToggleProjectPanel);
            startMenuPanel.settingButton.onClick.AddListener(ShowLocomotionModePanel);
        }
    }

    private void ShowLocomotionModePanel()
    {
        locomotionModePanel = UIManager.Instance.PushUI(LocomotionModePanelTag).GetComponent<LocomotionModePanel>();
    }


    private void ShowToggleProjectPanel()
    {
        toggleProjectPanel = UIManager.Instance.PushUI(ToggleProjectPanelTag).GetComponent<ToggleProjectPanel>();
        if (toggleProjectPanel != null)
        {
            if (turnoutProjectButtonIndex <= 0)
            {
                toggleProjectPanel.turnoutProjectButton.onClick.AddListener(ShowTurnoutProjectPanel);
                toggleProjectPanel.returnButton.onClick.AddListener(ReturnPrePanel);
                turnoutProjectButtonIndex++;
            }
        }
    }

    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }

    private void ShowTurnoutProjectPanel()
    {
        turnoutProjectPanel = UIManager.Instance.PushUI(TurnoutProjectPanelTag).GetComponent<TurnoutProjectPanel>();
        if (turnoutProjectPanel != null)
        {
            if (startTurnoutProjectButtonIndex <= 0)
            {
                turnoutProjectPanel.startTurnoutProjectButton.onClick.AddListener(JudgeTurnoutMap);
                turnoutProjectPanel.returnButton.onClick.AddListener(ReturnPrePanel);
                startTurnoutProjectButtonIndex++;
            }
        }
    }

    private void JudgeTurnoutMap()
    {
        if (turnoutProjectPanel.lineInputField.text == "" || turnoutProjectPanel.stationInputField.text == "")
            return;
        if (turnoutProjectPanel.lineInputField.text.Contains(LineName.一号线.ToString()))
        {
            for (int i = 0; i < EnumUtils.GetCount<Line1StationName>(); i++)
            {
                if (EnumUtils.GetValues<Line1StationName>()[i].ToString()
                    .Contains(turnoutProjectPanel.stationInputField.text))
                {
                    VRSceneManager.Instance.SwitchSceneAsync(turnoutProjectPanel.stationInputField.text);
                }
            }
        }
    }


    // private void StartGame()
    // {
    //     VRSceneManager.Instance.SwitchSceneAsync("DongHuanNan");
    // }
}