using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct DestroyEntityAfterTime : IComponentData
{
    public float TimeCreated;
    public float TimeToDestroy;
}
