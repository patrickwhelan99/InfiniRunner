using System;
using Unity.Entities;

[GenerateAuthoringComponent]
[Serializable]
public struct PrefabComponent : IComponentData
{
    public PrefabID Value;
}

public enum PrefabID
{
    NONE,
    STRAIGHT,
    LEFT_CORNER,
    RIGHT_CORNER,
    T_JUNCTION
}
