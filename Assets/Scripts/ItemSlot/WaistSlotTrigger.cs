using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class WaistSlotTrigger : MonoBehaviour
{
    [Header("检测范围")] 
    public float radius = 0.15f;

    [Header("左右手 Interactor")] 
    public XRDirectInteractor leftHandInteractor;
    public XRDirectInteractor rightHandInteractor;

    [Header("左右手输入绑定")] 
    public InputActionReference leftGrabAction;
    public InputActionReference rightGrabAction;

    [Header("对象池（替代预制体）")]
    public ObjectPool objectPool;  // 拖拽挂载ObjectPool的GameObject

    private bool leftInRange = false;
    private bool rightInRange = false;

    void OnEnable()
    {
        if (leftGrabAction?.action != null)
        {
            leftGrabAction.action.Enable();
            leftGrabAction.action.performed += OnLeftGrabPerformed;
        }

        if (rightGrabAction?.action != null)
        {
            rightGrabAction.action.Enable();
            rightGrabAction.action.performed += OnRightGrabPerformed;
        }
    }

    void OnDisable()
    {
        if (leftGrabAction?.action != null)
        {
            leftGrabAction.action.performed -= OnLeftGrabPerformed;
            leftGrabAction.action.Disable();
        }

        if (rightGrabAction?.action != null)
        {
            rightGrabAction.action.performed -= OnRightGrabPerformed;
            rightGrabAction.action.Disable();
        }
    }

    void Update()
    {
        CheckRange();
    }

    void CheckRange()
    {
        if (leftHandInteractor != null)
            leftInRange = Vector3.Distance(transform.position, leftHandInteractor.transform.position) <= radius;

        if (rightHandInteractor != null)
            rightInRange = Vector3.Distance(transform.position, rightHandInteractor.transform.position) <= radius;
    }

    private void OnLeftGrabPerformed(InputAction.CallbackContext context)
    {
        if (leftInRange && leftHandInteractor != null)
            SpawnClone(leftHandInteractor);
    }

    private void OnRightGrabPerformed(InputAction.CallbackContext context)
    {
        if (rightInRange && rightHandInteractor != null)
            SpawnClone(rightHandInteractor);
    }

    private void SpawnClone(XRDirectInteractor interactor)
    {
        if (objectPool == null)
        {
            Debug.LogError("ObjectPool 未设置！");
            return;
        }

        // 从对象池获取
        GameObject clone = objectPool.Get();
        if (clone == null)
        {
            Debug.LogWarning("对象池已满，无法获取对象");
            return;
        }

        // 设置位置和旋转
        Transform attachPoint = interactor.attachTransform;
        clone.transform.SetPositionAndRotation(attachPoint.position, attachPoint.rotation);

        // 重置状态
        if (clone.TryGetComponent<WaistItemClone>(out var waistItem))
        {
            waistItem.OnSpawn();
        }

        // 让手抓住它
        if (clone.TryGetComponent<XRGrabInteractable>(out var grab))
        {
            interactor.interactionManager.SelectEnter(interactor, grab);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}