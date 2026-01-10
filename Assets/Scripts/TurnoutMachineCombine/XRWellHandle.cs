using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Content.Interaction;
using System.Reflection;

[RequireComponent(typeof(XRKnob))]
public class XRWellHandle : MonoBehaviour
{
    [Header("水井参数")]
    public Vector3 localAxis = Vector3.up;   // 转轴方向（本地）
    public float   radius    = 0.15f;        // 判定半径

    XRKnob knob;
    bool   wasClamped;      // 缓存原来 clamp 状态
    float  origMin, origMax;

    IXRSelectInteractor interactor;
    bool  grabbed;
    Vector3 lastProj;

    void Awake()
    {
        knob = GetComponent<XRKnob>();
        // 缓存原始值
        wasClamped = knob.clampedMotion;
        origMin    = knob.minAngle;
        origMax    = knob.maxAngle;
    }

    /* ---------- 抓取/释放 ---------- */
    protected void OnEnable()
    {
        knob.selectEntered.AddListener(OnGrab);
        knob.selectExited .AddListener(OnRelease);
    }

    protected void OnDisable()
    {
        knob.selectEntered.RemoveListener(OnGrab);
        knob.selectExited .RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject;
        grabbed    = true;
        lastProj   = ProjectToRing(interactor.GetAttachTransform(knob).position);

        // 让 XRKnob 的旋转计算失效
        knob.clampedMotion = false;
        knob.minAngle = -99999f;
        knob.maxAngle =  99999f;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        grabbed    = false;
        interactor = null;

        // 恢复原值
        knob.clampedMotion = wasClamped;
        knob.minAngle = origMin;
        knob.maxAngle = origMax;
    }

    /* ---------- 每帧同步旋转 ---------- */
    void Update()
    {
        if (!grabbed || interactor == null) return;

        Vector3 currentProj = ProjectToRing(interactor.GetAttachTransform(knob).position);
        float delta = Vector3.SignedAngle(lastProj, currentProj,
                                          transform.TransformDirection(localAxis));

        if (knob.handle != null)
        {
            Vector3 eu = knob.handle.localEulerAngles;
            eu.y += delta;
            knob.handle.localEulerAngles = eu;
        }

        lastProj = currentProj;
    }

    /* ---------- 工具函数 ---------- */
    Vector3 ProjectToRing(Vector3 worldPos)
    {
        Vector3 center = knob.handle != null ? knob.handle.position : transform.position;
        Vector3 toPos  = worldPos - center;
        Vector3 n      = transform.TransformDirection(localAxis);
        Vector3 proj   = Vector3.ProjectOnPlane(toPos, n);
        return proj.normalized * radius;
    }
}