using UnityEngine;

using System.Linq;

using Paz.Utility.PathFinding;

public class TestPathfinding : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(TestPathing());
    }

    private System.Collections.IEnumerator TestPathing()
    {
        
        int Width = 64;
        int Size = Width*Width;

        AStar Pather = new AStar();
        Pather.AllNodes = new Node[Size];

        Unity.Mathematics.Random Randy = new Unity.Mathematics.Random();
        

        while(true)
        {
            uint Seed = (uint)new System.Random().Next();
            Randy.InitState(Seed);

            Debug.Log($"Seed: {Randy.state}");

            for (int i = 0; i < Size; i++)
            {
                Pather.AllNodes[i] = new Node(new Vector2Int(i % Width, i / Width));
                Pather.AllNodes[i].isBlocker = Randy.NextInt(0, 4) == 0;
            }

            int StartIndex = 0; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);
            int EndIndex = Pather.AllNodes.Length - 1; // UnityEngine.Random.Range(0, Pather.AllNodes.Length);

            Pather.StartNode = Pather.AllNodes[StartIndex];
            Pather.StartNode.isBlocker = false;

            Pather.EndNode = Pather.AllNodes[EndIndex];
            Pather.EndNode.isBlocker = false;

        
            yield return Pather.Execute(out _);
            yield return new WaitForSeconds(3.0f);
        }
    }
}
