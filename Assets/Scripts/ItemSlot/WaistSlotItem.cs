using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QFramework;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistSlotItem : MonoBehaviour, IWaistSlotItem, IXRHoverFilter, IXRSelectFilter, IController
{
    [Header("回家设置")] public float delayTime = 1f;
    public bool useInstantSnap = true;

    [Header("碰撞设置")] [Tooltip("腰带物品之间永远不碰撞（通过Tag识别）")]
    public bool ignoreCollisionWithSlotItems = true;

    [Tooltip("腰带物品的标签名称")] public string waistSlotItemTag = "WaistSlotsItem";

    [Header("专属槽位")] [SerializeField] private string homeSlotID;

    [Header("Socket交互设置")] [Tooltip("只有被Grab抓取时才允许与Socket交互，在插槽内时禁止")]
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

    // Socket 宽容期计时
    protected float lastReleaseTime = -999f;
    protected bool isInGracePeriod => Time.time - lastReleaseTime < socketGracePeriod;

    // 协程控制
    protected Coroutine pendingReturnCoroutine = null;
    protected bool isPendingReturn => pendingReturnCoroutine != null;

    // 状态标记
    protected bool forceReturnOnSocketDisconnect = false;
    protected bool isGrabbedByHand = false;

    // 🔥 新增：标记是否正在Socket交互中（解决第一次吸附问题）
    protected bool isInSceneSocket = false;

    // Filter 接口
    public bool canProcess => isActiveAndEnabled && isInitialized; // 🔥 关键：必须初始化后才处理

    //Audio
    private AudioManager audioManager;

    protected virtual void Awake()
    {
        audioManager = this.GetUtility<AudioManager>();
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        originalScale = transform.localScale;

        myColliders = GetComponentsInChildren<Collider>(true)
            .Where(c => !c.isTrigger)
            .ToArray();

        if (grab.attachTransform == null)
            grab.attachTransform = transform;

        SetupTagAndCollision();

        if (restrictSocketInteractionToGrabbedOnly)
        {
            grab.hoverFilters.Add(this);
            grab.selectFilters.Add(this);
        }
    }

    protected virtual void SetupTagAndCollision()
    {
        if (!string.IsNullOrEmpty(waistSlotItemTag))
        {
            gameObject.tag = waistSlotItemTag;
        }

        if (ignoreCollisionWithSlotItems && !string.IsNullOrEmpty(waistSlotItemTag))
        {
            IgnoreCollisionWithSameTag();
        }
    }

    protected virtual void IgnoreCollisionWithSameTag()
    {
        GameObject[] sameTagObjects = GameObject.FindGameObjectsWithTag(waistSlotItemTag);

        foreach (var otherObj in sameTagObjects)
        {
            if (otherObj == gameObject) continue;

            Collider[] otherColliders = otherObj.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            foreach (var myCol in myColliders)
            {
                if (myCol == null) continue;
                foreach (var otherCol in otherColliders)
                {
                    if (otherCol == null) continue;
                    Physics.IgnoreCollision(myCol, otherCol, true);
                }
            }
        }
    }

    protected virtual void RestoreCollisionWithSameTag()
    {
        if (string.IsNullOrEmpty(waistSlotItemTag)) return;

        GameObject[] sameTagObjects = GameObject.FindGameObjectsWithTag(waistSlotItemTag);

        foreach (var otherObj in sameTagObjects)
        {
            if (otherObj == gameObject) continue;

            Collider[] otherColliders = otherObj.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            foreach (var myCol in myColliders)
            {
                if (myCol == null) continue;
                foreach (var otherCol in otherColliders)
                {
                    if (otherCol == null) continue;
                    Physics.IgnoreCollision(myCol, otherCol, false);
                }
            }
        }
    }

    protected virtual void OnDestroy()
    {
        RestoreCollisionWithSameTag();

        if (grab != null)
        {
            grab.hoverFilters.Remove(this);
            grab.selectFilters.Remove(this);
        }

        UnsubscribeFromSocketEvents();
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

    #region Socket 事件监听

    protected virtual void SubscribeToSocketEvents()
    {
        grab.selectEntered.AddListener(OnAnySelectEntered);
        grab.selectExited.AddListener(OnAnySelectExited);
        grab.hoverEntered.AddListener(OnAnyHoverEntered);
        grab.hoverExited.AddListener(OnAnyHoverExited); // 🔥 新增：监听HoverExit
    }

    protected virtual void UnsubscribeFromSocketEvents()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnAnySelectEntered);
            grab.selectExited.RemoveListener(OnAnySelectExited);
            grab.hoverEntered.RemoveListener(OnAnyHoverEntered);
            grab.hoverExited.RemoveListener(OnAnyHoverExited);
        }
    }

    protected virtual void OnAnySelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket)
        {
            // 🔥 关键：检查是否是家的Socket
            bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());

            if (!isHomeSocket)
            {
                isInSceneSocket = true; // 🔥 标记进入场景Socket
                CancelPendingReturn($"被场景Socket [{socket.name}] 吸附");

                if (homeSlot != null && homeSlot.currentItem == this)
                {
                    WaistInventory.Instance?.ReleaseSlot(homeSlot);
                }
            }
            else
            {
                Debug.Log($"[{name}] 成功回到腰带插槽 [{homeSlotID}]");
            }
        }
        else if (IsHandInteractor(args.interactorObject))
        {
            isGrabbedByHand = true;
        }
    }

    protected virtual void OnAnySelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket)
        {
            bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());

            if (!isHomeSocket)
            {
                Debug.Log($"[{name}] 从场景Socket [{socket.name}] 断开");
                isInSceneSocket = false; // 🔥 重置标记

                // 🔥 关键：强制立即回家
                forceReturnOnSocketDisconnect = true;

                // 立即启动物理
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.WakeUp();
                }

                // 立即启动回家（不等待）
                if (homeSlot != null && !isInSlot)
                {
                    CancelPendingReturn("从Socket断开，强制立即回家");
                    StartCoroutine(ForceReturnHomeImmediate());
                }
            }
        }
        else if (IsHandInteractor(args.interactorObject))
        {
            isGrabbedByHand = false;
        }
    }

    protected virtual bool IsHandInteractor(IXRInteractor interactor)
    {
        return interactor is XRDirectInteractor || interactor is XRRayInteractor;
    }

    protected virtual void OnAnyHoverEntered(HoverEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket && isPendingReturn)
        {
            bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
            if (!isHomeSocket)
            {
                PausePendingReturn($"进入场景Socket [{socket.name}] 范围");
            }
        }
    }

    // 🔥 新增：监听HoverExit，恢复回家计时
    protected virtual void OnAnyHoverExited(HoverExitEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor socket)
        {
            bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
            if (!isHomeSocket && !isInSceneSocket && !isGrabbedByHand && homeSlot != null && !isInSlot)
            {
                ResumePendingReturn($"离开场景Socket [{socket.name}] 范围");
            }
        }
    }

    #endregion

    #region 回家协程管理

    protected virtual void StartPendingReturn()
    {
        if (forceReturnOnSocketDisconnect) return;
        if (isInSceneSocket) return; // 🔥 关键：如果在场景Socket中，不启动回家

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
        if (forceReturnOnSocketDisconnect) return;
        if (isInSceneSocket) return; // 🔥 关键：如果在场景Socket中，不恢复

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
            yield return StartCoroutine(ExecuteReturnHome());
        }
        else
        {
            pendingReturnCoroutine = null;
            Debug.Log($"[{name}] 回家条件不满足，取消回家");
        }
    }

    // 🔥 重写：强制立即回家，修复卡在半空问题
    protected virtual IEnumerator ForceReturnHomeImmediate()
    {
        Debug.Log($"[{name}] 🔥 强制立即回家启动");

        // 等待一帧确保状态稳定
        yield return null;

        // 如果被手抓住，取消
        if (isGrabbedByHand)
        {
            Debug.Log($"[{name}] 强制回家取消：被手抓住");
            forceReturnOnSocketDisconnect = false;
            yield break;
        }

        // 如果已经回到家，直接结束
        if (isInSlot || homeSlot == null)
        {
            forceReturnOnSocketDisconnect = false;
            yield break;
        }

        // 🔥 关键：等待物理系统稳定
        yield return new WaitForFixedUpdate();

        // 再次检查状态
        if (isGrabbedByHand || isInSlot)
        {
            forceReturnOnSocketDisconnect = false;
            yield break;
        }

        yield return StartCoroutine(ExecuteReturnHome());
    }

    // 🔥 重写：修复卡在半空的回家执行逻辑
    protected virtual IEnumerator ExecuteReturnHome()
    {
        if (homeSlot == null) yield break;

        isReturning = true;

        // 🔥 关键：先解绑，再设置物理，最后设置位置和父级
        Transform targetParent = homeSlot.attachPoint;
        Vector3 targetPos = targetParent.position;
        Quaternion targetRot = targetParent.rotation;

        // 1. 先解除当前父级（关键！避免Transform冲突）
        transform.SetParent(null, true);

        // 2. 等待一帧让Transform系统更新
        yield return null;

        // 3. 设置物理状态为Kinematic（必须在设置parent之前！）
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 4. 等待物理系统处理
        yield return new WaitForFixedUpdate();

        // 5. 设置位置和旋转（此时无父级，世界坐标）
        transform.position = targetPos;
        transform.rotation = targetRot;

        // 6. 设置父级
        transform.SetParent(targetParent, true);

        // 7. 重置本地坐标
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        // 8. 占用槽位
        WaistInventory.Instance?.OccupySlot(homeSlot, this);

        isInSlot = true;
        isReturning = false;
        forceReturnOnSocketDisconnect = false;
        isInSceneSocket = false;

        Debug.Log($"[{name}] 成功回到腰带插槽 [{homeSlotID}]");
    }

    protected virtual bool CanReturnHome()
    {
        if (forceReturnOnSocketDisconnect)
        {
            return homeSlot != null && !isInSlot && !isGrabbedByHand && !isInSceneSocket;
        }

        if (!isInitialized) return false;
        if (grab.isSelected) return false;
        if (homeSlot == null) return false;
        if (isInSlot) return false;
        if (isInSceneSocket) return false; // 🔥 关键检查
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

    #region Filter 接口实现 - 🔥 关键修复

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        // 🔥 关键：如果已经初始化且正在与场景Socket交互，允许
        if (isInSceneSocket) return true;

        // 🔥 关键：如果正在被手抓住，允许Socket hover（用于吸附）
        if (isGrabbedByHand) return true;

        // 🔥 关键：如果在宽容期内，允许
        if (isInGracePeriod && !isInSlot) return true;

        // 🔥 关键：如果已经在家的槽位中，禁止其他Socket hover
        if (isInSlot && interactor is XRSocketInteractor socket)
        {
            bool isHomeSocket = (homeSlot != null && socket == homeSlot.GetComponent<XRSocketInteractor>());
            return isHomeSocket;
        }

        // 其他情况：只有被手抓住时才允许Socket交互
        return CanInteractWithSocket(interactor);
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        // 🔥 关键：如果已经初始化且正在与场景Socket交互，允许
        if (isInSceneSocket) return true;

        // 🔥 关键：如果正在被手抓住，允许Socket select
        if (isGrabbedByHand) return true;

        // 🔥 关键：如果在宽容期内，允许
        if (isInGracePeriod && !isInSlot) return true;

        // 其他情况
        return CanInteractWithSocket(interactor);
    }

    protected virtual bool CanInteractWithSocket(IXRInteractor interactor)
    {
        if (!restrictSocketInteractionToGrabbedOnly)
            return true;

        if (!(interactor is XRSocketInteractor))
            return true;

        // 🔥 关键：使用多个条件判断，更宽松
        return isGrabbedByHand || (isInGracePeriod && !isInSlot) || isInSceneSocket;
    }

    #endregion

    #region 接口实现

    void IWaistSlotItem.SnapToSlot(WaistSlot slot)
    {
        if (slot == null || slot.attachPoint == null) return;

        homeSlot = slot;
        isInSlot = true;
        isInSceneSocket = false;
        forceReturnOnSocketDisconnect = false;

        // 🔥 关键：先设置父级，再设置物理
        transform.SetParent(slot.attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void IWaistSlotItem.OnGrabFromSlot()
    {
        if (homeSlot != null && homeSlot.currentItem == this)
        {
            WaistInventory.Instance?.ReleaseSlot(homeSlot);
        }

        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;
        isInSlot = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
        }
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

        isGrabbedByHand = IsHandInteractor(args.interactorObject);
        if (isGrabbedByHand)
        {
            audioManager.PlayGrabItemInWaistSlotsSound();
        }

        CancelPendingReturn("物品被抓住");

        if (homeSlot != null && homeSlot.currentItem == this)
        {
            ((IWaistSlotItem)this).OnGrabFromSlot();
        }
        else if (isInSlot)
        {
            isInSlot = false;
        }
    }

    protected virtual void OnRelease(SelectExitEventArgs args)
    {
        if (!isInitialized) return;

        bool wasHand = IsHandInteractor(args.interactorObject);
        if (wasHand)
        {
            isGrabbedByHand = false;
        }

        lastReleaseTime = Time.time;

        // 🔥 关键：如果是从Socket释放，不处理（由OnAnySelectExited处理）
        if (args.interactorObject is XRSocketInteractor) return;

        // 🔥 关键：如果正在强制回家，不干扰
        if (forceReturnOnSocketDisconnect) return;

        if (homeSlot != null && !isInSlot && !isInSceneSocket)
        {
            StartPendingReturn();
        }
    }

    #endregion

    #region 工具方法

    protected virtual void OnDrawGizmosSelected()
    {
        if (!string.IsNullOrEmpty(waistSlotItemTag))
        {
            Gizmos.color = Color.cyan;
            GameObject[] sameTagObjects = GameObject.FindGameObjectsWithTag(waistSlotItemTag);
            foreach (var obj in sameTagObjects)
            {
                if (obj != gameObject)
                    Gizmos.DrawLine(transform.position, obj.transform.position);
            }
        }

        if (forceReturnOnSocketDisconnect)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }

        if (isInSceneSocket)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.05f);
        }
    }

    #endregion

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}
