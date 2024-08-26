using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Collisions
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class TalentRaycastSystem : SystemBase
    {

        public enum CollisionLayer
        {
            Player = 1 << 0,
            Ground = 1 << 1,
            Enemy = 1 << 2,
            Powerup = 1 << 6,
        }

    
        protected override void OnUpdate()
        {
            Entity pickedUpActor = default;
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            bool updateMenu = false;

            var deps = Entities.WithNone<DestroyComponent>().ForEach((
                ref TalentItemComponent talentItemComponent,
                ref LocalTransform localTransform,
                in Entity entity
            ) =>
            {
                var start = localTransform.Position + new float3(0f, .38f, 0);
                var direction = new float3(0, 0, 0);
                var distance = 2f;
                var end = start + direction * distance;
                var pointDistanceInput = new PointDistanceInput
                {
                    Position = start,
                    MaxDistance = distance,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = (uint)CollisionLayer
                            .Ground, //odd player collides with ground here but since raycast after
                        CollidesWith = (uint)CollisionLayer.Player,
                        GroupIndex = 0
                    }
                };
                var hasPointHit = collisionWorld.CalculateDistance(pointDistanceInput, out var pointHit);
                if (hasPointHit && talentItemComponent.itemPickedUp == false)
                {
                    if (SystemAPI.HasComponent<TriggerComponent>(pointHit.Entity))
                    {
                        var parent = SystemAPI.GetComponent<TriggerComponent>(pointHit.Entity).ParentEntity;
                        //var e = physicsWorldSystem.PhysicsWorld.Bodies[pointHit.RigidBodyIndex].Entity;
                        var e = collisionWorld.Bodies[pointHit.RigidBodyIndex].Entity;
                        pickedUpActor = parent;

                        if (SystemAPI.HasComponent<EnemyComponent>(pickedUpActor) == false)
                        {
                            talentItemComponent. pickedUpActor = pickedUpActor;
                            talentItemComponent.addPickupEntityToInventory = pickedUpActor;
                            talentItemComponent.itemPickedUp = true;
                            localTransform.Position.y -= -100;
                            ecb.AddComponent(entity, talentItemComponent);
                            updateMenu = true;
                        }

                    }
                }



            }).Schedule(Dependency);
            deps.Complete();
            PickupMenuGroup.UpdateMenu = updateMenu;

            ecb.Playback(EntityManager);
            ecb.Dispose();


        }





    }
}












