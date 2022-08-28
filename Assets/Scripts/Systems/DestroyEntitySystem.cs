using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DestroyEntitySystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private EntityQuery EntitiesToDestroyQuery;

    protected override void OnCreate()
    {
        EntitiesToDestroyQuery = EntityManager.CreateEntityQuery(typeof(DestroyEntityTag));
    }

    protected override void OnUpdate()
    {
        DestroyEntitiesJob Job = new DestroyEntitiesJob()
        {
            Writer = Ecbs.CreateCommandBuffer().AsParallelWriter()
        };

        Job.ScheduleParallel(EntitiesToDestroyQuery).Complete();
    }

    private partial struct DestroyEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Writer;

        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        public int nativeThreadIndex;
        public void Execute(Entity Ent)
        {
            Writer.DestroyEntity(nativeThreadIndex, Ent);
        }
    }
}
