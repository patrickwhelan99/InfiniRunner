using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct DestroyLevelSegmentComponent : IComponentData
{
    public Vector2Int Value;
}
