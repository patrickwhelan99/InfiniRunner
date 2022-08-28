using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct SpawnPathEvent : IComponentData
{
    public int ChunkID;
}
