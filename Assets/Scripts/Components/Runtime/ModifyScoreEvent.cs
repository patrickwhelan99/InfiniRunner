using System;
using Unity.Entities;

[Serializable]
public struct ModifyScoreEvent : IComponentData
{
    public int Value;
}
