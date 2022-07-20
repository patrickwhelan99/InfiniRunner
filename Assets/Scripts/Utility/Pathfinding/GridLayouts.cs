using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Paz.Utility.PathFinding
{
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