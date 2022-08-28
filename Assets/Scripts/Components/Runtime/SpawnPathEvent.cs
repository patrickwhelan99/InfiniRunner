using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SpawnPathEvent : IComponentData
{
    public int ChunkID;
    public Vector2Int ChunkCoord;
}
