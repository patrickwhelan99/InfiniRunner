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
    public static T ChooseRandom<T>(this NativeArray<T> Collection, NativeArray<Unity.Mathematics.Random> Random) where T : unmanaged
    {
        int Index = Random[0].NextInt(Collection.Length);
        return Collection[Index];
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
        int Width = Collection[^1].Coord.x + 1;

        return (Coordinate.y * Width) + Coordinate.x;
    }

    public static Node GetNodeQuick(this NativeArray<Node> Collection, UnityEngine.Vector2Int Coordinate)
    {
        int Width = Collection[^1].Coord.x + 1;

        return Collection[(Coordinate.y * Width) + Coordinate.x];
    }






    public static NativeList<T> NativeWhere<T>(this NativeArray<T> Collection, System.Func<T, bool> Predicate) where T : unmanaged
    {
        NativeArray<T>.Enumerator Enumerator = Collection.GetEnumerator();
        NativeList<T> ReturnList = new NativeList<T>(Allocator.TempJob);

        while (Enumerator.MoveNext())
        {
            T Current = Enumerator.Current;

            if (Predicate.Invoke(Current))
            {
                ReturnList.Add(Current);
            }
        }

        return ReturnList;
    }
}
