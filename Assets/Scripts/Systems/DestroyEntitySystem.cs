using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DestroyEntitySystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    EntityQuery EntitiesToDestroyQuery;

    protected override void OnCreate()
    {
        EntitiesToDestroyQuery = EntityManager.CreateEntityQuery(typeof(DestroyEntityTag));
    }

    protected override void OnUpdate()
    {
        DestroyEntitiesJob Job = new DestroyEntitiesJob()
        {
            Writer = ecbs.CreateCommandBuffer().AsParallelWriter()
        };

        Job.ScheduleParallel(EntitiesToDestroyQuery).Complete();
    }

    partial struct DestroyEntitiesJob : IJobEntity
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
