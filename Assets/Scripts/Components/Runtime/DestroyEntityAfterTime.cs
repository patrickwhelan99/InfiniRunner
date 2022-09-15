using System;
using Unity.Entities;

[Serializable]
public struct DestroyEntityAfterTime : IComponentData
{
    public float TimeCreated;
    public float TimeToDestroy;
}
