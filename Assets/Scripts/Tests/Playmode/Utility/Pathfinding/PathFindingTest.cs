using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Paz.Utility.PathFinding;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;


public class PathFindingTest
{
    readonly static uint GOOD_SEED = 2u;
    readonly static int GRID_WIDTH = 64;

    // A Test behaves as an ordinary method
    [Test]
    public void AStar()
    {
        Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(GOOD_SEED);

        // Generate a grid of 64x64 with random tiles blocked
        // Using a seed that we know produces a completable maze
        NativeArray<Node> AllNodes = new NativeArray<Node>();
        GridLayouts.RandomBlockers.GenerateGrid(AllNodes, GRID_WIDTH, ref Rand);

        // Create our path finder
        var Back = new NativeParallelHashMap<Vector2Int, Vector2Int>(AllNodes.Length, Allocator.TempJob);
        var Path = new NativeList<Vector2Int>(Allocator.TempJob);
        var Open = new NativeParallelHashSet<Node>(64, Allocator.TempJob);
        var Vis = new NativeList<(Vector2Int, Color)>(Allocator.TempJob);
        var StartAndEndNodes = new NativeArray<Vector2Int>(new Vector2Int[]{Vector2Int.zero, new Vector2Int(GRID_WIDTH - 1, GRID_WIDTH - 1)}, Allocator.TempJob);
        AStar.AsJob PathFinder = new AStar.AsJob()
        {
            allNodes = AllNodes,
            backwardNodes = Back,
            path = Path,
            openSet = Open,
            
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
    }
}
