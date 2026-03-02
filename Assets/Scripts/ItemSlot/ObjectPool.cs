using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("预制体")]
    public GameObject prefab;
    
    [Header("初始池大小")]
    public int initialSize = 5;
    
    [Header("最大容量（0=无限制）")]
    public int maxSize = 0;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeObjects = new List<GameObject>();
    private int totalCreated = 0;

    void Start()
    {
        // 预生成对象
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject(bool enqueue = true)
    {
        if (maxSize > 0 && totalCreated >= maxSize) return null;
    
        GameObject obj = Instantiate(prefab);
        obj.name = prefab.name + "_Pooled_" + totalCreated;
        obj.SetActive(false);
    
        // 🔥 确保PoolItem存在（使用AddComponent会覆盖检查）
        var poolItem = obj.GetComponent<PoolItem>();
        if (poolItem == null)
        {
            poolItem = obj.AddComponent<PoolItem>();
        }
        poolItem.SetPool(this);
    
        totalCreated++;
    
        if (enqueue) pool.Enqueue(obj);
        return obj;
    }
    public GameObject Get()
    {
        GameObject obj = null;
        
        // 尝试从队列获取
        while (pool.Count > 0 && obj == null)
        {
            obj = pool.Dequeue();
        }
        
        // 池空了，创建新的
        if (obj == null)
        {
            obj = CreateNewObject(false);
        }
        
        if (obj != null)
        {
            obj.SetActive(true);
            activeObjects.Add(obj);
            
            // 重置状态
            var poolItem = obj.GetComponent<PoolItem>();
            poolItem?.OnGetFromPool();
        }
        
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        activeObjects.Remove(obj);
        pool.Enqueue(obj);
        
        // 重置状态
        var poolItem = obj.GetComponent<PoolItem>();
        poolItem?.OnReturnToPool();
    }

    public void ReturnAll()
    {
        // 复制列表避免修改时遍历错误
        var toReturn = new List<GameObject>(activeObjects);
        foreach (var obj in toReturn)
        {
            Return(obj);
        }
    }
}

// 标记组件，用于识别池对象
public class PoolItem : MonoBehaviour
{
    private ObjectPool pool;
    
    public void SetPool(ObjectPool p) => pool = p;
    
    public void ReturnToPool()
    {
        pool?.Return(gameObject);
    }
    
    public virtual void OnGetFromPool() { }
    public virtual void OnReturnToPool() { }
}