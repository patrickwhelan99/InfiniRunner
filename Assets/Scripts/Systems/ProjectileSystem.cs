using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using Unity.Physics;
using Unity.Physics.Systems;


public partial class ProjectileSystem : SystemBase
{
    // Physics stuff
    StepPhysicsWorld stepPhysicsWorld => World.GetOrCreateSystem<StepPhysicsWorld>();
    BuildPhysicsWorld buildPhysicsWorld => World.GetOrCreateSystem<BuildPhysicsWorld>();
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    EntityQuery ActiveProjectilesQuery;
    EntityQuery PlayerQuery;


    const float PROJECTILE_SPEED = 0.33f;
    const int PROJECTILE_CULL_DISTANCE = 150;

    protected override void OnCreate()
    {
        EntityQueryDesc Desc = new EntityQueryDesc()
        {
            All = new ComponentType[] {typeof(ProjectileTag)},
            None = new ComponentType[] {typeof(DestroyEntityTag)},
        };

        ActiveProjectilesQuery = EntityManager.CreateEntityQuery(Desc);
        PlayerQuery = EntityManager.CreateEntityQuery(typeof(PlayerTag));
    }


    protected override void OnStartRunning()
    {
        buildPhysicsWorld.RegisterPhysicsRuntimeSystemReadOnly();
    }

    
    protected override void OnUpdate()
    {
        CullProjectilesJob CullingJob = new CullProjectilesJob()
        {
            writer = ecbs.CreateCommandBuffer().AsParallelWriter()
        };
        Dependency = CullingJob.ScheduleParallel(ActiveProjectilesQuery);


        CollisionEventJob CollisionJob = new CollisionEventJob()
        {
            ecb = ecbs.CreateCommandBuffer(),
            enemies = GetComponentDataFromEntity<EnemyTag>(true),
            projectiles = GetComponentDataFromEntity<ProjectileTag>(true),
            players = GetComponentDataFromEntity<PlayerTag>(true),
        };
        Dependency = CollisionJob.Schedule(stepPhysicsWorld.Simulation, Dependency);


        ecbs.AddJobHandleForProducer(Dependency);

        Dependency.Complete();
    }


    partial struct CullProjectilesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter writer;

        public void Execute(Entity Ent, ref Translation Trans, in LocalToWorld Ltw)
        {
            if(Trans.Value.x > PROJECTILE_CULL_DISTANCE || Trans.Value.z > PROJECTILE_CULL_DISTANCE || Trans.Value.x < -PROJECTILE_CULL_DISTANCE || Trans.Value.z < -PROJECTILE_CULL_DISTANCE)
            {
                writer.AddComponent<DestroyEntityTag>(0, Ent);
            }

            Trans.Value += Ltw.Up * PROJECTILE_SPEED;
        }
    }


    [BurstCompile]
    public struct CollisionEventJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyTag> enemies;
        [ReadOnly]
        public ComponentDataFromEntity<ProjectileTag> projectiles;
        [ReadOnly]
        public ComponentDataFromEntity<PlayerTag> players;


        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity A = collisionEvent.EntityA;
            Entity B = collisionEvent.EntityB;

            if(CollidingTypes(A, B, enemies, projectiles, out Entity Enemy, out Entity _, out EnemyTag _, out ProjectileTag _))
            {
                ecb.AddComponent<DestroyEntityTag>(Enemy);
            }

            if(CollidingTypes(A, B, enemies, players, out Entity _, out Entity Player, out EnemyTag _, out PlayerTag _))
            {
                ecb.AddComponent<DestroyEntityTag>(Player);
            }
        }
    }

    public struct PlayerCollisionJob : ICollisionEventsJob
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly]
        public ComponentDataFromEntity<EnemyTag> enemies;
        [ReadOnly]
        public ComponentDataFromEntity<PlayerTag> players;


        // [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            // Entity A = collisionEvent.EntityA;
            // Entity B = collisionEvent.EntityB;

            // if(CollidingTypes(A, B, enemies, players, out Entity _, out Entity Player, out EnemyTag _, out PlayerTag _))
            // {
            //     ecb.AddComponent<DestroyEntityTag>(0, Player);
            // }

            if(players.HasComponent(collisionEvent.EntityA))
            {
                ecb.AddComponent<DestroyEntityTag>(0, collisionEvent.EntityA);
            }
        }
    }




    #region collisionFunc
    [BurstCompile]
    public static bool CollidingTypes<T1, T2>   (  
                                                    Entity A, 
                                                    Entity B, 
                                                    ComponentDataFromEntity<T1> Type1, 
                                                    ComponentDataFromEntity<T2> Type2,
                                                    out Entity entityOfType1,
                                                    out Entity entityOfType2,
                                                    out T1 outType1,
                                                    out T2 outType2
                                                ) 
                                                where T1 : struct, IComponentData
                                                where T2 : struct, IComponentData
        {

            /*
             *   BIT    |  MEANING 
             * ---------------------
             *   0      |  Collision occurred 
             *   1      |  T1 is zero sized
             *   2      |  T2 is zero sized
             */
            BitField32 collided = new BitField32();
            collided.SetBits(0, false);


            collided.SetBits(1, ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<T1>()).IsZeroSized);
            collided.SetBits(2, ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<T2>()).IsZeroSized);

            entityOfType1 = default(Entity);
            entityOfType2 = default(Entity);
            outType1 = default(T1);
            outType2 = default(T2);

            if((Type1.HasComponent(A) && Type2.HasComponent(B)))
            {
                entityOfType1 = A;
                entityOfType2 = B;

                outType1 = collided.GetBits(1) == 1 ? default(T1) : Type1[A];
                outType2 = collided.GetBits(2) == 1 ? default(T2) : Type2[B];

                collided.SetBits(0, true);
            }

            if((Type1.HasComponent(B) && Type2.HasComponent(A)))
            {
                entityOfType1 = B;
                entityOfType2 = A;

                outType1 = collided.GetBits(1) == 1 ? default(T1) : Type1[B];
                outType2 = collided.GetBits(2) == 1 ? default(T2) : Type2[A];

                collided.SetBits(0, true);
            }

            return collided.GetBits(0) == 1;
        }

        #endregion
}
