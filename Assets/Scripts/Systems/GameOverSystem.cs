using Unity.Entities;

public partial class GameOverSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    protected override void OnCreate()
    {
        EntityQuery GameOverEventQuery = EntityManager.CreateEntityQuery(typeof(GameOverEvent));
        RequireForUpdate(GameOverEventQuery);
    }
    protected override void OnUpdate()
    {
        // EntityQueryDesc DestructionQueryDesc = new EntityQueryDesc()
        // {
        //     Any = new ComponentType[] { typeof(PlayerTag), typeof(EnemyTag), typeof(LevelTileTag), typeof(GameOverEvent) }
        // };

        // EntityQuery DestructionQuery = EntityManager.CreateEntityQuery(DestructionQueryDesc);

        // Dependency = new DestroyEntitiesJob()
        // {
        //     Writer = Ecbs.CreateCommandBuffer().AsParallelWriter()
        // }.ScheduleParallel(DestructionQuery);

        // Ecbs.AddJobHandleForProducer(Dependency);

        World.DisposeAllWorlds();

        GameManager.Instance.GameOver();
    }

    private partial struct DestroyEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        public void Execute(Entity E)
        {
            Writer.DestroyEntity(0, E);
        }
    }
}
