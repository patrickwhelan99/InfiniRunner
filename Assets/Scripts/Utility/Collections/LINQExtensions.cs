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
}
