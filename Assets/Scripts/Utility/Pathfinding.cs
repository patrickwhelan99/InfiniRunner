using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System.Linq;

namespace Paz.Utility.PathFinding
{
    public class Node // : System.IComparable
    {
        public Vector2Int Coord;

        // g is the graph cost to this node
        // h is the heuristic cost to the end
        // f is the sum of g and h
        public float g, h, f;

        // Point to the node we arrived here from.
        // This helps with reconstructing a path at the end
        public Node backPtr;

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

        // public int CompareTo(object Obj)
        // {
        //     if(Obj is Node OtherNode)
        //     {
        //         return this == OtherNode ? 0 : f < OtherNode.f ? -1 : 1;
        //     }
        //     else
        //     {
        //         throw new System.ArgumentException($"{Obj} is not a {this.GetType()}!");
        //     }
        // }

        public static implicit operator Vector2Int(Node LHS)
        {
            return LHS.Coord;
        }
    }

    public class AStar
    {
        public Node[] AllNodes;
        public int width, height;

        // Min heap is probably a better data-structure to use but it's not available in .Net4
        // and I cba writing one :D
        // public SortedList<Node, float> OpenSet;

        public HashSet<Node> OpenSet;

        public Node StartNode;
        public Node EndNode;

        public float HeuristicWeight = 5.0f;

        public bool debug = false;
        private Visualiser visualiser;

        private IEnumerable<Node> Path = new Node[0];

        public IEnumerable<Node> Execute(out IEnumerable<Node> Walls)
        {
            Walls = new Node[0];

            if(debug)
            {
                visualiser = new Visualiser(AllNodes.Length);
            }
            

            // Grid dimensions
            width = AllNodes[AllNodes.Length -1].Coord.x + 1;
            height = AllNodes[AllNodes.Length -1].Coord.y + 1;


            // Set starting node's values
            StartNode.g = 0.0f;
            StartNode.h = CalculateHeuristic(StartNode, EndNode, HeuristicWeight);
            StartNode.f = StartNode.g + StartNode.h;

            // Create the set of open nodes we're currently looking at
            OpenSet = new HashSet<Node>();
            OpenSet.Add(StartNode);

            int loopCount = 0;
            int loopFailCount = (width*width+1)/2;
            
            // If the set is emptied we're out of options and no path is possible
            while(OpenSet.Count > 0 /* && loopCount++ < loopFailCount*/)
            {
                Node CurrentNode = OpenSet.OrderBy(x => x.f).First();

                // If finished reconstruct the path
                if(CurrentNode == EndNode)
                {
                    Path = RebuildPath(CurrentNode);
                    ConstructWalls(Path, AllNodes, width, ref Walls);
                    visualiser?.Update(CurrentNode, AllNodes, OpenSet, new Node[0]);
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

                visualiser?.Update(CurrentNode, AllNodes, OpenSet, Neighbours);
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

        private IEnumerable<Node> RebuildPath(Node CurrentNode)
        {
            Stack<Node> Route = new Stack<Node>();
            Route.Push(CurrentNode);

            while((CurrentNode = CurrentNode.backPtr) != null)
            {
                Route.Push(CurrentNode);
            }
            
            return Route;
        }

        
        private void ConstructWalls(IEnumerable<Node> Path, Node[] AllNodes, int Width, ref IEnumerable<Node> Walls)
        {
            HashSet<Node> AdjacentToPath = new HashSet<Node>();
            AllNodes.ToList().ForEach(x => x.isBlocker = false);
            OpenSet.Clear();

            HashSet<Node> PathHashSet = new HashSet<Node>(Path);

            // Old system
            for (int i = 0; i < Path.Count(); i++)
            {
                Node[] Neighbours = GetNeighbours(Path.ElementAt(i), AllNodes, Width).ToArray();
                for (int j = 0; j < Neighbours.Length; j++)
                {
                    if(!PathHashSet.Contains(Neighbours[j]) && !AdjacentToPath.Contains(Neighbours[j]))
                    {
                        AdjacentToPath.Add(Neighbours[j]);
                    }
                }
            }

            // New system
            // int Distance = 4;

            // List<Node> ValidWallNodes = new List<Node>();
            // Vector2Int[] Neighbours = new Vector2Int[2];
            // for (int i = 0; i < Path.Count(); i++)
            // {
            //     Neighbours = GetNeighbours(Path.ElementAt(i), AllNodes, Width).Where(x => !Path.Contains(x)).Select(x => x.Coord).ToArray();
            //     for (int j = 0; j < Neighbours.Length; j++)
            //     {
            //         Vector2Int DistancedCoord = Path.ElementAt(i).Coord + (Neighbours[j] - Path.ElementAt(i).Coord) * Distance;

            //         Node DistancedNode = AllNodes.FirstOrDefault(x => x.Coord.Equals(DistancedCoord));
            //         if(DistancedNode != null)
            //         {
            //             ValidWallNodes.Add(DistancedNode);
            //         }
            //     }
            // }



            // AdjacentToPath = ValidWallNodes;
            
            AdjacentToPath.ToList().ForEach(x => x.isBlocker = true);
            Walls = AdjacentToPath;

            // var WallSides = GetWallSides(Path, AllNodes, Width, Walls).ToArray();
        }

        public IEnumerable<IEnumerable<Node>> GetWallSides(IEnumerable<Node> Path, Node[] AllNodes, int Width, IEnumerable<Node> Walls)
        {
            Node[][] WallSides = new Node[2][];

            Node[] Neighbours = GetNeighbours(StartNode, AllNodes, Width, true, true).ToArray();

            Node[] BeginningOfWalls = Neighbours.Where(x => x.isBlocker).ToArray();

            WallSides[0] = GetWallSide(Path, AllNodes, Width, Walls, BeginningOfWalls[0]).ToArray();
            WallSides[1] = GetWallSide(Path, AllNodes, Width, Walls, BeginningOfWalls[1]).ToArray();

            return WallSides;
        }

        private IEnumerable<Node> GetWallSide(IEnumerable<Node> Path, Node[] AllNodes, int Width, IEnumerable<Node> Walls, Node StartNode)
        {
            HashSet<Node> Wall = new HashSet<Node>();
            Node CurrentNode = StartNode;
            while(CurrentNode != null)
            {
                Wall.Add(CurrentNode);
                CurrentNode = GetNeighbours(Wall.Last(), AllNodes, Width, true, true).FirstOrDefault(x => Walls.Contains(x) && !Wall.Contains(x));
            }

            return Wall;
        }
    }

    public class Visualiser
    {
        private static GameObject displayPrefab;
        private static GameObject displayObject;
        Texture2D displayTexture;
        Material materialToUpdate;

        public Visualiser(int Nodes)
        {
            displayPrefab ??= Resources.Load("Prefabs/DisplaySearchPath") as GameObject;
            displayObject ??= MonoBehaviour.Instantiate(displayPrefab);
            materialToUpdate = displayObject.GetComponent<Renderer>().material;

            displayObject.transform.position = Camera.main.transform.position + new Vector3(0.0f, -1.0f, 0.0f);
            Camera.main.orthographic = true;
        }

        public void Update(Node CurrentNode, Node[] AllNodes, HashSet<Node> OpenSet, Node[] Neighbours)
        {
            int Size = (int)Mathf.Sqrt(AllNodes.Length);
            
            displayTexture = new Texture2D(Size, Size, TextureFormat.RGB24, 2, true);

            for (int i = 0; i < AllNodes.Length; i++)
            {
                Node ThisNode = AllNodes[i];

                Color PixelColour = Color.white;
                if(ThisNode == CurrentNode)
                {
                    PixelColour = Color.red;
                }
                else if(Neighbours.Contains(ThisNode))
                {
                    PixelColour = Color.green;
                }
                else if(OpenSet.Contains(ThisNode))
                {
                    PixelColour = Color.blue;
                }
                else if(ThisNode.isBlocker)
                {
                    PixelColour = Color.black;
                }

                displayTexture.SetPixel(Size - 1 - i % Size, i / Size, PixelColour);
            }


            displayTexture.anisoLevel = 0;
            displayTexture.filterMode = 0;

            displayTexture.Apply(true, true);


            materialToUpdate.mainTexture = displayTexture;
        }
    }

    public static class GridLayouts
    {
        public static class RandomBlockers
        {
            public static Node[] GenerateGrid(int GridWidth, uint Seed = 0)
            {
                int Width = GridWidth;
                int Size = Width*Width;

                Node[] AllNodes = new Node[Size];

                Unity.Mathematics.Random Randy = new Unity.Mathematics.Random();

                Seed = Seed == 0u ? (uint)new System.Random().Next() : Seed;
                Randy.InitState(Seed);

                Debug.Log($"Seed: {Randy.state}");

                for (int i = 0; i < Size; i++)
                {
                    AllNodes[i] = new Node(new Vector2Int(i % Width, i / Width));
                    AllNodes[i].isBlocker = Randy.NextInt(0, 4) == 0;
                }

                int StartIndex = 0; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);
                int EndIndex = AllNodes.Length - 1; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);

                AllNodes[StartIndex].isBlocker = false;
                AllNodes[EndIndex].isBlocker = false;

                return AllNodes;
            }
        }


        public static class Ring
        {
            public static Node[] GenerateGrid()
            {
                Node[] Return = new Node[16];
                for (int i = 0; i < Return.Length; i++)
                {
                    Return[i] = new Node(new Vector2Int(i % 4, i / 4));
                }

                Return[1].isBlocker = true;
                Return[5].isBlocker = true;
                Return[9].isBlocker = true;
                Return[10].isBlocker = true;

                return Return;
            }
        }
    }

    
}
