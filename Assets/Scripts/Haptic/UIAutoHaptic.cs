using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 挂载到 UI 根节点，自动给所有子 UI 添加震动
/// 支持左右手区分震动，调用 PICO_HapticController
/// </summary>
public class UIAutoHaptic : MonoBehaviour
{
    [Header("震动设置")] 
    [SerializeField] private float hoverEnterAmp = 0.1f;
    [SerializeField] private float clickAmp = 0.1f;

    void Start()
    {
        AutoAddHapticToAllUI();
    }

    void AutoAddHapticToAllUI()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
            AddHapticToUI(btn.gameObject);

        var toggles = GetComponentsInChildren<Toggle>(true);
        foreach (var tgl in toggles)
            AddHapticToUI(tgl.gameObject);

        var sliders = GetComponentsInChildren<Slider>(true);
        foreach (var sld in sliders)
            AddHapticToUI(sld.gameObject);

        Debug.Log($"[UIAutoHaptic] 已为 {buttons.Length + toggles.Length + sliders.Length} 个 UI 添加震动");
    }

    void AddHapticToUI(GameObject uiElement)
    {
        var trigger = uiElement.GetComponent<EventTrigger>() ?? uiElement.AddComponent<EventTrigger>();

        AddEventEntry(trigger, EventTriggerType.PointerEnter, OnUIHoverEnter);
        AddEventEntry(trigger, EventTriggerType.PointerClick, OnUIClick);
    }

    void AddEventEntry(EventTrigger trigger, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    /// <summary>
    /// 🔥 修正：PICO 的 pointerId 是 3=左手, 2=右手
    /// </summary>
    public ControllerHand GetHand(BaseEventData data)
    {
        PointerEventData pointerData = data as PointerEventData;
        
        if (pointerData == null)
        {
            Debug.LogWarning($"[UIAutoHaptic] data 不是 PointerEventData，实际类型: {data?.GetType().Name}");
            return ControllerHand.Both;
        }

        Debug.Log($"[UIAutoHaptic] PointerId: {pointerData.pointerId}");

        // 🔥 PICO XR: 3=左手, 2=右手
        if (pointerData.pointerId == 3)
            return ControllerHand.Left;
        if (pointerData.pointerId == 2)
            return ControllerHand.Right;
        
        // 回退：如果未来变成 0/1 也能工作
        if (pointerData.pointerId == 0)
            return ControllerHand.Left;
        if (pointerData.pointerId == 1)
            return ControllerHand.Right;
        
        return ControllerHand.Both;
    }

    void OnUIHoverEnter(BaseEventData data)
    {
        var hand = GetHand(data);
        Debug.Log($"[UIAutoHaptic] HoverEnter 手: {hand}");
        PICO_HapticController.Instance?.TriggerHaptic(hand, hoverEnterAmp, 0.05f);
    }

    void OnUIClick(BaseEventData data)
    {
        var hand = GetHand(data);
        Debug.Log($"[UIAutoHaptic] Click 手: {hand}");
        PICO_HapticController.Instance?.TriggerHaptic(hand, clickAmp, 0.1f);
    }
}