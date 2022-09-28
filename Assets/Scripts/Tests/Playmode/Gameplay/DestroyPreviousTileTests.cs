using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Collections;

public class DestroyPreviousTileTests : ECSTestsFixture
{
    [Test]
    public void DestroyPreviousTileTest()
    {
        m_World.CreateSystem<EndSimulationEntityCommandBufferSystem>();

        // Create our system to test
        DestroyPreviousTileSystem System = m_World.GetOrCreateSystem<DestroyPreviousTileSystem>();
        // Chunk manager system is a dependency
        m_World.GetOrCreateSystem<ChunkManagerSystem>();

        // Create a player singleton required for the system update
        Entity Player = m_Manager.CreateEntity(typeof(PlayerTag), typeof(Translation));

        // Create two tiles
        // Entity Prefab = PrefabConverter.Convert(Resources.Load<GameObject>("Prefabs/Level/Test/Straight"), m_World);
        // m_World.Update();

        SceneSystem SceneSys = m_World.CreateSystem<SceneSystem>();
        Hash128 PrefabHash = SceneSys.GetSceneGUID("Assets/Scenes/Scene/Straight.unity");
        Entity LoadingEntity = SceneSys.LoadSceneAsync(PrefabHash, new SceneSystem.LoadParameters() { AutoLoad = true });

        // int loop = 0;

        // while (!SceneSys.IsSceneLoaded(LoadingEntity) && loop++ < 100)
        // {
        //     SceneSys.Update();

        //     DynamicBuffer<ResolvedSectionEntity> SectionBuffer = m_Manager.GetBuffer<ResolvedSectionEntity>(LoadingEntity);
        //     NativeArray<ResolvedSectionEntity> SectionEntities = SectionBuffer.ToNativeArray(Allocator.Temp);

        //     SectionEntities.Dispose();
        // }

        Entity Prefab = default;
        Entity FirstTile = m_Manager.Instantiate(Prefab);
        Entity SecondTile = m_Manager.Instantiate(Prefab);


        Translation FirstTileTrans = new Translation()
        {
            Value = float3.zero
        };

        Translation SecondTileTrans = new Translation()
        {
            Value = new float3(WorldConstants.REAL_WORLD_SCALE, 0.0f, 0.0f)
        };

        // Set the tiles adjacent with the player positioned on the first tile
        m_Manager.SetComponentData(Player, FirstTileTrans);
        m_Manager.SetComponentData(FirstTile, FirstTileTrans);

        m_Manager.SetComponentData(SecondTile, SecondTileTrans);

        // Update our system to register the player's position
        System.Update();

        // Move the player to the second tile
        m_Manager.SetComponentData(Player, SecondTileTrans);

        // Update our system to act on the player's new position
        System.Update();

        // Assert the first tile has been setup to animate
        EntityQuery ShaderComponents = m_Manager.CreateEntityQuery(typeof(DissolveShaderData));
        Assert.That(ShaderComponents.CalculateEntityCount() == 1);

        // Assert that the tile has been tagged for destruction
        EntityQuery DestructionTags = m_Manager.CreateEntityQuery(typeof(DestroyEntityAfterTime));
        Assert.That(DestructionTags.CalculateEntityCount() == 1);
    }
}
