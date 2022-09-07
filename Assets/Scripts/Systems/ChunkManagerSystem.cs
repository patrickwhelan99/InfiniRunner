using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(ChunkSpawningVariableSystemGroup))]
public partial class ChunkManagerSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private struct Chunk
    {
        public int ID;
        public Vector2Int Coord;
    }

    private NativeQueue<Chunk> currentChunks = new NativeQueue<Chunk>(Allocator.Persistent);
    private int newestChunkID;
    private Vector2Int currentChunkCoord;
    private Vector2Int nextChunkCoord;
    private Vector2Int endNode;
    private Chunk lastAdded;
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
    }
    protected override void OnStartRunning()
    {
        RequireSingletonForUpdate<PlayerTag>();
    }
    protected override void OnUpdate()
    {
        Entity Player = GetSingletonEntity<PlayerTag>();
        float3 PlayerTrans = GetComponentDataFromEntity<Translation>(true)[Player].Value;
        Vector2Int PlayerPos = new Vector2Int((int)(PlayerTrans.x + 0.5f), (int)(PlayerTrans.z + 0.5f));

        int ChunkPlayerInhabits = int.MaxValue;

        NativeArray<Chunk> ChunksArray = currentChunks.ToArray(Allocator.Temp);

        for (int i = 0; i < ChunksArray.Length; i++)
        {
            Vector2Int ChunkMin, ChunkMax;

            int ChunkSize = SpawnPath.GRID_WIDTH * SpawnPath.REAL_WORLD_SCALE;

            ChunkMin = new Vector2Int((ChunksArray[i].Coord.x * ChunkSize) - SpawnPath.REAL_WORLD_SCALE, (ChunksArray[i].Coord.y * ChunkSize) + SpawnPath.REAL_WORLD_SCALE);
            ChunkMax = ChunkMin + new Vector2Int(ChunkSize, -ChunkSize);

            if
            (
                PlayerPos.x < ChunkMax.x
                && PlayerPos.x > ChunkMin.x
                && PlayerPos.y > ChunkMax.y
                && PlayerPos.y < ChunkMin.y
            )
            {
                ChunkPlayerInhabits = i;
                break;
            }
        }

        // If the player is in the middle chunk
        if (ChunkPlayerInhabits == 1 || ChunksArray.Length < 2)
        {
            SpawnChunk();
        }

        ChunksArray.Dispose();
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
        // Delete the oldest chunk
        if (currentChunks.Count > 2)
        {
            Chunk ChunkToDelete = currentChunks.Dequeue();
            DestroyChunk(ChunkToDelete.ID);
        }

        Vector2Int DirPrevChunk = currentChunkCoord - nextChunkCoord;

        Vector2Int Direction = nextChunkCoord - currentChunkCoord;
        Vector2Int Start = newestChunkID > 0 ? GetOppositeEdgeNode(Direction, endNode, SpawnPath.GRID_WIDTH) : GetRandomEdgeNode(Direction, SpawnPath.GRID_WIDTH, ref random);

        // Pick the direction of the next new chunk
        currentChunkCoord = nextChunkCoord;
        newestChunkID++;

        nextChunkCoord = currentChunkCoord + Offsets.Where(v => !(currentChunkCoord + v).Equals(lastAdded.Coord)).ChooseRandom(ref random);

        // Enqueue the new chunk
        lastAdded = new Chunk()
        {
            ID = newestChunkID,
            Coord = currentChunkCoord
        };

        currentChunks.Enqueue(lastAdded);



        // Create an event for SpawnPath to listen for
        Entity SpawnPathEvent = EntityManager.CreateEntity(typeof(SpawnPathEvent));

        Direction = nextChunkCoord - currentChunkCoord;
        Vector2Int End = endNode = GetRandomEdgeNode(Direction, SpawnPath.GRID_WIDTH, ref random);

        EntityManager.SetComponentData(SpawnPathEvent, new SpawnPathEvent()
        {
            ChunkID = newestChunkID,
            ChunkCoord = currentChunkCoord,
            StartNode = Start,
            EndNode = End,
            DirectionOfPreviousChunk = DirPrevChunk,
            DirectionOfNextChunk = Direction
        });
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

    public Vector2Int GetRandomEdgeNode(Vector2Int Direction, int GridWidth, ref Unity.Mathematics.Random Rand)
    {
        int RandomNum = Rand.NextInt(GridWidth);
        return
                Direction.Equals(Vector2Int.up) ? new Vector2Int(RandomNum, 0) :
                Direction.Equals(Vector2Int.down) ? new Vector2Int(RandomNum, GridWidth - 1) :
                Direction.Equals(Vector2Int.left) ? new Vector2Int(0, RandomNum) :
                Direction.Equals(Vector2Int.right) ? new Vector2Int(GridWidth - 1, RandomNum) : default;
    }

    public Vector2Int GetOppositeEdgeNode(Vector2Int Direction, Vector2Int Node, int GridWidth)
    {
        return
                Direction.Equals(Vector2Int.up) ? new Vector2Int(Node.x, GridWidth - 1) :
                Direction.Equals(Vector2Int.down) ? new Vector2Int(Node.x, 0) :
                Direction.Equals(Vector2Int.left) ? new Vector2Int(GridWidth - 1, Node.y) :
                Direction.Equals(Vector2Int.right) ? new Vector2Int(0, Node.y) : default;
    }
}
