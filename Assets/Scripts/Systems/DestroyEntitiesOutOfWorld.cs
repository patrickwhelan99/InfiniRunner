using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(DestroyEntitySystem))]
public partial class DestroyEntitiesOutOfWorld : SystemBase
{
    private const float MIN_WORLD_HEIGHT = -250.0f;
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter Writer = Ecbs.CreateCommandBuffer().AsParallelWriter();
        Dependency = Entities.WithNone<DestroyEntityTag>().ForEach((Entity E, in Translation Trans) =>
        {
            if (Trans.Value.y < MIN_WORLD_HEIGHT)
            {
                Writer.AddComponent(0, E, new DestroyEntityTag());
            }
        }).ScheduleParallel(Dependency);

        Ecbs.AddJobHandleForProducer(Dependency);
    }
}
