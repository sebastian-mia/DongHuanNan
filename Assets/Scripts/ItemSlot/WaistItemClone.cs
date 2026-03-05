using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq;
using QFramework;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistItemClone : MonoBehaviour, IController
{
    [Header("松手后存活时间（秒）")] public float lifeAfterDrop = 2f;

    [Header("碰撞设置")] [Tooltip("克隆物品与腰带物品之间永远不碰撞")]
    public bool ignoreCollisionWithWaistItems = true;

    [Tooltip("克隆物品之间永远不碰撞")] public bool ignoreCollisionWithClones = true;

    [Tooltip("腰带物品的Tag")] public string waistItemTag = "WaistSlotsItem";

    private XRGrabInteractable grab;
    private PoolItem poolItem;
    private bool wasGrabbed = false;
    private Collider[] myColliders;

    //Audio
    private AudioManager audioManager;

    void Awake()
    {
        audioManager = this.GetUtility<AudioManager>();
        grab = GetComponent<XRGrabInteractable>();
        myColliders = GetComponentsInChildren<Collider>(true)
            .Where(c => !c.isTrigger)
            .ToArray();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnDropped);

        ApplyCollisionIgnores();
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnDropped);

        RestoreAllCollisions();
    }

    // 从池中取出时调用（重置状态）
    public void OnSpawn()
    {
        wasGrabbed = false;
        CancelInvoke(nameof(ReturnToPool));

        if (poolItem == null)
        {
            poolItem = GetComponent<PoolItem>();
            if (poolItem == null)
            {
                Debug.LogError($"[{name}] 缺少 PoolItem 组件！");
            }
        }

        // 重置物理状态
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 重新应用碰撞忽略
        ApplyCollisionIgnores();
    }

    #region 碰撞管理

    /// <summary>
    /// 应用所有碰撞忽略设置
    /// </summary>
    private void ApplyCollisionIgnores()
    {
        if (ignoreCollisionWithWaistItems)
        {
            IgnoreCollisionWithTag(waistItemTag);
        }

        if (ignoreCollisionWithClones)
        {
            IgnoreCollisionWithSelfType();
        }
    }

    /// <summary>
    /// 忽略与指定Tag物体的碰撞
    /// </summary>
    private void IgnoreCollisionWithTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);

        foreach (var target in targets)
        {
            if (target == null || target == gameObject) continue;

            var targetColliders = target.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            IgnoreColliders(targetColliders);
        }
    }

    /// <summary>
    /// 忽略与同类型（WaistItemClone）其他实例的碰撞
    /// </summary>
    private void IgnoreCollisionWithSelfType()
    {
        // 查找场景中所有其他 WaistItemClone
        var otherClones = FindObjectsOfType<WaistItemClone>()
            .Where(c => c != this && c.gameObject != gameObject)
            .ToArray();

        foreach (var clone in otherClones)
        {
            if (clone == null) continue;

            var cloneColliders = clone.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            IgnoreColliders(cloneColliders);
        }
    }

    /// <summary>
    /// 忽略与指定碰撞器数组的碰撞
    /// </summary>
    private void IgnoreColliders(Collider[] others)
    {
        foreach (var myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (var otherCol in others)
            {
                if (otherCol == null) continue;
                Physics.IgnoreCollision(myCol, otherCol, true);
            }
        }
    }

    /// <summary>
    /// 恢复所有碰撞
    /// </summary>
    private void RestoreAllCollisions()
    {
        // 恢复与腰带物品的碰撞
        if (!string.IsNullOrEmpty(waistItemTag))
        {
            RestoreCollisionWithTag(waistItemTag);
        }

        // 恢复与其他克隆物品的碰撞
        RestoreCollisionWithSelfType();
    }

    /// <summary>
    /// 恢复与指定Tag物体的碰撞
    /// </summary>
    private void RestoreCollisionWithTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);

        foreach (var target in targets)
        {
            if (target == null) continue;

            var targetColliders = target.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            RestoreCollisions(targetColliders);
        }
    }

    /// <summary>
    /// 恢复与同类型其他实例的碰撞
    /// </summary>
    private void RestoreCollisionWithSelfType()
    {
        var otherClones = FindObjectsOfType<WaistItemClone>()
            .Where(c => c != this)
            .ToArray();

        foreach (var clone in otherClones)
        {
            if (clone == null) continue;

            var cloneColliders = clone.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.isTrigger)
                .ToArray();

            RestoreCollisions(cloneColliders);
        }
    }

    /// <summary>
    /// 恢复与指定碰撞器数组的碰撞
    /// </summary>
    private void RestoreCollisions(Collider[] others)
    {
        foreach (var myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (var otherCol in others)
            {
                if (otherCol == null) continue;
                Physics.IgnoreCollision(myCol, otherCol, false);
            }
        }
    }

    #endregion

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        wasGrabbed = true;
        if (args.interactorObject.transform.parent.CompareTag("LeftController") ||
            args.interactorObject.transform.parent.CompareTag("RightController"))
        {
            audioManager.PlayGrabItemInWaistSlotsSound();
        }

        CancelInvoke(nameof(ReturnToPool));
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        if (wasGrabbed)
        {
            Invoke(nameof(ReturnToPool), lifeAfterDrop);
        }
    }

    private void ReturnToPool()
    {
        if (!grab.isSelected)
        {
            if (poolItem != null)
            {
                poolItem.ReturnToPool();
            }
            else
            {
                Debug.LogWarning($"[{name}] 没有PoolItem，直接销毁");
                Destroy(gameObject);
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}