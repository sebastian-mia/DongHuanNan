using UnityEngine;
using System.Linq;

public class WaistSlot : MonoBehaviour
{
    [Header("槽位配置")] public string slotID;
    public float snapRadius = 0.15f;
    public Transform attachPoint;

    [Header("可选：初始物品")] public MonoBehaviour initialItem;

    [HideInInspector] public IWaistSlotItem currentItem;
    public bool isOccupied => currentItem != null;

    void Awake()
    {
        if (attachPoint == null)
            attachPoint = transform;
    }

    void Start()
    {
        if (initialItem != null)
        {
            // 🔥 尝试强制转换为接口
            IWaistSlotItem itemInterface = null;

            try
            {
                // 方法1：直接转换
                itemInterface = (IWaistSlotItem)initialItem;
            }
            catch (System.InvalidCastException)
            {
                // 方法2：尝试从组件获取
                itemInterface = initialItem.GetComponent<IWaistSlotItem>();
            }

            if (itemInterface != null)
            {
                StartCoroutine(DelayedInit(itemInterface));
            }
            else
            {
                var components = initialItem.GetComponents<MonoBehaviour>();
                Debug.LogError(
                    $"[{initialItem.name}] 没有实现 IWaistSlotItem！\n" +
                    $"组件: {string.Join(", ", components.Select(c => c.GetType().Name))}\n" +
                    $"InfiniteWaistItem 类型: {typeof(WaistItemClone).FullName}\n" +
                    $"接口类型: {typeof(IWaistSlotItem).FullName}",
                    initialItem
                );
            }
        }
    }

    private System.Collections.IEnumerator DelayedInit(IWaistSlotItem item)
    {
        int timeout = 0;
        while (WaistInventory.Instance == null && timeout < 100)
        {
            yield return null;
            timeout++;
        }

        yield return null;

        if (item.HomeSlotID != slotID)
        {
            Debug.LogWarning($"[{slotID}] HomeSlotID ({item.HomeSlotID}) 不匹配", initialItem);
            yield break;
        }

        item.SnapToSlot(this);
        currentItem = item;

        Debug.Log($"[{slotID}] 初始物品已绑定: {initialItem.name}");
    }

    void OnDrawGizmosSelected()
    {
        if (attachPoint == null) return;

        Gizmos.color = isOccupied ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(attachPoint.position, snapRadius);

#if UNITY_EDITOR
        string label = slotID;
        if (isOccupied) label += $"\n{initialItem?.name}";
        UnityEditor.Handles.Label(attachPoint.position + Vector3.up * 0.05f, label);
#endif
    }
}