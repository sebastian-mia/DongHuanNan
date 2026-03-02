using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 挂载到 UI 根节点，自动给所有子 UI 添加震动
/// 支持左右手区分震动，调用 PICO_HapticController
/// </summary>
public class UIAutoHaptic : MonoBehaviour
{
    [Header("震动设置")] [SerializeField] private float hoverEnterAmp = 0.3f;
    [SerializeField] private float hoverStayAmp = 0.08f;
    [SerializeField] private float clickAmp = 0.5f;

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

    ControllerHand GetHand(BaseEventData data)
    {
        if (data is PointerEventData pointerData && pointerData.pointerPressRaycast.gameObject != null)
        {
            string name = pointerData.pointerPressRaycast.gameObject.name.ToLower();
            if (name.Contains("left")) return ControllerHand.Left;
            if (name.Contains("right")) return ControllerHand.Right;
        }

        return ControllerHand.Both;
    }

    void OnUIHoverEnter(BaseEventData data)
    {
        var hand = GetHand(data);
        PICO_HapticController.Instance?.TriggerHaptic(hand, hoverEnterAmp, 0.05f);
    }

    void OnUIClick(BaseEventData data)
    {
        var hand = GetHand(data);
        PICO_HapticController.Instance?.TriggerHaptic(hand, clickAmp, 0.1f);
    }
}