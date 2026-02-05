public static class EnumUtils
{
    /// <summary>
    /// 获取枚举的总数量
    /// </summary>
    public static int GetCount<T>() where T : System.Enum
    {
        return System.Enum.GetValues(typeof(T)).Length;
    }

    /// <summary>
    /// 获取枚举的所有值
    /// </summary>
    public static T[] GetValues<T>() where T : System.Enum
    {
        return (T[])System.Enum.GetValues(typeof(T));
    }
}