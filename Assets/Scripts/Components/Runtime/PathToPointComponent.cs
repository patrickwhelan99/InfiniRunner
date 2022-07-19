using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[Serializable]
[GenerateAuthoringComponent]
public struct PathToPointComponent : IComponentData
{
    public float3 TargetPoint;
    public float Speed;
    public float MAX_SPEED;
}
