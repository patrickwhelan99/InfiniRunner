using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SpawnPathEvent : IComponentData
{
    public int ChunkID;
    public Vector2Int ChunkCoord;
    public Vector2Int StartNode;
    public Vector2Int EndNode;
}
