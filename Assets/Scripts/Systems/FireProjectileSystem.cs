using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(DestroyEntitySystem))]
[AlwaysUpdateSystem]
public partial class FireProjectileSystem : SystemBase
{
    private EntityCommandBufferSystem Ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    private static NativeStream events;
    private static NativeStream.Writer streamWriter;
    private NativeArray<FireProjectileEvent> EventsArray;

    public struct FireProjectileEvent
    {
        public Entity Projectile;
        public LocalToWorld TransformMatrix;

        public FireProjectileEvent(Entity ToSpawn, LocalToWorld LTW)
        {
            Projectile = ToSpawn;
            TransformMatrix = LTW;
        }
    }

    protected override void OnUpdate()
    {

        if (events.IsCreated && !events.IsEmpty())
        {
            EventsArray = events.ToNativeArray<FireProjectileEvent>(Allocator.TempJob);

            Dependency = new ProcessEvents()
            {
                Ecb = Ecbs.CreateCommandBuffer().AsParallelWriter(),
                Events = EventsArray,
                Time = (float)Time.ElapsedTime
            }.Schedule(EventsArray.Length, 1);

            Dependency.Complete();

            EventsArray.Dispose();
            events.Dispose();
        }
    }

    protected override void OnDestroy()
    {
        if (events.IsCreated)
        {
            events.Dispose();
        }

        if (EventsArray != null && EventsArray.IsCreated)
        {
            EventsArray.Dispose();
        }
    }

    public partial struct ProcessEvents : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeArray<FireProjectileEvent> Events;
        public float Time;
        public void Execute(int index)
        {
            FireProjectileEvent Event = Events[index];

            Entity FiredProjectile = Ecb.Instantiate(index, Event.Projectile);

            Rotation ProjectileRotation = new Rotation()
            {
                Value = Event.TransformMatrix.Rotation * Quaternion.Euler(90.0f, 0.0f, 0.0f)
            };
            Ecb.SetComponent(index, FiredProjectile, ProjectileRotation);

            Translation ProjectileTranslation = new Translation()
            {
                Value = Event.TransformMatrix.Position + (Event.TransformMatrix.Forward * 2.0f)
            };
            Ecb.SetComponent(index, FiredProjectile, ProjectileTranslation);

            Ecb.AddComponent(index, FiredProjectile, new DestroyEntityAfterTime()
            {
                TimeCreated = Time,
                TimeToDestroy = Time + 3.0f
            });
        }
    }

    public static void FireProjectile(Entity Projectile, LocalToWorld TransformMatrix)
    {
        if (!events.IsCreated)
        {
            events = new NativeStream(1, Allocator.Persistent);
            streamWriter = events.AsWriter();
            streamWriter.BeginForEachIndex(0);
        }

        FireProjectileEvent NewEvent = new FireProjectileEvent(Projectile, TransformMatrix);
        streamWriter.Write(NewEvent);
        streamWriter.EndForEachIndex();
    }
}
