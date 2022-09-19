using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : new()
{
    public static T _instance;
    public static T Instance => _instance ??= new T();
    protected virtual void GameStart() { }
}
