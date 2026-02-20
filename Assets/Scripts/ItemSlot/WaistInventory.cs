using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WaistInventory : MonoBehaviour
{
    [Header("槽位配置")]
    public List<WaistSlot> slots = new List<WaistSlot>();

    public static WaistInventory Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        CollectSlots();
        ValidateSlots();
    }

    private void CollectSlots()
    {
        if (slots.Count == 0)
        {
            slots = GetComponentsInChildren<WaistSlot>(true).ToList();
        }
    }

    /// <summary>
    /// 根据ID获取槽位
    /// </summary>
    public WaistSlot GetSlotByID(string id)
    {
        return slots.FirstOrDefault(s => s != null && s.slotID == id);
    }

    /// <summary>
    /// 占用槽位
    /// </summary>
    public void OccupySlot(WaistSlot slot, IWaistSlotItem item)
    {
        if (slot == null || item == null) return;

        if (slot.currentItem != null && slot.currentItem != item)
        {
            Debug.LogWarning($"槽位 '{slot.slotID}' 被强制替换");
            slot.currentItem.ReleaseSlot();
        }

        slot.currentItem = item;
    }

    /// <summary>
    /// 释放槽位
    /// </summary>
    public void ReleaseSlot(WaistSlot slot)
    {
        if (slot != null) 
        {
            slot.currentItem = null;
        }
    }

    /// <summary>
    /// 根据物品查找槽位
    /// </summary>
    public WaistSlot GetSlotByItem(IWaistSlotItem item)
    {
        return slots.FirstOrDefault(s => s.currentItem == item);
    }

    private void ValidateSlots()
    {
        var ids = new HashSet<string>();
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            
            if (string.IsNullOrEmpty(slot.slotID))
            {
                Debug.LogError($"[{slot.name}] slotID 未设置！", slot);
                continue;
            }
            
            if (!ids.Add(slot.slotID))
            {
                Debug.LogError($"重复的 slotID: '{slot.slotID}'", slot);
            }
        }
    }
}