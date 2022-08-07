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
    }

    private void CreatePath()
    {
        uint Seed = 1880642439u; //(uint)new System.Random().Next();
        Debug.Log($"Seed: {Seed}");

        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(Seed);


        Node[] AllNodes = GridLayouts.RandomBlockers.GenerateGrid(64, Rand);

        // Find a path through a random maze (max 5 attempts)
        Node[] Path = FindPath(ref AllNodes, Rand);

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

        Node[][] Branches = FindPathBranches(10, AllNodes, Path, Rand);

        Path = Path.Union(Branches.SelectMany(x => x)).Distinct().ToArray();

        ObservableHashSet<Node> TraversedNodes = new ObservableHashSet<Node>();
        Visualiser Vis = new Visualiser(64);

        TraversedNodes.Register((x) => 
        {
            if(x.operation == CollectionModifiedEventEnum.ADDED)
            {
                Vis.EnqueueInstruction((x.added.Coord, Color.red));
            }
        });

        TraverseNodes(Path[0], ref TraversedNodes);

        Vis.Playback();

        Debug.Assert(Path.Length == TraversedNodes.Count);

        // Job #1 Spawn prefabs for each path tile
        NativeArray<Vector2Int> PathPositions = new NativeArray<Vector2Int>(Path.Select(x => x.Coord * REAL_WORLD_SCALE).ToArray(), Allocator.Persistent);
        NativeArray<Vector2Int> PathBranchPoints = new NativeArray<Vector2Int>(Branches.Where(x => x.Length > 0).SelectMany(x => new Node[] {x.First(), x.Last()}).ToArray().Select(x => x.Coord).ToArray(), Allocator.Persistent);

        SpawnPathSegments SpawnSegmentsJob = new SpawnPathSegments()
        {
            Writer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            Prefabs = LevelPrefabs,
            Positions = PathPositions,
            BranchPoints = PathBranchPoints
        };

        Dependency = SpawnSegmentsJob.Schedule(Path.Length, 8, Dependency);

        Dependency = Entities.WithAll<PlayerTag>().ForEach((ref Translation Trans) =>
        {
            Trans.Value = new float3(PathPositions[0].x, 3.0f, PathPositions[0].y * -1);
        }).Schedule(Dependency);


        Dependency.Complete();

        PathPositions.Dispose();
        PathBranchPoints.Dispose();



        // Add coord numbers
        Job.WithoutBurst().WithCode(() => 
        {
            foreach (Node N in Path)
            {
                GameObject Go = MonoBehaviour.Instantiate(TextPrefab);
                Vector3 Pos = new Vector3(N.Coord.x * REAL_WORLD_SCALE, 0.0f, N.Coord.y * -1.0f * REAL_WORLD_SCALE);
                TMP_Text T = Go.GetComponent<TMP_Text>();
                T.text = $"{N.Coord.x},{N.Coord.y}";
                Go.transform.position = Pos;
            }
        }).Run();


    }

    private Node[] FindPath(ref Node[] AllNodes, Unity.Mathematics.Random Rand, int MaxAttempts = 5)
    {
        int attempts = 0;
        Node[] Path = new Node[0];
        IEnumerable<Node> Walls = new Node[0];
        while (Path.Length < 1 && attempts++ < MaxAttempts)
        {
            AllNodes = GridLayouts.RandomBlockers.GenerateGrid(64, Rand);

            AStar PathFinder = new AStar(AllNodes, AllNodes[0], AllNodes[AllNodes.Length - 1]);
            Path = PathFinder.Execute().ToArray();
        }

        return Path;
    }

    Node[][] FindPathBranches(int Branches, Node[] AllNodes, Node[] OriginalPath, Unity.Mathematics.Random Rand)
    {
        Node[][] RetArray = new Node[Branches][];
        for (int i = 0; i < Branches; i++)
        {
            int attempts = 1;
            while((RetArray[i] = FindPathBranch(AllNodes, OriginalPath, Rand)).Length < 1)
            {
                if(attempts++ > 5)
                {
                    break;
                }
            }
        }

        return RetArray;
    }

    Node[] FindPathBranch(Node[] AllNodes, Node[] OriginalPath, Unity.Mathematics.Random Rand)
    {
        IEnumerable<Node> TrimmedPath = OriginalPath.Skip(3).SkipLast(3);

        // Select a node to branch from
        Node StartNode = TrimmedPath.Where(x => x.forwardNodes.Any(y => y == null)).ChooseRandom(Rand);

        // Branch towards this point
        Node TargetNode = AllNodes.Except(OriginalPath).Where(x => !x.isBlocker && Vector2Int.Distance(x.Coord, StartNode.Coord) < 10).ChooseRandom(Rand);

        // Return to this point on the path
        Node ReturnNode = TrimmedPath.SkipWhile(x => x != StartNode)/*.Where(x => Vector2Int.Distance(x.Coord, TargetNode.Coord) < 30)*/.ChooseRandom(Rand);

        AllNodes.ForEach(x => x.isBlocker = false);
        AllNodes.ForEach(x => x.backPtr = null);
        AllNodes.Intersect(OriginalPath).ForEach(x => x.isBlocker = true);
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
        [ReadOnly] public NativeArray<Vector2Int> Positions;
        [ReadOnly] public NativeArray<Vector2Int> BranchPoints;

        public void Execute(int index)
        {
            float3 SpawnPos = new float3()
            {
                x = Positions[index].x,
                y = -2.0f,
                z = Positions[index].y * -1.0f // so the graph is drawn from top left instead of bottom left
            };

            
            IsCorner(Positions, index, out bool Corner);
            GetSegmentPrefab(Positions, BranchPoints, index, Corner, out int PrefabIndex);
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

        private void GetSegmentPrefab(NativeArray<Vector2Int> Positions, NativeArray<Vector2Int> BranchPoints, int index, bool CornerPiece, out int PrefabIndex)
        {
            if(CornerPiece)
            {
                Vector2Int PrevPos = Positions[index - 1] / REAL_WORLD_SCALE;
                Vector2Int CurrentPos = Positions[index] / REAL_WORLD_SCALE;
                Vector2Int NextPos = Positions[index + 1] / REAL_WORLD_SCALE;


                if(BranchPoints.Contains(CurrentPos))
                {
                    PrefabIndex = 3;
                    return;
                }

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