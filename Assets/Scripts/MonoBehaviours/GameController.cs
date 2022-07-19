using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GameController : Singleton<GameController>
{
    

    void OnDestroy()
    {
        PrefabConverter.Dispose();
    }
}
