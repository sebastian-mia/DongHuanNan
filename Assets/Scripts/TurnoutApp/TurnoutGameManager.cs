using System.Collections;
using UnityEngine;
using QFramework;
using TMPro;
using UnityEngine.InputSystem;

public class TurnoutGameManager : MonoBehaviour, IController
{
    private TurnoutModel turnoutModel;
    private int correctCount = 0;
    public readonly string spawnPointInDownPath = "Perfab/StartPointInDown";
    public readonly string spawnPointInUpPath = "Perfab/StartPointInUp";
    private bool currentSpawnPointIsDown;
    private GameObject currentSpawnPoint;


    //RouteShow Correct
    private readonly string downSideToDownSideRouteShowName = "DownSideToDownSide";
    private readonly string downSideToTranslate1RouteShowName = "DownSideToTranslate1";
    private readonly string downSideToTranslate2RouteShowName = "DownSideToTranslate2";
    private readonly string translate1ToDownSideRouteShowName = "Translate1ToDownSide";
    private readonly string translate2ToDownSideRouteShowName = "Translate2ToDownSide";
    private readonly string upSideToTranslate1RouteShowName = "UpSideToTranslate1";
    private readonly string upSideToTranslate2RouteShowName = "UpSideToTranslate2";
    private readonly string translate1ToUpSideRouteShowName = "Translate1ToUpSide";
    private readonly string translate2ToUpSideRouteShowName = "Translate2ToUpSide";
    private readonly string upSideToUpSideRouteShowName = "UpSideToUpSide";
    private readonly string targetRouteTip = "TargetRouteTip";
    
    //RouteShow Error
    private readonly string downSideToDownSideErrorRouteShowName = "DownSideToDownSideError";
    private readonly string downSideToTranslate1ErrorRouteShowName = "DownSideToTranslate1Error";
    private readonly string downSideToTranslate2ErrorRouteShowName = "DownSideToTranslate2Error";
    private readonly string translate1ToDownSideErrorRouteShowName = "Translate1ToDownSideError";
    private readonly string translate2ToDownSideErrorRouteShowName = "Translate2ToDownSideError";
    private readonly string upSideToTranslate1ErrorRouteShowName = "UpSideToTranslate1Error";
    private readonly string upSideToTranslate2ErrorRouteShowName = "UpSideToTranslate2Error";
    private readonly string translate1ToUpSideErrorRouteShowName = "Translate1ToUpSideError";
    private readonly string translate2ToUpSideErrorRouteShowName = "Translate2ToUpSideError";
    private readonly string upSideToUpSideErrorRouteShowName = "UpSideToUpSide";
    
    private GameObject targetRoute;

    private readonly string scorePanelName = "ScorePanel";
    private string currentRouteName;
    private int randomNum;

    [Header("B 键输入")] [Tooltip("右手 B 键输入动作")]
    public InputActionReference rightBButton;

    //Panel
    private readonly string returnButtonPanelTag = "ReturnButtonPanel";
    private GameObject returnButtonPanel;
    private bool isReturnButtonPanelShow = true;

    //Audio
    private AudioManager audioManager;
    public float waitVoiceTime = 1f;
    public float waitVoiceIntervalTime = 1f;
    public float longTimeWaitVoiceMinuteTime = 5f;
    private bool nodeHasFault;

    //Counter
    private int second;
    [SerializeField] private TMP_Text counterTimeText;
    private string counterTimeName = "CounterTime";
    private string counterTimeString = "00:00:00";
    private Coroutine counterTimeCoroutine;


    void Awake()
    {
        turnoutModel = this.GetModel<TurnoutModel>();
        UIManager.Instance.ScanCanvas();
        CreateTargetRoute();
        InitspawnPoint();
        audioManager = this.GetUtility<AudioManager>();
    }

    void Start()
    {
        this.RegisterEvent<ShowScorePanelEvent>(e =>
        {
            if (turnoutModel.scorePanelIndex <= 0)
            {
                UIManager.Instance.HideUI(targetRoute);
                StopCoroutine(counterTimeCoroutine);
                this.SendCommand<SetCounterTimeCommand>(new SetCounterTimeCommand(counterTimeString));
                var scorePanel = UIManager.Instance.ShowUI(scorePanelName);
                var turnoutScoreUI = scorePanel.GetComponent<TrunoutScoreUI>();
                if (turnoutScoreUI != null)
                {
                    turnoutScoreUI.subProjectName.text = currentRouteName;
                    turnoutScoreUI.counterTime.text = counterTimeString;
                    turnoutScoreUI.ShowNodeValue();
                }
                ShowRoute(turnoutModel.isCorrectRoute);
                JudgeGameStateToPlayVoice();

                turnoutModel.scorePanelIndex++;
            }
        }).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void OnEnable()
    {
        audioManager.PlayturnoutBackGroundMusic();
        StartCoroutine("PlayTurnoutProjectStartVoice");
        StartCoroutine("LongTimeTipCounter");
        if (rightBButton?.action != null)
        {
            rightBButton.action.Enable();
            rightBButton.action.started += OnRightBButtonPressed;
        }
    }

    private void OnDisable()
    {
        // 禁用输入动作
        if (rightBButton?.action != null)
        {
            rightBButton.action.started -= OnRightBButtonPressed;
        }
    }

    private void InitspawnPoint()
    {
        if (randomNum <= 5)
        {
            currentSpawnPointIsDown = true;
            currentSpawnPoint = Instantiate(Resources.Load<GameObject>(spawnPointInDownPath));
        }

        if (randomNum > 5 && randomNum <= 10)
        {
            currentSpawnPointIsDown = false;
            currentSpawnPoint = Instantiate(Resources.Load<GameObject>(spawnPointInUpPath));
        }
    }

    private void CreateTargetRoute()
    {
        randomNum = this.GetUtility<RandomNumber>().Range(1, this.GetModel<TurnoutModel>().routeCount);
        Debug.Log("randomNum:" + randomNum);
        // randomNum = 1;
        targetRoute = UIManager.Instance.ShowUI(targetRouteTip);
        if (randomNum == 1)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:邱隘东下行至东环南路下行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:邱隘东下行至东环南路下行进路";
            currentRouteName = "请准备:邱隘东下行至东环南路下行进路";
            this.SendCommand<PerpareDownSideToDownSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 2)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:东环南路下行至折1道进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路下行至折1道进路";
            currentRouteName = "请准备:东环南路下行至折1道进路";
            this.SendCommand<PerpareDownSideToTranslate1RouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 3)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:东环南路下行至折2道进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路下行至折2道进路";
            currentRouteName = "请准备:东环南路下行至折2道进路";
            this.SendCommand<PerpareDownSideToTranslate2RouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 4)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:折1道至东环南路下行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折1道至东环南路下行进路";
            currentRouteName = "请准备:折1道至东环南路下行进路";
            this.SendCommand<PerpareTranslate1ToDownSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 5)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:折2道至东环南路下行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折2道至东环南路下行进路";
            currentRouteName = "请准备:折2道至东环南路下行进路";
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 6)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:东环南路上行至邱隘东上行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至邱隘东上行进路";
            currentRouteName = "请准备:东环南路上行至邱隘东上行进路";
            this.SendCommand<PerpareUpSideToUpSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 7)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:东环南路上行至折1道进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至折1道进路";
            currentRouteName = "请准备:东环南路上行至折1道进路";
            this.SendCommand<PerpareUpSideToTranslate1RouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 8)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:东环南路上行至折2道进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至折2道进路";
            currentRouteName = "请准备:东环南路上行至折2道进路";
            this.SendCommand<PerpareUpSideToTranslate2RouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 9)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:折1道至东环南路上行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折1道至东环南路上行进路";
            currentRouteName = "请准备:折1道至东环南路上行进路";
            this.SendCommand<PerpareTranslate1ToUpSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }

        if (randomNum == 10)
        {
            UITweenAnimation.TypewriterRichText(targetRoute.GetComponent<TextMeshProUGUI>(), "请准备:折2道至东环南路上行进路", 2f);
            // targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折2道至东环南路上行进路";
            currentRouteName = "请准备:折2道至东环南路上行进路";
            this.SendCommand<PerpareTranslate2ToUpSideRouteCommand>();
            this.SendCommand<SetCurrentSubProjectNameCommand>(new SetCurrentSubProjectNameCommand(currentRouteName));
        }
    }

    private void ShowRoute(bool isCorrect)
    {
        if (randomNum == 1)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(downSideToDownSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(downSideToDownSideErrorRouteShowName);
            
        }

        if (randomNum == 2)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(downSideToTranslate1RouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(downSideToTranslate1ErrorRouteShowName);
           
        }

        if (randomNum == 3)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(downSideToTranslate2RouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(downSideToTranslate2ErrorRouteShowName);
        }

        if (randomNum == 4)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(translate1ToDownSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(translate1ToDownSideErrorRouteShowName);
        }

        if (randomNum == 5)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(translate2ToDownSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(translate2ToDownSideRouteShowName);
           
        }

        if (randomNum == 6)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(upSideToUpSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(upSideToUpSideErrorRouteShowName);
        }

        if (randomNum == 7)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(upSideToTranslate1RouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(upSideToTranslate1ErrorRouteShowName);
        }

        if (randomNum == 8)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(upSideToTranslate2RouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(upSideToTranslate2ErrorRouteShowName);
        }

        if (randomNum == 9)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(translate1ToUpSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(translate1ToUpSideErrorRouteShowName);
        }

        if (randomNum == 10)
        {
            if (turnoutModel.isCorrectRoute)
            {
                UIManager.Instance.ShowUI(translate2ToUpSideRouteShowName);
                return;
            }
            UIManager.Instance.ShowUI(translate2ToUpSideErrorRouteShowName);
        }
    }

    private void CaculateScore()
    {
    }

    // ========== 事件回调方法 ==========

    private void OnRightBButtonPressed(InputAction.CallbackContext context)
    {
        if (isReturnButtonPanelShow)
        {
            returnButtonPanel = UIManager.Instance.ShowUI(returnButtonPanelTag);
        }
        else
        {
            if (returnButtonPanel != null)
                UIManager.Instance.HideUI(returnButtonPanel);
        }

        isReturnButtonPanelShow = !isReturnButtonPanelShow;
    }


    void Update()
    {
        // var currents = this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines;
        // var targets = this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines;
        // foreach (var current in currents)
        // {
        //     Debug.Log("Current TtunoutId:" + current.Value.TurnoutID + "TurnoutState" + current.Value.TurnoutState);
        // }
        //
        // foreach (var target in targets)
        // {
        //     Debug.Log("Target TtunoutId:" + target.Value.TurnoutID + "TurnoutState" + target.Value.TurnoutState);
        // }
    }

    //音效
    private IEnumerator PlayTurnoutProjectStartVoice()
    {
        yield return new WaitForSeconds(waitVoiceTime);
        counterTimeCoroutine = StartCoroutine(counterTimeName);
        if (currentSpawnPointIsDown)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectWelcomeDownTailSoundPath);
        }
        else
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectWelcomeUpHeadSoundPath);
        }

        if (randomNum == 1)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteDownSideToDownSideSoundPath);
        }

        if (randomNum == 2)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteDownSideToTranslate1SoundPath);
        }

        if (randomNum == 3)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteDownSideToTranslate2SoundPath);
        }

        if (randomNum == 4)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteTranslate1ToDownSideSoundPath);
        }

        if (randomNum == 5)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteTranslate2ToDownSideSoundPath);
        }

        if (randomNum == 6)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteUpSideToUpSideSoundPath);
        }

        if (randomNum == 7)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteUpSideToTranslate1SoundPath);
        }

        if (randomNum == 8)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteUpSideToTranslate2SoundPath);
        }

        if (randomNum == 9)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteTranslate1ToUpSideSoundPath);
        }

        if (randomNum == 10)
        {
            audioManager.EnqueueVoice(DongHuanNanRouteVoicePath.turnoutProjectRouteTranslate2ToUpSideSideSoundPath);
        }
    }

    private IEnumerator LongTimeTipCounter()
    {
        yield return new WaitForSeconds(longTimeWaitVoiceMinuteTime * 60f);
        audioManager.PlayVoiceWaitLongTimeSound();
    }

    private void JudgeGameStateToPlayVoice()
    {
        for (int i = 0; i < turnoutModel.nodeValues.Count; i++)
        {
            if (turnoutModel.nodeValues[i].nodeName.Equals(RouteNodeTag.红闪灯.ToString()))
            {
                if (!turnoutModel.nodeValues[i].isConsistence)
                {
                    audioManager.PlayVoiceInRedLightFault();
                    return;
                }
            }
        }

        if (!turnoutModel.isCorrectRoute)
        {
            audioManager.PlayVoiceInRouteFault();
            return;
        }

        for (int i = 0; i < turnoutModel.nodeValues.Count; i++)
        {
            if (!turnoutModel.nodeValues[i].isConsistence)
            {
                nodeHasFault = true;
                audioManager.PlayVoiceInComplete();
                return;
            }
        }

        if (!nodeHasFault)
        {
            audioManager.PlayVoiceInPerfectComplete();
            return;
        }
    }

    private IEnumerator CounterTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            second++;
            counterTimeString = FormatTime();
        }
    }

    private string FormatTime()
    {
        int hours = second / 3600;
        int mins = (second % 3600) / 60;
        int secs = second % 60;
        counterTimeText.text = hours.ToString("00") + ":" + mins.ToString("00") + ":" + secs.ToString("00");
        return mins.ToString("00") + ":" + secs.ToString("00");
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}