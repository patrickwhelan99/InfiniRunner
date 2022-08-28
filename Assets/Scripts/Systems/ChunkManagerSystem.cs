using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(ChunkSpawningVariableSystemGroup))]
[AlwaysUpdateSystem]
public partial class ChunkManagerSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private struct Chunk
    {
        public int ID;
        public Vector2Int Coord;
    }

    private NativeQueue<Chunk> currentChunks = new NativeQueue<Chunk>(Allocator.Persistent);
    private int newsestChunkID;
    private Vector2Int newestChunkCoord;
    private Unity.Mathematics.Random random;

    private readonly Vector2Int[] Offsets = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.down,
        Vector2Int.right
    };

    protected override void OnCreate()
    {
        random.InitState((uint)new System.Random().Next());
        currentChunks.Enqueue(new Chunk());
    }
    protected override void OnStartRunning()
    {

    }
    protected override void OnUpdate()
    {
        SpawnChunk();
    }
    protected override void OnDestroy()
    {
        if (currentChunks.IsCreated)
        {
            currentChunks.Dispose();
        }
    }

    private void SpawnChunk()
    {
        // Enqueue the new chunk
        currentChunks.Enqueue(new Chunk()
        {
            ID = newsestChunkID,
            Coord = newestChunkCoord
        });

        //Vector2Int OldChunkCoord = new Vector2Int(int.MaxValue, int.MaxValue);

        // Delete the oldest chunk
        if (currentChunks.Count > 3)
        {
            Chunk ChunkToDelete = currentChunks.Dequeue();
            //OldChunkCoord = ChunkToDelete.Coord;
            DestroyChunk(ChunkToDelete.ID);
        }


        // Create an event for SpawnPath to listen for
        Entity SpawnPathEvent = EntityManager.CreateEntity(typeof(SpawnPathEvent));
        EntityManager.SetComponentData(SpawnPathEvent, new SpawnPathEvent() { ChunkID = newsestChunkID, ChunkCoord = newestChunkCoord });


        // Pick the direction of the next new chunk
        newestChunkCoord += Offsets.Where(x => !(newestChunkCoord + x).Equals(currentChunks.Peek())).ChooseRandom(ref random);
        newsestChunkID++;

        Debug.Log($"Old {currentChunks.Peek().Coord}\nNew {newestChunkCoord}");
    }

    private void DestroyChunk(int ChunkToDestroyID)
    {
        EntityQuery SegmentsInChunkQuery = EntityManager.CreateEntityQuery(typeof(LevelTileTag), typeof(ChunkSharedComponent));
        SegmentsInChunkQuery.SetSharedComponentFilter(new ChunkSharedComponent() { ChunkID = ChunkToDestroyID });

        Dependency = new DestroyChunkJob()
        {
            Writer = Ecbs.CreateCommandBuffer().AsParallelWriter(),
            entityTypeHandle = GetEntityTypeHandle(),
        }.Schedule(SegmentsInChunkQuery, Dependency);
    }

    private struct DestroyChunkJob : IJobEntityBatch
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        [ReadOnly] public EntityTypeHandle entityTypeHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            NativeArray<Entity> PathSegments = batchInChunk.GetNativeArray(entityTypeHandle);
            for (int i = 0; i < PathSegments.Length; i++)
            {
                Writer.DestroyEntity(i, PathSegments[i]);
            }
        }
    }
}
