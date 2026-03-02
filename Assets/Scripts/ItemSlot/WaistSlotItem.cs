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
    [Tooltip("插槽内物品互相不碰撞，且外部物品与插槽内物品也不碰撞")]
    public bool ignoreCollisionWithSlotItems = true;

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
    
    // 🔥 关键：isInSlot 表示"此物品在插槽内"
    public bool isInSlot { get; protected set; } = false;

    protected Vector3 originalScale;
    protected Transform originalParent;
    protected WaistSlot homeSlot;
    protected Collider[] myColliders;

    // 静态列表：跟踪所有腰带物品
    protected static List<WaistSlotItem> allWaistItems = new List<WaistSlotItem>();
    
    // 🔥 修改：不再用 ignoredColliders 计数，而是基于对方状态的动态判断
    // 但我们需要跟踪当前忽略的碰撞器以便清理
    protected HashSet<Collider> ignoredColliders = new HashSet<Collider>();

    // Socket 宽容期计时
    protected float lastReleaseTime = -999f;
    protected bool isInGracePeriod => Time.time - lastReleaseTime < socketGracePeriod;

    // 事件驱动：回家协程控制
    protected Coroutine pendingReturnCoroutine = null;
    protected bool isPendingReturn => pendingReturnCoroutine != null;

    // Filter 接口实现
    public bool canProcess => true;

    protected virtual void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        originalScale = transform.localScale;

        myColliders = GetComponentsInChildren<Collider>(true)
            .Where(c => !c.isTrigger)
            .ToArray();

        if (grab.attachTransform == null)
            grab.attachTransform = transform;

        if (!allWaistItems.Contains(this))
            allWaistItems.Add(this);

        if (restrictSocketInteractionToGrabbedOnly)
        {
            grab.hoverFilters.Add(this);
            grab.selectFilters.Add(this);
        }

        // 🔥 订阅全局插槽状态变化事件
        SubscribeToSlotStateChanges();
    }

    protected virtual void OnDestroy()
    {
        // 🔥 清理时恢复所有碰撞
        RestoreAllCollisions();
        allWaistItems.Remove(this);
        
        if (grab != null)
        {
            grab.hoverFilters.Remove(this);
            grab.selectFilters.Remove(this);
        }

        UnsubscribeFromSocketEvents();
        UnsubscribeFromSlotStateChanges();
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

        SubscribeToSocketEvents();

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

    #region 🔥 核心：全局插槽状态管理

    // 静态事件：任何物品的插槽状态变化时触发
    public static event System.Action<WaistSlotItem, bool> OnAnySlotStateChanged;

    /// <summary>
    /// 订阅插槽状态变化
    /// </summary>
    protected virtual void SubscribeToSlotStateChanges()
    {
        OnAnySlotStateChanged += OnOtherItemSlotStateChanged;
    }

    protected virtual void UnsubscribeFromSlotStateChanges()
    {
        OnAnySlotStateChanged -= OnOtherItemSlotStateChanged;
    }

    /// <summary>
    /// 🔥 当其他物品的插槽状态变化时，更新碰撞关系
    /// </summary>
    protected virtual void OnOtherItemSlotStateChanged(WaistSlotItem item, bool enteredSlot)
    {
        if (item == this) return; // 忽略自己
        
        // 如果对方进入插槽，我忽略它
        // 如果对方离开插槽，我恢复与它的碰撞（如果我也在外面）
        UpdateCollisionWithItem(item);
    }

    /// <summary>
    /// 🔥 更新与特定物品的碰撞关系（基于双方状态）
    /// </summary>
    protected virtual void UpdateCollisionWithItem(WaistSlotItem other)
    {
        if (!ignoreCollisionWithSlotItems) return;
        if (other?.myColliders == null) return;

        // 规则：只要有一方在插槽内，双方就不碰撞
        bool shouldIgnore = this.isInSlot || other.isInSlot;

        foreach (var myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (var otherCol in other.myColliders)
            {
                if (otherCol == null) continue;
                
                Physics.IgnoreCollision(myCol, otherCol, shouldIgnore);
                
                if (shouldIgnore)
                    ignoredColliders.Add(otherCol);
                else
                    ignoredColliders.Remove(otherCol);
            }
        }
    }

    /// <summary>
    /// 🔥 更新与所有物品的碰撞关系
    /// </summary>
    protected virtual void UpdateAllCollisionRelationships()
    {
        if (!ignoreCollisionWithSlotItems) return;

        foreach (var otherItem in allWaistItems)
        {
            if (otherItem == this || otherItem == null) continue;
            UpdateCollisionWithItem(otherItem);
        }
    }

    /// <summary>
    /// 🔥 恢复所有碰撞（清理时用）
    /// </summary>
    protected virtual void RestoreAllCollisions()
    {
        foreach (var otherItem in allWaistItems)
        {
            if (otherItem?.myColliders == null) continue;
            
            foreach (var myCol in myColliders)
            {
                if (myCol == null) continue;
                foreach (var otherCol in otherItem.myColliders)
                {
                    if (otherCol == null) continue;
                    Physics.IgnoreCollision(myCol, otherCol, false);
                }
            }
        }
        ignoredColliders.Clear();
    }

    #endregion

    #region 事件驱动核心 - Socket 事件监听

    protected virtual void SubscribeToSocketEvents()
    {
        grab.selectEntered.AddListener(OnAnySelectEntered);
        grab.hoverEntered.AddListener(OnAnyHoverEntered);
    }

    protected virtual void UnsubscribeFromSocketEvents()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnAnySelectEntered);
            grab.hoverEntered.RemoveListener(OnAnyHoverEntered);
        }
    }

    protected virtual void OnAnySelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket)
        {
            OnSocketAttached(socket);
        }
    }

    protected virtual void OnAnyHoverEntered(HoverEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket && isPendingReturn)
        {
            OnSocketHoverStarted(socket);
        }
    }

    protected virtual void OnSocketAttached(XRSocketInteractor socket)
    {
        bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
        
        if (!isHomeSocket)
        {
            CancelPendingReturn($"被场景Socket [{socket.name}] 吸附");
            
            if (homeSlot != null && homeSlot.currentItem == this)
            {
                WaistInventory.Instance?.ReleaseSlot(homeSlot);
                homeSlot = null;
            }
        }
        else
        {
            Debug.Log($"[{name}] 成功回到腰带插槽 [{homeSlotID}]");
        }
    }

    protected virtual void OnSocketHoverStarted(XRSocketInteractor socket)
    {
        bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
        
        if (!isHomeSocket)
        {
            PausePendingReturn($"进入场景Socket [{socket.name}] 范围");
        }
    }

    protected virtual bool IsHoveredByAnySceneSocket()
    {
        foreach (var interactor in grab.interactorsHovering)
        {
            if (interactor is XRSocketInteractor socket)
            {
                bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
                if (!isHomeSocket)
                    return true;
            }
        }
        return false;
    }

    #endregion

    #region 回家协程管理

    protected virtual void StartPendingReturn()
    {
        CancelPendingReturn("启动新的回家计时");
        pendingReturnCoroutine = StartCoroutine(PendingReturnCoroutine());
        Debug.Log($"[{name}] 启动回家计时 ({delayTime}秒)");
    }

    protected virtual void CancelPendingReturn(string reason)
    {
        if (pendingReturnCoroutine != null)
        {
            StopCoroutine(pendingReturnCoroutine);
            pendingReturnCoroutine = null;
            Debug.Log($"[{name}] 取消回家: {reason}");
        }
        isReturning = false;
    }

    protected virtual void PausePendingReturn(string reason)
    {
        if (pendingReturnCoroutine != null)
        {
            StopCoroutine(pendingReturnCoroutine);
            pendingReturnCoroutine = null;
            Debug.Log($"[{name}] 暂停回家: {reason}");
        }
    }

    protected virtual void ResumePendingReturn(string reason)
    {
        if (!isPendingReturn && !grab.isSelected && homeSlot != null && !isInSlot)
        {
            pendingReturnCoroutine = StartCoroutine(PendingReturnCoroutine());
            Debug.Log($"[{name}] 恢复回家: {reason}");
        }
    }

    protected virtual IEnumerator PendingReturnCoroutine()
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

        if (CanReturnHome())
        {
            pendingReturnCoroutine = null;
            StartCoroutine(AutoSnapBack());
        }
        else
        {
            pendingReturnCoroutine = null;
            Debug.Log($"[{name}] 回家条件不满足，取消回家");
        }
    }

    protected virtual bool CanReturnHome()
    {
        if (!isInitialized) return false;
        if (grab.isSelected) return false;
        if (homeSlot == null) return false;
        if (isInSlot) return false;
        if (IsAttachedToSceneSocket()) return false;
        if (IsHoveredByAnySceneSocket()) return false;
        
        return true;
    }

    protected virtual bool IsAttachedToSceneSocket()
    {
        foreach (var interactor in grab.interactorsSelecting)
        {
            if (interactor is XRSocketInteractor socket)
            {
                bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
                if (!isHomeSocket)
                    return true;
            }
        }
        return false;
    }

    #endregion

    #region Filter 接口实现

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        if (interactable is XRBaseInteractable baseInteractable)
        {
            if (baseInteractable.isHovered && IsInteractorHovering(baseInteractable, interactor))
            {
                return true;
            }
        }
        
        return CanInteractWithSocket(interactor);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactable is XRBaseInteractable baseInteractable)
        {
            if (baseInteractable.isSelected && IsInteractorSelecting(baseInteractable, interactor))
            {
                return true;
            }
        }
        
        return CanInteractWithSocket(interactor);
    }

    private bool IsInteractorSelecting(XRBaseInteractable interactable, IXRInteractor interactor)
    {
        foreach (var selectingInteractor in interactable.interactorsSelecting)
        {
            if (selectingInteractor == interactor)
                return true;
        }
        return false;
    }

    private bool IsInteractorHovering(XRBaseInteractable interactable, IXRInteractor interactor)
    {
        foreach (var hoveringInteractor in interactable.interactorsHovering)
        {
            if (hoveringInteractor == interactor)
                return true;
        }
        return false;
    }

    protected virtual bool CanInteractWithSocket(IXRInteractor interactor)
    {
        if (!restrictSocketInteractionToGrabbedOnly)
            return true;

        if (!(interactor is XRSocketInteractor))
            return true;

        bool isGrabbedByHand = grab.isSelected && IsGrabbedByHandInteractor();
        bool inGracePeriod = isInGracePeriod && !isInSlot;
        
        return isGrabbedByHand || inGracePeriod;
    }

    protected virtual bool IsGrabbedByHandInteractor()
    {
        foreach (var selectingInteractor in grab.interactorsSelecting)
        {
            if (selectingInteractor is XRDirectInteractor || selectingInteractor is XRRayInteractor)
                return true;
        }
        return false;
    }

    #endregion

    #region 🔥 修改后的接口实现 - 基于全局状态的碰撞管理

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

        // 🔥 进入插槽状态
        SetInSlotState(true);
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

        // 🔥 离开插槽状态（但保持与插槽内物品的碰撞忽略）
        SetInSlotState(false);
    }

    void IWaistSlotItem.ReleaseSlot()
    {
        homeSlot = null;
    }

    /// <summary>
    /// 🔥 核心：设置插槽状态，并通知所有其他物品更新碰撞关系
    /// </summary>
    protected virtual void SetInSlotState(bool inSlot)
    {
        if (isInSlot == inSlot) return; // 无变化
        
        isInSlot = inSlot;
        
        // 🔥 更新与所有物品的碰撞关系
        UpdateAllCollisionRelationships();
        
        // 🔥 触发全局事件，通知其他物品更新与我的碰撞关系
        OnAnySlotStateChanged?.Invoke(this, inSlot);
        
        Debug.Log($"[{name}] 插槽状态: {(inSlot ? "进入" : "离开")}，碰撞关系已更新");
    }

    #endregion

    #region 抓取事件

    protected virtual void OnGrab(SelectEnterEventArgs args)
    {
        if (!isInitialized) return;

        CancelPendingReturn("物品被抓住");

        if (homeSlot != null && homeSlot.currentItem == this)
        {
            ((IWaistSlotItem)this).OnGrabFromSlot();
        }
        else if (isInSlot)
        {
            // 🔥 通过 SetInSlotState 离开插槽，自动处理碰撞关系
            SetInSlotState(false);
        }
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        if (!isInitialized) return;

        lastReleaseTime = Time.time;

        // 🔥 注意：松手时不恢复任何碰撞，保持与插槽内物品的忽略关系
        
        if (homeSlot != null && !isInSlot)
        {
            StartPendingReturn();
        }
    }

    #endregion

    #region 回槽逻辑

    private IEnumerator AutoSnapBack()
    {
        if (homeSlot == null) yield break;

        if (!CanReturnHome())
        {
            Debug.Log($"[{name}] AutoSnapBack 最终检查失败，取消回家");
            yield break;
        }

        isReturning = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 🔥 注意：回家过程中不恢复碰撞，保持与插槽内物品的忽略关系
        
        transform.SetParent(null, true);
        transform.position = homeSlot.attachPoint.position;
        transform.rotation = homeSlot.attachPoint.rotation;
        
        transform.SetParent(homeSlot.attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        WaistInventory.Instance?.OccupySlot(homeSlot, this);

        // 🔥 通过 SetInSlotState 进入插槽，自动处理碰撞关系
        SetInSlotState(true);

        isReturning = false;
        
        Debug.Log($"[{name}] 成功回到腰带插槽 [{homeSlotID}]");
    }

    #endregion

    #region 工具方法

    protected virtual void OnDrawGizmosSelected()
    {
        // 可视化当前忽略的碰撞
        if (ignoredColliders.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (var col in ignoredColliders)
            {
                if (col != null)
                    Gizmos.DrawLine(transform.position, col.transform.position);
            }
        }
    }

    #endregion
}