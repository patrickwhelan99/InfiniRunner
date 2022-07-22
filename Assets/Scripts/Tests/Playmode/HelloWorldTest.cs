using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class HelloWorldTest : ECSTestsFixture
{ 
    [SetUp]
    public override void Setup()
    {
        // CreateDefaultWorld = true;
        base.Setup();
    }

    // A Test behaves as an ordinary method
    [Test]
    public void HelloWorldTestSimplePasses()
    {
        
        // Use the Assert class to test conditions
        Assert.IsNotNull(base.m_Manager.World.GetExistingSystem(typeof(ArrowSystem)));
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    // [UnityTest]
    // public IEnumerator HelloWorldTestWithEnumeratorPasses()
    // {
    //     // Use the Assert class to test conditions.
    //     // Use yield to skip a frame.
    //     yield return null;
    // }
}
