using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class  NodeValue
{
    public string nodeName { get; set; }
    public string targetValue { get; set; }
    public string currentValue { get; set; }
    public bool isConsistence { get; set; }

    public NodeValue(string nodeName, string targetValue, string currentValue, bool isConsistence)
    {
        this.nodeName = nodeName;
        this.targetValue = targetValue;
        this.currentValue = currentValue;
        this.isConsistence = isConsistence;
    }
}