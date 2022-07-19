using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

public partial class ArrowSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        Dependency = new RotateToFaceVelocity().ScheduleParallel();
    }

    partial struct RotateToFaceVelocity : IJobEntity
    {
        public void Execute([EntityInQueryIndex] int Index, ref PhysicsVelocity Velocity, ref Rotation Rot, in ArrowTag arrowTag)
        {
            // Rotate to face the player
            Rot.Value = quaternion.LookRotation(math.normalize(Velocity.Linear), new float3(0, 1, 0));
            Rot.Value = math.mul(Rot.Value, quaternion.RotateX(math.radians(90.0f)));
        }
    }
}
