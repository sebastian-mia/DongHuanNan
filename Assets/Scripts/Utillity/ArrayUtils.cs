using System;
using System.Collections.Generic;

public static class ArrayUtils
{
    /// <summary>
    /// 获取排序后的数组（不修改原数组）
    /// </summary>
    public static T[] GetSorted<T>(this T[] array) where T : IComparable<T>
    {
        T[] result = new T[array.Length];
        System.Array.Copy(array, result, array.Length);
        System.Array.Sort(result);
        return result;
    }

    /// <summary>
    /// 查找插入位置（保持有序）
    /// </summary>
    public static int FindInsertIndex<T>(this List<T> sortedList, T item) where T : IComparable<T>
    {
        int index = sortedList.BinarySearch(item);
        return index >= 0 ? index : ~index;
    }
}