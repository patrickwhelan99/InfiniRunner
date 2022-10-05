using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

[DisableAutoCreation]
public partial class LoadSubSceneSystem : SystemBase
{
    private SceneSystem SceneSys;
    private bool done = false;
    private Entity SubSceneLoadingEntity;

    Entity Result;

    protected override void OnCreate()
    {
        SceneSys = World.GetOrCreateSystem<SceneSystem>();
    }

    protected override void OnUpdate()
    {
        if (!done)
        {
            Hash128 PrefabHash = SceneSys.GetSceneGUID("Assets/Scenes/Scene/Straight.unity");
            SubSceneLoadingEntity = SceneSys.LoadSceneAsync(PrefabHash);
            done = true;
        }

        if (!SceneSys.IsSceneLoaded(SubSceneLoadingEntity))
        {
            return;
        }

        EntityQueryDesc QueryDesc = new EntityQueryDesc()
        {
            All = new ComponentType[] { typeof(PrefabComponent) },
            Options = EntityQueryOptions.IncludePrefab
        };

        EntityQuery PrefabsQuery = EntityManager.CreateEntityQuery(QueryDesc);

        NativeArray<Entity> Prefabs = PrefabsQuery.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabComponent> PrefabComps = PrefabsQuery.ToComponentDataArray<PrefabComponent>(Allocator.Temp);

        Result = EntityManager.Instantiate(Prefabs[0]);

        Prefabs.Dispose();
        PrefabComps.Dispose();

        Enabled = false;
    }
}
