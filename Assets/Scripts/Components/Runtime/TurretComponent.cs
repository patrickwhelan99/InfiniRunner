using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct TurretComponent : IComponentData
{
    public float3 Target;
    [NonSerialized]
    public float LastFireTime;
    public float FireRate;
    public float3 ArrowSpawnOffset;
    public Entity Arrow;
}
