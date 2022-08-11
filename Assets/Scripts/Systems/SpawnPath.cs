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
using TMPro;

public partial class SpawnPath : SystemBase
{

    enum Direction {NONE, STRAIGHT, LEFT, RIGHT};

    static readonly int REAL_WORLD_SCALE = 20;
    static readonly int GRID_SIZE = 64;

    NativeArray<Entity> LevelPrefabs;

    GameObject TextPrefab;

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

        RequireSingletonForUpdate<PlayerTag>();
    }

    private void CreatePath()
    {
        uint Seed = (uint)new System.Random().Next();// 116478577u;
        Debug.Log($"Seed: {Seed}");

        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(Seed);


        Node[] AllNodes = new Node[0];

        // Find a path through a random maze (max 5 attempts)
        Node[] Path = FindPath(ref AllNodes, ref Rand);

        // We failed to find a path
        if(Path.Length < 1)
        {
            return;
        }

        for (int i = 0; i < Path.Length; i++)
        {
            Path[i].backPtr = null;
            Path[i].forwardNodes[0] = i < Path.Length - 1 ? Path[i+1] : null;
        }

        Node[][] Branches = FindPathBranches(100, AllNodes, Path, ref Rand);

        // Path = Path.Union(Branches.SelectMany(x => x)).Distinct().ToArray();

        Dictionary<int, Color> BranchColours = new Dictionary<int, Color>();
        Node[][] ActualBranches = Branches.Where(x => x.Length > 0).ToArray();
        for (int i = 0; i < ActualBranches.Length; i++)
        {
            int ColourValue = 255 / ActualBranches.Length * i;
            BranchColours[i] = new Color(ColourValue, ColourValue, ColourValue);
        }


        ObservableHashSet<Node> TraversedNodes = new ObservableHashSet<Node>();
        Visualiser Vis = new Visualiser(GRID_SIZE);

        TraversedNodes.Register((x) => 
        {
            if(x.operation == CollectionModifiedEventEnum.ADDED)
            {
                int ColourIndex = System.Array.IndexOf(Branches, Branches.FirstOrDefault(y => y.Contains(x.added)));
                Vis.EnqueueInstruction((x.added.Coord, BranchColours.ContainsKey(ColourIndex) ? BranchColours[ColourIndex] : Color.red));
            }
        });

        TraverseNodes(Path[0], ref TraversedNodes);

        Vis.Playback();

        Debug.Assert(Path.Length == TraversedNodes.Count);

        IEnumerable<Vector2Int> BranchPoints = Branches.Where(x => x.Length > 0).SelectMany(x => new Node[] {x.First(), x.Last()}).ToArray().Select(x => x.Coord);

        // Spawn the main path (excluding intersections)
        SpawnPathParts(Path, BranchPoints, false);

        // Spawn all branches and intersections
        foreach (Node[] Branch in Branches)
        {
            if(Branch == null || Branch.Length < 1)
            {
                continue;
            }

            SpawnPathParts(Branch, BranchPoints, true, Path);
        }

        

        // Dependency = Entities.WithAll<PlayerTag>().ForEach((ref Translation Trans) =>
        // {
        //     Trans.Value = new float3(Path[0].Coord.x * REAL_WORLD_SCALE, 3.0f, Path[0].Coord.y * REAL_WORLD_SCALE * -1);
        // }).Schedule(Dependency);


        Dependency.Complete();



        // Add coord numbers
        Job.WithoutBurst().WithCode(() => 
        {
            foreach (Node N in Path.Union(Branches.SelectMany(x => x)))
            {
                GameObject Go = MonoBehaviour.Instantiate(TextPrefab);
                Vector3 Pos = new Vector3(N.Coord.x * REAL_WORLD_SCALE, 0.0f, N.Coord.y * -1.0f * REAL_WORLD_SCALE);
                TMP_Text T = Go.GetComponent<TMP_Text>();
                T.text = $"{N.Coord.x},{N.Coord.y}";
                Go.transform.position = Pos;
            }
        }).Run();


    }

    private void SpawnPathParts(IEnumerable<Node> Path, IEnumerable<Vector2Int> BranchPoints, bool ProcessBranchPoints = true, IEnumerable<Node> MainPath = null)
    {
        // Job #1 Spawn prefabs for each path tile

        Vector2Int[] PathPositionsManaged = Path.Select(x => x.Coord * REAL_WORLD_SCALE).ToArray();

        if(ProcessBranchPoints)
        {
            PathPositionsManaged = PathPositionsManaged.Union(MainPath.Select(x => x.Coord * REAL_WORLD_SCALE)).ToArray();
        }

        NativeArray<Vector2Int> PathPositions = new NativeArray<Vector2Int>(PathPositionsManaged, Allocator.Persistent);
        NativeArray<Vector2Int> PathBranchPoints = new NativeArray<Vector2Int>(BranchPoints.ToArray(), Allocator.Persistent);

        SpawnPathSegments SpawnSegmentsJob = new SpawnPathSegments()
        {
            Writer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            Prefabs = LevelPrefabs,
            Positions = PathPositions,
            BranchPoints = PathBranchPoints,
            DoBranchPoints = ProcessBranchPoints
        };

        SpawnSegmentsJob.Run(Path.Count());
        // Dependency = SpawnSegmentsJob.Schedule(Path.Count(), 8, Dependency);

        PathPositions.Dispose(Dependency);
        PathBranchPoints.Dispose(Dependency);
    }

    private Node[] FindPath(ref Node[] AllNodes, ref Unity.Mathematics.Random Rand, int MaxAttempts = 5)
    {
        int attempts = 0;
        Node[] Path = new Node[0];
        IEnumerable<Node> Walls = new Node[0];
        while (Path.Length < 1 && attempts++ < MaxAttempts)
        {
            AllNodes = GridLayouts.RandomBlockers.GenerateGrid(GRID_SIZE, ref Rand);

            IEnumerable<Node> EdgeNodes = AllNodes.Where(x => x.Coord.x == 0 || x.Coord.y == 0 || x.Coord.x == GRID_SIZE - 1 || x.Coord.y == GRID_SIZE - 1);

            AStar PathFinder = new AStar(AllNodes, EdgeNodes.ChooseRandom(ref Rand), EdgeNodes.ChooseRandom(ref Rand));
            Path = PathFinder.Execute().ToArray();
        }

        return Path;
    }

    Node[][] FindPathBranches(int Branches, Node[] AllNodes, Node[] OriginalPath, ref Unity.Mathematics.Random Rand)
    {
        Node[][] RetArray = new Node[Branches][];

        IEnumerable<Node> Blockers = OriginalPath;

        for (int i = 0; i < Branches; i++)
        {
            int attempts = 1;
            while((RetArray[i] = FindPathBranch(AllNodes, OriginalPath, Blockers, ref Rand)).Length < 1)
            {
                if(attempts++ > 10)
                {
                    break;
                }
            }

            if (RetArray[i].Length > 0)
            {
                Blockers = Blockers.Union(RetArray[i]).ToArray();
            }
        }

        return RetArray;
    }

    Node[] FindPathBranch(Node[] AllNodes, Node[] OriginalPath, IEnumerable<Node> Blockers, ref Unity.Mathematics.Random Rand)
    {
        IEnumerable<Node> TrimmedPath = OriginalPath.Skip(3).SkipLast(3);

        // Select a node to branch from
        Node StartNode = TrimmedPath.Where(x => x.forwardNodes.Any(y => y == null)).ChooseRandom(ref Rand);

        // Branch towards this point
        Node TargetNode = AllNodes.Except(OriginalPath).Where(x => !x.isBlocker && Vector2Int.Distance(StartNode.Coord, x.Coord) < 10).ChooseRandom(ref Rand);

        // Return to this point on the path
        Node ReturnNode = TrimmedPath.SkipWhile(x => x != StartNode).Where(x => Vector2Int.Distance(x.Coord, TargetNode.Coord) < 30 ).ChooseRandom(ref Rand);

        AllNodes.ForEach(x => x.isBlocker = false);
        AllNodes.ForEach(x => x.backPtr = null);
        AllNodes.Intersect(Blockers).ForEach(x => x.isBlocker = true);
        StartNode.isBlocker = false;
        TargetNode.isBlocker = false;
        ReturnNode.isBlocker = false;

        // Find first half
        IEnumerable<Node> BranchPathFirstHalf = new AStar(AllNodes, StartNode, TargetNode).Execute();
        if(BranchPathFirstHalf.Count() < 1)
        {
            return new Node[0];
        }

        // Block out the first half
        AllNodes.Intersect(BranchPathFirstHalf).ForEach(x => x.isBlocker = true);
        TargetNode.isBlocker = false;

        // Find second Half
        IEnumerable<Node> BranchPathSecondHalf = new AStar(AllNodes, TargetNode, ReturnNode).Execute();
        if(BranchPathSecondHalf.Count() < 1)
        {
            return new Node[0];
        }
        
        // Join the halves
        IEnumerable<Node> FullBranchPath = BranchPathFirstHalf.Union(BranchPathSecondHalf).Distinct();

        // Set branch in start node
        for (int i = 0; i < StartNode.forwardNodes.Length; i++)
        {
            if(StartNode.forwardNodes[i] == null)
            {
                StartNode.forwardNodes[i] = FullBranchPath.ElementAt(1);
                break;
            }
        }

        // Set forward node in the rest of the path
        for (int i = 1; i < FullBranchPath.Count(); i++)
        {
            FullBranchPath.ElementAt(i).forwardNodes[0] = i < FullBranchPath.Count() - 1 ? FullBranchPath.ElementAt(i+1) : FullBranchPath.ElementAt(i).forwardNodes[0];
        }

        return FullBranchPath.ToArray();
    }

    protected override void OnUpdate()
    {    
        SetComponent(GetSingletonEntity<PlayerTag>(), new Translation()
        {
            Value = new float3(0.0f * REAL_WORLD_SCALE, 3.0f, 0.0f)
        });

        Enabled = false;
    }

    protected override void OnStopRunning()
    {
        if(LevelPrefabs.IsCreated)
        {
            LevelPrefabs.Dispose();
        }
    }

    // [BurstCompile]
    public struct SpawnPathSegments : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        [ReadOnly] public NativeArray<Entity> Prefabs;
        [ReadOnly] public NativeArray<Vector2Int> Positions;
        [ReadOnly] public NativeArray<Vector2Int> BranchPoints;
        [ReadOnly] public bool DoBranchPoints;

        public void Execute(int index)
        {
            if(!DoBranchPoints && BranchPoints.Contains(Positions[index] / REAL_WORLD_SCALE))
            {
                return;
            }


            float3 SpawnPos = new float3()
            {
                x = Positions[index].x,
                y = -2.0f,
                z = Positions[index].y * -1.0f // so the graph is drawn from top left instead of bottom left
            };

            
            IsCorner(Positions, index, out bool Corner);
            GetSegmentPrefab(Positions, BranchPoints, index, Corner, out int PrefabIndex);
            GetSegmentRotation(Positions, index, PrefabIndex, out float3 Rotation);

            if(PrefabIndex == -1)
            {
                return;
            }

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

        private void GetSegmentPrefab(NativeArray<Vector2Int> Positions, NativeArray<Vector2Int> BranchPoints, int index, bool CornerPiece, out int PrefabIndex)
        {
            if(BranchPoints.Contains(Positions[index] / REAL_WORLD_SCALE))
            {
                PrefabIndex = 3;
                return;
            }

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

        private void GetSegmentRotation(NativeArray<Vector2Int> Positions, int index, int PrefabIndex, out float3 Rotation)
        {
            Rotation = new float3(0.0f, 0.0f, 1.0f);
            if(PrefabIndex != 3 && index < 1)
            {
                index++;
            }

            // T
            if(PrefabIndex == 3)
            {
                Vector2Int CurrentCoord = Positions[index] / REAL_WORLD_SCALE;
                IEnumerable<Vector2Int> AllNeighbours = AStar.GetNeighbours(Positions[index] / REAL_WORLD_SCALE, GRID_SIZE);
                IEnumerable<Vector2Int> PathNeighbours = AllNeighbours.Where(x => Positions.Contains(x * REAL_WORLD_SCALE));
                IEnumerable<Vector2Int> Neighbours = PathNeighbours.Select(x => x - CurrentCoord);
                Vector2Int ClosedDirection = Vector2Int.zero;
                if(Neighbours.Sum(x => x.x) != 0)
                {
                    ClosedDirection = Neighbours.FirstOrDefault(x => x.x != 0) * -1;
                }
                else
                {
                    ClosedDirection = Neighbours.FirstOrDefault(x => x.y != 0);
                }

                // (0, -1) == T
                // (-1, 0) == |-
                // (0, 1) == _|_
                // (1, 0) == -|
                // Rotation = new float3(-1, 0, 0);
                // return;

                Rotation = new float3(ClosedDirection.x, 0.0f, ClosedDirection.y );
            }
            // Corner
            else if(PrefabIndex == 1 || PrefabIndex == 2)
            {
                Vector2Int BeforeToThis = Positions[index] / REAL_WORLD_SCALE - Positions[index - 1] / REAL_WORLD_SCALE;
                Rotation = new float3(BeforeToThis.x, 0.0f, -BeforeToThis.y);
            }
            // Straight
            else if(PrefabIndex == 0)
            {
                Vector2Int Difference = Positions[index] / REAL_WORLD_SCALE - Positions[index - 1] / REAL_WORLD_SCALE;
                Rotation = new float3(math.abs(Difference.y), 0.0f, math.abs(Difference.x));
            }
        }
    }

    private void TraverseNodes(Node Start, ref ObservableHashSet<Node> TraversedNodes)
    {
        TraversedNodes.Add(Start);
        
        for (int i = 0; i < Start.forwardNodes.Length; i++)
        {
            if(Start.forwardNodes[i] != null && !TraversedNodes.Contains(Start.forwardNodes[i]))
            {
                TraverseNodes(Start.forwardNodes[i], ref TraversedNodes);
            }
        }
    }
}