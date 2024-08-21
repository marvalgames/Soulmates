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
    public partial class ResourceRaycastSystem : SystemBase
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



            var deps = Entities.WithoutBurst().WithNone<DestroyComponent>().ForEach((
                ref ResourceItemComponent resourceItemComponent,
                ref LocalTransform localTransform,
                in Entity entity
            ) =>
            {
                //var physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
                //var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

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


                if (hasPointHit && resourceItemComponent.itemPickedUp == false)
                {
                    if (SystemAPI.HasComponent<TriggerComponent>(pointHit.Entity))
                    {
                        var parent = SystemAPI.GetComponent<TriggerComponent>(pointHit.Entity).ParentEntity;
                        var e = collisionWorld.Bodies[pointHit.RigidBodyIndex].Entity;
                        pickedUpActor = parent;

                        if (SystemAPI.HasComponent<EnemyComponent>(pickedUpActor) == false)
                        {
                            resourceItemComponent.pickedUpActor = pickedUpActor;
                            resourceItemComponent.addPickupEntityToInventory = pickedUpActor;
                            resourceItemComponent.itemPickedUp = true;
                            localTransform.Position.y -= -100;
                            ecb.AddComponent(entity, resourceItemComponent);
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








    public partial class ResourceAttachSystem : SystemBase//attach to player ie grabs money then add to player currency
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
                ref ResourceItemComponent resourceItemComponent,
                in LocalTransform localTransform,
                in Entity entity
            ) =>
            {

                pickedUpActor = resourceItemComponent.pickedUpActor;
                if (pickedUpActor == Entity.Null) return;
                if (SystemAPI.HasComponent<EnemyComponent>(pickedUpActor) == true) return;


                if (SystemAPI.HasComponent<CurrencyComponent>(entity) && resourceItemComponent.enabled == false)
                {
                    //tr.gameObject.SetActive(false);
                    usedItem = entity.Index;
                    var currencyPower = SystemAPI.GetComponent<CurrencyComponent>(entity);
                    resourceItemComponent.count -= 1;//na
                    resourceItemComponent.enabled = true;
                    var instanceEntity = resourceItemComponent.particleSystemEntity;

                    var ps = new PickupSystemComponent
                    {
                        followActor = false,
                        pickedUpActor = pickedUpActor
                    };

                    ecb.AddComponent(instanceEntity, ps);

                    //Debug.Log("instance entity " + instanceEntity);

                    var currencyPlayer = new CurrencyComponent()
                    {
                        psAttached = instanceEntity,//attached to player on speed pick up
                        pickedUpActor = pickedUpActor,
                        itemEntity = entity,
                        enabled = true,
                        currencyValue = currencyPower.currencyValue
                    };
                    show = 1;
                    //ecb.AddComponent(entity, new DestroyComponent());
                    ecb.AddComponent(pickedUpActor, currencyPlayer);
                
                }



            }).Run();

            Entities.WithoutBurst().WithStructuralChanges().ForEach(
                (in ShowMessageMenuComponent messageMenuComponent, in ShowMessageMenuGroup messageMenu) =>
                {

                    if (show == 1)
                    {
                        messageMenu.messageString = "... RESOURCE ... ";
                        messageMenu.ShowMenu();
                    }

                }
            ).Run();

      
            ecb.Playback(EntityManager);
            ecb.Dispose();


        }





    }
}