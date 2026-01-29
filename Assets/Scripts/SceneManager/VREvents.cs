using UnityEngine;
using System;

// 全局事件总线（放在Persistent Scene）
public static class VREvents
{
    public static event Action<SceneData> OnSceneWillLoad;
    public static event Action<SceneData> OnSceneLoaded;
    public static event Action OnAllScenesReady;
    
    public static void TriggerSceneWillLoad(SceneData data) => OnSceneWillLoad?.Invoke(data);
    public static void TriggerSceneLoaded(SceneData data) => OnSceneLoaded?.Invoke(data);
}

public struct SceneData
{
    public string SceneName;
    public bool IsAdditive;
}