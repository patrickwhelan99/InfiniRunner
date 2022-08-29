using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace Paz.Utility.PathFinding
{
    public struct Node : System.IEquatable<Node>
    {
        public Vector2Int Coord;

        // g is the graph cost to this node
        // h is the heuristic cost to the end
        // f is the sum of g and h
        public float g, h, f;

        // Is this node traversable?
        public bool isBlocker;

        public static readonly Vector2Int _defaultInvalid = new Vector2Int(-1, -1);


        public Node(Vector2Int Coordinates)
        {
            Coord = Coordinates;

            g = float.MaxValue;
            h = float.MaxValue;
            f = float.MaxValue;

            isBlocker = false;
        }

        public static implicit operator Vector2Int(Node LHS)
        {
            return LHS.Coord;
        }

        public bool Equals(Node RHS)
        {
            return Coord.x == RHS.Coord.x && Coord.y == RHS.Coord.y;
        }

        public override int GetHashCode()
        {
            return Coord.GetHashCode();
        }
    }

    public class AStar
    {
        private static float CalculateHeuristic(Vector2Int N, Vector2Int End, float HeuristicWeight = 1.0f)
        {
            return Vector2Int.Distance(N, End) * HeuristicWeight;
        }

        [BurstCompile]
        public struct AsJob : IJob
        {
            public NativeArray<Node> allNodes;
            public NativeParallelHashMap<Vector2Int, Vector2Int> backwardNodes;
            public NativeList<Vector2Int> path;
            public NativeParallelHashSet<Node> openSet;
            [ReadOnly] public NativeArray<Vector2Int> startAndEndNodes;
            private Node startNode, currentNode, endNode;
            [ReadOnly] public float heuristicWeight;
            [ReadOnly] public int width;
            [ReadOnly] public int startNodeIndex;
            [ReadOnly] public int endNodeIndex;

            public NativeList<(Vector2Int, Color)> visualiserInstructionStack;

            [BurstCompile]
            public void Execute()
            {
                if (startAndEndNodes[0] == new Vector2Int(-1, -1) || startAndEndNodes[startNodeIndex] == startAndEndNodes[endNodeIndex])
                {
                    return;
                }

                // Grid dimensions
                width = allNodes[^1].Coord.x + 1;

                startNode = allNodes.GetNodeQuick(startAndEndNodes[startNodeIndex]);
                endNode = allNodes.GetNodeQuick(startAndEndNodes[endNodeIndex]);

                // Set starting node's values
                startNode.g = 0.0f;
                startNode.h = CalculateHeuristic(startNode, endNode, heuristicWeight);
                startNode.f = startNode.g + startNode.h;


                openSet.Add(startNode);

                // If the set is emptied we're out of options and no path is possible
                while (openSet.Count() > 0)
                {
                    currentNode = GetLowestFScore(openSet);

                    // If finished reconstruct the path
                    if (currentNode.Equals(endNode))
                    {
                        RebuildPath(path, currentNode);

                        foreach (Vector2Int Coord in path)
                        {
                            visualiserInstructionStack.Add((Coord, Color.green));
                        }

                        // Reverse the completed path
                        // Burst doesn't let us use IEnumerable as it boxes NativeArray
                        Vector2Int Tmp;
                        for (int i = 0; i < path.Length / 2; i++)
                        {
                            Tmp = path[i];
                            path[i] = path[path.Length - 1 - i];
                            path[path.Length - 1 - i] = Tmp;
                        }

                        break;
                    }

                    openSet.Remove(currentNode);
                    visualiserInstructionStack.Add((currentNode.Coord, Color.red));

                    NativeList<Vector2Int> NeighboursCoords = new NativeList<Vector2Int>(Allocator.Temp);
                    GetNeighboursQuick(NeighboursCoords, allNodes, currentNode, width);



                    for (int i = 0; i < NeighboursCoords.Length; i++)
                    {
                        // NOTE: Node is a struct and therefore copy on fetch
                        Node Neighbour = allNodes[(NeighboursCoords[i].y * width) + NeighboursCoords[i].x];

                        // If the current route is quicker than the pre-existing route to this node
                        float NewlyCalculatedG = currentNode.g + Vector2Int.Distance(currentNode, Neighbour);
                        if (NewlyCalculatedG < Neighbour.g)
                        {
                            backwardNodes[Neighbour.Coord] = currentNode;

                            Neighbour.g = NewlyCalculatedG;
                            Neighbour.h = CalculateHeuristic(Neighbour, endNode, heuristicWeight);
                            Neighbour.f = Neighbour.g + Neighbour.h;

                            if (openSet.Contains(Neighbour))
                            {
                                openSet.Remove(Neighbour);
                            }

                            openSet.Add(Neighbour);
                            visualiserInstructionStack.Add((Neighbour.Coord, Color.blue));


                            // Write back to the master array
                            allNodes[(NeighboursCoords[i].y * width) + NeighboursCoords[i].x] = Neighbour;
                        }
                    }

                    visualiserInstructionStack.Add((currentNode.Coord, Color.white));

                    NeighboursCoords.Dispose();
                }

                // path-finding has failed and is impossible.
                // A* is just Dijkstra with heuristics
                // If a path is possible it will be found
                return;
            }

            private Node GetLowestFScore(NativeParallelHashSet<Node> Set)
            {
                NativeParallelHashSet<Node>.Enumerator E = Set.GetEnumerator();

                Node LowestNode = new Node
                {
                    f = float.MaxValue
                };

                Node CurrentNode;

                while (E.MoveNext())
                {
                    CurrentNode = E.Current;
                    LowestNode = CurrentNode.f < LowestNode.f ? CurrentNode : LowestNode;
                }

                return LowestNode;
            }

            private void RebuildPath(NativeList<Vector2Int> ReturnList, Node CurrentNode)
            {
                ReturnList.Add(CurrentNode);

                Vector2Int BackCoord;

                do
                {
                    BackCoord = backwardNodes[CurrentNode.Coord];
                    CurrentNode = allNodes[(BackCoord.y * width) + BackCoord.x];
                    ReturnList.Add(CurrentNode);
                }
                while (!CurrentNode.Equals(startNode));
            }

            public void GetNeighboursQuick(NativeList<Vector2Int> ReturnList, NativeArray<Node> AllNodes, Vector2Int CurrentNode, int Width)
            {
                int CurrIndex = (CurrentNode.y * Width) + CurrentNode.x;

                if (CurrentNode.y > 0 && !AllNodes[CurrIndex - Width].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex - Width]);
                }
                if (CurrentNode.y < Width - 1 && !AllNodes[CurrIndex + Width].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex + Width]);
                }
                if (CurrentNode.x > 0 && !AllNodes[CurrIndex - 1].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex - 1]);
                }
                if (CurrentNode.x < Width - 1 && !AllNodes[CurrIndex + 1].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex + 1]);
                }
            }
        }
    }
}
