using Unity.Entities;

[GenerateAuthoringComponent]
[Unity.Rendering.MaterialProperty("_Cutoff_Height", Unity.Rendering.MaterialPropertyFormat.Float)]
public struct DissolveShaderData : IComponentData
{
    public float Value;
}
