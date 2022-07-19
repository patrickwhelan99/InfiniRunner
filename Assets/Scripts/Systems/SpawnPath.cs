using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Paz.Utility.PathFinding;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public partial class SpawnPath : SystemBase
{

    enum Direction {NONE, STRAIGHT, LEFT, RIGHT};

    static readonly int REAL_WORLD_SCALE = 20;

    NativeArray<Entity> LevelPrefabs;
    NativeArray<Vector2Int> PathPositions;

    protected override void OnStartRunning()
    {
        LevelPrefabs = new NativeArray<Entity>(3, Allocator.TempJob);

        LevelPrefabs[0] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Straight"));
        LevelPrefabs[1] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner"));
        LevelPrefabs[2] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner 1"));

        
        // Find a path through a random maze (max 5 attempts)
        int attempts = 0;
        Node[] Path = new Node[0];
        IEnumerable<Node> Walls = new Node[0];
        while(Path.Length < 1 && attempts++ < 5)
        {
            Node[] AllNodes = GridLayouts.RandomBlockers.GenerateGrid(64);

            AStar PathFinder = new AStar()
            {
                AllNodes = AllNodes,
                debug = true,

                StartNode = AllNodes[0],
                EndNode = AllNodes[AllNodes.Length - 1]
            };

            Path = PathFinder.Execute(out Walls).ToArray();
        }



        // Job #1 Spawn prefabs for each path tile
        PathPositions = new NativeArray<Vector2Int>(Path.Select(x => x.Coord * REAL_WORLD_SCALE).ToArray(), Allocator.TempJob);

        SpawnPathSegments SpawnSegmentsJob = new SpawnPathSegments()
        {
            Writer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            Prefabs = LevelPrefabs,
            Positions = PathPositions,
        };

        Dependency = SpawnSegmentsJob.Schedule(Path.Length, 8);


        Dependency.Complete();

        PathPositions.Dispose();
        LevelPrefabs.Dispose();
    }
    protected override void OnUpdate()
    {    
        
    }

    protected override void OnStopRunning()
    {
        if(PathPositions.IsCreated)
        {
            PathPositions.Dispose();
        }

        if(LevelPrefabs.IsCreated)
        {
            LevelPrefabs.Dispose();
        }
    }

    [BurstCompile]
    public struct SpawnPathSegments : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        [ReadOnly] public NativeArray<Entity> Prefabs;
        [ReadOnly] public NativeArray<Vector2Int> Positions;

        public void Execute(int index)
        {
            float3 SpawnPos = new float3()
            {
                x = Positions[index].x,
                y = -2.0f,
                z = Positions[index].y * -1.0f // so the graph is drawn from top left instead of bottom left
            };

            
            IsCorner(Positions, index, out bool Corner);
            GetSegmentPrefab(Positions, index, Corner, out int PrefabIndex);
            GetSegmentRotation(Positions, index, Corner, out float3 Rotation);

            

            Entity SpawnedEntity = Writer.Instantiate(index, Prefabs[PrefabIndex]);
            Writer.SetComponent<Translation>(index, SpawnedEntity, new Translation() { Value = SpawnPos});
            Writer.SetComponent<Rotation>(index, SpawnedEntity, new Rotation() { Value = quaternion.LookRotation(Rotation, new float3(0, 1, 0))});
        }

        private void IsCorner(NativeArray<Vector2Int> Positions, int index, out bool IsCorner)
        {
            Vector2Int Difference = new Vector2Int();
            if(index > 0 && index < Positions.Length - 1)
            {
                // Difference in grid co-ordinates between the previous and next nodes
                Difference = Positions[index + 1] / REAL_WORLD_SCALE - Positions[index - 1] / REAL_WORLD_SCALE;
            }

            IsCorner = Difference.x != 0 && Difference.y != 0;
        }

        private void GetSegmentPrefab(NativeArray<Vector2Int> Positions, int index, bool CornerPiece, out int PrefabIndex)
        {
            if(CornerPiece)
            {
                Vector2Int PrevPos = Positions[index - 1] / REAL_WORLD_SCALE;
                Vector2Int CurrentPos = Positions[index] / REAL_WORLD_SCALE;
                Vector2Int NextPos = Positions[index + 1] / REAL_WORLD_SCALE;

                // Determine which side of the vector the point lies on
                bool RightTurn = ((CurrentPos.x - PrevPos.x)*(NextPos.y - PrevPos.y) - (CurrentPos.y - PrevPos.y)*(NextPos.x - PrevPos.x)) > 0;

                if(RightTurn)
                {
                    PrefabIndex = 1;
                }
                else
                {
                    PrefabIndex = 2;
                }
            }
            else
            {
                PrefabIndex = 0;
            }
        }

        private void GetSegmentRotation(NativeArray<Vector2Int> Positions, int index, bool CornerPiece, out float3 Rotation)
        {
            Rotation = new float3(0.0f, 0.0f, 1.0f);
            if(index < 1)
            {
                index++;
            }


            if(CornerPiece)
            {
                Vector2Int BeforeToThis = Positions[index] / REAL_WORLD_SCALE - Positions[index - 1] / REAL_WORLD_SCALE;
                Rotation = new float3(BeforeToThis.x, 0.0f, -BeforeToThis.y);
            }
            else
            {
                Vector2Int Difference = Positions[index] / REAL_WORLD_SCALE - Positions[index - 1] / REAL_WORLD_SCALE;
                Rotation = new float3(math.abs(Difference.y), 0.0f, math.abs(Difference.x));
            }
        }
    }
}
