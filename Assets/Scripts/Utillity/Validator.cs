using System.Text.RegularExpressions;
using QFramework;

public class Validator : IUtility
{
    /// <summary>
    /// 检测是否为6位数字字符串
    /// </summary>
    public bool IsSixDigitNumber(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return Regex.IsMatch(input, @"^\d{6}$");
    }

    // 无正则版本（性能更好）
    public bool IsSixDigitNumberFast(string input)
    {
        if (input?.Length != 6) return false;

        foreach (char c in input)
        {
            if (c < '0' || c > '9') return false;
        }

        return true;
    }
}