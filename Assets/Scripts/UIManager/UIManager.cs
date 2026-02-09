using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas 自动查找规则")] [SerializeField]
    string canvasTag = "UICanvas";

    [SerializeField] string canvasName = "CanvasUI";

    private Transform canvasRoot;
    private readonly Dictionary<string, GameObject> prefabDict = new(32);
    private readonly Dictionary<string, Stack<GameObject>> poolDict = new(32);
    private readonly HashSet<GameObject> showSet = new(32);

    [Header("UI 堆栈设置")] [SerializeField] bool enableStack = true;
    [SerializeField] bool autoHidePrevious = true;
    [SerializeField] bool restorePreviousOnClose = true;

    private class StackNode
    {
        public GameObject Instance;
        public string PrefabName;
        public bool WasVisible;
        public object UserData;
    }

    private readonly Stack<StackNode> uiStack = new();
    private readonly Dictionary<GameObject, StackNode> nodeLookup = new();

    public event Action<GameObject, string> OnUIPushed;
    public event Action<GameObject, string> OnUIPopped;
    public event Action OnStackCleared;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        else Destroy(gameObject);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        foreach (var go in showSet)
        {
            if (go != null) Destroy(go);
        }

        showSet.Clear();

        foreach (var kvp in poolDict)
        {
            var pool = kvp.Value;
            while (pool.Count > 0)
            {
                var go = pool.Pop();
                if (go != null) Destroy(go);
            }
        }

        poolDict.Clear();

        uiStack.Clear();
        nodeLookup.Clear();
        canvasRoot = null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    public void ScanCanvas()
    {
        Canvas c = null;
        if (!string.IsNullOrEmpty(canvasTag))
        {
            GameObject go = GameObject.FindGameObjectWithTag(canvasTag);
            if (go != null) c = go.GetComponent<Canvas>();
        }

        if (c == null && !string.IsNullOrEmpty(canvasName))
        {
            GameObject go = GameObject.Find(canvasName);
            if (go != null) c = go.GetComponent<Canvas>();
        }

        if (c == null) c = FindFirstObjectByType<Canvas>();

        if (c != null) canvasRoot = c.transform;
        else Debug.LogError("[UIManager] 当前场景找不到符合条件的 Canvas！");
    }

    public GameObject ShowUI(string prefabName)
    {
        if (canvasRoot == null) ScanCanvas();
        if (canvasRoot == null) return null;

        if (!prefabDict.TryGetValue(prefabName, out GameObject prefab))
        {
            string path = $"UI/{prefabName}";
            prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 找不到预制体：{path}");
                return null;
            }
            prefabDict[prefabName] = prefab;
        }

        if (!poolDict.TryGetValue(prefabName, out var pool))
            pool = poolDict[prefabName] = new Stack<GameObject>();

        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Pop();
            go.transform.SetParent(canvasRoot, false);
        }
        else
        {
            go = Instantiate(prefab, canvasRoot);
        }

        go.name = prefabName;
        go.SetActive(true);
        showSet.Add(go);
        return go;
    }

    public void HideUI(GameObject instance)
    {
        if (instance == null || !showSet.Contains(instance)) return;
        instance.SetActive(false);
        showSet.Remove(instance);

        string key = instance.name;
        if (poolDict.TryGetValue(key, out var pool))
            pool.Push(instance);
        else
            Destroy(instance);
    }

    public void CloseUI(GameObject instance)
    {
        if (instance == null || !showSet.Contains(instance)) return;
        showSet.Remove(instance);
        Destroy(instance);
    }

    public GameObject PushUI(string prefabName, object userData = null)
    {
        if (!enableStack) return ShowUI(prefabName);

        if (autoHidePrevious && uiStack.Count > 0)
        {
            var currentTop = uiStack.Peek();
            if (currentTop.Instance != null)
            {
                currentTop.WasVisible = currentTop.Instance.activeSelf;
                currentTop.Instance.SetActive(false);
            }
        }

        GameObject instance = ShowUI(prefabName);
        if (instance == null) return null;

        var node = new StackNode
        {
            Instance = instance,
            PrefabName = prefabName,
            WasVisible = true,
            UserData = userData
        };

        uiStack.Push(node);
        nodeLookup[instance] = node;

        OnUIPushed?.Invoke(instance, prefabName);
        return instance;
    }

    public bool PopUI(bool returnToPool = true)
    {
        if (uiStack.Count == 0) return false;

        var node = uiStack.Pop();
        nodeLookup.Remove(node.Instance);

        if (returnToPool)
            HideUI(node.Instance);
        else
            CloseUI(node.Instance);

        OnUIPopped?.Invoke(node.Instance, node.PrefabName);

        if (restorePreviousOnClose && uiStack.Count > 0)
        {
            var previous = uiStack.Peek();
            if (previous.Instance != null && previous.WasVisible)
                previous.Instance.SetActive(true);
        }

        return true;
    }

    public void RemoveFromStack(GameObject instance, bool returnToPool = true)
    {
        if (!nodeLookup.TryGetValue(instance, out var node))
        {
            if (returnToPool) HideUI(instance);
            else CloseUI(instance);
            return;
        }

        if (uiStack.Peek().Instance == instance)
        {
            PopUI(returnToPool);
            return;
        }

        var tempList = new List<StackNode>();
        while (uiStack.Count > 0 && uiStack.Peek().Instance != instance)
            tempList.Add(uiStack.Pop());

        if (uiStack.Count > 0)
        {
            var target = uiStack.Pop();
            nodeLookup.Remove(target.Instance);

            if (returnToPool) HideUI(target.Instance);
            else CloseUI(target.Instance);

            OnUIPopped?.Invoke(target.Instance, target.PrefabName);
        }

        for (int i = tempList.Count - 1; i >= 0; i--)
            uiStack.Push(tempList[i]);
    }

    public void ClearStack(bool returnToPool = true)
    {
        while (uiStack.Count > 0)
        {
            var node = uiStack.Pop();
            nodeLookup.Remove(node.Instance);

            if (returnToPool) HideUI(node.Instance);
            else CloseUI(node.Instance);
        }

        OnStackCleared?.Invoke();
    }

    public GameObject GetCurrentUI() => uiStack.Count > 0 ? uiStack.Peek().Instance : null;
    public string GetCurrentPrefabName() => uiStack.Count > 0 ? uiStack.Peek().PrefabName : null;
    public object GetCurrentUserData() => uiStack.Count > 0 ? uiStack.Peek().UserData : null;
    public bool IsInStack(GameObject instance) => nodeLookup.ContainsKey(instance);
    public int StackDepth => uiStack.Count;
    public bool IsStackEmpty => uiStack.Count == 0;

    public List<(string prefabName, GameObject instance)> GetStackSnapshot()
    {
        var list = new List<(string, GameObject)>();
        foreach (var node in uiStack)
            list.Add((node.PrefabName, node.Instance));
        list.Reverse();
        return list;
    }
}