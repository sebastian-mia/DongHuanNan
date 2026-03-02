using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class RandomNumber : IUtility
{
    // 唯一实例，Unity 主线程里随便用
    public readonly System.Random r = new System.Random();

    // 整数 [min, max)
    public int Range(int min, int max) => r.Next(min, max + 1);

    // 浮点数 [0, 1)
    public float Value => (float)r.NextDouble();
}