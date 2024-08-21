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
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class PickupPowerUpRaycastSystem : SystemBase
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
            var ecb = new EntityCommandBuffer(Allocator.TempJob);


            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            bool updateMenu = false;


            var deps = Entities.WithNone<DestroyComponent>().ForEach((
                //Transform tr,
                ref PowerItemComponent powerItemComponent,
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
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Ground,//odd player collides with ground here but since raycast after
                        CollidesWith = (uint)CollisionLayer.Player,
                        GroupIndex = 0
                    }
                };



                var hasPointHit = collisionWorld.CalculateDistance(pointDistanceInput, out var pointHit);


                if (hasPointHit && powerItemComponent.itemPickedUp == false)
                {
                    if (SystemAPI.HasComponent<TriggerComponent>(pointHit.Entity))
                    {
                        var parent = SystemAPI.GetComponent<TriggerComponent>(pointHit.Entity).ParentEntity;
                    
                        var e = collisionWorld.Bodies[pointHit.RigidBodyIndex].Entity;
                        pickedUpActor = parent;

                        if (SystemAPI.HasComponent<EnemyComponent>(pickedUpActor) == false)
                        {
                            powerItemComponent.pickedUpActor = pickedUpActor;
                            //tr.gameObject.SetActive(false);
                            //Debug.Log(" pickup " + e);
                            powerItemComponent.addPickupEntityToInventory = pickedUpActor;
                            powerItemComponent.itemPickedUp = true;
                            //LocalTransform.Value.y -= -100;
                            ecb.AddComponent(entity, powerItemComponent);

                            updateMenu = true;
                        }

                    }
                }



            }).Schedule(this.Dependency);
            deps.Complete();

            PickupMenuGroup.UpdateMenu = updateMenu;


            ecb.Playback(EntityManager);
            ecb.Dispose();


        }





    }



    public partial class PickupInputPowerUpUseImmediateSystem : SystemBase//move to new file later 
    {

        protected override void OnUpdate()
        {
            //bool pickedUp = false;
            var pickedUpActor = Entity.Null;
            var usedItem = 0;
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var show = 0;//1 is speed 2 is health
        

            Entities.WithoutBurst().ForEach((
                //Transform tr,
                ref PowerItemComponent powerItemComponent,
                in ImmediateUseComponent immediateUseComponent,
                in LocalTransform LocalTransform,
                in Entity entity
            ) =>
            {
            
                pickedUpActor = powerItemComponent.pickedUpActor;
                if (pickedUpActor == Entity.Null) return;
                if (SystemAPI.HasComponent<EnemyComponent>(pickedUpActor) == true) return;


                if (SystemAPI.HasComponent<DashPower>(entity) && powerItemComponent.enabled == false)
                {
                    if (powerItemComponent.enabled == false)
                    {
                        //tr.gameObject.SetActive(false);
                        usedItem = entity.Index;
                        powerItemComponent.enabled = true;
                        var instanceEntity = powerItemComponent.particleSystemEntity;
                        var ps = new PickupSystemComponent
                        {
                            followActor = true,
                            pickedUpActor = pickedUpActor
                        };

                        ecb.AddComponent(instanceEntity, ps);

                        var power = SystemAPI.GetComponent<DashPower>(entity);
                        var PowerPlayer = new DashPower
                        {
                            psAttached = instanceEntity,//attached to player picking up
                            pickedUpActor = pickedUpActor,
                            itemEntity = entity,
                            enabled = true,
                        };//doesn't this get destroyed in powersSystem anyway? 
                    
                        ecb.AddComponent(entity, new DestroyComponent());
                        ecb.AddComponent(pickedUpActor, PowerPlayer);
                        //prefer how to use mechanic in powersSystem - leave for now
                        if (SystemAPI.HasComponent<PlayerDashComponent>(pickedUpActor))
                        {
                            var playerDash = SystemAPI.GetComponent<PlayerDashComponent>(pickedUpActor);
                            playerDash.active = true;
                            playerDash.uses += power.useIncrease;
                            SystemAPI.SetComponent(pickedUpActor, playerDash);
                        
                        }



                    }


                }





                if (SystemAPI.HasComponent<HealthPower>(entity) && powerItemComponent.enabled == false)
                {
                    if (powerItemComponent.enabled == false)
                    {
                        //tr.gameObject.SetActive(false);
                        usedItem = entity.Index;
                        powerItemComponent.enabled = true;
                        var instanceEntity = powerItemComponent.particleSystemEntity;
                        var ps = new PickupSystemComponent
                        {
                            followActor = false,
                            pickedUpActor = pickedUpActor
                        };

                        ecb.AddComponent(instanceEntity, ps);

                        var healthPower = SystemAPI.GetComponent<HealthPower>(entity);
                        var healthPowerPlayer = new HealthPower
                        {
                            psAttached = instanceEntity,//attached to player picking up
                            pickedUpActor = pickedUpActor,
                            itemEntity = entity,
                            enabled = true,
                            healthMultiplier = healthPower.healthMultiplier,
                            slowDown = healthPower.slowDown
                        };
                        show = 2;
                        if(healthPower.slowDown)
                        {
                            show = 3;
                        }
                        Debug.Log("HEALTH DESTROY");
                        ecb.AddComponent(entity, new DestroyComponent());
                        //ecb.AddComponent(instanceEntity, new DestroyComponent());
                        ecb.AddComponent(pickedUpActor, healthPowerPlayer);



                    }

                }





                if (SystemAPI.HasComponent<Speed>(entity) && powerItemComponent.enabled == false)
                {
                    //tr.gameObject.SetActive(false);
                    usedItem = entity.Index;
                    var speedPower = SystemAPI.GetComponent<Speed>(entity);
                    powerItemComponent.count -= 1;
                    powerItemComponent.enabled = true;
                    var instanceEntity = powerItemComponent.particleSystemEntity;

                    var ps = new PickupSystemComponent
                    {
                        followActor = true,
                        pickedUpActor = pickedUpActor
                    };

                    ecb.AddComponent(instanceEntity, ps);


                    var speedPowerPlayer = new Speed
                    {
                        psAttached = instanceEntity,//attached to player on  speed pick up
                        pickedUpActor = pickedUpActor,
                        itemEntity = entity,
                        enabled = true,
                        timeOn = speedPower.timeOn,
                        multiplier = speedPower.multiplier
                    };
                    show = 1;
                    ecb.RemoveComponent<ImmediateUseComponent>(entity);
                    ecb.AddComponent(entity, new DestroyComponent());
                    ecb.AddComponent(pickedUpActor, speedPowerPlayer);
                }



            }).Run();

            Entities.WithoutBurst().WithStructuralChanges().ForEach(
                (in ShowMessageMenuComponent messageMenuComponent, in ShowMessageMenuGroup messageMenu) =>
                {
                 
                    if (show == 1)
                    {
                        messageMenu.messageString = "... Speed BURST ... ";
                        messageMenu.ShowMenu();
                    }
                    else if (show == 2)
                    {
                        messageMenu.messageString = "... Health BOOST ... ";
                        messageMenu.ShowMenu();
                    }
                    else if (show == 3)
                    {
                        messageMenu.messageString = "... Health Damage Rate SLOWED ... ";
                        messageMenu.ShowMenu();
                    }

                }
            ).Run();

            if (SystemAPI.HasSingleton<PickupMenuComponent>())
            {
                var e = SystemAPI.GetSingletonEntity<PickupMenuComponent>();
                var pu = SystemAPI.GetComponent<PickupMenuComponent>(e);
                if(usedItem > 0)
                {
                    pu.usedItem = usedItem;
                }
                SystemAPI.SetSingleton(pu);

            }

            ecb.Playback(EntityManager);
            ecb.Dispose();


        }





    }
}