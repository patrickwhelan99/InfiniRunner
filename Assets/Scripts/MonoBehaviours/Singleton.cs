using UnityEngine;

public class Singleton<T> : MonoBehaviour
{
    public static Singleton<T> instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError($"A singleton of type {typeof(T)} already exists!");
        }

        GameStart();
    }

    protected virtual void GameStart() { }
}
