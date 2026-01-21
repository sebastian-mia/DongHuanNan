using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
public class InfiniteWaistItem : MonoBehaviour
{
    [Header("克隆体预制（留空则克隆自己）")]
    public GameObject clonePrefab;

    [Header("失去交互后多久删除（秒）")]
    public float lifeAfterDrop = 0.5f;

    private XRGrabInteractable grab;
    private bool isClone = false;          // 运行时判断
    private float dropTime = float.MaxValue;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        isClone = name.Contains("(Clone)");

        // 克隆体：监听松手，启动倒计时
        if (isClone)
        {
            grab.selectExited.AddListener(OnCloneDrop);
            return;
        }

        // 原始物体：监听被抓，生成克隆体
        grab.selectEntered.AddListener(OnOriginalGrab);
    }

    /* 原始物体被抓 → 生成克隆体并让 XR 抓它 */
    private void OnOriginalGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor directInteractor)
        {
            GameObject prefab = clonePrefab != null ? clonePrefab : gameObject;
            GameObject clone = Instantiate(prefab, transform.position, transform.rotation);

            // 让 XR 直接抓克隆体
            XRGrabInteractable cloneGrab = clone.GetComponent<XRGrabInteractable>();
            if (cloneGrab)
                directInteractor.interactionManager.SelectEnter(directInteractor, cloneGrab);

            // 原始物体退出交互（留在槽内）
            grab.interactionManager.SelectExit(directInteractor, grab);
        }
    }

    /* 克隆体松手 → 开始倒计时 */
    private void OnCloneDrop(SelectExitEventArgs args)
    {
        dropTime = Time.time + lifeAfterDrop;
    }

    void Update()
    {
        // 克隆体：无人交互且超时 → 自毁
        if (isClone && Time.time >= dropTime && !grab.isSelected)
            Destroy(gameObject);
    }
}