using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(XRGrabInteractable))]
public class WaistSlotItem : MonoBehaviour
{
    [Header("回家提示（可选）")]
    public GameObject ghostPrefab;
    private GameObject ghostInst;

    [HideInInspector] public XRSocketInteractor homeSocket;
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isReturning = false;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb   = GetComponent<Rigidbody>();

        // 旧版兼容：记录运行前已插好的槽
        var selecting = grab.interactorsSelecting;
        if (selecting.Count > 0 && selecting[0] is XRSocketInteractor s)
            homeSocket = s;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited .AddListener(OnRelease);
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
            rb.useGravity  = false;
            StopAllCoroutines();
            isReturning = false;
            HideGhost();

            // 🔥直接断电自家 Socket
            if (homeSocket != null) homeSocket.enabled = false;
        }
    }

    /* 松手：保持自家 disabled → 延迟1帧遍历 → 再决定是否打开自家 */
    private void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;

            // 🔥保持自家 disabled，先不参与竞争
            StartCoroutine(DecideAfterFrame());
        }
    }

    /* 延迟1帧：让所有 Socket 先抢完 → 再决定回不回 */
    private IEnumerator DecideAfterFrame()
    {
        yield return null; // 等竞争完成

        // 看谁捕获了本物品（自家仍 disabled，不会出现在结果里）
        var captor = FindObjectsOfType<XRSocketInteractor>()
                     .FirstOrDefault(s => s.selectTarget == grab);

        if (captor == null)
        {
            // 没人捕获 → 打开自家并回家
            if (homeSocket != null)
            {
                homeSocket.enabled = true; // 🔥现在才打开
                // 启动自动飞回
                StartCoroutine(AutoSnapBack());
            }
        }
        else
        {
            // 被别家捕获 → 留下，亮提示
            ShowGhost();
            // 阻止自动飞回
            StopCoroutine(AutoSnapBack());
        }
    }

    /* 回家协程：瞬移 → 强制塞进原槽 */
    private IEnumerator ReturnToHomeSocket()
    {
        isReturning = true;

        grab.enabled = false;
        transform.SetPositionAndRotation(homeSocket.attachTransform.position,
                                         homeSocket.attachTransform.rotation);
        grab.enabled = true;

        // homeSocket.StartManualInteraction(grab);   //🔧AutoSnap：改由 AutoSnapBack 负责
        yield return new WaitUntil(() => homeSocket.selectTarget == grab);
        isReturning = false;
    }

    //🔧AutoSnap：新增自动飞回并插回
    private IEnumerator AutoSnapBack()
    {
        yield return null; // 等 XR 释放完毕
        if (!homeSocket) yield break;

        // 冻结物理
        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
        rb.useGravity      = false;

        // 瞬移到槽口
        transform.SetPositionAndRotation(homeSocket.attachTransform.position,
                                         homeSocket.attachTransform.rotation);

        // 强制 socket 重新抓取
        homeSocket.enabled = true;
        grab.enabled       = false;
        grab.enabled       = true;
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
