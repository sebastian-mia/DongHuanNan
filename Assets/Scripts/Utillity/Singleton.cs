using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour
    where T : Singleton<T>
{
    private static T instance;

    public static T Instance
    {
        get { return instance; }
        set
        {
            if (instance != null)
                return;
            instance = value;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = (T)this;
        }
    }

    void OnDestory()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}