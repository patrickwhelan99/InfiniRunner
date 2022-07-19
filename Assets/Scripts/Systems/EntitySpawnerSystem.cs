using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;

[AlwaysUpdateSystem]
public partial class EntitySpawner : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    EntityCommandBuffer ecb;

    Entity enemyPrefab;

    const int TO_SPAWN = 25;

    bool hasSpawned = false;


    public void Spawn()
    {
        SpawnJob SpawnDudes = new SpawnJob()
        {
            ParallelWriter = ecbs.CreateCommandBuffer().AsParallelWriter(),
            EntityPrefab = enemyPrefab,
            Randoms = RandomSystem.random
        };

        Dependency = SpawnDudes.Schedule(TO_SPAWN, 1, Dependency);

        
        ecbs.AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        hasSpawned = true;
    }

    protected override void OnUpdate()
    {
        if(enemyPrefab == default)
        {
            PrefabConverter.Convert(UnityEngine.Resources.Load<UnityEngine.GameObject>(""));
            return;
        }

        if(!hasSpawned)
        {
            Spawn();
        }
    }

    [BurstCompile]
    struct SpawnJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ParallelWriter;
        public Entity EntityPrefab;

        public float3 CircleCentre;
        public float Radius;

        [NativeDisableParallelForRestriction]
        public NativeArray<Unity.Mathematics.Random> Randoms; 

        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        int threadIndex;

        [BurstCompile]
        public void Execute(int index)
        {
            Unity.Mathematics.Random Rand = Randoms[threadIndex];

            Entity SpawnedEntity = ParallelWriter.Instantiate(index, EntityPrefab);

            SpawnDudesCallback(ParallelWriter, SpawnedEntity, index);


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
