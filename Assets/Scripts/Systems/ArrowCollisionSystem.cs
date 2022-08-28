using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

public partial class ArrowCollisionSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    public static ArrowCollisionSystem Instance { get; set; }

    public BuildPhysicsWorld buildPhysicsWorld;
    public StepPhysicsWorld stepPhysicsWorld;

    protected override void OnStartRunning()
    {
        Instance = this;

        buildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetExistingSystem<StepPhysicsWorld>();

        this.RegisterPhysicsRuntimeSystemReadWrite();
    }


    protected override void OnUpdate()
    {
        Dependency = new CollisionEventJob<ArrowTag, EnemyTag>()
        {
            EntityCommandBuffer = Ecbs.CreateCommandBuffer(),
            CurrentTime = (float)Time.ElapsedTime,
            Type1s = GetComponentDataFromEntity<ArrowTag>(),
            Type2s = GetComponentDataFromEntity<EnemyTag>()
        }.Schedule(stepPhysicsWorld.Simulation, Dependency);

        Dependency.Complete();
    }

    private partial struct CollisionEventJob<T1, T2> : ICollisionEventsJob where T1 : struct, IComponentData
                                                                    where T2 : struct, IComponentData
    {
        public EntityCommandBuffer EntityCommandBuffer;
        [ReadOnly]
        public float CurrentTime;

        public ComponentDataFromEntity<T1> Type1s;

        public ComponentDataFromEntity<T2> Type2s;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity[] CollidingEntities = new Entity[] { collisionEvent.EntityA, collisionEvent.EntityB };

            for (int i = 0; i < CollidingEntities.Length; i++)
            {
                Entity E = CollidingEntities[i];

                ArrowCollided(E, EntityCommandBuffer, Type1s);
            }

            if (CollidingTypes(collisionEvent.EntityA, collisionEvent.EntityB, Type1s, Type2s, out CollidingThings Result))
            {
                EntityCommandBuffer.AddComponent<DestroyEntityTag>(Result.entityOfType2);
            }
        }

        private void ArrowCollided(Entity E, EntityCommandBuffer ECB, ComponentDataFromEntity<T1> ArrowTags)
        {
            if (ArrowTags.HasComponent(E))
            {
                ECB.RemoveComponent<PhysicsWorldIndex>(E);

                DestroyEntityAfterTime Comp = new DestroyEntityAfterTime()
                {
                    TimeCreated = CurrentTime,
                    TimeToDestroy = CurrentTime + 1.0f
                };

                ECB.AddComponent(E, Comp);
            }
        }

        #region collisionFunc

        public struct CollidingThings
        {
            public Entity entityOfType1;
            public Entity entityOfType2;
            public T1 outType1;
            public T2 outType2;
        }

        public bool CollidingTypes(Entity A,
                                   Entity B,
                                   ComponentDataFromEntity<T1> Type1,
                                   ComponentDataFromEntity<T2> Type2,
                                   out CollidingThings Result)

        {
            Result = new CollidingThings();


            /*
             *   BIT    |  MEANING 
             * ---------------------
             *   0      |  Collision occurred 
             *   1      |  T1 is zero sized
             *   2      |  T2 is zero sized
             */
            BitField32 collided = new BitField32();
            collided.SetBits(0, false);

            collided.SetBits(1, ComponentType.FromTypeIndex(TypeManager.GetTypeIndex(typeof(T1))).IsZeroSized);
            collided.SetBits(2, ComponentType.FromTypeIndex(TypeManager.GetTypeIndex(typeof(T2))).IsZeroSized);

            Result.entityOfType1 = default(Entity);
            Result.entityOfType2 = default(Entity);
            Result.outType1 = default(T1);
            Result.outType2 = default(T2);

            if (Type1.HasComponent(A) && Type2.HasComponent(B))
            {
                Result.entityOfType1 = A;
                Result.entityOfType2 = B;

                Result.outType1 = collided.GetBits(1) == 1 ? default(T1) : Type1[A];
                Result.outType2 = collided.GetBits(2) == 1 ? default(T2) : Type2[B];

                collided.SetBits(0, true);
            }

            if (Type1.HasComponent(B) && Type2.HasComponent(A))
            {
                Result.entityOfType1 = B;
                Result.entityOfType2 = A;

                Result.outType1 = collided.GetBits(1) == 1 ? default(T1) : Type1[B];
                Result.outType2 = collided.GetBits(2) == 1 ? default(T2) : Type2[A];

                collided.SetBits(0, true);
            }

            return collided.GetBits(0) == 1;
        }

        #endregion
    }
}
