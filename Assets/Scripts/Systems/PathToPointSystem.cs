using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMovementSystem))]
public partial class PathToPointSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    protected override void OnStartRunning()
    {
        Entities.ForEach((int nativeThreadIndex, ref PathToPointComponent Pathing) =>
        {
            Random Rand = RandomSystem.random[nativeThreadIndex];
            Pathing.Speed = Rand.NextFloat(0.13f, 0.15f);
            RandomSystem.random[nativeThreadIndex] = Rand;

        }).WithoutBurst().Run();
    }

    protected override void OnUpdate()
    {
        if(ecbs.TryGetSingletonEntity<PlayerTag>(out Entity Player))
        {
            float3 Target = ecbs.GetComponentDataFromEntity<LocalToWorld>()[Player].Position;
            Dependency = new UpdateEnemiesTargetPosition() 
            {
                NewTargetPosition = Target
            }.ScheduleParallel(Dependency);

            Dependency = new MoveTowardsTargetJob().ScheduleParallel(Dependency);

            Dependency.Complete();
        }
    }

    [BurstCompile]
    partial struct UpdateEnemiesTargetPosition : IJobEntity
    {
        public float3 NewTargetPosition;
        public void Execute(Entity Ent, ref PathToPointComponent Pather, in SetPathTargetTag _)
        {
            Pather.TargetPoint = NewTargetPosition;
        }
    }

    [BurstCompile]
    partial struct MoveTowardsTargetJob : IJobEntity
    {
        public void Execute([EntityInQueryIndex] int EntityIndex, Entity Ent, ref PhysicsVelocity SubjectsVelocity, in Translation SubjectsTranslation, in PathToPointComponent Pather, in LocalToWorld LTW)
        {
            Translation NewTrans = SubjectsTranslation;
            
            // Get Vector between points
            float3 Vector = Pather.TargetPoint - SubjectsTranslation.Value;

            float Norm = math.sqrt(math.pow(Vector[0], 2) + math.pow(Vector[1], 2) + math.pow(Vector[2], 2));
            float3 NormalizedVector = Vector / Norm;

            float3 ResultantVelocity = SubjectsVelocity.Linear + NormalizedVector * Pather.Speed;
            if(ResultantVelocity.x <= Pather.MAX_SPEED)
            {
                SubjectsVelocity.Linear.x = ResultantVelocity.x;
            }
            if(ResultantVelocity.z <= Pather.MAX_SPEED)
            {
                SubjectsVelocity.Linear.z = ResultantVelocity.z;
            }

            SubjectsVelocity.Linear.y = ResultantVelocity.y;
            // SubjectsVelocity.Angular = float3.zero;

            // FireProjectileSystem.FireProjectileEvent NewEvent = new FireProjectileSystem.FireProjectileEvent(PrefabConverter.projectile, LTW);
            
            //streamWriter.Write(NewEvent);

            //FireProjectileSystem.FireProjectile(PrefabConverter.projectile, LTW, EntityIndex);
        }
    }
}
