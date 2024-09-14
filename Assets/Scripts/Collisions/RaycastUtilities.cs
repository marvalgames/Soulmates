using Collisions;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public static class RaycastUtilities
    {
        // Static method to perform raycasting
        public static bool PerformRaycast(PhysicsWorldSingleton collisionWorld, float3 start, float3 end,
            out Unity.Physics.RaycastHit hit)
        {
            // Set up the raycast input
            var rayInput = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = CollisionFilter.Default // Adjust collision layers as needed
            };

            // Perform the raycast
            return collisionWorld.CastRay(rayInput, out hit);
        }

        public static float ExecuteRaycast(PhysicsWorldSingleton collisionWorld, float3 position,
            RaycastComponent raycastComponent)
        {
            var yPosition = 0.0f;
            //var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var origin = position;
            var direction = math.normalize(new float3(0, -1, 0));

            var rayInput = new RaycastInput
            {
                Start = origin,
                End = origin + direction * 1000f,
                Filter = new CollisionFilter
                {
                    BelongsTo = (uint)CollisionLayer.Enemy,
                    CollidesWith = (uint)CollisionLayer.Terrain,
                    GroupIndex = 0
                }
            };

            if (collisionWorld.CastRay(rayInput, out var hit))
            {
                raycastComponent.HitPosition = hit.Position;
                raycastComponent.HasHit = true;
                yPosition = raycastComponent.HitPosition.y;
            }
            else
            {
                raycastComponent.HasHit = false;
            }

            return yPosition;
        }

    }
}

