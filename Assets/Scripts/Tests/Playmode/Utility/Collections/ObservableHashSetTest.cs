using NUnit.Framework;

using Paz.Utility.Collections;

public class ObservableHashSetTest
{
    private static readonly int TESTNUMBER = 1;

    [Test]
    public void ADD_AND_REMOVE()
    {
        // Setup
        ObservableHashSet<int> HashSet = new ObservableHashSet<int>();
        HashSet.Register((EventData) =>
        {
            switch (EventData.operation)
            {
                case CollectionModifiedEventEnum.ADDED:
                    Assert.That(EventData.added == TESTNUMBER);
                    break;

                case CollectionModifiedEventEnum.REMOVED:
                    Assert.That(EventData.removed == TESTNUMBER);
                    break;

                case CollectionModifiedEventEnum.REPLACED:
                default:
                    break;
            }
        });


        // Assert
        HashSet.Add(TESTNUMBER);
        HashSet.Remove(TESTNUMBER);
    }
}
