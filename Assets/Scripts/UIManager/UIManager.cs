using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas 自动查找规则")] [SerializeField]
    string canvasTag = "UICanvas";

    [SerializeField] string canvasName = "CanvasUI";

    private Transform canvasRoot;
    private readonly Dictionary<string, GameObject> prefabDict = new(32);
    private readonly Dictionary<string, Stack<GameObject>> poolDict = new(32);
    private readonly HashSet<GameObject> showSet = new(32); // 记录实例

    // ==================== 新增：UI 堆栈功能（完全不干扰原有代码）====================

    [Header("UI 堆栈设置")] [SerializeField] bool enableStack = true; // 是否启用堆栈
    [SerializeField] bool autoHidePrevious = true; // 打开新UI时自动隐藏前一个
    [SerializeField] bool restorePreviousOnClose = true; // 关闭时恢复前一个

    // 堆栈节点：记录 UI 实例和它的显隐状态
    private class StackNode
    {
        public GameObject Instance; // UI 实例
        public string PrefabName; // 预制体名字
        public bool WasVisible; // 压栈前是否可见
        public object UserData; // 可选数据
    }

    private readonly Stack<StackNode> uiStack = new(); // UI 堆栈
    private readonly Dictionary<GameObject, StackNode> nodeLookup = new(); // 实例->节点快速查找

    // 事件：供外部监听
    public event Action<GameObject, string> OnUIPushed; // 压栈事件 (实例, 预制名)
    public event Action<GameObject, string> OnUIPopped; // 出栈事件 (实例, 预制名)
    public event Action OnStackCleared; // 清空事件

    // ================================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
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

    /// <summary>
    /// 显示任意预制体：名字 = 完整文件名（不含扩展名）
    /// </summary>
    /// <param name="prefabName">Resources/UI/下的文件名</param>
    /// <returns>实例 GameObject，自己存引用</returns>
    public GameObject ShowUI(string prefabName)
    {
        if (canvasRoot == null) ScanCanvas();
        if (canvasRoot == null) return null;

        /* 1. 第一次加载原型 */
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

        /* 2. 按名字分池 */
        if (!poolDict.TryGetValue(prefabName, out var pool))
            pool = poolDict[prefabName] = new Stack<GameObject>();

        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Pop();
        }
        else
        {
            go = Instantiate(prefab, canvasRoot);
        }

        /* 3. 激活 + 登记 */
        go.SetActive(true);
        showSet.Add(go);
        return go;
    }

    /// <summary>
    /// 按实例引用关闭（回池）
    /// </summary>
    public void HideUI(GameObject instance)
    {
        if (!showSet.Contains(instance)) return;
        instance.SetActive(false);
        showSet.Remove(instance);

        string key = instance.name.Replace("(Clone)", ""); // 去掉克隆后缀
        if (poolDict.TryGetValue(key, out var pool))
            pool.Push(instance);
    }

    /// <summary>
    /// 直接销毁（不保留在池）
    /// </summary>
    public void CloseUI(GameObject instance)
    {
        if (!showSet.Contains(instance)) return;
        showSet.Remove(instance);
        Destroy(instance);
    }

    // ==================== 新增：UI 堆栈 API（与原有代码零耦合）====================

    #region UI Stack API

    /// <summary>
    /// 压栈显示 UI（自动处理前一个 UI 的显隐）
    /// </summary>
    /// <param name="prefabName">预制体名字</param>
    /// <param name="userData">可选自定义数据</param>
    /// <returns>UI 实例</returns>
    public GameObject PushUI(string prefabName, object userData = null)
    {
        if (!enableStack) return ShowUI(prefabName); // 堆栈禁用时退化为普通显示

        // 处理当前栈顶：如果需要自动隐藏
        if (autoHidePrevious && uiStack.Count > 0)
        {
            var currentTop = uiStack.Peek();
            if (currentTop.Instance != null)
            {
                currentTop.WasVisible = currentTop.Instance.activeSelf;
                currentTop.Instance.SetActive(false);
            }
        }

        // 创建新 UI（复用原有 ShowUI 逻辑）
        GameObject instance = ShowUI(prefabName);
        if (instance == null) return null;

        // 创建堆栈节点
        var node = new StackNode
        {
            Instance = instance,
            PrefabName = prefabName,
            WasVisible = true, // 新 UI 默认是显示的
            UserData = userData
        };

        uiStack.Push(node);
        nodeLookup[instance] = node;

        OnUIPushed?.Invoke(instance, prefabName);
        return instance;
    }

    /// <summary>
    /// 出栈关闭当前 UI（自动恢复前一个）
    /// </summary>
    /// <param name="returnToPool">是否回收到对象池</param>
    /// <returns>是否成功出栈</returns>
    public bool PopUI(bool returnToPool = true)
    {
        if (uiStack.Count == 0) return false;

        var node = uiStack.Pop();
        nodeLookup.Remove(node.Instance);

        // 关闭当前 UI
        if (returnToPool)
            HideUI(node.Instance); // 复用原有 HideUI（回池）
        else
            CloseUI(node.Instance); // 复用原有 CloseUI（销毁）

        OnUIPopped?.Invoke(node.Instance, node.PrefabName);

        // 恢复前一个 UI
        if (restorePreviousOnClose && uiStack.Count > 0)
        {
            var previous = uiStack.Peek();
            if (previous.Instance != null && previous.WasVisible)
            {
                previous.Instance.SetActive(true);
            }
        }

        return true;
    }

    /// <summary>
    /// 关闭指定 UI 并从堆栈移除（如果它在栈中）
    /// </summary>
    public void RemoveFromStack(GameObject instance, bool returnToPool = true)
    {
        if (!nodeLookup.TryGetValue(instance, out var node))
        {
            // 不在堆栈中，按普通方式关闭
            if (returnToPool) HideUI(instance);
            else CloseUI(instance);
            return;
        }

        // 如果是栈顶，直接 Pop
        if (uiStack.Peek().Instance == instance)
        {
            PopUI(returnToPool);
            return;
        }

        // 如果在栈中间，需要重建堆栈（较少见）
        var tempList = new List<StackNode>();
        while (uiStack.Count > 0 && uiStack.Peek().Instance != instance)
        {
            tempList.Add(uiStack.Pop());
        }

        if (uiStack.Count > 0) // 找到了
        {
            var target = uiStack.Pop(); // 移除目标
            nodeLookup.Remove(target.Instance);

            if (returnToPool) HideUI(target.Instance);
            else CloseUI(target.Instance);

            OnUIPopped?.Invoke(target.Instance, target.PrefabName);
        }

        // 重建堆栈
        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            uiStack.Push(tempList[i]);
        }
    }

    /// <summary>
    /// 清空整个堆栈（常用于返回主菜单）
    /// </summary>
    /// <param name="returnToPool">是否回收到对象池</param>
    public void ClearStack(bool returnToPool = true)
    {
        while (uiStack.Count > 0)
        {
            var node = uiStack.Pop();
            nodeLookup.Remove(node.Instance);

            if (returnToPool)
                HideUI(node.Instance);
            else
                CloseUI(node.Instance);
        }

        OnStackCleared?.Invoke();
    }

    /// <summary>
    /// 获取当前栈顶 UI
    /// </summary>
    public GameObject GetCurrentUI()
    {
        return uiStack.Count > 0 ? uiStack.Peek().Instance : null;
    }

    /// <summary>
    /// 获取栈顶预制体名字
    /// </summary>
    public string GetCurrentPrefabName()
    {
        return uiStack.Count > 0 ? uiStack.Peek().PrefabName : null;
    }

    /// <summary>
    /// 获取栈顶用户数据
    /// </summary>
    public object GetCurrentUserData()
    {
        return uiStack.Count > 0 ? uiStack.Peek().UserData : null;
    }

    /// <summary>
    /// 查询指定实例是否在堆栈中
    /// </summary>
    public bool IsInStack(GameObject instance)
    {
        return nodeLookup.ContainsKey(instance);
    }

    /// <summary>
    /// 获取堆栈深度
    /// </summary>
    public int StackDepth => uiStack.Count;

    /// <summary>
    /// 堆栈是否为空
    /// </summary>
    public bool IsStackEmpty => uiStack.Count == 0;

    /// <summary>
    /// 获取堆栈快照（用于调试）
    /// </summary>
    public List<(string prefabName, GameObject instance)> GetStackSnapshot()
    {
        var list = new List<(string, GameObject)>();
        foreach (var node in uiStack)
        {
            list.Add((node.PrefabName, node.Instance));
        }

        list.Reverse(); // 让栈顶在最后，便于阅读
        return list;
    }

    #endregion
}