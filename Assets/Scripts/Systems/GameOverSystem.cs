using Unity.Entities;
using Unity.Scenes;

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
        GameOverReason Reason = GameOverReason.NONE;

        EntityCommandBuffer Ecb = Ecbs.CreateCommandBuffer();
        Entities.WithAll<GameOverEvent>().ForEach((Entity E, in GameOverEvent Event) =>
        {
            Reason = Event.Value;
            Ecb.DestroyEntity(E);
        }).Run();
        
        GameManager.Instance.GameOver();
    }
}
