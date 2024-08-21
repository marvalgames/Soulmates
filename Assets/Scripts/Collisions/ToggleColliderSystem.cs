using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

//using UnityEngine;


namespace Collisions
{

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(global::Collisions.ToggleColliderSystem))]

    public partial class SetDefaultColliderSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithStructuralChanges().WithNone<ToggleFilterComponent>()
                .ForEach((Entity e, in PhysicsCollider collider) =>
                    {
                        if (collider.IsValid)
                        {
                            var filter = collider.Value.Value.GetCollisionFilter();
                            //Debug.Log("FILTER " + filter);
                            EntityManager.AddComponentData
                                (e, new ToggleFilterComponent()
                                {
                                    defaultFilter = filter
                                }

                                );
                        }
                    }
                ).Run();
        }
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(global::Sandbox.Player.PlayerDashSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class ToggleColliderSystem : SystemBase
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
            Camera = 1 << 9,
            Crosshair = 1 << 10,
            Breakable = 1 << 11
        }


        protected override void OnCreate()
        {
            var query = GetEntityQuery
            (
                ComponentType.ReadOnly<ActorCollisionBufferElement>()
            );

            RequireForUpdate(query);
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var actorCollisionBufferElement = GetBufferLookup<ActorCollisionBufferElement>(true);


            var inputDeps = 
                Entities.ForEach((Entity e, ref PlayerDashComponent playerDashComponent) =>
                {
                    var actorCollisionElement = actorCollisionBufferElement[e];
                    if (actorCollisionElement.Length <= 0 || playerDashComponent.active == false)
                        return;

                    var addColliders = false;
                    var removeColliders = false;
                    bool hasActorWeaponAim = SystemAPI.HasComponent<ActorWeaponAimComponent>(e);


                    var hasToggleCollision =
                        SystemAPI.HasComponent<ToggleCollisionComponent>(e);
                    //SystemAPI.HasComponent<ToggleCollisionComponent>(e) && SystemAPI.HasComponent<ToggleFilterComponent>(e);
                    playerDashComponent.Invincible = false;
                    if (playerDashComponent.DashTimeTicker >= playerDashComponent.invincibleStart &&
                        playerDashComponent.DashTimeTicker < playerDashComponent.invincibleEnd)
                    {
                        playerDashComponent.Invincible = true;
                        if (hasToggleCollision)
                        {
                            ecb.RemoveComponent<ToggleCollisionComponent>(e);
                            //Debug.Log("remove colliders");
                            removeColliders = true;
                        }
                    }
                    else if ((playerDashComponent.DashTimeTicker >= playerDashComponent.invincibleEnd ||
                              playerDashComponent.DashTimeTicker == 0) && hasToggleCollision == false)
                    {
                        ecb.AddComponent(e, new ToggleCollisionComponent { });
                        addColliders = true;//set default colliders back
                    }

                    if (hasActorWeaponAim && removeColliders)
                    {
                        var actorWeaponAimComponent = SystemAPI.GetComponent<ActorWeaponAimComponent>(e);
                        actorWeaponAimComponent.startDashAimMode = actorWeaponAimComponent.aimMode;
                        actorWeaponAimComponent.aimMode = false;
                        SystemAPI.SetComponent(e, actorWeaponAimComponent);
                    }
                    else if (hasActorWeaponAim && addColliders)
                    {
                        var actorWeaponAimComponent = SystemAPI.GetComponent<ActorWeaponAimComponent>(e);
                        actorWeaponAimComponent.aimMode = actorWeaponAimComponent.startDashAimMode;
                        SystemAPI.SetComponent(e, actorWeaponAimComponent);
                    }

                    for (var i = 0; i < actorCollisionElement.Length; i++) //fix for ecs 1.0
                    {
                        var childEntity = actorCollisionElement[i]._child;
                        bool hasToggleFilter = SystemAPI.HasComponent<ToggleFilterComponent>(childEntity);
                        bool hasPhysicsCollider = SystemAPI.HasComponent<PhysicsCollider>(childEntity);
                        if (!hasPhysicsCollider || !hasToggleFilter) continue;
                         //recent change: player probably doesn't have collider only child colliders  

                        if (addColliders)
                        {
                            var collider = SystemAPI.GetComponent<PhysicsCollider>(childEntity);
                            var filter = SystemAPI.GetComponent<ToggleFilterComponent>(childEntity).defaultFilter;
                            collider.Value.Value.SetCollisionFilter(filter);
                            //CollisionFilter r = collider.Value.Value.GetCollisionFilter();
                            Debug.Log("ADD COLLIDERS");
                            ecb.SetComponent(childEntity, collider);
                        }
                        else if (removeColliders)
                        {
                            var collider = SystemAPI.GetComponent<PhysicsCollider>(childEntity);
                            Debug.Log("REMOVE COLLIDERS");
                            var filter = new CollisionFilter()
                            {
                                BelongsTo = (uint)CollisionLayer.Player,
                                CollidesWith = (uint)CollisionLayer.Ground
                            };
                            collider.Value.Value.SetCollisionFilter(filter);
                            ecb.SetComponent(childEntity, collider);
                        }
                    }
                }
            ).Schedule(this.Dependency);

            inputDeps.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}