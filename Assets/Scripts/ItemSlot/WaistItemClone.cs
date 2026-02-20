using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistItemClone : MonoBehaviour
{
    [Header("松手后存活时间（秒）")]
    public float lifeAfterDrop = 2f;

    private XRGrabInteractable grab;
    private PoolItem poolItem;
    private bool wasGrabbed = false;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        // 不在Awake获取poolItem，因为对象池可能还没初始化
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnDropped);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnDropped);
    }

    // 从池中取出时调用（重置状态）
    public void OnSpawn()
    {
        wasGrabbed = false;
        CancelInvoke(nameof(ReturnToPool));
        
        // 🔥 延迟获取PoolItem（确保对象池已初始化）
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
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        wasGrabbed = true;
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
            // 🔥 安全调用
            if (poolItem != null)
            {
                poolItem.ReturnToPool();
            }
            else
            {
                // 如果没有PoolItem，直接销毁（兜底）
                Debug.LogWarning($"[{name}] 没有PoolItem，直接销毁");
                Destroy(gameObject);
            }
        }
    }
}