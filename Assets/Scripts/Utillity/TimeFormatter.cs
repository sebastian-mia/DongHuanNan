using System;
using QFramework;

public class TimeFormatter : IUtility
{
    private const string Format = "yyyy/MM/dd HH:mm:ss";

    /// <summary>
    /// 使用 TryFormat 格式化时间，无GC分配
    /// </summary>
    public bool TryFormatDateTime(DateTime time, Span<char> destination, out int charsWritten)
    {
        return time.TryFormat(destination, out charsWritten, Format);
    }

    /// <summary>
    /// 便捷方法：返回格式化后的字符串（有GC）
    /// </summary>
    public string FormatDateTime(DateTime time)
    {
        return time.ToString(Format);
    }
}