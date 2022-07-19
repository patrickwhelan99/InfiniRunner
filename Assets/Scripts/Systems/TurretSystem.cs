using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using System.Linq;

public partial class TurretSystem : SystemBase
{    
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();    

    protected override void OnUpdate()
    {
        float CurrentTime = (float)Time.ElapsedTime;

        Dependency.Complete();

        EntityQuery Enemies = EntityManager.CreateEntityQuery(typeof(EnemyTag), typeof(Translation));
        
        if(Enemies.CalculateEntityCount() < 1)
        {
            return;
        }
        
        NativeArray<Translation> EnemyPositions = Enemies.ToComponentDataArray<Translation>(Allocator.TempJob);

        Dependency = new FireArrowsJob()
        {
            Writer = ecbs.CreateCommandBuffer().AsParallelWriter(),
            CurrentTime = CurrentTime,
            EnemiesNativeArray = EnemyPositions
        }.ScheduleParallel();

        ecbs.AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        EnemyPositions.Dispose();
    }

    partial struct FireArrowsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Writer;
        public float CurrentTime;
        public NativeArray<Translation> EnemiesNativeArray;

        [Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndex]
        int ThreadIndex;

        public void Execute([EntityInQueryIndex] int Index, ref TurretComponent Turret, in Translation Trans)
        {
            if(CurrentTime - Turret.LastFireTime < Turret.FireRate)
            {
                return;
            }

            Translation[] Enemies = EnemiesNativeArray.ToArray();

            float3 TurretPosition = Trans.Value;

            Enemies.OrderBy(x => math.distance(TurretPosition, x.Value));
            Turret.Target = Enemies[0].Value;

            Turret.LastFireTime = CurrentTime;

            Entity SpawnedArrow = Writer.Instantiate(Index, Turret.Arrow);
            Writer.SetComponent<Translation>(Index * 1000, SpawnedArrow, new Translation() {Value = Trans.Value + Turret.ArrowSpawnOffset});


            Unity.Mathematics.Random Rand = RandomSystem.random[ThreadIndex];

            float3 RandomOffsets = Rand.NextFloat3(-5.0f, 5.0f);
            float3 VectorToTarget = Turret.Target - (Trans.Value + Turret.ArrowSpawnOffset);
            float3 VectorToTargetWithRandom = VectorToTarget + RandomOffsets;
            float3 FinalVector = math.normalize(VectorToTargetWithRandom);

            float ShotSpeed = Rand.NextFloat(70.0f, 90.0f);

            RandomSystem.random[ThreadIndex] = Rand;
            
            PhysicsVelocity PV = new PhysicsVelocity() 
            {
                Linear = FinalVector * ShotSpeed
            };

            Writer.SetComponent<PhysicsVelocity>(Index * 2000, SpawnedArrow, PV);
        }
    }
}
