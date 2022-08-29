using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SpawnPathEvent : IComponentData
{
    public int ChunkID;
    public Vector2Int ChunkCoord;
    public Vector2Int SpawnOffset => ChunkCoord * SpawnPath.GRID_WIDTH * SpawnPath.REAL_WORLD_SCALE;
    public Vector2Int StartNode;
    public Vector2Int EndNode;
    public Vector2Int DirectionOfPreviousChunk;
    public Vector2Int DirectionOfNextChunk;
}
