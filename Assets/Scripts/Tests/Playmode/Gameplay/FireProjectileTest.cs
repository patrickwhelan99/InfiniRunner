using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Entities;
using Unity.Transforms;

public class FireProjectileTest : ECSTestsFixture
{
    [UnityTest]
    public IEnumerator Test()
    {
        // Create our system to test
        FireProjectileSystem System = m_World.GetOrCreateSystem<FireProjectileSystem>();

        // 'Fake' some data and fire our projectile
        Entity ToFire = System.EntityManager.CreateEntity(typeof(ProjectileTag));
        FireProjectileSystem.FireProjectile(ToFire, new LocalToWorld());

        // Wait a frame for the projectile to be spawned
        yield return null;

        // Assert that the projectile has been spawned
        Assert.That(System.TryGetSingletonEntity<ProjectileTag>(out _));
    }
}
