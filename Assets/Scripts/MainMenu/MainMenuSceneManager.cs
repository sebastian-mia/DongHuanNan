using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class MainMenuSceneManager : MonoBehaviour, IController
{
    public float waitVoiceTime = 3f;
    private AudioManager audioManager;
    private GameRecordModel gameRecordModel;

    void Start()
    {
        audioManager = this.GetUtility<AudioManager>();
        gameRecordModel = this.GetModel<GameRecordModel>();
    }

    void OnEnable()
    {
        StartCoroutine("WaitVoiceCoroutine");
    }

    private IEnumerator WaitVoiceCoroutine()
    {
        Debug.Log("主界面语音");

        yield return new WaitForSeconds(waitVoiceTime);
        audioManager.PlayMainBackGroundMusic();
        if (gameRecordModel.IsSecordToMainMenu)
        {
            audioManager.PlayVoiceSecondInMainMenuScene();
        }
        else
        {
            audioManager.PlayVoiceInMainMenuScene();
        }

        gameRecordModel.IsSecordToMainMenu = true;
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}