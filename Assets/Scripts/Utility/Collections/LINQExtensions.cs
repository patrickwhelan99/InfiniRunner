using System.Collections;
using System.Collections.Generic;

using System.Linq;

public static class LINQExtensions
{
    public static T ChooseRandom<T>(this IEnumerable<T> Collection)
    {
        int Index = new System.Random().Next(Collection.Count());
        return Collection.ElementAt(Index);
    }
    public static T ChooseRandom<T>(this IEnumerable<T> Collection, Unity.Mathematics.Random Random)
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
}
