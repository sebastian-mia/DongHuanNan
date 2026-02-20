/// <summary>
/// 腰部插槽物品接口
/// </summary>
public interface IWaistSlotItem
{
    /// <summary>
    /// 专属槽位ID
    /// </summary>
    string HomeSlotID { get; }
    
    /// <summary>
    /// 是否已初始化完成
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// 吸附到插槽
    /// </summary>
    /// <param name="slot">目标槽位</param>
    void SnapToSlot(WaistSlot slot);
    
    /// <summary>
    /// 从插槽被抓取
    /// </summary>
    void OnGrabFromSlot();
    
    /// <summary>
    /// 释放插槽占用
    /// </summary>
    void ReleaseSlot();
}