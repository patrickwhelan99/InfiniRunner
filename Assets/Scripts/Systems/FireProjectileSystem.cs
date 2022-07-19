using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(DestroyEntitySystem))]
[AlwaysUpdateSystem]
public partial class FireProjectileSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();
    static NativeStream events;
    static NativeStream.Writer streamWriter;

    NativeArray<FireProjectileEvent> EventsArray;

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

    protected override void OnCreate()
    {
        events = new NativeStream(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount, Allocator.Persistent);
        streamWriter = events.AsWriter();
        for (int i = 0; i < events.AsWriter().ForEachCount; i++)
        {
            streamWriter.BeginForEachIndex(i);
        }

        streamWriter.EndForEachIndex();
    }

    protected override void OnUpdate()
    {
        streamWriter.EndForEachIndex();
        EventsArray = events.ToNativeArray<FireProjectileEvent>(Allocator.TempJob);

        Dependency = new ProcessEvents()
        {
            Ecb = ecbs.CreateCommandBuffer().AsParallelWriter(),
            Events = EventsArray
        }.Schedule(EventsArray.Length, 1);

        Dependency.Complete();

        EventsArray.Dispose();
        events.Dispose();
        events = new NativeStream(Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount, Allocator.Persistent);
        streamWriter = events.AsWriter();
        for (int i = 0; i < events.AsWriter().ForEachCount; i++)
        {
            streamWriter.BeginForEachIndex(i);
        }
    }

    protected override void OnDestroy()
    {
        events.Dispose();

        if(EventsArray != null && EventsArray.IsCreated)
        {
            EventsArray.Dispose();
        }
    }

    public partial struct ProcessEvents : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeArray<FireProjectileEvent> Events;
        public void Execute(int index)
        {
            FireProjectileEvent Event = Events[index];

            Entity FiredProjectile = Ecb.Instantiate(index, Event.Projectile);

            Rotation ProjectileRotation = new Rotation()
            {
                Value = Event.TransformMatrix.Rotation * Quaternion.Euler(90.0f, 0.0f, 0.0f)
            };
            Ecb.SetComponent<Rotation>(index, FiredProjectile, ProjectileRotation);

            Translation ProjectileTranslation = new Translation()
            {
                Value = Event.TransformMatrix.Position + Event.TransformMatrix.Forward * 2.0f
            };
            Ecb.SetComponent<Translation>(index, FiredProjectile, ProjectileTranslation);
        }
    }

    public static void FireProjectile(Entity Projectile, LocalToWorld TransformMatrix, int ThreadIndex)
    {
        FireProjectileEvent NewEvent = new FireProjectileEvent(Projectile, TransformMatrix);
        streamWriter.Write(NewEvent);
    }
}
