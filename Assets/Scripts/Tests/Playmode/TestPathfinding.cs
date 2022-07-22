using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Paz.Utility.PathFinding;
using System.Linq;

public class TestPathfinding : ECSTestsFixture
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestPathfindingSimplePasses()
    {
        // Generate a grid of 64x64 with random tiles blocked
        Node[] AllNodes = GridLayouts.RandomBlockers.GenerateGrid(64);

        // Create our path finder
        AStar PathFinder = new AStar()
        {
            AllNodes = AllNodes,
            debug = false,

            StartNode = AllNodes[0],
            EndNode = AllNodes[AllNodes.Length - 1]
        };

        // Assert that a path has been found
        Assert.True(PathFinder.Execute(out _).Count() > 0);
    }
}
