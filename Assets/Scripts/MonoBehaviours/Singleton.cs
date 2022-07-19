using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour
{
    public static Singleton<T> instance;

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError($"A singleton of type {typeof(T).ToString()} already exists!");
        }

        GameStart();
    }

    protected virtual void GameStart(){}
    
}
