using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ChunkManagerTests : ECSTestsFixture
{
    public ChunkManagerTests()
    {
        CreateDefaultWorld = true;
    }

    //[Test]
    //public void ChunkManagerTestsSimplePasses()
    //{

    //}

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator ChunkManagerTestsWithEnumeratorPasses()
    {
        // Create our system to test
        ChunkManagerSystem System = m_World.GetOrCreateSystem<ChunkManagerSystem>();

        // Create a player singleton required for the system update
        m_Manager.CreateEntity(typeof(PlayerTag), typeof(Unity.Transforms.Translation));


        Unity.Entities.EntityQuery Query = m_Manager.CreateEntityQuery(typeof(SpawnPathEvent));

        float StartTime = Time.realtimeSinceStartup;

        // Allow 5 seconds for the chunk manage to emit 2 events for spawning paths
        while (Time.realtimeSinceStartup - StartTime < 5.0f && Query.CalculateEntityCount() < 2)
        {
            System.Update();
            yield return null;
        }

        Assert.That(Query.CalculateEntityCount() == 2);
    }

    //[TearDown]
    //public void TearDownWorld()
    //{
    //    base.TearDown();
    //}
}
