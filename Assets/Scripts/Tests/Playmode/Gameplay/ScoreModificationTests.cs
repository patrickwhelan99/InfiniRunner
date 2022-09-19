using NUnit.Framework;
using Unity.Entities;

public class ScoreModificationTests : ECSTestsFixture
{
    private bool callbackHasBeenCalled = false;

    public ScoreModificationTests()
    {
        CreateDefaultWorld = true;
    }

    [Test]
    public void ScoringSystemTest()
    {
        // Create our system to test
        ScoringSystem System = m_World.GetOrCreateSystem<ScoringSystem>();

        // Run the first test
        TestOne(System);

        // De-register our callback
        System.UnregisterCallback(ScoreModifiedCallbackOne);
        callbackHasBeenCalled = false;

        // Run the second test
        TestTwo(System);

        // Ensure our callback method was reached
        Assert.That(callbackHasBeenCalled);
    }

    /// <summary>
    /// Test one asserts the score is modified
    /// </summary>
    /// <param name="System"></param>
    private void TestOne(ScoringSystem System)
    {
        // Register our callback for a score modified event
        System.RegisterCallback(ScoreModifiedCallbackOne);

        // Create a score modifying event
        Entity CreatedEvent = m_Manager.CreateEntity(typeof(ModifyScoreEvent));

        m_Manager.SetComponentData(CreatedEvent, new ModifyScoreEvent()
        {
            Value = 5
        });

        // Run our system for one world tick
        System.Update();
    }

    /// <summary>
    /// Test two asserts the score cannot be negative
    /// </summary>
    /// <param name="System"></param>
    private void TestTwo(ScoringSystem System)
    {
        // Register our callback for a score modified event
        System.RegisterCallback(ScoreModifiedCallbackTwo);

        // Create a score modifying event
        Entity CreatedEvent = m_Manager.CreateEntity(typeof(ModifyScoreEvent));

        m_Manager.SetComponentData(CreatedEvent, new ModifyScoreEvent()
        {
            Value = -10
        });

        // Run our system for one world tick
        System.Update();
    }

    private void ScoreModifiedCallbackOne(int NewScore)
    {
        callbackHasBeenCalled = true;
        Assert.That(NewScore == 5);
    }

    private void ScoreModifiedCallbackTwo(int NewScore)
    {
        callbackHasBeenCalled = true;
        Assert.That(NewScore == 0);
    }
}
