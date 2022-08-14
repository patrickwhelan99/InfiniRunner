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
using Unity.Physics.Systems;
using Paz.Utility.Collections;
using Unity.Physics;
using TMPro;

[UpdateInGroup(typeof(VariableSystemGroupThreeSeconds))]
[AlwaysUpdateSystem]
public partial class SpawnPath : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    enum Direction {NONE, STRAIGHT, LEFT, RIGHT};

    static readonly int REAL_WORLD_SCALE = 20;
    static readonly int GRID_SIZE = 64;

    NativeArray<Entity> LevelPrefabs;

    GameObject TextPrefab;

    Vector3 playerSpawnPos;

    Entity playerEnt;

    protected override void OnCreate()
    {
        LevelPrefabs = new NativeArray<Entity>(4, Allocator.Persistent);

        LevelPrefabs[0] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Straight"));
        LevelPrefabs[1] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner"));
        LevelPrefabs[2] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner 1"));
        LevelPrefabs[3] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/T"));

        TextPrefab = Resources.Load<GameObject>("Prefabs/Level/CoordNumsPrefab");
    }

    protected override void OnStartRunning()
    {
        this.RegisterPhysicsRuntimeSystemReadWrite();
        CreatePath();
    }

    private NativeArray<Vector2Int> FindPathJob(NativeArray<Node> AllNodes, NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes, Vector2Int StartNode, Vector2Int EndNode)
    {
        var Path = new NativeList<Vector2Int>(Allocator.TempJob);
        var OpenSet = new NativeParallelHashSet<Node>(64, Allocator.TempJob);
        var VisualiserInstructionStack = new NativeList<(Vector2Int, Color)>(Allocator.Persistent);

        AStar.AsJob PathFinder = new AStar.AsJob()
        {
            allNodes = AllNodes,
            backwardNodes = BackwardNodes,
            path = Path,
            openSet = OpenSet,
            startNodeCoord = StartNode,
            endNodeCoord = EndNode,
            heuristicWeight = 5.0f,
            width = GRID_SIZE,

            visualiserInstructionStack = VisualiserInstructionStack
        };

        Dependency = PathFinder.Schedule(Dependency);

        OpenSet.Dispose(Dependency);

        Dependency.Complete();

        // PathFindingVisualiser Vis = new PathFindingVisualiser(GRID_SIZE);
        // Vis.SetInstructions(VisualiserInstructionStack.ToArray());
        // Vis.Playback(AllNodes.ToArray());        

        // VisualiserInstructionStack.Dispose(Dependency);

        return Path;
    }

    private void CreatePath()
    {
        uint Seed = (uint)new System.Random().Next(); // 680881328u
        Debug.Log($"Seed: {Seed}");

        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(Seed);


        NativeArray<Node> AllNodes = GridLayouts.RandomBlockers.GenerateGrid(GRID_SIZE, ref Rand);


        NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes = new NativeParallelMultiHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);
        NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes = new NativeParallelHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);

        // Find a path through a random maze (max 5 attempts)
        NativeArray<Vector2Int> Path;

        // We failed to find a path
        while((Path = FindPathJob(AllNodes, BackwardNodes, Vector2Int.zero, new Vector2Int(GRID_SIZE - 1, GRID_SIZE - 1))).Length < 1)
        {
            AllNodes.Dispose();
            AllNodes = GridLayouts.RandomBlockers.GenerateGrid(GRID_SIZE, ref Rand);

            ForwardNodes.Dispose();
            ForwardNodes = new NativeParallelMultiHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);

            BackwardNodes.Dispose();
            BackwardNodes = new NativeParallelHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);

            Path.Dispose();
        }

        playerSpawnPos = new Vector3(Path[0].x * REAL_WORLD_SCALE, 3.0f, Path[0].y * REAL_WORLD_SCALE);

        for (int i = 0; i < Path.Length; i++)
        {
            int Index = AllNodes.GetNodeIndex(Path[i]);

            BackwardNodes[Path[i]] = Node._defaultInvalid;
            ForwardNodes.Remove(Path[i]);

            ForwardNodes.Add(Path[i], i < Path.Length - 1 ? Path[i+1] : Node._defaultInvalid);
        }

        int Attempts = 0;
        NativeArray<Vector2Int> Branch;
        while((Branch = FindPathBranch(AllNodes, ForwardNodes, BackwardNodes, Path, AllNodes.Where(x => x.isBlocker), ref Rand)).Length < 1 && Attempts++ < 10)
        {
            Branch.Dispose();
        }

        

        //Node[][] Branches = FindPathBranches(10, AllNodes, Path, ref Rand);

        // Path = Path.Union(Branches.SelectMany(x => x)).Distinct().ToArray();


        // DebugPath(Path, Branches);


        //IEnumerable<Vector2Int> BranchPoints = Branches.Where(x => x.Length > 0).SelectMany(x => new Node[] {x.First(), x.Last()}).ToArray().Select(x => x.Coord);


        Vector2Int[] BranchPoints = Branch.Length > 0 ? new Vector2Int[]{Branch.First(), Branch.Last()} : new Vector2Int[0];

        // Spawn the main path (excluding intersections)
        SpawnPathParts(Path, BranchPoints, false);

        // Spawn branches (including intersections)
        if(Branch.Length > 0)
        {
            SpawnPathParts(Branch, BranchPoints, true, Path);
        }


        // Add coord numbers
        // ShowDebugCoords(Path.Union(Branch).Distinct());


        Path.Dispose(Dependency);
        Branch.Dispose(Dependency);
        ForwardNodes.Dispose(Dependency);
        BackwardNodes.Dispose(Dependency);
    }

    private void ShowDebugCoords(IEnumerable<Vector2Int> AllPathNodes)
    {
        Job.WithoutBurst().WithCode(() => 
        {
            foreach (Vector2Int N in AllPathNodes)
            {
                GameObject Go = MonoBehaviour.Instantiate(TextPrefab);
                Vector3 Pos = new Vector3(N.x * REAL_WORLD_SCALE, 0.0f, N.y * -1.0f * REAL_WORLD_SCALE);
                TMP_Text T = Go.GetComponent<TMP_Text>();
                T.text = $"{N.x},{N.y}";
                Go.transform.position = Pos;
            }
        }).Run();
    }

    private void SpawnPathParts(NativeArray<Vector2Int> Path, IEnumerable<Vector2Int> BranchPoints, bool ProcessBranchPoints = true, NativeArray<Vector2Int> MainPath = default)
    {
        // Job #2 Spawn prefabs for each path tile
        // IEnumerable<Node> PathNodesManaged = ProcessBranchPoints ? Path.Union(MainPath).ToArray() : Path.ToArray();


        // NativeArray<Vector2Int> PathPositions = new NativeArray<Vector2Int>(Path.Select(x => x.Coord).ToArray(), Allocator.Persistent);
        NativeArray<Vector2Int> PathBranchPoints = new NativeArray<Vector2Int>(BranchPoints.ToArray(), Allocator.Persistent);

        bool ResponsibleForMainPath = false;
        if(MainPath == default)
        {
            MainPath = new NativeArray<Vector2Int>(0, Allocator.TempJob);
            ResponsibleForMainPath = true;
        }

        SpawnPathSegments SpawnSegmentsJob = new SpawnPathSegments()
        {
            Writer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            Prefabs = LevelPrefabs,
            PathCoords = Path,
            BranchPoints = PathBranchPoints,
            MainPath = MainPath,
            DoBranchPoints = ProcessBranchPoints
        };

        

        Dependency = SpawnSegmentsJob.Schedule(Path.Count(), 8, Dependency);

        // PathPositions.Dispose(Dependency);
        PathBranchPoints.Dispose(Dependency);

        if(ResponsibleForMainPath)
        {
            MainPath.Dispose(Dependency);
        }
    }


    // struct FindPathBranchJob : IJob
    // {
    //     NativeArray<Node> AllNodes;
    //     NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes;
    //     NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes;
    //     NativeArray<Vector2Int> OriginalPath;
    //     IEnumerable<Node> Blockers;
    //     public void Execute()
    //     {
            
    //     }
    // }

    NativeArray<Vector2Int> FindPathBranch(NativeArray<Node> AllNodes, NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes, NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes, NativeArray<Vector2Int> OriginalPath, IEnumerable<Node> Blockers, ref Unity.Mathematics.Random Rand)
    {
        IEnumerable<Vector2Int> TrimmedPath = OriginalPath.Skip(3).SkipLast(3);

        // Select a node to branch from
        Node StartNode = AllNodes.GetNodeQuick(TrimmedPath.Where(x => ForwardNodes.CountValuesForKey(x) < 3).ChooseRandom(ref Rand));

        // Branch towards this point
        Node TargetNode = AllNodes.GetNodeQuick(AllNodes.Except(OriginalPath.Select(x => AllNodes.GetNodeQuick(x))).Where(x => !x.isBlocker && Vector2Int.Distance(StartNode.Coord, x.Coord) < 10).ChooseRandom(ref Rand));

        // Return to this point on the path
        Node ReturnNode = AllNodes.GetNodeQuick(TrimmedPath.SkipWhile(x => !x.Equals(StartNode)).Where(x => Vector2Int.Distance(x, TargetNode.Coord) < 30 ).ChooseRandom(ref Rand));

        AllNodes.ForEach(x => x.isBlocker = false);
        AllNodes.ForEach(x => BackwardNodes[x] = Node._defaultInvalid);
        AllNodes.Intersect(Blockers).ForEach(x => x.isBlocker = true);
        
        // Set all original path nodes as blockers
        for (int i = 0; i < OriginalPath.Length; i++)
        {
            Node CopyOfNode = AllNodes.GetNodeQuick(OriginalPath[i]);
            CopyOfNode.isBlocker = true;
            AllNodes[AllNodes.GetNodeIndex(OriginalPath[i])] = CopyOfNode;
        }

        Node NewStartNode = AllNodes.GetNodeQuick(StartNode);
        NewStartNode.isBlocker = false;
        AllNodes[AllNodes.GetNodeIndex(NewStartNode)] = NewStartNode;

        Node NewTargetNode = AllNodes.GetNodeQuick(TargetNode);
        NewTargetNode.isBlocker = false;
        AllNodes[AllNodes.GetNodeIndex(NewTargetNode)] = NewTargetNode;

        Node NewReturnNodeNode = AllNodes.GetNodeQuick(ReturnNode);
        NewReturnNodeNode.isBlocker = false;
        AllNodes[AllNodes.GetNodeIndex(NewReturnNodeNode)] = NewReturnNodeNode;


        

        StartNode.isBlocker = false;
        TargetNode.isBlocker = false;
        ReturnNode.isBlocker = false;

        // Find first half
        // IEnumerable<Node> BranchPathFirstHalf = new AStar(AllNodes, StartNode, TargetNode).Execute();
        NativeArray<Vector2Int> BranchPathFirstHalf = FindPathJob(AllNodes, BackwardNodes, StartNode.Coord, TargetNode.Coord);
        if(BranchPathFirstHalf.Count() < 1)
        {
            BranchPathFirstHalf.Dispose();
            return new NativeArray<Vector2Int>(0, Allocator.TempJob);
        }

        // Block out the first half
        for (int i = 0; i < BranchPathFirstHalf.Length; i++)
        {
            Node CopyOfNode = AllNodes.GetNodeQuick(BranchPathFirstHalf[i]);
            CopyOfNode.isBlocker = true;
            AllNodes[AllNodes.GetNodeIndex(BranchPathFirstHalf[i])] = CopyOfNode;
        }
        TargetNode.isBlocker = false;

        // Find second Half
        NativeArray<Vector2Int> BranchPathSecondHalf = FindPathJob(AllNodes, BackwardNodes, TargetNode.Coord, ReturnNode.Coord);
        if(BranchPathSecondHalf.Count() < 1)
        {
            BranchPathFirstHalf.Dispose();
            BranchPathSecondHalf.Dispose();
            return new NativeArray<Vector2Int>(0, Allocator.TempJob);
        }
        
        // Join the halves
        NativeArray<Vector2Int> FullBranchPath = new NativeArray<Vector2Int>(BranchPathFirstHalf.Union(BranchPathSecondHalf).Distinct().ToArray(), Allocator.Persistent);

        // Set branch in start node
        ForwardNodes.Add(StartNode.Coord, FullBranchPath.ElementAt(1));

        // Set forward node in the rest of the path
        for (int i = 1; i < FullBranchPath.Count(); i++)
        {
            Vector2Int ForwardNode;
            if(i < FullBranchPath.Count() - 1 )
            {
                ForwardNode = FullBranchPath.ElementAt(i+1);
            }
            else
            {
                if(ForwardNodes.TryGetFirstValue(FullBranchPath.ElementAt(i), out Vector2Int Item, out _))
                {
                    ForwardNode = Item;
                }
                else
                {
                    ForwardNode = Node._defaultInvalid;
                }
            }

            ForwardNodes.Add(FullBranchPath.ElementAt(i), ForwardNode);
        }

        BranchPathFirstHalf.Dispose();
        BranchPathSecondHalf.Dispose();

        return FullBranchPath;
    }

    protected override void OnUpdate()
    {    
        EntityCommandBuffer.ParallelWriter Writer = ecbs.CreateCommandBuffer().AsParallelWriter();

        Dependency = Entities.WithAll<LevelTileTag>().ForEach((Entity E) => 
        {
            Writer.AddComponent(E.Index, E, new DestroyEntityTag());
        }).ScheduleParallel(Dependency);

        CreatePath();

        // Dependency = Job.WithoutBurst().WithCode(() =>
        // {
        //     CreatePath();
        // }).Schedule(Dependency);

        // Dependency.Complete();

        
        // playerEnt = GetSingletonEntity<PlayerTag>();
        

        // Job.WithBurst().WithCode(() => 
        // {
        //     EntityManager.SetComponentData(playerEnt, new PhysicsVelocity());

        //     EntityManager.SetComponentData(playerEnt, new Translation()
        //     {
        //         Value = playerSpawnPos
        //     });
        // }).Schedule(Dependency);        
    }

    protected override void OnStopRunning()
    {
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
        [ReadOnly] public NativeArray<Vector2Int> PathCoords;
        [ReadOnly] public NativeArray<Vector2Int> BranchPoints;
        [ReadOnly] public NativeArray<Vector2Int> MainPath;
        [ReadOnly] public bool DoBranchPoints;

        [BurstCompile]
        public void Execute(int index)
        {
            if(!DoBranchPoints && BranchPoints.Contains(PathCoords[index]))
            {
                return;
            }


            float3 SpawnPos = new float3()
            {
                x = PathCoords[index].x * REAL_WORLD_SCALE,
                y = -2.0f,
                z = PathCoords[index].y * REAL_WORLD_SCALE * -1.0f // so the graph is drawn from top left instead of bottom left
            };

            
            bool Corner = IsCorner(PathCoords, index);
            int PrefabIndex = GetSegmentPrefab(PathCoords, BranchPoints, index, Corner);
            float3 Rotation = GetSegmentRotation(PathCoords, index, PrefabIndex);

            Entity SpawnedEntity = Writer.Instantiate(index, Prefabs[PrefabIndex]);
            Writer.SetComponent<Translation>(index, SpawnedEntity, new Translation() { Value = SpawnPos});
            Writer.SetComponent<Rotation>(index, SpawnedEntity, new Rotation() { Value = quaternion.LookRotation(Rotation, new float3(0, 1, 0))});
        }

        [BurstCompile]
        private bool IsCorner(NativeArray<Vector2Int> Positions, int index)
        {
            Vector2Int Difference = new Vector2Int();
            if(index > 0 && index < Positions.Length - 1)
            {
                // Difference in grid co-ordinates between the previous and next nodes
                Difference = Positions[index + 1] - Positions[index - 1];
            }

            return Difference.x != 0 && Difference.y != 0;
        }

        [BurstCompile]
        private int GetSegmentPrefab(NativeArray<Vector2Int> Positions, NativeArray<Vector2Int> BranchPoints, int index, bool CornerPiece)
        {
            if(BranchPoints.Contains(Positions[index]))
            {
                return 3;
            }

            if(CornerPiece)
            {
                Vector2Int PrevPos = Positions[index - 1];
                Vector2Int CurrentPos = Positions[index];
                Vector2Int NextPos = Positions[index + 1];

                // Determine which side of the vector the point lies on
                bool RightTurn = ((CurrentPos.x - PrevPos.x)*(NextPos.y - PrevPos.y) - (CurrentPos.y - PrevPos.y)*(NextPos.x - PrevPos.x)) > 0;

                if(RightTurn)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 0;
            }
        }

        [BurstCompile]
        private float3 GetSegmentRotation(NativeArray<Vector2Int> PathCoords, int index, int PrefabIndex)
        {
            float3 Rotation = new float3(0.0f, 0.0f, 1.0f);
            if(PrefabIndex != 3 && index < 1)
            {
                index++;
            }

            // T
            if(PrefabIndex == 3)
            {
                Vector2Int CurrentCoord = PathCoords[index];

                NativeArray<Vector2Int> Neighbours = new NativeArray<Vector2Int>(4, Allocator.Temp);
                Neighbours[0] = new Vector2Int(CurrentCoord.x, CurrentCoord.y - 1);
                Neighbours[1] = new Vector2Int(CurrentCoord.x + 1, CurrentCoord.y);
                Neighbours[2] = new Vector2Int(CurrentCoord.x, CurrentCoord.y + 1);
                Neighbours[3] = new Vector2Int(CurrentCoord.x - 1, CurrentCoord.y);

                Vector2Int ClosedDirection = Vector2Int.zero;



                for (int i = 0; i < Neighbours.Length; i++)
                {
                    if(!PathCoords.Union(MainPath).Contains(Neighbours[i]))
                    {
                        ClosedDirection = Neighbours[i] - CurrentCoord;
                    }
                }

                Rotation = new float3(ClosedDirection.x, 0.0f, ClosedDirection.y * -1);

                Neighbours.Dispose();
                return Rotation;
            }
            // Corner
            else if(PrefabIndex == 1 || PrefabIndex == 2)
            {
                Vector2Int BeforeToThis = PathCoords[index] - PathCoords[index - 1];
                Rotation = new float3(BeforeToThis.x, 0.0f, -BeforeToThis.y);
                return Rotation;
            }
            // Straight
            else if(PrefabIndex == 0)
            {
                Vector2Int Difference = PathCoords[index] - PathCoords[index - 1];
                Rotation = new float3(math.abs(Difference.y), 0.0f, math.abs(Difference.x));
                return Rotation;
            }

            return Rotation;
        }
    }

    // private void TraverseNodes(Node Start, ref ObservableHashSet<Node> TraversedNodes)
    // {
    //     TraversedNodes.Add(Start);
        
    //     for (int i = 0; i < Start.forwardNodes.Length; i++)
    //     {
    //         if(Start.forwardNodes[i] != null && !TraversedNodes.Contains(Start.forwardNodes[i]))
    //         {
    //             TraverseNodes(Start.forwardNodes[i], ref TraversedNodes);
    //         }
    //     }
    // }
}