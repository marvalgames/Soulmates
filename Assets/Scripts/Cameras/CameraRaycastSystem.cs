using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;




//[UpdateAfter(typeof(Unity.Physics.Systems.AfterPhysicsSystemGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class CameraRaycastSystem : SystemBase
{


    private enum CollisionLayer
    {
        Player = 1 << 0,
        Ground = 1 << 1,
        Enemy = 1 << 2,
        WeaponItem = 1 << 3,
        Obstacle = 1 << 4,
        NPC = 1 << 5,
        PowerUp = 1 << 6,
        Stairs = 1 << 7,
        Particle = 1 << 8,
        Camera = 1 << 9
    }


    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        Entities.WithoutBurst().ForEach((Entity entity, in CameraControlsComponent cameraControls) =>
        {
            if (cameraControls.active == false) return;
            var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
            //var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var LocalTransform = SystemAPI.GetComponent<LocalTransform>(entity);
           


            var start = LocalTransform.Position + new float3(0, 0, 0);
            var direction = new float3(0, 0, 1);
            float distance = 100;
            var end = start + direction * distance;


            var inputForward = new RaycastInput()
            {
                Start = start,
                End = end,
                //Filter = CollisionFilter.Default
                Filter = new CollisionFilter()
                {
                    BelongsTo = (uint)CollisionLayer.Camera,
                    CollidesWith = (uint)CollisionLayer.Enemy,
                    GroupIndex = 0
                }
            };
            var hitForward = new Unity.Physics.RaycastHit();
            Debug.DrawRay(inputForward.Start, direction, Color.green, distance);

            var hasPointHitForward = collisionWorld.CastRay(inputForward, out hitForward);

            if (hasPointHitForward)
            {
                var e = collisionWorld.Bodies[hitForward.RigidBodyIndex].Entity;

            }




        }).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();




    }
}











