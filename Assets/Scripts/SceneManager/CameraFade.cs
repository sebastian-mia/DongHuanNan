using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CameraFade : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fadeImage;
    
    [Header("Settings")]
    [SerializeField] private Color fadeColor = Color.black;
    
    private void Awake()
    {
        // 强制自动获取 CanvasGroup（即使 Inspector 没赋值）
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                Debug.LogError("CameraFade: 未找到 CanvasGroup 组件！", this);
                enabled = false;
                return;
            }
        }
            
        // 自动创建或查找 Image
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>();
            
            if (fadeImage == null)
            {
                // 创建全屏黑色背景
                GameObject imgObj = new GameObject("FadeImage");
                imgObj.transform.SetParent(transform, false);
                fadeImage = imgObj.AddComponent<Image>();
                fadeImage.color = fadeColor;
                
                RectTransform rect = fadeImage.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }
        }
        
        // 确保初始透明
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
    
    public Tween FadeOut(float duration)
    {
        if (canvasGroup == null) return null;
        canvasGroup.blocksRaycasts = true;
        return canvasGroup.DOFade(1f, duration).SetEase(Ease.InOutQuad);
    }
    
    public Tween FadeIn(float duration)
    {
        if (canvasGroup == null) return null;
        return canvasGroup.DOFade(0f, duration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => canvasGroup.blocksRaycasts = false);
    }
    
    public void SetBlack()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    
    public void SetTransparent()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}