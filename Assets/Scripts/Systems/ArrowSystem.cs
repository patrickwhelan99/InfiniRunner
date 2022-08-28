using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

public partial class ArrowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Dependency = new RotateToFaceVelocity().ScheduleParallel();
    }

    private partial struct RotateToFaceVelocity : IJobEntity
    {
        public void Execute(ref PhysicsVelocity Velocity, ref Rotation Rot, in ArrowTag _)
        {
            // Rotate to face the player
            Rot.Value = quaternion.LookRotation(math.normalize(Velocity.Linear), new float3(0, 1, 0));
            Rot.Value = math.mul(Rot.Value, quaternion.RotateX(math.radians(90.0f)));
        }
    }
}
