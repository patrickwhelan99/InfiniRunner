using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(ChunkSpawningVariableSystemGroup))]
[AlwaysUpdateSystem]
public partial class ChunkManagerSystem : SystemBase
{
    EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    struct Chunk
    {
        public int ID;
        public Vector2Int Coord;
    }

    NativeQueue<Chunk> currentChunks = new NativeQueue<Chunk>(Allocator.Persistent);
    int newsestChunkID;

    protected override void OnCreate()
    {
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
        if(currentChunks.IsCreated)
        {
            currentChunks.Dispose();
        }
    }

    private void SpawnChunk()
    {
        newsestChunkID++;

        currentChunks.Enqueue(new Chunk()
        {
            ID = newsestChunkID,
        });


        if(currentChunks.Count > 3)
        {
            Chunk ChunkToDelete = currentChunks.Dequeue();
            DestroyChunk(ChunkToDelete.ID);
        }

        Entity SpawnPathEvent = EntityManager.CreateEntity(typeof(SpawnPathEvent));
        EntityManager.SetComponentData(SpawnPathEvent, new SpawnPathEvent() {ChunkID = newsestChunkID});
    }

    private void DestroyChunk(int ChunkToDestroyID)
    {
        EntityQuery SegmentsInChunkQuery = EntityManager.CreateEntityQuery(typeof(LevelTileTag), typeof(ChunkSharedComponent));
        SegmentsInChunkQuery.SetSharedComponentFilter(new ChunkSharedComponent() {ChunkID = ChunkToDestroyID});

        Dependency = new DestroyChunkJob()
        {
            Writer = Ecbs.CreateCommandBuffer().AsParallelWriter(),
            entityTypeHandle = GetEntityTypeHandle(),
        }.Schedule(SegmentsInChunkQuery, Dependency);
    }

    struct DestroyChunkJob : IJobEntityBatch
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
