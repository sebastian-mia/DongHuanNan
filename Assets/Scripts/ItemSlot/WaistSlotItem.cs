using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistSlotItem : MonoBehaviour
{
    [Header("回家提示（可选）")] public GameObject ghostPrefab;
    private GameObject ghostInst;
    public float delayTime = 1f;

    [HideInInspector] public XRSocketInteractor homeSocket;
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isReturning = false;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // 旧版兼容：记录运行前已插好的槽
        var selecting = grab.interactorsSelecting;
        if (selecting.Count > 0 && selecting[0] is XRSocketInteractor s)
            homeSocket = s;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    public void SetHome(XRSocketInteractor socket)
    {
        if (homeSocket == null) homeSocket = socket;
    }

    /* 抓取瞬间：直接关闭自家 Socket（整个禁用） */
    private void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            StopAllCoroutines();
            isReturning = false;
            HideGhost();

            // 🔥直接断电自家 Socket
            if (homeSocket != null) homeSocket.enabled = false;
        }
    }

    /* 松手：保持自家 disabled → 延迟1秒后再决定是否拉回 */
    private void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // 🔥1秒后再判断
            StartCoroutine(DelayedDecide());
        }
    }

    /* 新增1秒延迟 */
    private IEnumerator DelayedDecide()
    {
        yield return new WaitForSeconds(delayTime);
        // 下面完全复用你原来的逻辑
        var captor = FindObjectsOfType<XRSocketInteractor>()
            .FirstOrDefault(s => s.selectTarget == grab);

        if (captor == null)
        {
            if (homeSocket != null)
            {
                homeSocket.enabled = true;
                StartCoroutine(AutoSnapBack());
            }
        }
        else
        {
            ShowGhost();
            StopCoroutine(AutoSnapBack());
        }
    }

    /* 自动飞回并插回 */
    private IEnumerator AutoSnapBack()
    {
        yield return null; // 等 XR 释放完毕
        if (!homeSocket) yield break;

        // 冻结物理
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        // 瞬移到槽口
        transform.SetPositionAndRotation(homeSocket.attachTransform.position,
            homeSocket.attachTransform.rotation);

        // 强制 socket 重新抓取
        homeSocket.enabled = true;
        grab.enabled = false;
        grab.enabled = true;
        homeSocket.StartManualInteraction(grab);

        yield return new WaitUntil(() => homeSocket.selectTarget == grab);
    }

    public void ShowGhost()
    {
        if (ghostPrefab == null) return;
        if (ghostInst == null)
        {
            ghostInst = Instantiate(ghostPrefab,
                homeSocket.attachTransform.position,
                homeSocket.attachTransform.rotation);
        }
        else
        {
            ghostInst.SetActive(true);
            ghostInst.transform.SetPositionAndRotation(homeSocket.attachTransform.position,
                homeSocket.attachTransform.rotation);
        }

        CancelInvoke(nameof(HideGhost));
        Invoke(nameof(HideGhost), 1.5f);
    }

    private void HideGhost() => ghostInst?.SetActive(false);
}