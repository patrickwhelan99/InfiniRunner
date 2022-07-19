using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class DestroyEntityAfterTimeSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    protected override void OnUpdate()
    {
        Dependency = new DestroyAfterTime()
        {
            writer = ecbs.CreateCommandBuffer().AsParallelWriter(),
            currentTime = (float)Time.ElapsedTime
        }.ScheduleParallel();
    }

    public partial struct DestroyAfterTime : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter writer;
        [ReadOnly]
        public float currentTime;
        public void Execute([EntityInQueryIndex]int EntityIndex, Entity Entity, in DestroyEntityAfterTime DestroyEntity)
        {
            if(currentTime >= DestroyEntity.TimeToDestroy)
            {
                writer.RemoveComponent<DestroyEntityAfterTime>(EntityIndex, Entity);
                writer.AddComponent<DestroyEntityTag>(EntityIndex, Entity);
            }
        }
    }
}
