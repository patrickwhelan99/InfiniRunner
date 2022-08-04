using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using System.Linq;

// [AlwaysUpdateSystem]
[UpdateInGroup(typeof(VariableSystemGroupThreeSeconds))]
public partial class EntitySpawner : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    EntityCommandBuffer ecb;

    Entity enemyPrefab;
    EntityQuery levelTilePieces;

    const int TO_SPAWN = 3;

    double lastSpawnTime;

    protected override void OnCreate()
    {
        levelTilePieces = EntityManager.CreateEntityQuery(typeof(LevelTileTag), typeof(Translation));
        RequireForUpdate(levelTilePieces);

        enemyPrefab = PrefabConverter.Convert((UnityEngine.GameObject)UnityEngine.Resources.Load("Prefabs/Capsule"));
    }

    protected override void OnUpdate()
    {
        if(!TryGetSingleton(out PlayerTag _))
        {
            return;
        }


        float3 PlayerPos = EntityManager.GetComponentData<Translation>(GetSingletonEntity<PlayerTag>()).Value;


        NativeArray<Translation> Ts = levelTilePieces.ToComponentDataArray<Translation>(Allocator.Temp);
        float3[] Translations = Ts.Select(x => x.Value).OrderBy(x => x.Distance(PlayerPos)).ToArray();
        float3 SpawnTilePosition = Translations.Skip(1).Take(2).ChooseRandom();

        Ts.Dispose();


        SpawnJob SpawnDudes = new SpawnJob()
        {
            ParallelWriter = ecbs.CreateCommandBuffer().AsParallelWriter(),
            EntityPrefab = enemyPrefab,
            Randoms = RandomSystem.random,

            SpawnPos = SpawnTilePosition + new float3(0.0f, 5.0f, 0.0f)
        };

        Dependency = SpawnDudes.Schedule(TO_SPAWN, 1, Dependency);
        
        ecbs.AddJobHandleForProducer(Dependency);
        Dependency.Complete();
    }


    // [BurstCompile]
    struct SpawnJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ParallelWriter;
        public Entity EntityPrefab;

        public float3 CircleCentre;
        public float Radius;

        public float3 SpawnPos;

        [NativeDisableParallelForRestriction]
        public NativeArray<Unity.Mathematics.Random> Randoms; 

        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        int threadIndex;

        // [BurstCompile]
        public void Execute(int index)
        {
            Unity.Mathematics.Random Rand = Randoms[threadIndex];

            Entity SpawnedEntity = ParallelWriter.Instantiate(index, EntityPrefab);


            ParallelWriter.SetComponent<Translation>(index, SpawnedEntity, new Translation() 
            {
                Value = SpawnPos + Rand.NextFloat3(5)
            });


            // SpawnDudesCallback(ParallelWriter, SpawnedEntity, index);


            Randoms[threadIndex] = Rand;
        }
    }

    private static void SpawnDudesCallback(EntityCommandBuffer.ParallelWriter ParallelWriter, Entity SpawnedEntity, int Index)
    {
        ParallelWriter.SetComponent<Translation>(Index, SpawnedEntity, new Translation() 
        {
            // Value = GetSpawnTranslationCircle(Rand, CircleCentre, Radius)
            Value = GetSpawnTranslationRect(Index, 10, new float3(-10.0f, 1.0f, -10.0f))
        });
    }

    private static float3 GetSpawnTranslationCircle(Random Rand, float3 CircleCentre, float Radius)
    {
        Radius = Rand.NextFloat(30.0f, 50.0f);
   
        float RandDegree = Rand.NextFloat(360.0f);
        float RandX = CircleCentre.x + Radius * math.cos(RandDegree);
        float RandZ = CircleCentre.z + Radius * math.sin(RandDegree);

        return new float3(RandX, 0.0f, RandZ);
    }

    private static float3 GetSpawnTranslationRect(int Index, int Length, float3 Origin = default)
    {
        float Spacing = 1.1f;
        return Origin + new float3(-(Index / Length * Spacing), 0.0f, Index % Length * Spacing);
    }

    private partial struct SetPathingTargetJob : IJobEntity
    {
        public float3 TargetTranslation;
        public void Execute(ref PathToPointComponent PathingComponent)
        {
            PathingComponent.TargetPoint = TargetTranslation;
        }
    }
}
