using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BeginFixedStepSimulationEntityCommandBufferSystem))]
public partial class PlayerMovementSystem : SystemBase
{
    EntityCommandBufferSystem ecbs => World.GetOrCreateSystem<EntityCommandBufferSystem>();


    Entity projectilePrefab;

    const float PLAYER_SPEED = 0.5f;
    const float MAX_PLAYER_SPEED = 3.0f;
    const float FIRING_COOLDOWN_TIME = 0.1f;

    double timeOfLastFireEvent;
    double CurrentTime;

    (KeyCode Key, float3 Direction)[] movementDirections = new (KeyCode, float3)[]
    {
        (KeyCode.W, new float3(0.0f, 0.0f, 1.0f)),
        (KeyCode.S, new float3(0.0f, 0.0f, -1.0f)),
        (KeyCode.A, new float3(-1.0f, 0.0f, 0.0f)),
        (KeyCode.D, new float3(1.0f, 0.0f, 0.0f)),
    };


    protected override void OnCreate()
    {
        projectilePrefab = PrefabConverter.Convert((GameObject)Resources.Load("Prefabs/Projectile"));
    }

    protected override void OnUpdate()
    {
        CurrentTime = Time.ElapsedTime;

        EntityCommandBuffer Ecb = ecbs.CreateCommandBuffer();

        Entities.WithoutBurst().WithAll<PlayerTag>().ForEach((ref PhysicsVelocity Velocity, ref Rotation Rot, in Translation Trans, in LocalToWorld TransformMatrix) => 
        {
            foreach ((KeyCode Key, float3 Direction) Tuple in movementDirections)
            {
                if(Input.GetKey(Tuple.Key))
                {
                    float ResultantX = Velocity.Linear.x + Tuple.Direction.x * PLAYER_SPEED;
                    float ResultantZ = Velocity.Linear.z + Tuple.Direction.z * PLAYER_SPEED;

                    if(math.abs(ResultantX) < MAX_PLAYER_SPEED)
                    {
                        Velocity.Linear.x = ResultantX;
                    }
                    if(math.abs(ResultantZ) < MAX_PLAYER_SPEED)
                    {
                        Velocity.Linear.z = ResultantZ;
                    }
                }
            }

            float3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0.0f, 0.0f, 10.0f));
            MousePosition.y = 0.0f;

            float3 PlayerPos = Trans.Value;
            PlayerPos.y = 0.0f;

            Vector3 Vector = math.normalize(MousePosition - PlayerPos);

            Rot.Value = quaternion.LookRotation(Vector, TransformMatrix.Up);

            if(Input.GetMouseButton(0) && CurrentTime - timeOfLastFireEvent > FIRING_COOLDOWN_TIME)
            {
                FireProjectileSystem.FireProjectile(projectilePrefab, TransformMatrix, 0);
                timeOfLastFireEvent = CurrentTime;
            }

            TrackCameraToPlayer.PlayerPosition = Trans.Value;
        }).Run();
    }
}
