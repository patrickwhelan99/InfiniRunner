using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

public partial class LockAxiesSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = ecbs.CreateCommandBuffer();

        Entities.WithAll<LockAxiesTag>().ForEach((Entity Ent, ref PhysicsMass Mass) => 
        {
            Mass.InverseInertia = new float3(0.0f, 1.0f, 0.0f);
            ecb.RemoveComponent<LockAxiesTag>(Ent);
        }).Schedule(Dependency).Complete();
    }
}
