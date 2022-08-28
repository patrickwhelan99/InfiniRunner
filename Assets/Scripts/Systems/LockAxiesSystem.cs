using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

public partial class LockAxiesSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = Ecbs.CreateCommandBuffer();

        Entities.WithAll<LockAxiesTag>().ForEach((Entity Ent, ref PhysicsMass Mass) =>
        {
            Mass.InverseInertia = new float3(0.0f, 1.0f, 0.0f);
            ecb.RemoveComponent<LockAxiesTag>(Ent);
        }).Schedule(Dependency).Complete();
    }
}
