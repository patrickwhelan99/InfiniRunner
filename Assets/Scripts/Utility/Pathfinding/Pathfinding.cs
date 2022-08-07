using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System.Linq;

using Paz.Utility.Collections;

namespace Paz.Utility.PathFinding
{
    public class Node
    {
        public Vector2Int Coord;

        // g is the graph cost to this node
        // h is the heuristic cost to the end
        // f is the sum of g and h
        public float g, h, f;

        // Point to the node we arrived here from.
        // This helps with reconstructing a path at the end
        public Node backPtr;

        // Array of references to the next node(s) in the path
        public Node[] forwardNodes = new Node[3];

        // Is this node traversable?
        public bool isBlocker = false;

        
        public Node(Vector2Int Coordinates)
        {
            Coord = Coordinates;

            g = float.MaxValue;
            h = float.MaxValue;
            f = float.MaxValue;

            backPtr = null;
        }

        public static implicit operator Vector2Int(Node LHS)
        {
            return LHS.Coord;
        }
    }

    public class AStar
    {
        public Node[] AllNodes;
        public int width, height;

        private Node _currentNode;
        private Node CurrentNode 
        {
            get
            {
                return _currentNode;
            }
            set
            {
                if(debug)
                {
                    if(value == null && _currentNode != null)
                    {
                        visualiser.EnqueueInstruction((_currentNode.Coord, Color.white));
                    }
                    else
                    {
                        visualiser.EnqueueInstruction((value.Coord, Color.red));
                    }
                }

                _currentNode = value;
            }
        }
        public ObservableHashSet<Node> OpenSet;

        public Node StartNode;
        public Node EndNode;

        public float HeuristicWeight = 5.0f;

        public bool debug = false;
        private PathFindingVisualiser visualiser;

        private IEnumerable<Node> Path = new Node[0];

        public AStar(){}
        public AStar(IEnumerable<Node> All, Node Start, Node End, bool DebugMode = false)
        {
            AllNodes = All.ToArray();
            StartNode = Start;
            EndNode = End;
            debug = DebugMode;
        }

        public IEnumerable<Node> Execute()
        {
            // Grid dimensions
            width = AllNodes[AllNodes.Length - 1].Coord.x + 1;
            height = AllNodes[AllNodes.Length - 1].Coord.y + 1;


            // Set starting node's values
            StartNode.g = 0.0f;
            StartNode.h = CalculateHeuristic(StartNode, EndNode, HeuristicWeight);
            StartNode.f = StartNode.g + StartNode.h;

            // Create the set of open nodes we're currently looking at
            OpenSet = new ObservableHashSet<Node>();

            // Register our visualiser which will recieve updates 
            // when the collection is modified
            if(debug)
            {
                visualiser = new PathFindingVisualiser(this);
                OpenSet.Register(visualiser.ObservedSetModified);
            }


            OpenSet.Add(StartNode);
            
            // If the set is emptied we're out of options and no path is possible
            while(OpenSet.Count > 0)
            {
                CurrentNode = OpenSet.OrderBy(x => x.f).First();

                // If finished reconstruct the path
                if(CurrentNode == EndNode)
                {
                    visualiser?.Playback();
                    Path = RebuildPath(CurrentNode);
                    break;
                }

                OpenSet.Remove(CurrentNode);


                Node[] Neighbours = GetNeighbours(CurrentNode, AllNodes, width).ToArray();


                for (int i = 0; i < Neighbours.Length; i++)
                {
                    Node Neighbour = Neighbours[i];

                    // If the current route is quicker than the pre-existing route to this node
                    float NewlyCalculatedG = CurrentNode.g + Vector2Int.Distance(CurrentNode, Neighbour);
                    if(NewlyCalculatedG < Neighbour.g)
                    {
                        Neighbour.backPtr = CurrentNode;

                        Neighbour.g = NewlyCalculatedG;
                        Neighbour.h = CalculateHeuristic(Neighbour, EndNode, HeuristicWeight);
                        Neighbour.f = Neighbour.g + Neighbour.h;

                        if(!OpenSet.Contains(Neighbour))
                        {
                            OpenSet.Add(Neighbour);
                        }
                    }
                }

                CurrentNode = null;
            }

            // Path-finding has failed and is impossible.
            // A* is just Dijkstra with heuristics
            // If a path is possible it will be found
            return Path;
        }

        

        public static IEnumerable<Node> GetNeighbours(Node CurrentNode, Node[] AllNodes, int Width, bool UseDiagonals = false, bool ReturnBlockers = false)
        {
            List<Node> ReturnList = new List<Node>();
            int IndexOfCurrent = System.Array.IndexOf(AllNodes, CurrentNode);
            
            // Get all adjacent squares (hollow 3x3 square)
            for(int i = -1; i < 2; i++)
            {
                for(int j = -1; j < 2; j++)
                {
                    if(!UseDiagonals)
                    {
                        if((i != 0) && (j != 0))
                        {
                            continue;
                        }
                    }


                    // Current Node
                    if(i == 0 && j == 0)
                    {
                        continue;
                    }

                    int x = CurrentNode.Coord.x + i;
                    int y = CurrentNode.Coord.y + j;
                    if(x > -1 && x < Width && y > -1 && y < Width)
                    {
                        int IndexOfNode = IndexOfCurrent + i + j * Width;

                        if(!AllNodes[IndexOfNode].isBlocker || ReturnBlockers)
                        {
                            ReturnList.Add(AllNodes[IndexOfNode]);
                        }
                    }
                }
            }

            return ReturnList;
        }

        
        private float CalculateHeuristic(Node N, Node End, float HeuristicWeight = 1.0f)
        {
            return Vector2Int.Distance(N, End) * HeuristicWeight;
        }

        private IEnumerable<Node> RebuildPath(Node Nodez)
        {
            Stack<Node> Route = new Stack<Node>();
            Route.Push(Nodez);

            while((Nodez = Nodez.backPtr) != null)
            {
                Route.Push(Nodez);
            }
            
            return Route;
        }

        public Node CoordToNode(Vector2Int Coord)
        {
            return AllNodes[Coord.x + Coord.y * width];
        }
    }
}
