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

        SceneSystem sceneSystem = World.GetExistingSystem<SceneSystem>();
        Hash128 guid = sceneSystem.GetSceneGUID("Assets/Scenes/MainMenu.unity");
        Entity sceneEntity = sceneSystem.LoadSceneAsync(guid);

        GameManager.Instance.GameOver();
    }
}
