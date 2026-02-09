// UITweenAnimation.cs - DOTween动画系统
using DG.Tweening;
using UnityEngine;
using System;

public static class UITweenAnimation
{
    // 淡入淡出（支持Image、Text、RawImage等Graphic组件）
    public static void FadeIn(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        if (target == null) return;
        
        target.SetActive(true);
        
        // 获取所有Graphic组件
        var graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        
        foreach (var graphic in graphics)
        {
            Color color = graphic.color;
            color.a = 0;
            graphic.color = color;
            
            graphic.DOFade(1, duration).SetEase(ease).SetUpdate(true);
        }
        
        // 延迟执行完成回调
        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke()).SetUpdate(true);
    }

    public static void FadeOut(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null, bool deactivateOnComplete = true)
    {
        if (target == null) return;
        
        var graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        
        foreach (var graphic in graphics)
        {
            graphic.DOFade(0, duration).SetEase(ease).SetUpdate(true);
        }
        
        DOVirtual.DelayedCall(duration, () => 
        {
            if (deactivateOnComplete)
                target.SetActive(false);
            onComplete?.Invoke();
        }).SetUpdate(true);
    }

    // 缩放动画
    public static void ScaleIn(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutBack, Action onComplete = null)
    {
        if (target == null) return;
        
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;
        
        target.transform.DOScale(Vector3.one, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void ScaleOut(this GameObject target, float duration = 0.3f, Ease ease = Ease.InBack, Action onComplete = null, bool deactivateOnComplete = true)
    {
        if (target == null) return;
        
        target.transform.DOScale(Vector3.zero, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => 
            {
                if (deactivateOnComplete)
                    target.SetActive(false);
                onComplete?.Invoke();
            });
    }

    // 滑动进入（从屏幕边缘）
    public static void SlideInFromLeft(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        if (target == null) return;
        
        target.gameObject.SetActive(true);
        
        // 记录原始位置
        Vector3 originalPos = target.anchoredPosition3D;
        
        // 计算屏幕左边缘位置
        float leftEdge = -Screen.width;
        target.anchoredPosition3D = new Vector3(leftEdge, originalPos.y, originalPos.z);
        
        // 动画到原始位置
        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromRight(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        if (target == null) return;
        
        target.gameObject.SetActive(true);
        
        Vector3 originalPos = target.anchoredPosition3D;
        float rightEdge = Screen.width;
        target.anchoredPosition3D = new Vector3(rightEdge, originalPos.y, originalPos.z);
        
        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromTop(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        if (target == null) return;
        
        target.gameObject.SetActive(true);
        
        Vector3 originalPos = target.anchoredPosition3D;
        float topEdge = Screen.height;
        target.anchoredPosition3D = new Vector3(originalPos.x, topEdge, originalPos.z);
        
        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromBottom(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        if (target == null) return;
        
        target.gameObject.SetActive(true);
        
        Vector3 originalPos = target.anchoredPosition3D;
        float bottomEdge = -Screen.height;
        target.anchoredPosition3D = new Vector3(originalPos.x, bottomEdge, originalPos.z);
        
        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // 弹跳动画
    public static void BounceIn(this GameObject target, float duration = 0.5f, Action onComplete = null)
    {
        if (target == null) return;
        
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;
        
        target.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBounce).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // 震动动画
    public static void Shake(this RectTransform target, float duration = 0.5f, float strength = 10f, Action onComplete = null)
    {
        if (target == null) return;
        
        target.DOShakeAnchorPos(duration, strength).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // 旋转动画
    public static void RotateIn(this GameObject target, float duration = 0.3f, Action onComplete = null)
    {
        if (target == null) return;
        
        target.SetActive(true);
        target.transform.rotation = Quaternion.Euler(0, 0, 180);
        
        target.transform.DORotate(Vector3.zero, duration).SetEase(Ease.OutBack).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // 脉冲动画（常用于按钮点击反馈）
    public static void Pulse(this RectTransform target, float scale = 1.1f, float duration = 0.2f, Action onComplete = null)
    {
        if (target == null) return;
        
        target.DOScale(Vector3.one * scale, duration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(true)
            .OnComplete(() => 
            {
                target.DOScale(Vector3.one, duration * 0.5f).SetEase(Ease.InQuad).SetUpdate(true)
                    .OnComplete(() => onComplete?.Invoke());
            });
    }

    // 左右拉伸循环动画（呼吸效果）- 返回Sequence供外部控制
    public static Sequence StretchLoop(
        this Transform target,
        float stretchScaleX = 1.3f,
        float stretchDuration = 0.3f,
        float shrinkDuration = 0.3f,
        Ease easeStretch = Ease.OutQuad,
        Ease easeShrink = Ease.InQuad,
        float delayBetween = 0f,
        LoopType loopType = LoopType.Restart)
    {
        if (target == null) return null;
        
        // 创建序列
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        
        // 保存原始缩放
        Vector3 originalScale = target.localScale;
        
        // 1. 拉伸阶段：X轴放大
        sequence.Append(
            target.DOScaleX(originalScale.x * stretchScaleX, stretchDuration)
                  .SetEase(easeStretch)
        );
        
        // 可选：添加延迟
        if (delayBetween > 0)
        {
            sequence.AppendInterval(delayBetween);
        }
        
        // 2. 收缩阶段：回到原始X轴缩放
        sequence.Append(
            target.DOScaleX(originalScale.x, shrinkDuration)
                  .SetEase(easeShrink)
        );
        
        // 设置无限循环
        sequence.SetLoops(-1, loopType);
        
        return sequence;
    }

    // 序列动画（组合多个动画）
    public static void SequenceAnimation(this GameObject target, params Tween[] tweens)
    {
        if (target == null || tweens == null || tweens.Length == 0) return;
        
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        
        foreach (var tween in tweens)
        {
            sequence.Append(tween);
        }
    }

    // 取消所有动画
    public static void KillAllAnimations(this GameObject target)
    {
        if (target == null) return;
        
        target.transform.DOKill();
        
        var graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>();
        foreach (var graphic in graphics)
        {
            graphic.DOKill();
        }
    }
}