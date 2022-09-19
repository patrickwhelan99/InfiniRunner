using Unity.Entities;
using Unity.Mathematics;

public partial class ScoringSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    public static int Score { get; private set; }

    private EntityQuery scoreModifiersQuery;

    public event System.Action<int> OnScoreModified;

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
        int Change = Score;

        EntityCommandBuffer Ecb = Ecbs.CreateCommandBuffer();
        Entities.ForEach((Entity E, in ModifyScoreEvent Event) =>
        {
            Score = math.max(0, Score + Event.Value);
            Ecb.DestroyEntity(E);
        }).Run();

        Change = Score - Change;

        if (Change != 0)
        {
            OnScoreModified?.Invoke(Score);
        }
    }
}
