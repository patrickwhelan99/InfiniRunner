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
        bool AddDissolveComponent = false;

        // Check that the tile we're entering isn't already scheduled to be destroyed
        // This stops the player going backwards and triggering the destruction of the current tile
        bool Continue = false;
        Entities.WithAll<LevelTileTag, Translation>().WithNone<DestroyEntityAfterTime>().ForEach((Entity E, in Translation Trans) =>
        {
            PositionAsInt = new int3((int)math.round(Trans.Value.x), -2, (int)math.round(Trans.Value.z));

            if (PositionAsInt.Equals(LocalCurrTilePos))
            {
                Continue = true;
            }
        }).Run();


        if (!Continue)
        {
            return;
        }

        EntityCommandBuffer.ParallelWriter Writer = Ecbs.CreateCommandBuffer().AsParallelWriter();
        Entities.WithAll<LevelTileTag, Translation>().WithNone<DestroyEntityAfterTime>().ForEach((Entity E, in Translation Trans) =>
        {
            PositionAsInt = new int3((int)math.round(Trans.Value.x), -2, (int)math.round(Trans.Value.z));

            if (PositionAsInt.Equals(LocalPrevTilePos))
            {
                AddDissolveComponent = true;
                Writer.AddComponent(0, E, new DestroyEntityAfterTime() { TimeCreated = CurrentTime, TimeToDestroy = CurrentTime + 2.0f });
                TheEntity = E;
            }
        }).Run();

        if (AddDissolveComponent)
        {
            AddDissolveComponentToChildren(TheEntity, Ecbs.CreateCommandBuffer());
        }

        if (EntityManager.HasComponent<DestroyLevelSegmentComponent>(TheEntity))
        {
            Vector2Int CoordToDestroy = EntityManager.GetComponentData<DestroyLevelSegmentComponent>(TheEntity).Value;

            Entities.WithAll<LevelTileTag, Translation>().ForEach((Entity E, in Translation Trans) =>
            {
                PositionAsInt = new int3((int)math.round(Trans.Value.x), -2, (int)math.round(Trans.Value.z));

                int X = (PositionAsInt.x - (ThisChunk.Coord.x * WorldConstants.GRID_WIDTH * WorldConstants.REAL_WORLD_SCALE)) / WorldConstants.REAL_WORLD_SCALE;
                int Y = (PositionAsInt.z - (ThisChunk.Coord.y * WorldConstants.GRID_WIDTH * WorldConstants.REAL_WORLD_SCALE)) / WorldConstants.REAL_WORLD_SCALE;

                Vector2Int Coord = new Vector2Int(X, Y * -1);


                if (Coord.Equals(CoordToDestroy))
                {
                    Writer.AddComponent(0, E, new DestroyEntityTag());
                }
            }).Run();
        }

        PreviousTilePosition = CurrentTilePosition;
    }


    /// <summary>
    /// Recursively search children for entities with the DissolveMaterial. 
    /// A component is added so that the shader's values can be driven from code
    /// </summary>
    private void AddDissolveComponentToChildren(Entity TheEntity, EntityCommandBuffer Ecb)
    {
        EntityQuery EQ = EntityManager.CreateEntityQuery(typeof(Unity.Rendering.RenderMesh));
        if (EQ.Matches(TheEntity))
        {
            Unity.Rendering.RenderMesh RM = EntityManager.GetSharedComponentData<Unity.Rendering.RenderMesh>(TheEntity);
            if (!RM.Equals(default) && RM.material.name == "DissolveMaterial")
            {
                Ecb.AddComponent<DissolveShaderData>(TheEntity);
            }
        }


        if (GetBufferFromEntity<Child>().TryGetBuffer(TheEntity, out DynamicBuffer<Child> Children))
        {
            foreach (Child ChildComponent in Children)
            {
                AddDissolveComponentToChildren(ChildComponent.Value, Ecb);
            }
        }
    }

    private int3 GetPlayersCurrentTile(float3 PlayersPosition)
    {
        return new int3(RoundToGridSize(PlayersPosition.x), -2, RoundToGridSize(PlayersPosition.z));
    }

    private int RoundToGridSize(float Num)
    {
        float N = Num / WorldConstants.REAL_WORLD_SCALE;

        int LowerX = ((int)math.floor(N)) * WorldConstants.REAL_WORLD_SCALE;
        int UpperX = ((int)math.ceil(N)) * WorldConstants.REAL_WORLD_SCALE;
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
