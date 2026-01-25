using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas 自动查找规则")]
    [SerializeField] string canvasTag  = "UICanvas";
    [SerializeField] string canvasName = "CanvasUI";

    private Transform canvasRoot;
    private readonly Dictionary<string, GameObject> prefabDict = new(32);
    private readonly Dictionary<string, Stack<GameObject>> poolDict = new(32);
    private readonly HashSet<GameObject> showSet = new(32);   // 记录实例

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
}