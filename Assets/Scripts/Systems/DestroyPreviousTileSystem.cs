using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class DestroyPreviousTileSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private EntityQuery TilesQuery;
    private int3 PreviousTilePosition;
    private int3 CurrentTilePosition;

    protected override void OnStartRunning()
    {
        RequireSingletonForUpdate<PlayerTag>();

        TilesQuery = EntityManager.CreateEntityQuery(typeof(LevelTileTag));
        RequireForUpdate(TilesQuery);
    }
    protected override void OnUpdate()
    {
        Entity Player = GetSingletonEntity<PlayerTag>();
        float3 PlayerPos = GetComponentDataFromEntity<Translation>(true)[Player].Value;

        CurrentTilePosition = GetPlayersCurrentTile(PlayerPos);
        if (CurrentTilePosition.Equals(PreviousTilePosition))
        {
            return;
        }

        int3 LocalPrevTilePos = PreviousTilePosition;
        int3 LocalCurrTilePos = CurrentTilePosition;
        ChunkManagerSystem ChunkManager = World.GetExistingSystem<ChunkManagerSystem>();
        ChunkManagerSystem.Chunk ThisChunk = ChunkManager.GetChunkFromPosition(CurrentTilePosition);

        int3 PositionAsInt = new int3();

        Entity TheEntity = default;

        float CurrentTime = (float)Time.ElapsedTime;

        EntityCommandBuffer.ParallelWriter Writer = Ecbs.CreateCommandBuffer().AsParallelWriter();
        Entities.WithAll<LevelTileTag, Translation>().ForEach((Entity E, in Translation Trans) =>
        {
            PositionAsInt = new int3((int)math.round(Trans.Value.x), -2, (int)math.round(Trans.Value.z));

            if (PositionAsInt.Equals(LocalPrevTilePos))
            {
                Writer.AddComponent(0, E, new DestroyEntityAfterTime() { TimeCreated = CurrentTime, TimeToDestroy = CurrentTime + 0.2f });
                TheEntity = E;
            }
        }).Run();

        if (EntityManager.HasComponent<DestroyLevelSegmentComponent>(TheEntity))
        {
            Vector2Int CoordToDestroy = EntityManager.GetComponentData<DestroyLevelSegmentComponent>(TheEntity).Value;

            Entities.WithAll<LevelTileTag, Translation>().ForEach((Entity E, in Translation Trans) =>
            {
                PositionAsInt = new int3((int)math.round(Trans.Value.x), -2, (int)math.round(Trans.Value.z));

                int X = (PositionAsInt.x - (ThisChunk.Coord.x * SpawnPath.GRID_WIDTH * SpawnPath.REAL_WORLD_SCALE)) / SpawnPath.REAL_WORLD_SCALE;
                int Y = (PositionAsInt.z - (ThisChunk.Coord.y * SpawnPath.GRID_WIDTH * SpawnPath.REAL_WORLD_SCALE)) / SpawnPath.REAL_WORLD_SCALE;

                Vector2Int Coord = new Vector2Int(X, Y * -1);


                if (Coord.Equals(CoordToDestroy))
                {
                    Writer.AddComponent(0, E, new DestroyEntityTag());
                }
            }).Run();
        }

        PreviousTilePosition = CurrentTilePosition;
    }

    private int3 GetPlayersCurrentTile(float3 PlayersPosition)
    {
        return new int3(RoundToGridSize(PlayersPosition.x), -2, RoundToGridSize(PlayersPosition.z));
    }

    private int RoundToGridSize(float Num)
    {
        float N = Num / SpawnPath.REAL_WORLD_SCALE;

        int LowerX = ((int)math.floor(N)) * SpawnPath.REAL_WORLD_SCALE;
        int UpperX = ((int)math.ceil(N)) * SpawnPath.REAL_WORLD_SCALE;
        int RoundedN = Num - LowerX > UpperX - Num ? UpperX : LowerX;

        return RoundedN;
    }

    /// <summary>
    /// Recursive function to walk the path until a specified node is reached.
    /// Along the way the provided NativeList is populated
    /// </summary>
    private static void TraverseNodes(ChunkManagerSystem.Chunk ThisChunk, NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes, NativeList<Vector2Int> Path, Vector2Int Start, Vector2Int TargetNode)
    {
        NativeParallelMultiHashMap<Vector2Int, Vector2Int>.Enumerator Enumerator = ForwardNodes.GetValuesForKey(Start);

        while (Enumerator.MoveNext())
        {
            if (Enumerator.Current.Equals(TargetNode))
            {
                return;
            }
            else
            {
                Path.Add(Enumerator.Current);
                TraverseNodes(ThisChunk, ForwardNodes, Path, Enumerator.Current, TargetNode);
            }
        }
    }
}
