using System.Collections;
using System.Collections.Generic;

using System.Linq;
using Paz.Utility.PathFinding;
using Unity.Collections;

public static class LINQExtensions
{
    public static T ChooseRandom<T>(this IEnumerable<T> Collection)
    {
        int Index = new System.Random().Next(Collection.Count());
        return Collection.ElementAt(Index);
    }
    public static T ChooseRandom<T>(this IEnumerable<T> Collection, ref Unity.Mathematics.Random Random)
    {
        int Index = Random.NextInt(Collection.Count());
        return Collection.ElementAt(Index);
    }

    public static void ForEach<T>(this IEnumerable<T> Collection, System.Action<T> Function)
    {
        foreach (T item in Collection)
        {
            Function(item);
        }
    }

    public static int GetNodeIndex(this NativeArray<Node> Collection, UnityEngine.Vector2Int Coordinate)
    {
        int Width = Collection.Last().Coord.x + 1;

        return Coordinate.y * Width + Coordinate.x;
    }

    public static Node GetNodeQuick(this NativeArray<Node> Collection, UnityEngine.Vector2Int Coordinate)
    {
        int Width = Collection[Collection.Length - 1].Coord.x + 1;

        return Collection[Coordinate.y * Width + Coordinate.x];
    }
}
