using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WaistSlotVisual : MonoBehaviour, IWaistSlotItem
{
    [Header("克隆体预制（必须有 XRGrabInteractable）")]
    public GameObject clonePrefab;

    [Header("专属槽位")]
    [SerializeField] private string homeSlotID;

    string IWaistSlotItem.HomeSlotID => homeSlotID;
    bool IWaistSlotItem.IsInitialized => isInitialized;

    private bool isInitialized = false;
    private WaistSlot homeSlot;

    void IWaistSlotItem.SnapToSlot(WaistSlot slot)
    {
        if (slot == null || slot.attachPoint == null) return;
        homeSlot = slot;
        
        transform.SetParent(slot.attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        Debug.Log($"[{name}] 已锁定到槽位 {slot.slotID}");
    }

    void IWaistSlotItem.OnGrabFromSlot() { }
    void IWaistSlotItem.ReleaseSlot() { homeSlot = null; }

    void Start()
    {
        if (!string.IsNullOrEmpty(homeSlotID))
        {
            StartCoroutine(InitializeSlot());
        }
    }

    private System.Collections.IEnumerator InitializeSlot()
    {
        while (WaistInventory.Instance == null) yield return null;
        yield return null;
        
        var slot = WaistInventory.Instance.GetSlotByID(homeSlotID);
        if (slot != null)
        {
            ((IWaistSlotItem)this).SnapToSlot(slot);
            WaistInventory.Instance.OccupySlot(slot, this);
            isInitialized = true;
        }
    }

    public void SpawnAndAttach(XRDirectInteractor directInteractor)
    {
        Debug.Log($"========== SpawnAndAttach 开始 ==========");
        Debug.Log($"directInteractor = {directInteractor?.name}");
        Debug.Log($"homeSlot = {homeSlot}");
        Debug.Log($"clonePrefab = {clonePrefab}");
        
        if (homeSlot == null)
        {
            Debug.LogError("失败：不在槽位中！");
            return;
        }
        
        if (clonePrefab == null)
        {
            Debug.LogError("失败：clonePrefab 未设置！");
            return;
        }

        // 获取生成位置
        Transform attachPoint = directInteractor.attachTransform;
        Debug.Log($"attachPoint = {attachPoint?.name}, 位置 = {attachPoint?.position}");
        
        if (attachPoint == null)
        {
            Debug.LogError("失败：interactor 没有 attachTransform！");
            return;
        }
        
        // 生成克隆体
        Vector3 spawnPos = attachPoint.position;
        Quaternion spawnRot = attachPoint.rotation;
        
        Debug.Log($"准备生成: {clonePrefab.name} 在 {spawnPos}");
        
        GameObject clone = Instantiate(clonePrefab, spawnPos, spawnRot);
        clone.name = clonePrefab.name + "(Clone)";
        
        Debug.Log($"克隆体已生成: {clone.name}, 位置 = {clone.transform.position}");

        // 让手抓克隆体
        if (clone.TryGetComponent<XRGrabInteractable>(out var cloneGrab))
        {
            Debug.Log($"找到 XRGrabInteractable: {cloneGrab.name}");
            Debug.Log($"调用 SelectEnter: interactor={directInteractor.name}, interactable={cloneGrab.name}");
            
            directInteractor.interactionManager.SelectEnter(directInteractor, cloneGrab);
            
            Debug.Log("SelectEnter 调用完成");
        }
        else
        {
            Debug.LogError($"失败：克隆体 {clone.name} 缺少 XRGrabInteractable！");
            Debug.Log($"克隆体组件: {string.Join(", ", clone.GetComponents<Component>().Select(c => c.GetType().Name))}");
        }
        
        Debug.Log($"========== SpawnAndAttach 结束 ==========");
    }

    void OnDestroy()
    {
        if (homeSlot != null)
            WaistInventory.Instance?.ReleaseSlot(homeSlot);
    }
}