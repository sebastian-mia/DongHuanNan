using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

/// <summary>
/// 挂载到 UI 根节点，自动给所有子 UI 添加声音反馈
/// 使用 AudioManager 播放声音
/// </summary>
public class UIAutoSound : MonoBehaviour, IController
{
    [Header("防 Spam 设置")] [SerializeField] private float hoverCooldown = 0.1f; // Hover 冷却时间

    private AudioManager _audioManager;
    private float _lastHoverTime;

    void Start()
    {
        // 获取 AudioManager（QFramework 的 IUtility）
        _audioManager = this.GetUtility<AudioManager>();

        AutoAddSoundToAllUI();
    }

    void AutoAddSoundToAllUI()
    {
        // 给所有 Selectable UI 添加（Button、Toggle、Slider、Dropdown、InputField 等）
        var uiElements = GetComponentsInChildren<Selectable>(true);
        foreach (var ui in uiElements)
        {
            AddSoundToUI(ui.gameObject);
        }

        Debug.Log($"[UIAutoSound] 已为 {uiElements.Length} 个 UI 添加声音反馈");
    }

    void AddSoundToUI(GameObject uiElement)
    {
        // 添加 Event Trigger
        var eventTrigger = uiElement.GetComponent<EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = uiElement.AddComponent<EventTrigger>();

        // Hover Enter - 播放 Hover 声音
        var hoverEntry = new EventTrigger.Entry();
        hoverEntry.eventID = EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((data) => { PlayHoverSound(); });
        eventTrigger.triggers.Add(hoverEntry);

        // Click - 播放 Click 声音
        var clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((data) => { PlayClickSound(); });
        eventTrigger.triggers.Add(clickEntry);
    }

    void PlayHoverSound()
    {
        // 冷却检查，防止快速悬停 spam
        if (Time.time - _lastHoverTime < hoverCooldown) return;
        _lastHoverTime = Time.time;

        _audioManager?.PlayButtonHoverSound();
        // Debug.Log("[UIAutoSound] Hover");
    }

    void PlayClickSound()
    {
        _audioManager?.PlayButtonClickSound();
        // Debug.Log("[UIAutoSound] Click");
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}