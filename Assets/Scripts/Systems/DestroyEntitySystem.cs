using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DestroyEntitySystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private EntityQuery entitiesToDestroyQuery;
    private EntityQuery playerQuery;

    protected override void OnCreate()
    {
        entitiesToDestroyQuery = EntityManager.CreateEntityQuery(typeof(DestroyEntityTag));
        playerQuery = EntityManager.CreateEntityQuery(typeof(PlayerTag));
    }

    protected override void OnUpdate()
    {
        DestroyEntitiesJob Job = new DestroyEntitiesJob()
        {
            Writer = Ecbs.CreateCommandBuffer().AsParallelWriter(),
            PlayersQuery = GetComponentDataFromEntity<PlayerTag>(true),
        };

        Job.ScheduleParallel(entitiesToDestroyQuery).Complete();
    }

    private partial struct DestroyEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        [ReadOnly] public ComponentDataFromEntity<PlayerTag> PlayersQuery;

        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        public int nativeThreadIndex;
        public void Execute(Entity Ent)
        {
            if (PlayersQuery.HasComponent(Ent))
            {
                Entity GameOverEntity = Writer.CreateEntity(nativeThreadIndex);
                Writer.AddComponent(nativeThreadIndex, GameOverEntity, new GameOverEvent()
                {
                    Value = GameOverReason.PLAYER_FELL
                });
            }

            Writer.DestroyEntity(nativeThreadIndex, Ent);
        }
    }
}
