using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XRLoadingCanvas : MonoBehaviour
{
    [Header("VR 进度面板")]
    public CanvasGroup group;
    public Slider slider;
    public TextMeshProUGUI percent;
    public Image spinner;

    void OnEnable()
    {
        XRSceneLoader.OnStart    += Show;
        XRSceneLoader.OnProgress += UpdateProgress;
        XRSceneLoader.OnComplete += Hide;
        group.alpha = 0;
    }

    void OnDisable()
    {
        XRSceneLoader.OnStart    -= Show;
        XRSceneLoader.OnProgress -= UpdateProgress;
        XRSceneLoader.OnComplete -= Hide;
    }

    void Show(string _)        
    {
        slider.value = 0;
        group.DOFade(1, 0.3f);
        spinner.transform.DORotate(new Vector3(0,0,-360), 1f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
    }

    void UpdateProgress(float p)
    {
        slider.value = p;
        percent.text = $"{Mathf.RoundToInt(p*100)}%";
    }

    void Hide(string _)
    {
        group.DOFade(0, 0.3f);
        spinner.transform.DOKill();
    }
}