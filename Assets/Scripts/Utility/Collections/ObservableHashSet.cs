using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Paz.Utility.Collections
{
    // Event Data
    public enum CollectionModifiedEventEnum {ADDED, REMOVED, REPLACED}
    public struct CollectionModifiedEventData<T>
    {
        public CollectionModifiedEventEnum operation;
        public IEnumerable<T> collection;
        public T removed;
        public T added;

        public CollectionModifiedEventData(CollectionModifiedEventEnum O, IEnumerable<T> C, T R, T A)
        {
            operation = O;
            collection = C;
            removed = R;
            added = A;
        }
    }


    public class ObservableHashSet<T> : HashSet<T>
    {
        public delegate void CollectionModifiedEvent(CollectionModifiedEventData<T> EventData);
        private event CollectionModifiedEvent collectionModified;

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
                CollectionModifiedEventData<T> EventData = new CollectionModifiedEventData<T>(CollectionModifiedEventEnum.ADDED, this, default, ToAdd);

                collectionModified?.Invoke(EventData);
            }
        }

        new public void Remove(T ToRemove)
        {
            if (base.Remove(ToRemove))
            {
                CollectionModifiedEventData<T> EventData = new CollectionModifiedEventData<T>(CollectionModifiedEventEnum.REMOVED, this, ToRemove, default);
                collectionModified?.Invoke(EventData);
            }
        }
    }
}