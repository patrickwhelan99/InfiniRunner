using NUnit.Framework;
using UnityEngine;

using Paz.Utility.PathFinding;
using Unity.Collections;
using Unity.Jobs;


public class PathFindingTest
{
    private static readonly uint GOOD_SEED = 2u;
    private static readonly int GRID_WIDTH = 64;

    // A Test behaves as an ordinary method
    [Test]
    public void AStar()
    {
        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(GOOD_SEED);

        // Generate a grid of 64x64 with random tiles blocked
        // Using a seed that we know produces a completable maze
        NativeArray<Node> AllNodes = new NativeArray<Node>(GRID_WIDTH * GRID_WIDTH, Allocator.TempJob);
        GridLayouts.RandomBlockers.GenerateGrid(AllNodes, GRID_WIDTH, ref Rand);

        // Create our path finder
        NativeParallelHashMap<Vector2Int, Vector2Int> Back = new NativeParallelHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);
        NativeList<Vector2Int> Path = new NativeList<Vector2Int>(Allocator.TempJob);
        NativeParallelHashSet<Node> Open = new NativeParallelHashSet<Node>(64, Allocator.TempJob);
        NativeList<(Vector2Int, Color)> Vis = new NativeList<(Vector2Int, Color)>(Allocator.TempJob);
        NativeArray<Vector2Int> StartAndEndNodes = new NativeArray<Vector2Int>(new Vector2Int[] { Vector2Int.zero, new Vector2Int(GRID_WIDTH - 1, GRID_WIDTH - 1) }, Allocator.TempJob);
        AStar.AsJob PathFinder = new AStar.AsJob()
        {
            allNodes = AllNodes,
            backwardNodes = Back,
            path = Path,
            openSet = Open,

            startAndEndNodes = StartAndEndNodes,
            startNodeIndex = 0,
            endNodeIndex = 1,


            heuristicWeight = 5.0f,
            width = GRID_WIDTH,

            visualiserInstructionStack = Vis,
        };

        // Find our path
        PathFinder.Run();

        // Assert that a path has been found
        Assert.That(Path.Length > 0);


        // Clean up
        AllNodes.Dispose();
        Back.Dispose();
        Path.Dispose();
        Open.Dispose();
        Vis.Dispose();
        StartAndEndNodes.Dispose();
    }
}
