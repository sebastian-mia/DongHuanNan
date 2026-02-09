using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnoutMachineUI : UIBase, IController
{
    public TurnoutID turnoutID;
    public TMP_Text handlerTag;
    public TMP_Text cutPowerTag;
    private Sequence handlerTagStretchSequence;
    private Sequence cutPowerTagStretchSequence;

    void Start()
    {
        this.RegisterEvent<CutPowerEvent>(e => { cutPowerTagStretchSequence.Kill(); })
            .UnRegisterWhenGameObjectDestroyed(this);
    }

    void OnEnable()
    {
        // 启动循环动画
        handlerTagStretchSequence = handlerTag.transform.StretchLoop(
            stretchScaleX: 1.2f,
            stretchDuration: 0.5f,
            shrinkDuration: 0.7f,
            easeStretch: Ease.OutBack,
            easeShrink: Ease.InOutSine
        );
        cutPowerTagStretchSequence = cutPowerTag.transform.StretchLoop(
            stretchScaleX: 1.2f,
            stretchDuration: 0.5f,
            shrinkDuration: 0.7f,
            easeStretch: Ease.OutBack,
            easeShrink: Ease.InOutSine
        );
        handlerTagStretchSequence.Play();
        cutPowerTagStretchSequence.Play();
    }

    void OnDisable()
    {
        // 必须手动停止，防止内存泄漏
        handlerTagStretchSequence.Kill();
        cutPowerTagStretchSequence.Kill();
    }

    public override int GetParamHash()
    {
        return (int)turnoutID;
    }

    public override void Refresh(int paramHash)
    {
        turnoutID = (TurnoutID)paramHash;
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}