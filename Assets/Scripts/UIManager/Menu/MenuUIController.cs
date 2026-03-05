using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using Record.GameRecord.Command;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour, IController
{
    // public Button startGameBtn;
    // public Button settingBtn;

    //UIPanelName
    private readonly string StartMenuPanelTag = "StartMenuPanel";
    private readonly string ToggleProjectPanelTag = "ToggleProjectPanel";
    private readonly string TurnoutProjectPanelTag = "TurnoutProjectPanel";
    private readonly string LocomotionModePanelTag = "LocomotionModePanel";
    private readonly string UserIdPanelTag = "UserIdPanel";
    private readonly string TurnoutProjectScorePanelTag = "TurnoutProjectScorePanel";
    private readonly string ToggleProjectScorePanelTag = "ToggleProjectScorePanel";
    private readonly string InfoPanelTag = "InfoPanel";


    //Panel
    private MenuPanel startMenuPanel;
    private ToggleProjectPanel toggleProjectPanel;
    private ToggleProjectScorePanel toggleProjectScorePanel;
    private TurnoutProjectPanel turnoutProjectPanel;
    private TurnoutProjectScorePanel turnoutProjectScorePanel;
    private LocomotionModePanel locomotionModePanel;
    private UserIdPanel userIdPanel;
    private InfoPanel infoPanel;

    //Index
    private int turnoutProjectButtonIndex;
    private int userIdPanelIndex;
    private int startTurnoutProjectButtonIndex;
    private int turnoutProjectScorePanelIndex;

    private EnumUtils enumUtils;
    private Validator validator;

    void Start()
    {
        enumUtils = this.GetUtility<EnumUtils>();
        validator = this.GetUtility<Validator>();
        // startGameBtn.onClick.AddListener(StartGame);
        startMenuPanel = UIManager.Instance.PushUI(StartMenuPanelTag).GetComponent<MenuPanel>();
        if (startMenuPanel != null)
        {
            startMenuPanel.startGameButton.onClick.AddListener(ShowSetUserIdPanel);
            startMenuPanel.settingButton.onClick.AddListener(ShowLocomotionModePanel);
            startMenuPanel.boardButton.onClick.AddListener(ShowToggleProjectScorePanel);
            startMenuPanel.infoButton.onClick.AddListener(ShowInfoPanel);
        }
    }

    private void ShowInfoPanel()
    {
        infoPanel = UIManager.Instance.PushUI(InfoPanelTag).GetComponent<InfoPanel>();
    }

    private void ShowLocomotionModePanel()
    {
        locomotionModePanel = UIManager.Instance.PushUI(LocomotionModePanelTag).GetComponent<LocomotionModePanel>();
    }

    private void ShowSetUserIdPanel()
    {
        userIdPanel = UIManager.Instance.PushUI(UserIdPanelTag).GetComponent<UserIdPanel>();
        if (userIdPanel != null)
        {
            if (userIdPanelIndex <= 0)
            {
                userIdPanel.confirmButton.onClick.AddListener(() =>
                {
                    if (userIdPanel.userId.text != null && userIdPanel.userId.text != "")
                    {
                        if (validator.IsSixDigitNumberFast(userIdPanel.userId.text))
                        {
                            userIdPanel.tip.gameObject.SetActive(false);
                            this.SendCommand<SetCurrentUserIdCommand>(
                                new SetCurrentUserIdCommand(userIdPanel.userId.text));
                            ShowToggleProjectPanel();
                        }

                        userIdPanel.tip.gameObject.SetActive(true);
                    }
                });
                userIdPanel.returnButton.onClick.AddListener(ReturnPrePanel);
                userIdPanelIndex++;
            }
        }
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

    private void ShowToggleProjectScorePanel()
    {
        toggleProjectScorePanel = UIManager.Instance.PushUI(ToggleProjectScorePanelTag)
            .GetComponent<ToggleProjectScorePanel>();
        if (toggleProjectScorePanel != null)
        {
            if (turnoutProjectScorePanelIndex <= 0)
            {
                toggleProjectScorePanel.turnoutProjectButton.onClick.AddListener(ShowTurnoutProjectScorePanel);
                toggleProjectScorePanel.returnButton.onClick.AddListener(ReturnPrePanel);
                turnoutProjectScorePanelIndex++;
            }
        }
    }

    private void ShowTurnoutProjectScorePanel()
    {
        turnoutProjectScorePanel = UIManager.Instance.PushUI(TurnoutProjectScorePanelTag)
            .GetComponent<TurnoutProjectScorePanel>();
    }

    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }

    private void ShowTurnoutProjectPanel()
    {
        this.SendCommand<SetCurrentProjectIdCommand>();
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
            for (int i = 0; i < enumUtils.GetCount<Line1StationName>(); i++)
            {
                if (enumUtils.GetValues<Line1StationName>()[i].ToString()
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
    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}