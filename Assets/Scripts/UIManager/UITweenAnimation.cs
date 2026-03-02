// UITweenAnimation.cs - DOTween动画系统

using DG.Tweening;
using UnityEngine;
using System;
using TMPro;

public static class UITweenAnimation
{
    // ==================== 原有方法保持不变 ====================

    // 淡入淡出（支持Image、Text、RawImage等Graphic组件）
    public static void FadeIn(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        if (target == null) return;

        target.SetActive(true);

        var graphics = target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);

        foreach (var graphic in graphics)
        {
            Color color = graphic.color;
            color.a = 0;
            graphic.color = color;

            graphic.DOFade(1, duration).SetEase(ease).SetUpdate(true);
        }

        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke()).SetUpdate(true);
    }

    public static void FadeOut(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null, bool deactivateOnComplete = true)
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
    public static void ScaleIn(this GameObject target, float duration = 0.3f, Ease ease = Ease.OutBack,
        Action onComplete = null)
    {
        if (target == null) return;

        target.SetActive(true);
        target.transform.localScale = Vector3.zero;

        target.transform.DOScale(Vector3.one, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void ScaleOut(this GameObject target, float duration = 0.3f, Ease ease = Ease.InBack,
        Action onComplete = null, bool deactivateOnComplete = true)
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
    public static void SlideInFromLeft(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        if (target == null) return;

        target.gameObject.SetActive(true);
        Vector3 originalPos = target.anchoredPosition3D;
        float leftEdge = -Screen.width;
        target.anchoredPosition3D = new Vector3(leftEdge, originalPos.y, originalPos.z);

        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromRight(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        if (target == null) return;

        target.gameObject.SetActive(true);
        Vector3 originalPos = target.anchoredPosition3D;
        float rightEdge = Screen.width;
        target.anchoredPosition3D = new Vector3(rightEdge, originalPos.y, originalPos.z);

        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromTop(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        if (target == null) return;

        target.gameObject.SetActive(true);
        Vector3 originalPos = target.anchoredPosition3D;
        float topEdge = Screen.height;
        target.anchoredPosition3D = new Vector3(originalPos.x, topEdge, originalPos.z);

        target.DOAnchorPos3D(originalPos, duration).SetEase(ease).SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public static void SlideInFromBottom(this RectTransform target, float duration = 0.3f, Ease ease = Ease.OutQuad,
        Action onComplete = null)
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
    public static void Shake(this RectTransform target, float duration = 0.5f, float strength = 10f,
        Action onComplete = null)
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
    public static void Pulse(this RectTransform target, float scale = 1.1f, float duration = 0.2f,
        Action onComplete = null)
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

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        Vector3 originalScale = target.localScale;

        sequence.Append(
            target.DOScaleX(originalScale.x * stretchScaleX, stretchDuration)
                .SetEase(easeStretch)
        );

        if (delayBetween > 0)
        {
            sequence.AppendInterval(delayBetween);
        }

        sequence.Append(
            target.DOScaleX(originalScale.x, shrinkDuration)
                .SetEase(easeShrink)
        );

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

    // ==================== 新增：打字机效果 ====================

    /// <summary>
    /// 打字机效果 - 逐字显示文本（TMP_Text）
    /// </summary>
    /// <param name="target">TMP_Text 组件</param>
    /// <param name="content">完整文本内容</param>
    /// <param name="duration">总持续时间</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="onCharacterTyped">每打出一个字的回调（参数：当前索引，当前字符）</param>
    /// <returns>返回 Sequence 供外部控制（如暂停、停止）</returns>
    public static Sequence Typewriter(TMP_Text target, string content, float duration,
        Action onComplete = null, Action<int, char> onCharacterTyped = null)
    {
        if (target == null) return null;

        target.text = "";
        target.gameObject.SetActive(true);

        int charCount = content.Length;
        if (charCount == 0)
        {
            onComplete?.Invoke();
            return null;
        }

        // 计算每个字符的间隔时间
        float interval = duration / charCount;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < charCount; i++)
        {
            int index = i; // 捕获索引
            char character = content[i];

            sequence.AppendCallback(() =>
            {
                target.text += character;
                onCharacterTyped?.Invoke(index, character);
            });

            sequence.AppendInterval(interval);
        }

        sequence.OnComplete(() => onComplete?.Invoke());

        return sequence;
    }

    /// <summary>
    /// 打字机效果 - 支持富文本标签（TMP_Text）
    /// 会自动处理 <color>, <b>, <i> 等标签，不打断标签
    /// </summary>
    public static Sequence TypewriterRichText(TMP_Text target, string content, float duration,
        Action onComplete = null, Action<int, string> onTagParsed = null)
    {
        if (target == null) return null;

        target.text = "";
        target.gameObject.SetActive(true);

        // 解析文本，分离普通字符和富文本标签
        var parser = new RichTextParser(content);
        var segments = parser.Parse();

        if (segments.Count == 0)
        {
            onComplete?.Invoke();
            return null;
        }

        float interval = duration / segments.Count;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        string currentText = "";

        foreach (var segment in segments)
        {
            var seg = segment; // 捕获

            sequence.AppendCallback(() =>
            {
                if (seg.IsTag)
                {
                    // 富文本标签：直接追加，不触发打字音效
                    currentText += seg.Content;
                    onTagParsed?.Invoke(currentText.Length, seg.Content);
                }
                else
                {
                    // 普通字符：逐字追加
                    currentText += seg.Content;
                }

                target.text = currentText;
            });

            sequence.AppendInterval(interval);
        }

        sequence.OnComplete(() => onComplete?.Invoke());

        return sequence;
    }

    /// <summary>
    /// 打字机效果 - 带光标闪烁（TMP_Text）
    /// </summary>
    public static Sequence TypewriterWithCursor(TMP_Text target, string content, float duration,
        string cursorChar = "_", float blinkInterval = 0.3f, Action onComplete = null)
    {
        if (target == null) return null;

        target.text = "";
        target.gameObject.SetActive(true);

        int charCount = content.Length;
        float typeInterval = duration / charCount;

        Sequence mainSequence = DOTween.Sequence().SetUpdate(true);

        // 打字部分
        for (int i = 0; i < charCount; i++)
        {
            int index = i;
            char character = content[i];

            mainSequence.AppendCallback(() =>
            {
                // 移除旧光标，添加新字符和新光标
                string currentText = content.Substring(0, index + 1);
                target.text = currentText + cursorChar;
            });

            mainSequence.AppendInterval(typeInterval);
        }

        // 打字完成后，光标闪烁
        Sequence blinkSequence = DOTween.Sequence().SetUpdate(true);
        blinkSequence.SetLoops(-1);

        blinkSequence.AppendCallback(() => target.text = content + cursorChar);
        blinkSequence.AppendInterval(blinkInterval);
        blinkSequence.AppendCallback(() => target.text = content + " ");
        blinkSequence.AppendInterval(blinkInterval);

        // 合并序列
        Sequence finalSequence = DOTween.Sequence().SetUpdate(true);
        finalSequence.Append(mainSequence);
        finalSequence.AppendCallback(() =>
        {
            // 打字完成，开始闪烁
            blinkSequence.Play();
        });

        // 提供停止闪烁的方法
        finalSequence.OnComplete(() =>
        {
            blinkSequence.Kill();
            target.text = content; // 移除光标
            onComplete?.Invoke();
        });

        // 保存引用以便外部控制
        finalSequence.SetId($"Typewriter_{target.GetInstanceID()}");

        return finalSequence;
    }

    /// <summary>
    /// 打字机效果 - 加速版（支持用户按键加速）
    /// </summary>
    public static Sequence TypewriterAccelerated(TMP_Text target, string content, float baseDuration,
        float speedUpMultiplier = 3f, KeyCode speedUpKey = KeyCode.Space, Action onComplete = null)
    {
        if (target == null) return null;

        target.text = "";
        target.gameObject.SetActive(true);

        int charCount = content.Length;
        float baseInterval = baseDuration / charCount;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < charCount; i++)
        {
            int index = i;
            char character = content[i];

            sequence.AppendCallback(() =>
            {
                target.text += character;

                // 检测加速键
                if (Input.GetKey(speedUpKey))
                {
                    // 加速：减少后续等待时间
                    sequence.timeScale = speedUpMultiplier;
                }
                else
                {
                    sequence.timeScale = 1f;
                }
            });

            sequence.AppendInterval(baseInterval);
        }

        sequence.OnComplete(() =>
        {
            sequence.timeScale = 1f;
            onComplete?.Invoke();
        });

        return sequence;
    }

    /// <summary>
    /// 停止指定 TMP_Text 的打字机效果
    /// </summary>
    public static void KillTypewriter(TMP_Text target, bool completeImmediately = false)
    {
        if (target == null) return;

        string id = $"Typewriter_{target.GetInstanceID()}";

        if (completeImmediately)
        {
            DOTween.Complete(id);
        }
        else
        {
            DOTween.Kill(id);
        }
    }

    /// <summary>
    /// 富文本解析器（内部类）
    /// </summary>
    private class RichTextParser
    {
        private readonly string _text;

        public RichTextParser(string text)
        {
            _text = text;
        }

        public System.Collections.Generic.List<TextSegment> Parse()
        {
            var segments = new System.Collections.Generic.List<TextSegment>();
            int i = 0;

            while (i < _text.Length)
            {
                if (_text[i] == '<')
                {
                    // 找到标签结束
                    int end = _text.IndexOf('>', i);
                    if (end > i)
                    {
                        string tag = _text.Substring(i, end - i + 1);
                        segments.Add(new TextSegment { Content = tag, IsTag = true });
                        i = end + 1;
                        continue;
                    }
                }

                // 普通字符
                segments.Add(new TextSegment { Content = _text[i].ToString(), IsTag = false });
                i++;
            }

            return segments;
        }
    }

    private struct TextSegment
    {
        public string Content;
        public bool IsTag;
    }
}