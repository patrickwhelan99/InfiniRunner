using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Paz.Utility.PathFinding;
using System.Linq;

public class PathFindingTest
{
    readonly static uint GOOD_SEED = 2u;

    // A Test behaves as an ordinary method
    // [Test]
    // public void AStar()
    // {
    //     Unity.Mathematics.Random Rand = new Unity.Mathematics.Random(GOOD_SEED);

    //     // Generate a grid of 64x64 with random tiles blocked
    //     // Using a seed that we know produces a completable maze
    //     Node[] AllNodes = GridLayouts.RandomBlockers.GenerateGrid(64, ref Rand);

    //     // Create our path finder
    //     AStar PathFinder = new AStar()
    //     {
    //         AllNodes = AllNodes,
    //         debug = false,

    //         StartNode = AllNodes[0],
    //         EndNode = AllNodes[AllNodes.Length - 1]
    //     };

    //     // Assert that a path has been found
    //     IEnumerable<Node> Path = PathFinder.Execute();
    //     Assert.That(Path.Count() > 0);
    // }
}
