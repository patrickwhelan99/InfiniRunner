using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObservableHashSet<T> : HashSet<T>
{
    // The underlying collection we'll use
    // private HashSet<T> collection;

    // Event Data
    public struct CollectionModifiedEventData
    {
        public T removed;
        public T added;

        public CollectionModifiedEventData(T R, T A)
        {
            removed = R;
            added = A;
        }
    }

    public delegate void CollectionModifiedEvent(CollectionModifiedEventData EventData);
    private event CollectionModifiedEvent collectionModified;

    public ObservableHashSet()
    {
        // collection = new HashSet<T>();
    }

    public void Register(CollectionModifiedEvent Function)
    {
        collectionModified += Function;
    }

    public void Deregister(CollectionModifiedEvent Function)
    {
        collectionModified -= Function;
    }

    new public void Add(T ToAdd)
    {
        if(base.Add(ToAdd))
        {
            CollectionModifiedEventData EventData = new CollectionModifiedEventData()
            {
                added = ToAdd
            };
            collectionModified.Invoke(EventData);
        }
    }

    new public void Remove(T ToRemove)
    {
        if (base.Remove(ToRemove))
        {
            CollectionModifiedEventData EventData = new CollectionModifiedEventData()
            {
                removed = ToRemove
            };
            collectionModified.Invoke(EventData);
        }
    }
}
