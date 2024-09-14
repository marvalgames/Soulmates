using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class SphereRaycastSystem : SystemBase
    {
        private PhysicsSystemGroup _physicsSystemGroup;

        protected override void OnCreate()
        {
            _physicsSystemGroup = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PhysicsSystemGroup>();
        }


        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            if (SystemAPI.HasSingleton<PhysicsWorldSingleton>() == false) return;


            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();


            var inputDeps0 = Entities.ForEach((Entity entity, ref AmmoComponent ammoComponent,
                in LocalTransform localTransform) =>
            {
                var start = localTransform.Position;
                var radius = 5f;
                var distance = 5.0f;

                var pointDistanceInput = new PointDistanceInput
                {
                    Position = start,
                    MaxDistance = distance,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.WeaponItem,
                        CollidesWith = (uint)CollisionLayer.Enemy,
                        GroupIndex = 0
                    }
                };


                NativeList<DistanceHit> pointHits = new NativeList<DistanceHit>(Allocator.Temp);
                var hasPointHit = collisionWorld.OverlapSphere(start, radius, ref pointHits, pointDistanceInput.Filter);
                if (hasPointHit && !ammoComponent.isColliding)
                {
                    ammoComponent.isColliding = true;
                    //Debug.Log("Colliding");
                    var hitEntity = pointHits[0].Entity;
                    var collisionComponent =
                        new CollisionComponent()
                        {
                            Character_entity = entity,
                            Character_other_entity = hitEntity
                        };
                    ecb.AddComponent(entity, collisionComponent);
                }
                else if (hasPointHit)
                {
                    ammoComponent.frameSkipCounter++;
                    if (ammoComponent.frameSkipCounter > ammoComponent.framesToSkip)
                    {
                        ammoComponent.isColliding = false;
                        ammoComponent.frameSkipCounter = 0;
                        //Debug.Log("Colliding Already");
                    }
                }
            }).Schedule(this.Dependency);

            inputDeps0.Complete();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}