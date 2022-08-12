using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Paz.Utility.PathFinding
{
    public static class GridLayouts
    {
        public static class RandomBlockers
        {
            public static NativeArray<Node> GenerateGrid(int GridWidth, ref Unity.Mathematics.Random Rand)
            {
                int Width = GridWidth;
                int Size = Width*Width;

                NativeArray<Node> AllNodes = new NativeArray<Node>(GridWidth * GridWidth, Allocator.TempJob);

                int StartIndex = 0; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);
                int EndIndex = AllNodes.Length - 1; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);

                for (int i = 0; i < Size; i++)
                {
                    Node NewNode = new Node(new Vector2Int(i % Width, i / Width));
                    NewNode.isBlocker = i != StartIndex && i != EndIndex && Rand.NextInt(0, 4) == 0;

                    AllNodes[i] = NewNode;
                }

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