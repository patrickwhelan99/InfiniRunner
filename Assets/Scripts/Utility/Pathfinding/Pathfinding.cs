using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System.Linq;
using Unity.Collections;


using Paz.Utility.Collections;
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

        public static Vector2Int _defaultInvalid = new Vector2Int(-1, -1);


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
        // public Node[] AllNodes;
        // public int width, height;

        // private Node _currentNode;
        // private Node CurrentNode 
        // {
        //     get
        //     {
        //         return _currentNode;
        //     }
        //     set
        //     {
        //         if(debug)
        //         {
        //             if(value == null && _currentNode != null)
        //             {
        //                 visualiser.EnqueueInstruction((_currentNode.Coord, Color.white));
        //             }
        //             else
        //             {
        //                 visualiser.EnqueueInstruction((value.Coord, Color.red));
        //             }
        //         }

        //         _currentNode = value;
        //     }
        // }
        // public ObservableHashSet<Node> OpenSet;

        // public Node StartNode;
        // public Node EndNode;

        // public float HeuristicWeight = 5.0f;

        // public bool debug = false;
        // private PathFindingVisualiser visualiser;

        // private IEnumerable<Node> Path = new Node[0];

        // public AStar(){}
        // public AStar(IEnumerable<Node> All, Node Start, Node End, bool DebugMode = false)
        // {
        //     AllNodes = All.ToArray();
        //     StartNode = Start;
        //     EndNode = End;
        //     debug = DebugMode;
        // }

        // public IEnumerable<Node> Execute()
        // {
        //     // Grid dimensions
        //     width = AllNodes[AllNodes.Length - 1].Coord.x + 1;
        //     height = AllNodes[AllNodes.Length - 1].Coord.y + 1;


        //     // Set starting node's values
        //     StartNode.g = 0.0f;
        //     StartNode.h = CalculateHeuristic(StartNode, EndNode, HeuristicWeight);
        //     StartNode.f = StartNode.g + StartNode.h;

        //     // Create the set of open nodes we're currently looking at
        //     OpenSet = new ObservableHashSet<Node>();

        //     // Register our visualiser which will recieve updates 
        //     // when the collection is modified
        //     if(debug)
        //     {
        //         visualiser = new PathFindingVisualiser(this);
        //         OpenSet.Register(visualiser.ObservedSetModified);
        //     }


        //     OpenSet.Add(StartNode);

        //     // If the set is emptied we're out of options and no path is possible
        //     while(OpenSet.Count > 0)
        //     {
        //         CurrentNode = OpenSet.OrderBy(x => x.f).First();

        //         // If finished reconstruct the path
        //         if(CurrentNode == EndNode)
        //         {
        //             visualiser?.Playback();
        //             Path = RebuildPath(CurrentNode);
        //             break;
        //         }

        //         OpenSet.Remove(CurrentNode);


        //         Node[] Neighbours = GetNeighboursQuick(AllNodes, CurrentNode, width).ToArray(); // GetNeighbours(CurrentNode, AllNodes, width).ToArray(); 


        //         for (int i = 0; i < Neighbours.Length; i++)
        //         {
        //             Node Neighbour = Neighbours[i];

        //             // If the current route is quicker than the pre-existing route to this node
        //             float NewlyCalculatedG = CurrentNode.g + Vector2Int.Distance(CurrentNode, Neighbour);
        //             if(NewlyCalculatedG < Neighbour.g)
        //             {
        //                 Neighbour.backPtr = CurrentNode;

        //                 Neighbour.g = NewlyCalculatedG;
        //                 Neighbour.h = CalculateHeuristic(Neighbour, EndNode, HeuristicWeight);
        //                 Neighbour.f = Neighbour.g + Neighbour.h;

        //                 if(!OpenSet.Contains(Neighbour))
        //                 {
        //                     OpenSet.Add(Neighbour);
        //                 }
        //             }
        //         }

        //         CurrentNode = null;
        //     }

        //     // Path-finding has failed and is impossible.
        //     // A* is just Dijkstra with heuristics
        //     // If a path is possible it will be found
        //     return Path;
        // }

        // public static IEnumerable<Node> GetNeighboursQuick(Node[] AllNodes, Node CurrentNode, int Width)
        // {
        //     int CurrIndex = CurrentNode.Coord.y * Width + CurrentNode.Coord.x;
        //     int Max = AllNodes.Length - 1;
        //     return new Node[]
        //     {
        //         CurrIndex - Width > 0 ? AllNodes[CurrIndex - Width] : null,
        //         CurrIndex + Width < Max ? AllNodes[CurrIndex + Width] : null,
        //         CurrIndex != 0 ? AllNodes[CurrIndex - 1] : null,
        //         CurrIndex < Max ? AllNodes[CurrIndex + 1] : null
        //     }.Where(x => x != null && !x.isBlocker);
        // }

        


        public static IEnumerable<Node> GetNeighbours(Node CurrentNode, Node[] AllNodes, int Width, bool UseDiagonals = false, bool ReturnBlockers = false)
        {
            List<Node> ReturnList = new List<Node>();
            int IndexOfCurrent = System.Array.IndexOf(AllNodes, CurrentNode);

            // Get all adjacent squares (hollow 3x3 square)
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (!UseDiagonals)
                    {
                        if ((i != 0) && (j != 0))
                        {
                            continue;
                        }
                    }


                    // Current Node
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    int x = CurrentNode.Coord.x + i;
                    int y = CurrentNode.Coord.y + j;
                    if (x > -1 && x < Width && y > -1 && y < Width)
                    {
                        int IndexOfNode = IndexOfCurrent + i + j * Width;

                        if (!AllNodes[IndexOfNode].isBlocker || ReturnBlockers)
                        {
                            ReturnList.Add(AllNodes[IndexOfNode]);
                        }
                    }
                }
            }

            return ReturnList;
        }


        // private static float CalculateHeuristic(Node N, Node End, float HeuristicWeight = 1.0f)
        // {
        //     return Vector2Int.Distance(N, End) * HeuristicWeight;
        // }
        private static float CalculateHeuristic(Vector2Int N, Vector2Int End, float HeuristicWeight = 1.0f)
        {
            return Vector2Int.Distance(N, End) * HeuristicWeight;
        }

        // private IEnumerable<Node> RebuildPath(Node Nodez)
        // {
        //     Stack<Node> Route = new Stack<Node>();
        //     Route.Push(Nodez);

        //     while (!(Nodez = backwardsNodes[Nodez.Coord]).Equals(default))
        //     {
        //         Route.Push(Nodez);
        //     }

        //     return Route;
        // }

        // public static Node CoordToNode(Vector2Int Coord)
        // {
        //     return AllNodes[Coord.x + Coord.y * width];
        // }






        [BurstCompile]
        public struct AsJob : IJob
        {
            public NativeArray<Node> allNodes;
            public NativeParallelMultiHashMap<Vector2Int, Vector2Int> forwardNodes;
            public NativeParallelHashMap<Vector2Int, Vector2Int> backwardNodes;
            public NativeList<Vector2Int> path;
            public NativeParallelHashSet<Node> openSet;
            public Node startNode, currentNode, endNode;
            public float heuristicWeight;
            public int width;

            public NativeList<(Vector2Int, Color)> visualiserInstructionStack;

            [BurstCompile]
            public void Execute()
            {
                // Grid dimensions
                width = allNodes[allNodes.Length - 1].Coord.x + 1;

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

                        break;
                    }

                    openSet.Remove(currentNode);
                    visualiserInstructionStack.Add((currentNode.Coord, Color.red));

                    NativeList<Vector2Int> NeighboursCoords = new NativeList<Vector2Int>(Allocator.Temp);
                    GetNeighboursQuick(NeighboursCoords, allNodes, currentNode, width);



                    for (int i = 0; i < NeighboursCoords.Length; i++)
                    {
                        // NOTE: Node is a struct and therefore copy on fetch
                        Node Neighbour = allNodes[NeighboursCoords[i].y * width + NeighboursCoords[i].x];

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
                            allNodes[NeighboursCoords[i].y * width + NeighboursCoords[i].x] = Neighbour;
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
                
                Node LowestNode = new Node();
                LowestNode.f = float.MaxValue;

                Node CurrentNode;

                while(E.MoveNext())
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
                    CurrentNode = allNodes[BackCoord.y * width + BackCoord.x];
                    ReturnList.Add(CurrentNode);
                }
                while(!(BackCoord = backwardNodes[CurrentNode.Coord]).Equals(default));
            }

            public void GetNeighboursQuick(NativeList<Vector2Int> ReturnList, NativeArray<Node> AllNodes, Vector2Int CurrentNode, int Width)
            {
                int CurrIndex = CurrentNode.y * Width + CurrentNode.x;
                int Max = AllNodes.Length - 1;

                if (CurrIndex - Width > 0 && !AllNodes[CurrIndex - Width].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex - Width]);
                }
                if (CurrIndex + Width <= Max && !AllNodes[CurrIndex + Width].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex + Width]);
                }
                if (CurrIndex != 0 && !AllNodes[CurrIndex - 1].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex - 1]);
                }
                if (CurrIndex < Max && !AllNodes[CurrIndex + 1].isBlocker)
                {
                    ReturnList.Add(AllNodes[CurrIndex + 1]);
                }
            }
        }
    }
}
