using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistSlotItem : MonoBehaviour, IWaistSlotItem, IXRHoverFilter, IXRSelectFilter
{
    [Header("回家设置")] 
    public float delayTime = 1f;
    public bool useInstantSnap = true;

    [Header("碰撞设置")]
    [Tooltip("插槽内物品互相不碰撞")]
    public bool ignoreCollisionInSlot = true;
    [Tooltip("拿取时与槽内其他物品不碰撞")]
    public bool ignoreCollisionWithSlotItemsWhenGrabbed = true;

    [Header("专属槽位")] 
    [SerializeField] private string homeSlotID;

    [Header("Socket交互设置")]
    [Tooltip("只有被Grab抓取时才允许与Socket交互，在插槽内时禁止")]
    public bool restrictSocketInteractionToGrabbedOnly = true;
    [Tooltip("松开后多长时间内仍允许Socket吸附（秒）"), Range(0f, 2f)]
    public float socketGracePeriod = 0.5f;

    string IWaistSlotItem.HomeSlotID => homeSlotID;
    bool IWaistSlotItem.IsInitialized => isInitialized;

    protected bool isInitialized = false;
    protected XRGrabInteractable grab;
    protected Rigidbody rb;
    public bool isReturning { get; protected set; } = false;
    public bool isInSlot { get; protected set; } = false;

    protected Vector3 originalScale;
    protected Transform originalParent;
    protected WaistSlot homeSlot;
    protected Collider[] myColliders;

    // 静态列表：跟踪所有腰带物品
    protected static List<WaistSlotItem> allWaistItems = new List<WaistSlotItem>();
    // 当前忽略的碰撞器
    protected Dictionary<Collider, int> ignoredColliders = new Dictionary<Collider, int>();

    // 🔥 新增：Socket 宽容期计时
    protected float lastReleaseTime = -999f;
    protected bool isInGracePeriod => Time.time - lastReleaseTime < socketGracePeriod;

    // Filter 接口实现
    public bool canProcess => true;

    protected virtual void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        originalScale = transform.localScale;

        // 获取所有实体碰撞器
        myColliders = GetComponentsInChildren<Collider>(true)
            .Where(c => !c.isTrigger)
            .ToArray();

        if (grab.attachTransform == null)
            grab.attachTransform = transform;

        // 注册到全局列表
        if (!allWaistItems.Contains(this))
            allWaistItems.Add(this);

        // 注册 Filter 到 GrabInteractable
        if (restrictSocketInteractionToGrabbedOnly)
        {
            grab.hoverFilters.Add(this);
            grab.selectFilters.Add(this);
        }
    }

    protected virtual void OnDestroy()
    {
        // 清理时恢复所有碰撞
        RestoreAllCollisions();
        allWaistItems.Remove(this);
        
        // 移除 Filter
        if (grab != null)
        {
            grab.hoverFilters.Remove(this);
            grab.selectFilters.Remove(this);
        }
    }

    protected virtual void Start()
    {
        if (rb == null)
        {
            Debug.LogError($"[{name}] 缺少 Rigidbody！");
            enabled = false;
            return;
        }

        grab.trackPosition = true;
        grab.trackRotation = true;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        if (!string.IsNullOrEmpty(homeSlotID))
            StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        while (WaistInventory.Instance == null)
            yield return null;
        yield return null;

        var slot = WaistInventory.Instance.GetSlotByID(homeSlotID);
        if (slot == null)
        {
            Debug.LogError($"[{name}] 找不到槽位: {homeSlotID}");
            yield break;
        }

        float dist = Vector3.Distance(transform.position, slot.attachPoint.position);
        if (dist <= slot.snapRadius)
        {
            ((IWaistSlotItem)this).SnapToSlot(slot);
            WaistInventory.Instance.OccupySlot(slot, this);
        }

        isInitialized = true;
    }

    #region 🔥 Filter 接口实现 - 控制 Socket 交互权限

    /// <summary>
    /// 控制是否允许 Hover（悬停）
    /// 返回 true = 允许交互，false = 禁止交互
    /// </summary>
    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return CanInteractWithSocket(interactor);
    }

    /// <summary>
    /// 控制是否允许 Select（选择/抓取）
    /// 返回 true = 允许交互，false = 禁止交互
    /// </summary>
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return CanInteractWithSocket(interactor);
    }

    /// <summary>
    /// 🔥 核心逻辑：只有被 Grab 拿取时，或松开后宽容期内，才允许与 Socket 交互
    /// </summary>
    protected virtual bool CanInteractWithSocket(IXRInteractor interactor)
    {
        if (!restrictSocketInteractionToGrabbedOnly)
            return true;

        // 如果不是 Socket Interactor，允许交互（比如 Direct Interactor 手抓）
        if (!(interactor is XRSocketInteractor))
            return true;

        // 🔥 关键逻辑：
        // 1. 被手抓取时 -> 允许
        // 2. 松开后宽容期内 -> 允许（给 Socket 时间捕获）
        // 3. 在插槽内静止时 -> 禁止
        
        bool isGrabbedByHand = grab.isSelected && IsGrabbedByHandInteractor();
        bool inGracePeriod = isInGracePeriod && !isInSlot; // 宽容期且不在插槽内
        
        // 调试日志
        // Debug.Log($"[{name}] Socket检查: isGrabbed={isGrabbedByHand}, gracePeriod={inGracePeriod}({Time.time - lastReleaseTime:F2}s), isInSlot={isInSlot}");
        
        return isGrabbedByHand || inGracePeriod;
    }

    /// <summary>
    /// 检查是否被手（Direct/Ray Interactor）抓取，而非 Socket
    /// </summary>
    protected virtual bool IsGrabbedByHandInteractor()
    {
        foreach (var selectingInteractor in grab.interactorsSelecting)
        {
            // 如果是 Direct Interactor 或 Ray Interactor，认为是手抓
            if (selectingInteractor is XRDirectInteractor || selectingInteractor is XRRayInteractor)
                return true;
        }
        return false;
    }

    #endregion

    #region 碰撞管理核心

    /// <summary>
    /// 🔥 进入插槽：与所有其他腰带物品忽略碰撞
    /// </summary>
    protected virtual void EnterSlotCollisionMode()
    {
        if (!ignoreCollisionInSlot) return;
        
        isInSlot = true;

        foreach (var otherItem in allWaistItems)
        {
            if (otherItem == this || otherItem == null) continue;
            
            // 与其他物品的所有碰撞器设置忽略
            IgnoreCollisionWith(otherItem, true);
        }

        Debug.Log($"[{name}] 进入插槽模式，忽略 {ignoredColliders.Count} 个碰撞器");
    }

    /// <summary>
    /// 🔥 离开插槽：恢复与所有物品的碰撞
    /// </summary>
    protected virtual void ExitSlotCollisionMode()
    {
        isInSlot = false;

        if (ignoreCollisionWithSlotItemsWhenGrabbed && grab.isSelected)
        {
            // 如果被拿取，保持与槽内物品的忽略碰撞
            UpdateGrabbedCollisionMode();
        }
        else
        {
            // 完全恢复所有碰撞
            RestoreAllCollisions();
        }
    }

    /// <summary>
    /// 🔥 被拿取时：与槽内物品保持忽略，与场景恢复碰撞
    /// </summary>
    protected virtual void UpdateGrabbedCollisionMode()
    {
        if (!ignoreCollisionWithSlotItemsWhenGrabbed) return;

        // 先恢复所有
        RestoreAllCollisions();

        // 再重新忽略与槽内物品的碰撞
        foreach (var otherItem in allWaistItems)
        {
            if (otherItem == this || otherItem == null) continue;
            
            // 只忽略仍在槽内的物品
            if (otherItem.isInSlot)
            {
                IgnoreCollisionWith(otherItem, true);
            }
        }

        Debug.Log($"[{name}] 拿取模式，忽略 {ignoredColliders.Count} 个槽内物品碰撞器");
    }

    /// <summary>
    /// 🔥 恢复所有碰撞关系
    /// </summary>
    protected virtual void RestoreAllCollisions()
    {
        foreach (var kvp in ignoredColliders)
        {
            Collider otherCol = kvp.Key;
            if (otherCol == null) continue;

            foreach (var myCol in myColliders)
            {
                if (myCol == null) continue;
                Physics.IgnoreCollision(myCol, otherCol, false);
            }
        }

        ignoredColliders.Clear();
    }

    /// <summary>
    /// 🔥 与另一个物品设置/取消忽略碰撞
    /// </summary>
    protected virtual void IgnoreCollisionWith(WaistSlotItem other, bool ignore)
    {
        if (other?.myColliders == null) return;

        foreach (var myCol in myColliders)
        {
            if (myCol == null) continue;

            foreach (var otherCol in other.myColliders)
            {
                if (otherCol == null) continue;

                Physics.IgnoreCollision(myCol, otherCol, ignore);

                if (ignore)
                {
                    if (!ignoredColliders.ContainsKey(otherCol))
                        ignoredColliders[otherCol] = 0;
                    ignoredColliders[otherCol]++;
                }
            }
        }
    }

    #endregion

    #region 接口实现

    void IWaistSlotItem.SnapToSlot(WaistSlot slot)
    {
        if (slot == null || slot.attachPoint == null) return;

        homeSlot = slot;

        transform.SetParent(slot.attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        // 冻结物理
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 🔥 进入插槽碰撞模式
        EnterSlotCollisionMode();
    }

    void IWaistSlotItem.OnGrabFromSlot()
    {
        if (homeSlot != null && homeSlot.currentItem == this)
        {
            WaistInventory.Instance?.ReleaseSlot(homeSlot);
        }

        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;

        // 启用物理
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
        }

        // 🔥 离开插槽，但更新为拿取模式
        ExitSlotCollisionMode();
    }

    void IWaistSlotItem.ReleaseSlot()
    {
        homeSlot = null;
    }

    #endregion

    #region 抓取事件

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        if (!isInitialized) return;

        // 中断回槽
        if (isReturning)
        {
            StopAllCoroutines();
            isReturning = false;
        }

        // 从槽中取出
        if (homeSlot != null && homeSlot.currentItem == this)
        {
            ((IWaistSlotItem)this).OnGrabFromSlot();
        }
        else if (isInSlot)
        {
            // 如果已经在槽内模式，更新为拿取模式
            ExitSlotCollisionMode();
        }
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        if (!isInitialized) return;

        // 🔥 记录松开时间，启动宽容期
        lastReleaseTime = Time.time;

        StopAllCoroutines();

        // 松手时：如果之前在拿取模式，恢复所有碰撞
        if (!isInSlot && ignoredColliders.Count > 0)
        {
            RestoreAllCollisions();
        }

        StartCoroutine(DelayedCheckAndDecide());
    }

    #endregion

    #region 回槽逻辑

    private IEnumerator DelayedCheckAndDecide()
    {
        yield return null;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
        }

        yield return new WaitForSeconds(delayTime);
        yield return null;

        // 检查是否仍被抓住（包括 Socket）
        if (grab.interactorsSelecting.Count > 0) yield break;

        // 检查是否被其他 Socket 吸附
        if (IsInOtherSocket()) yield break;

        // 执行回家
        if (homeSlot != null)
        {
            StartCoroutine(AutoSnapBack());
        }
    }

    private bool IsInOtherSocket()
    {
        foreach (var interactor in grab.interactorsSelecting)
        {
            if (interactor is XRSocketInteractor socket && 
                socket.GetComponent<WaistSlot>() != homeSlot)
                return true;
        }

        // 检查是否有 Socket 在 hover
        var allSockets = FindObjectsOfType<XRSocketInteractor>();
        foreach (var socket in allSockets)
        {
            if (socket.GetComponent<WaistSlot>() == homeSlot) continue;
            
            if (socket.interactablesHovered.Contains(grab))
            {
                // 给 0.3 秒吸附时间
                StartCoroutine(WaitForSocketAttach(socket));
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitForSocketAttach(XRSocketInteractor socket)
    {
        yield return new WaitForSeconds(0.3f);
        // 如果最终没被吸附，让 DelayedCheckAndDecide 继续执行
        if (!grab.interactorsSelecting.Any(i => i is XRSocketInteractor))
        {
            StartCoroutine(DelayedCheckAndDecide());
        }
    }

    private IEnumerator AutoSnapBack()
    {
        if (homeSlot == null) yield break;

        isReturning = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 恢复所有碰撞（准备进槽重新设置）
        RestoreAllCollisions();

        // 瞬移
        transform.SetParent(null, true);
        transform.position = homeSlot.attachPoint.position;
        transform.rotation = homeSlot.attachPoint.rotation;
        
        transform.SetParent(homeSlot.attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        WaistInventory.Instance?.OccupySlot(homeSlot, this);

        // 🔥 重新进入插槽碰撞模式
        EnterSlotCollisionMode();

        isReturning = false;
    }

    #endregion

    #region 工具方法

    protected virtual void OnDrawGizmosSelected()
    {
        // 可视化当前忽略的碰撞
        if (ignoredColliders.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (var col in ignoredColliders.Keys)
            {
                if (col != null)
                    Gizmos.DrawLine(transform.position, col.transform.position);
            }
        }
    }

    #endregion
}