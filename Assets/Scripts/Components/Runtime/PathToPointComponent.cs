using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct PathToPointComponent : IComponentData
{
    public float3 TargetPoint;
    public float Speed;
    public float MAX_SPEED;
}
