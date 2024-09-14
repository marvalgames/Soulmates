using Collisions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Sandbox.Player
{
    //Schedule the Job
    public partial struct RaycastHeightSystem : ISystem
    {
        [BurstCompile]
        public partial struct RaycastHeightJob : IJobEntity
        {
            [ReadOnly] public PhysicsWorldSingleton CollisionWorld;

            public void Execute(ref RaycastComponent raycastComponent, in LocalTransform LocalTransform)
            {
                var origin = LocalTransform.Position;
                var direction = math.normalize(new float3(0, -1, 0));
                var rayInput = new RaycastInput
                {
                    Start = origin,
                    End = origin + direction * 1000f,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Player,
                        CollidesWith = (uint)CollisionLayer.Terrain,
                        GroupIndex = 0
                    }
                };

                // Perform the raycast
                if (CollisionWorld.CastRay(rayInput, out var hit))
                {
                    raycastComponent.HitPosition = hit.Position;
                    raycastComponent.HasHit = true;
                }
                else
                {
                    raycastComponent.HasHit = false;
                }
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Bot>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Retrieve the current physics world (required for raycasting)
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            // Schedule the raycast job
            var raycastJob = new RaycastHeightJob()
            {
                CollisionWorld = collisionWorld
            };

            // Schedule the job to run in parallel over entities with RaycastComponent
            raycastJob.ScheduleParallel();
        }
    }
}