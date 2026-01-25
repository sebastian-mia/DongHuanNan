using UnityEngine;

/// <summary>
/// UI 基类：提供参数哈希分池 + 刷新接口
/// </summary>
public abstract class UIBase : MonoBehaviour
{
    /* 必须由子类给出“区分实例”的哈希，用于分池钥匙 */
    public abstract int GetParamHash();

    /// <summary>
    /// 根据参数哈希刷新表现（文本、图片、数据…）
    /// </summary>
    public abstract void Refresh(int paramHash);

    /* 生命周期钩子 */
    public virtual void OnShow() => gameObject.SetActive(true);
    public virtual void OnHide() => gameObject.SetActive(false);
    public virtual void OnClose() => Destroy(gameObject);
}