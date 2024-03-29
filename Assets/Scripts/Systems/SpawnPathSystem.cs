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
using Paz.Utility.Collections;
using Unity.Physics;
using TMPro;

public partial class SpawnPathSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    private enum Direction { NONE, STRAIGHT, LEFT, RIGHT };

    private NativeArray<Entity> LevelPrefabs;

    private GameObject TextPrefab;

    public static uint InitialRandomState { get; private set; } = 0u;

    private Unity.Mathematics.Random GameSeeder = new Unity.Mathematics.Random();

    protected override void OnCreate()
    {
        LevelPrefabs = new NativeArray<Entity>(4, Allocator.Persistent);

        LevelPrefabs[0] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Straight"));
        LevelPrefabs[1] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner"));
        LevelPrefabs[2] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Corner 1"));
        LevelPrefabs[3] = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/T"));

        TextPrefab = Resources.Load<GameObject>("Prefabs/Level/CoordNumsPrefab");

        InitialRandomState = (uint)new System.Random().Next(); // 785443340u; // 655220412u;
        Debug.Log(InitialRandomState);
        GameSeeder.InitState(InitialRandomState);
    }

    protected override void OnDestroy()
    {
        if (LevelPrefabs.IsCreated)
        {
            LevelPrefabs.Dispose();
        }
    }

    protected override void OnStartRunning()
    {

    }

    private void FindPathJob(NativeList<Vector2Int> Path, NativeArray<Node> AllNodes, NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes, NativeArray<Vector2Int> StartAndEndNodes, int StartNodeIndex = 0, int EndNodeIndex = 1)
    {
        NativeParallelHashSet<Node> OpenSet = new NativeParallelHashSet<Node>(64, Allocator.TempJob);
        NativeList<(Vector2Int, Color)> VisualiserInstructionStack = new NativeList<(Vector2Int, Color)>(Allocator.Persistent);

        AStar.AsJob PathFinder = new AStar.AsJob()
        {
            allNodes = AllNodes,
            backwardNodes = BackwardNodes,
            path = Path,
            openSet = OpenSet,
            startAndEndNodes = StartAndEndNodes,
            heuristicWeight = 5.0f,
            width = WorldConstants.GRID_WIDTH,

            startNodeIndex = StartNodeIndex,
            endNodeIndex = EndNodeIndex,

            visualiserInstructionStack = VisualiserInstructionStack
        };

        Dependency = PathFinder.Schedule(Dependency);

        OpenSet.Dispose(Dependency);

        VisualiserInstructionStack.Dispose(Dependency);
    }

    private void CreatePath(SpawnPathEvent Event)
    {
        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(GameSeeder.NextUInt());


        NativeArray<Node> AllNodes = new NativeArray<Node>(WorldConstants.GRID_WIDTH * WorldConstants.GRID_WIDTH, Allocator.TempJob);
        GridLayouts.RandomBlockers.GenerateGrid(AllNodes, WorldConstants.GRID_WIDTH, ref Rand);


        NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes = new NativeParallelMultiHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);
        NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes = new NativeParallelHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);

        // Find a path through a random maze (max 5 attempts)
        NativeList<Vector2Int> Path = new NativeList<Vector2Int>(Allocator.TempJob);

        NativeArray<Vector2Int> StartAndEndNodes = new NativeArray<Vector2Int>(2, Allocator.TempJob);
        StartAndEndNodes[0] = Event.StartNode;
        StartAndEndNodes[1] = Event.EndNode;

        FindPathJob(Path, AllNodes, BackwardNodes, StartAndEndNodes);

        Dependency = Job.WithCode(() =>
        {
            if (Path.Length < 1)
            {
                return;
            }

            for (int i = 0; i < Path.Length; i++)
            {
                int Index = AllNodes.GetNodeIndex(Path[i]);

                BackwardNodes[Path[i]] = default;
                ForwardNodes.Remove(Path[i]);

                ForwardNodes.Add(Path[i], i < Path.Length - 1 ? Path[i + 1] : default);
            }
        }).Schedule(Dependency);


        NativeList<Vector2Int> BranchPoints = new NativeList<Vector2Int>(0, Allocator.TempJob);


        NativeList<Vector2Int> Branch = new NativeList<Vector2Int>(Allocator.TempJob);
        NativeList<Vector2Int> Blockers = new NativeList<Vector2Int>(Allocator.TempJob);

        NativeArray<Vector2Int> KeyPoints = new NativeArray<Vector2Int>(3, Allocator.TempJob);

        NativeArray<Vector2Int> PenultimateNodes = new NativeArray<Vector2Int>(4, Allocator.TempJob);

        NativeArray<Unity.Mathematics.Random> Randoms = new NativeArray<Unity.Mathematics.Random>(1, Allocator.TempJob);
        Randoms[0] = Rand;

        FindPathBranch(Branch, KeyPoints, AllNodes, ForwardNodes, BackwardNodes, Path, Blockers, PenultimateNodes, Randoms);

        Dependency = Job.WithCode(() =>
        {
            Rand = Randoms[0];
        }).Schedule(Dependency);

        Randoms.Dispose(Dependency);

        Dependency = Job.WithCode(() =>
        {
            if (Branch.Length > 0)
            {
                BranchPoints.Add(Branch[0]);
                BranchPoints.Add(Branch[^1]);
            }
        }).Schedule(Dependency);

        // Spawn branches (including intersections)
        SpawnPathParts(Event, Branch, BranchPoints, ForwardNodes, Path, PenultimateNodes, true);

        Blockers.Dispose(Dependency);
        KeyPoints.Dispose(Dependency);

        // Spawn the main path (excluding intersections)
        // We don't want to pass anything in but using default gives an unitialiazed array error
        NativeList<Vector2Int> MainPath = new NativeList<Vector2Int>(0, Allocator.TempJob);
        SpawnPathParts(Event, Path, BranchPoints, ForwardNodes, MainPath, PenultimateNodes, false);




        // If we've failed to create a path, try again
        EntityCommandBuffer.ParallelWriter Writer = Ecbs.CreateCommandBuffer().AsParallelWriter();
        Dependency = Job.WithCode(() =>
        {
            if (Path.Length < 1)
            {
                Entity E = Writer.CreateEntity(0);
                Writer.AddComponent(1, E, Event);
            }
        }).Schedule(Dependency);

        if (Event.ChunkID == 1)
        {
            EntityQuery PlayerQuery = EntityManager.CreateEntityQuery(typeof(PlayerTag));

            Dependency = new SetPlayerSpawnPosJob()
            {
                Path = Path,
                Offset = Event.SpawnOffset
            }.Schedule(PlayerQuery, Dependency);

            Dependency = Job.WithCode(() =>
            {
                Entity E = Writer.CreateEntity(0);
                Writer.AddComponent<GameReadyEvent>(0, E);
            }).Schedule(Dependency);
        }


        Dependency.Complete();

        // Add coord numbers
        // ShowDebugCoords(Path.ToArray().Union(Branch.ToArray()).Distinct());

        AllNodes.Dispose(Dependency);
        Path.Dispose(Dependency);
        ForwardNodes.Dispose(Dependency);
        BackwardNodes.Dispose(Dependency);
        BranchPoints.Dispose(Dependency);
        StartAndEndNodes.Dispose(Dependency);
        MainPath.Dispose(Dependency);
        PenultimateNodes.Dispose(Dependency);
        Branch.Dispose(Dependency);
    }

    private partial struct SetPlayerSpawnPosJob : IJobEntity
    {
        [ReadOnly] public NativeList<Vector2Int> Path;
        [ReadOnly] public Vector2Int Offset;
        public void Execute(ref Translation Trans)
        {
            if (Path.Length < 1)
            {
                return;
            }

            Trans = new Translation()
            {
                Value = new float3
                (
                    Offset.x + (Path[0].x * WorldConstants.REAL_WORLD_SCALE),
                    5.0f,
                    Offset.y + (Path[0].y * -1 * WorldConstants.REAL_WORLD_SCALE)
                )
            };
        }
    }

    private void ShowDebugCoords(IEnumerable<Vector2Int> AllPathNodes)
    {
        Dependency.Complete();

        Job.WithoutBurst().WithCode(() =>
        {
            foreach (Vector2Int N in AllPathNodes)
            {
                GameObject Go = Object.Instantiate(TextPrefab);
                Vector3 Pos = new Vector3(N.x * WorldConstants.REAL_WORLD_SCALE, 0.0f, N.y * -1.0f * WorldConstants.REAL_WORLD_SCALE);
                TMP_Text T = Go.GetComponent<TMP_Text>();
                T.text = $"{N.x},{N.y}";
                Go.transform.position = Pos;
            }
        }).Run();
    }

    private void SpawnPathParts(SpawnPathEvent Event, NativeList<Vector2Int> Path, NativeList<Vector2Int> BranchPoints, NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes, NativeList<Vector2Int> MainPath, NativeArray<Vector2Int> PenultimateNodes, bool ProcessBranchPoints = true)
    {
        NativeList<Vector2Int> PathBranchPoints = new NativeList<Vector2Int>(0, Allocator.TempJob);
        NativeList<Vector2Int> UnionOfPaths = new NativeList<Vector2Int>(0, Allocator.TempJob);

        Dependency = Job.WithCode(() =>
        {
            PathBranchPoints.AddRange(BranchPoints);
            UnionOfPaths.AddRange(Path);

            if (Path.Length > 0 && MainPath.Length > 0)
            {
                UnionOfPaths.RemoveAt(UnionOfPaths.Length - 1);
                UnionOfPaths.AddRange(MainPath);
            }

            // Add adjacent chunk's nodes for the main path
            if ((!ProcessBranchPoints && Path.Length > 0) || (ProcessBranchPoints && Path.Length > 0 && (Path[^1].x % (WorldConstants.GRID_WIDTH - 1) == 0 || Path[^1].y % (WorldConstants.GRID_WIDTH - 1) == 0)))
            {
                // For the first chunk, we have no previous chunks to go on
                if (Event.ChunkID != 1)
                {
                    Path.InsertRangeWithBeginEnd(0, 1);
                    Path[0] = new Node(Path[1] + new Vector2Int(Event.DirectionOfPreviousChunk.x, Event.DirectionOfPreviousChunk.y * -1));
                }

                Path.Add(new Node(Path[^1] - (Event.DirectionOfNextChunk * new Vector2Int(-1, 1))));
            }

        }).Schedule(Dependency);


        SpawnPathSegmentsJob SpawnSegmentsJob = new SpawnPathSegmentsJob()
        {
            Writer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            Prefabs = LevelPrefabs,
            PathCoords = Path,
            BranchPoints = PathBranchPoints,
            UnionOfPathAndMainPath = UnionOfPaths,
            DoBranchPoints = ProcessBranchPoints,
            PenultimateNodes = PenultimateNodes,

            SpawnOffset = new Vector3Int(Event.SpawnOffset.x, 0, Event.SpawnOffset.y),
            ThisChunkID = Event.ChunkID,

            // Only conscider the other chunks when creating the main path
            DirectionOfPreviousChunk = ProcessBranchPoints ? Vector2Int.zero : Event.DirectionOfPreviousChunk,
            DirectionOfNextChunk = ProcessBranchPoints ? Vector2Int.zero : Event.DirectionOfNextChunk,

            ForwardNodes = ForwardNodes,
        };

        Dependency = SpawnSegmentsJob.Schedule(Path, 8, Dependency);



        PathBranchPoints.Dispose(Dependency);
        UnionOfPaths.Dispose(Dependency);
    }

    private static SetupFindBranchJob job;

    private struct SetupFindBranchJob : IJob
    {
        public NativeArray<Vector2Int> KeyPoints;
        public NativeArray<Node> AllNodes;
        public NativeList<Vector2Int> Blockers;
        public NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes;
        public NativeArray<Unity.Mathematics.Random> Randoms;

        [ReadOnly] public NativeList<Vector2Int> OriginalPath;
        [ReadOnly] public NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes;

        public void Execute()
        {
            job = this;

            Unity.Mathematics.Random Rand = Randoms[0];

            AllNodes.Where(x => x.isBlocker).Select(x => x.Coord).ForEach(x => job.Blockers.Add(x));


            // START NODE
            NativeList<Vector2Int> ValidStartNodes = new NativeList<Vector2Int>(Allocator.Temp);
            for (int i = 3; i < OriginalPath.Length - 4; i++)
            {
                if (ForwardNodes.CountValuesForKey(OriginalPath[i]) < 3)
                {
                    ValidStartNodes.Add(OriginalPath[i]);
                }
            }

            if (ValidStartNodes.Length < 1)
            {
                ValidStartNodes.Dispose();
                KeyPoints[0] = new Vector2Int(-1, -1);
                return;
            }

            // Select a node to branch from
            int NodeIndex = Rand.NextInt(ValidStartNodes.Length);
            int StartNodeIndex = NodeIndex;
            Node StartNode = AllNodes.GetNodeQuick(ValidStartNodes[NodeIndex]);



            ValidStartNodes.Dispose();



            // MIDDLE NODE

            NativeParallelHashSet<Vector2Int> PathHashSet = new NativeParallelHashSet<Vector2Int>(OriginalPath.Length, Allocator.Temp);
            for (int i = 0; i < OriginalPath.Length; i++)
            {
                PathHashSet.Add(OriginalPath[i]);
            }


            NativeList<Vector2Int> NotInPath = new NativeList<Vector2Int>(AllNodes.Length - OriginalPath.Length, Allocator.Temp);
            for (int i = 0; i < AllNodes.Length; i++)
            {
                if (!PathHashSet.Contains(AllNodes[i]))
                {
                    NotInPath.Add(AllNodes[i]);
                }
            }

            if (NotInPath.Length < 1)
            {
                PathHashSet.Dispose();
                NotInPath.Dispose();
                KeyPoints[0] = new Vector2Int(-1, -1);
                return;
            }

            // Branch towards this point
            NodeIndex = Rand.NextInt(NotInPath.Length);
            Node TargetNode = AllNodes.GetNodeQuick(NotInPath[NodeIndex]);


            NotInPath.Dispose();
            PathHashSet.Dispose();



            // RETURN NODE

            // Return to this point on the path
            NativeList<Vector2Int> ValidReturnNodes = new NativeList<Vector2Int>(Allocator.Temp);
            for (int i = StartNodeIndex; i < OriginalPath.Length; i++)
            {
                if (Vector2Int.Distance(TargetNode, OriginalPath[i]) < 30)
                {
                    ValidReturnNodes.Add(OriginalPath[i]);
                }
            }

            if (ValidReturnNodes.Length < 1)
            {
                ValidReturnNodes.Dispose();
                KeyPoints[0] = new Vector2Int(-1, -1);
                return;
            }

            NodeIndex = Rand.NextInt(ValidReturnNodes.Length);
            Node ReturnNode = AllNodes.GetNodeQuick(ValidReturnNodes[NodeIndex]);


            ValidReturnNodes.Dispose();





            AllNodes.ForEach(x => x.isBlocker = false);
            AllNodes.ForEach(x => job.BackwardNodes[x] = Node._defaultInvalid);
            for (int i = 0; i < Blockers.Length; i++)
            {
                Node x = AllNodes.GetNodeQuick(Blockers[i]);
                x.isBlocker = false;
                AllNodes[AllNodes.GetNodeIndex(Blockers[i])] = x;
            }

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


            Randoms[0] = Rand;


            KeyPoints[0] = NewStartNode;
            KeyPoints[1] = NewTargetNode;
            KeyPoints[2] = NewReturnNodeNode;

        }
    }

    private void FindPathBranch(NativeList<Vector2Int> FullBranchPath, NativeArray<Vector2Int> KeyPoints, NativeArray<Node> AllNodes, NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes, NativeParallelHashMap<Vector2Int, Vector2Int> BackwardNodes, NativeList<Vector2Int> OriginalPath, NativeList<Vector2Int> Blockers, NativeArray<Vector2Int> PenultimateNodes, NativeArray<Unity.Mathematics.Random> Rand)
    {
        SetupFindBranchJob SetupJob = new SetupFindBranchJob()
        {
            KeyPoints = KeyPoints,
            AllNodes = AllNodes,
            OriginalPath = OriginalPath,
            Blockers = Blockers,
            Randoms = Rand,
            ForwardNodes = ForwardNodes,
            BackwardNodes = BackwardNodes,
        };

        Dependency = SetupJob.Schedule(Dependency);


        // Find first half
        NativeList<Vector2Int> BranchPathFirstHalf = new NativeList<Vector2Int>(Allocator.TempJob);
        FindPathJob(BranchPathFirstHalf, AllNodes, BackwardNodes, KeyPoints);

        Dependency = Job.WithCode(() =>
        {
            if (BranchPathFirstHalf.Length < 1)
            {
                return;
            }

            // Block out the first half
            for (int i = 0; i < BranchPathFirstHalf.Length; i++)
            {
                Node CopyOfNode = AllNodes.GetNodeQuick(BranchPathFirstHalf[i]);
                CopyOfNode.isBlocker = true;
                AllNodes[AllNodes.GetNodeIndex(BranchPathFirstHalf[i])] = CopyOfNode;
            }

            Node TargetNode = AllNodes.GetNodeQuick(KeyPoints[1]);
            TargetNode.isBlocker = true;
            AllNodes[AllNodes.GetNodeIndex(KeyPoints[1])] = TargetNode;

        }).Schedule(Dependency);


        // Find second Half
        NativeList<Vector2Int> BranchPathSecondHalf = new NativeList<Vector2Int>(Allocator.TempJob);
        FindPathJob(BranchPathSecondHalf, AllNodes, BackwardNodes, KeyPoints, 1, 2);

        Dependency = Job.WithCode(() =>
        {
            if (BranchPathFirstHalf.Length < 1 || BranchPathSecondHalf.Length < 1)
            {
                return;
            }

            // Remove the target node from the first list to prevent duplicates
            BranchPathFirstHalf.RemoveAt(BranchPathFirstHalf.Length - 1);

            // Join the halves
            NativeCollectionsUtilities.CombineNativeLists(FullBranchPath, BranchPathFirstHalf, BranchPathSecondHalf);

            // Set branch in start node
            ForwardNodes.Add(KeyPoints[0], FullBranchPath[1]);

            // Set forward node in the rest of the path
            for (int i = 1; i < FullBranchPath.Length; i++)
            {
                Vector2Int ForwardNode = i < FullBranchPath.Length - 1 ?
                    FullBranchPath[i + 1] :
                    ForwardNodes.TryGetFirstValue(FullBranchPath[i], out Vector2Int Item, out _) ? Item : Node._defaultInvalid;

                ForwardNodes.Add(FullBranchPath[i], ForwardNode);
            }
        }).Schedule(Dependency);


        // Nodes before the reconvention
        Dependency = Job.WithCode(() =>
        {
            if (FullBranchPath.Length < 1)
            {
                return;
            }

            PenultimateNodes[0] = OriginalPath[OriginalPath.IndexOf(KeyPoints[0]) + 1];
            PenultimateNodes[1] = FullBranchPath[1];
            PenultimateNodes[2] = FullBranchPath[^2];
            PenultimateNodes[3] = OriginalPath[OriginalPath.IndexOf(KeyPoints[2]) - 1];
        }).Schedule(Dependency);


        BranchPathFirstHalf.Dispose(Dependency);
        BranchPathSecondHalf.Dispose(Dependency);
    }

    private class EventSorter : IComparer<SpawnPathEvent>
    {
        public int Compare(SpawnPathEvent x, SpawnPathEvent y)
        {
            return x.ChunkID.CompareTo(y.ChunkID);
        }
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter Writer = Ecbs.CreateCommandBuffer().AsParallelWriter();

        Dependency = Entities.ForEach((Entity E, in SpawnPathEvent Event) =>
        {
            Writer.DestroyEntity(0, E);
        }).ScheduleParallel(Dependency);

        Dependency.Complete();

        EntityQuery EventsQuery = EntityManager.CreateEntityQuery(typeof(SpawnPathEvent));
        NativeArray<SpawnPathEvent> Events = EventsQuery.ToComponentDataArray<SpawnPathEvent>(Allocator.Temp);

        Events.Sort(new EventSorter());

        for (int i = 0; i < Events.Length; i++)
        {
            Vector3Int SpawnOffset = new Vector3Int(Events[i].ChunkCoord.x * WorldConstants.GRID_WIDTH * WorldConstants.REAL_WORLD_SCALE, 0, Events[i].ChunkCoord.y * WorldConstants.GRID_WIDTH * WorldConstants.REAL_WORLD_SCALE);
            int CurrentChunkID = Events[i].ChunkID;
            CreatePath(Events[i]);
        }
    }


    public struct SpawnPathSegmentsJob : IJobParallelForDefer
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        [ReadOnly] public NativeArray<Entity> Prefabs;
        [ReadOnly] public NativeList<Vector2Int> PathCoords;
        [ReadOnly] public NativeList<Vector2Int> BranchPoints;
        [ReadOnly] public NativeList<Vector2Int> UnionOfPathAndMainPath;
        [ReadOnly] public NativeArray<Vector2Int> PenultimateNodes;
        [ReadOnly] public bool DoBranchPoints;
        [ReadOnly] public Vector3Int SpawnOffset;
        [ReadOnly] public int ThisChunkID;
        [ReadOnly] public Vector2Int DirectionOfPreviousChunk;
        [ReadOnly] public Vector2Int DirectionOfNextChunk;
        [ReadOnly] public NativeParallelMultiHashMap<Vector2Int, Vector2Int> ForwardNodes;

        [BurstDiscard]
        public void Execute(int index)
        {
            if (PathCoords.Length < 1)
            {
                return;
            }

            // For the main path we add the first nodes from adjacent chunks to the beginning and end of the list
            // This makes the prefab spawning much easier, but we don't want to actually create prefabs for these
            // Segments in different chunks
            if ((index == 0 || index == PathCoords.Length - 1) && IsNotInChunk(PathCoords[index]))
            {
                return;
            }


            if (!DoBranchPoints && BranchPoints.Contains(PathCoords[index]))
            {
                return;
            }

            float3 SpawnPos = new float3()
            {
                x = SpawnOffset.x + (PathCoords[index].x * WorldConstants.REAL_WORLD_SCALE),
                y = -2.0f,
                z = SpawnOffset.z + (PathCoords[index].y * WorldConstants.REAL_WORLD_SCALE * -1.0f) // so the graph is drawn from top left instead of bottom left
            };


            // Get segment data
            bool Corner = IsCorner(PathCoords, index);
            int PrefabIndex = GetSegmentPrefab(PathCoords, BranchPoints, index, Corner);
            float3 Rotation = GetSegmentRotation(PathCoords, index, PrefabIndex);


            // Write segment data
            Entity SpawnedEntity = Writer.Instantiate(index, Prefabs[PrefabIndex]);
            Writer.SetComponent(index, SpawnedEntity, new Translation() { Value = SpawnPos });
            Writer.SetComponent(index, SpawnedEntity, new Rotation() { Value = quaternion.LookRotation(Rotation, new float3(0, 1, 0)) });
            AddSharedComponent(index, SpawnedEntity);


            if (PenultimateNodes[0] == default)
            {
                return;
            }

            // If we're after branch node add a tag to block access to the other branch
            for (int i = 0; i < 2; i++)
            {
                if (PenultimateNodes[i].Equals(PathCoords[index]))
                {
                    Writer.AddComponent(index, SpawnedEntity, new DestroyLevelSegmentComponent()
                    {
                        Value = PenultimateNodes[i + 2]
                    });
                }
            }
        }

        [BurstCompile]
        private bool IsNotInChunk(Vector2Int Coord)
        {
            return Coord.x < 0 || Coord.y < 0 || Coord.x > WorldConstants.GRID_WIDTH - 1 || Coord.y > WorldConstants.GRID_WIDTH - 1;
        }

        [BurstCompile]
        private bool IsCorner(NativeArray<Vector2Int> Positions, int index)
        {
            Vector2Int Difference = new Vector2Int();
            if (index > 0 && index < Positions.Length - 1)
            {
                // Difference in grid co-ordinates between the previous and next nodes
                Difference = Positions[index + 1] - Positions[index - 1];
            }

            return Difference.x != 0 && Difference.y != 0;
        }

        [BurstCompile]
        private int GetSegmentPrefab(NativeArray<Vector2Int> Positions, NativeArray<Vector2Int> BranchPoints, int index, bool CornerPiece)
        {
            if (BranchPoints.Contains(Positions[index]))
            {
                return 3;
            }

            if (CornerPiece)
            {
                Vector2Int PrevPos = Positions[index - 1];
                Vector2Int CurrentPos = Positions[index];
                Vector2Int NextPos = Positions[index + 1];

                // Determine which side of the vector the point lies on
                bool RightTurn = (((CurrentPos.x - PrevPos.x) * (NextPos.y - PrevPos.y)) - ((CurrentPos.y - PrevPos.y) * (NextPos.x - PrevPos.x))) > 0;

                return RightTurn ? 1 : 2;
            }
            else
            {
                return 0;
            }
        }

        [BurstCompile]
        private float3 GetSegmentRotation(NativeArray<Vector2Int> PathCoords, int Index, int PrefabIndex)
        {
            float3 Rotation = new float3(0.0f, 0.0f, 1.0f);
            if (PrefabIndex != 3 && Index < 1)
            {
                Index++;
            }

            // T Junction
            if (PrefabIndex == 3)
            {
                Vector2Int CurrentCoord = PathCoords[Index];

                NativeArray<Vector2Int> Neighbours = new NativeArray<Vector2Int>(4, Allocator.Temp);
                Neighbours[0] = new Vector2Int(CurrentCoord.x, CurrentCoord.y - 1);
                Neighbours[1] = new Vector2Int(CurrentCoord.x + 1, CurrentCoord.y);
                Neighbours[2] = new Vector2Int(CurrentCoord.x, CurrentCoord.y + 1);
                Neighbours[3] = new Vector2Int(CurrentCoord.x - 1, CurrentCoord.y);

                Vector2Int ClosedDirection = Vector2Int.zero;

                for (int i = 0; i < Neighbours.Length; i++)
                {
                    bool NodePointsToCurrent = false;

                    // If it's in the path but not the currect chunk then we must be adjacent & connected to it
                    // This is because there is only one node from the next chunk in the path, and it is the next
                    // Node to be traversed
                    if (IsNotInChunk(Neighbours[i]) && PathCoords.NativeAny(x => x == Neighbours[i]))
                    {
                        NodePointsToCurrent = true;
                    }

                    // Check if current node points forwards to it
                    if (!NodePointsToCurrent)
                    {
                        foreach (Vector2Int item in ForwardNodes.GetValuesForKey(CurrentCoord))
                        {
                            if (item == Neighbours[i])
                            {
                                NodePointsToCurrent = true;
                                break;
                            }
                        }
                    }

                    if (!NodePointsToCurrent)
                    {
                        // Check if neighbour points forward to current node
                        foreach (Vector2Int item in ForwardNodes.GetValuesForKey(Neighbours[i]))
                        {
                            if (item == CurrentCoord)
                            {
                                NodePointsToCurrent = true;
                                break;
                            }
                        }
                    }

                    if (!NodePointsToCurrent)
                    {
                        ClosedDirection = Neighbours[i] - CurrentCoord;
                        break;
                    }
                }

                Rotation = new float3(ClosedDirection.x, 0.0f, ClosedDirection.y * -1);

                Neighbours.Dispose();
                return Rotation;
            }
            // Corner
            else if (PrefabIndex is 1 or 2)
            {
                Vector2Int BeforeToThis = PathCoords[Index] - PathCoords[Index - 1];
                Rotation = new float3(BeforeToThis.x, 0.0f, -BeforeToThis.y);
                return Rotation;
            }
            // Straight
            else if (PrefabIndex == 0)
            {
                Vector2Int Difference = PathCoords[Index] - PathCoords[Index - 1];
                Rotation = new float3(math.abs(Difference.y), 0.0f, math.abs(Difference.x));
                return Rotation;
            }

            return Rotation;
        }

        [BurstDiscard]
        private void AddSharedComponent(int index, Entity SpawnedEntity)
        {
            Writer.AddSharedComponent(index, SpawnedEntity, new ChunkSharedComponent() { ChunkID = ThisChunkID });
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