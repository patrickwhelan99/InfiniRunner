using Unity.Entities;
using Unity.Jobs;

public partial class AnimateDestroyingTileSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float Delta = Time.DeltaTime;
        Entities.ForEach((ref DissolveShaderData ShaderData) =>
        {
            if (ShaderData.Value > 2.0f)
            {
                ShaderData.Value = 0.0f;
            }

            ShaderData.Value += Delta;
        }).Schedule();
    }
}
