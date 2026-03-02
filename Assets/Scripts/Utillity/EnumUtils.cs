using QFramework;

public class EnumUtils : IUtility
{
    /// <summary>
    /// 获取枚举的总数量
    /// </summary>
    public int GetCount<T>() where T : System.Enum
    {
        return System.Enum.GetValues(typeof(T)).Length;
    }

    /// <summary>
    /// 获取枚举的所有值
    /// </summary>
    public T[] GetValues<T>() where T : System.Enum
    {
        return (T[])System.Enum.GetValues(typeof(T));
    }
}