using System;
using Unity.Entities;

[Serializable]
public struct ChunkSharedComponent : ISharedComponentData
{
    public int ChunkID;
}
