using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Paz.Utility.Collections;

public class ObservableHashSetTest
{
    readonly static int TESTNUMBER = 1;

    [Test]
    public void ADD_AND_REMOVE()
    {
        // Setup
        ObservableHashSet<int> HashSet = new ObservableHashSet<int>();
        HashSet.Register((EventData) => 
        {
            switch(EventData.operation)
            {
                case CollectionModifiedEventEnum.ADDED:
                    Assert.That(EventData.added == TESTNUMBER);
                    break;

                case CollectionModifiedEventEnum.REMOVED:
                    Assert.That(EventData.removed == TESTNUMBER);
                    break;
            }
        });


        // Assert
        HashSet.Add(TESTNUMBER);
        HashSet.Remove(TESTNUMBER);
    }
}
