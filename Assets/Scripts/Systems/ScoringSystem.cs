using Unity.Entities;
using Unity.Mathematics;

public partial class ScoringSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private int _score;
    public int Score
    {
        get => _score;

        private set
        {
            if (value != _score)
            {
                _score = value;
                OnScoreModified?.Invoke(value);
            }
        }
    }

    private EntityQuery scoreModifiersQuery;

    public static event System.Action<int> OnScoreModified;

    public void RegisterCallback(System.Action<int> Action)
    {
        OnScoreModified += Action;
    }

    public void UnregisterCallback(System.Action<int> Action)
    {
        OnScoreModified -= Action;
    }

    protected override void OnCreate()
    {
        scoreModifiersQuery = EntityManager.CreateEntityQuery(typeof(ModifyScoreEvent));
        RequireForUpdate(scoreModifiersQuery);
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer Ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp); //Ecbs.CreateCommandBuffer();
        Entities.ForEach((Entity E, in ModifyScoreEvent Event) =>
        {
            Score = math.max(0, Score + Event.Value);
            Ecb.DestroyEntity(E);
        }).WithoutBurst().Run();

        Ecb.Playback(EntityManager);
        Ecb.Dispose();
    }
}
