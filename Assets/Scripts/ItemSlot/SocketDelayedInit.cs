using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketAutoAttach : MonoBehaviour
{
    [SerializeField] private XRSocketInteractor socket;
    [SerializeField] private XRGrabInteractable objectToAttach;

    private void Start()
    {
        StartCoroutine(AttachSequence());
    }

    private IEnumerator AttachSequence()
    {
        // 等待多帧确保所有系统就绪
        yield return null; // 第1帧
        yield return null; // 第2帧
        yield return new WaitForEndOfFrame(); // 等待渲染结束

        // 关键：先禁用 Socket
        socket.enabled = false;

        // 设置 startingSelectedInteractable
        socket.startingSelectedInteractable = objectToAttach;

        yield return null;

        // 重新启用 Socket，这会触发初始化
        socket.enabled = true;

        // 验证
        yield return new WaitForSeconds(0.1f);

        if (!socket.hasSelection)
        {
            Debug.LogWarning("自动吸附失败，尝试手动方式");
            ManualAttach();
        }
    }

    private void ManualAttach()
    {
        // 禁用物理
        var rb = objectToAttach.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 直接父子关系
        objectToAttach.transform.SetParent(socket.attachTransform, false);
        objectToAttach.transform.localPosition = Vector3.zero;
        objectToAttach.transform.localRotation = Quaternion.identity;
    }
}