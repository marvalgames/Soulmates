
using Collisions;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;



[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]


public partial class BossAmmoHandlerSystem : SystemBase
{
    //BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
//        m_EntityCommandBufferSystem = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
        //ecbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {

        //if (LevelManager.instance.endGame == true) return;
        if (LevelManager.instance.endGame == true || LevelManager.instance.gameResult == GameResult.Loser ||
            LevelManager.instance.gameResult == GameResult.Winner) return;
        

        EntityQuery playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        NativeArray<Entity> playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
        int players = playerEntities.Length;
        if(players == 0) return;

        var dt = SystemAPI.Time.DeltaTime;//gun duration
        var positionBuffer = GetBufferLookup<BossWaypointBufferElement>(true);

        //var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        var ammoGroup = SystemAPI.GetComponentLookup<AmmoComponent>(false);
        var dep = Entities.WithNone<Pause>().ForEach(
            (
                 Entity entity,
                 ref BossAmmoManagerComponent bulletManagerComponent,
                in BossMovementComponent bossMovementComponent,
                in BossStrategyComponent bossStrategyComponent,
                in DefensiveStrategyComponent defensiveStrategyComponent
                 ) =>
            {

                var targetPointBuffer = positionBuffer[entity];
                if (targetPointBuffer.Length <= 0)
                    return;

                var playerE = defensiveStrategyComponent.closestEnemiesAttackEntity;
                
                if (!SystemAPI.HasComponent<BossWeaponComponent>(entity)) return;

                var bossWeapon = SystemAPI.GetComponent<BossWeaponComponent>(entity);

                var primaryAmmoEntity = bossWeapon.PrimaryAmmo;
                var ammoDataComponent = SystemAPI.GetComponent<AmmoDataComponent>(primaryAmmoEntity);
                var rate = ammoDataComponent.GameRate;
                var strength = ammoDataComponent.GameStrength;
                
                if (bossWeapon.ChangeAmmoStats > 0)
                {
                    strength = strength * (100 - bossWeapon.ChangeAmmoStats * 2) / 100;
                    if (strength <= 0) strength = 0;
                }
                
                if (bossWeapon.IsFiring == 1)
                {
                    bossWeapon.IsFiring = 0;
                    //var playerMove = SystemAPI.GetComponent<LocalTransform>(playerE);
                    var bossLocalTransform = SystemAPI.GetComponent<LocalTransform>(entity);
                    var e = commandBuffer.Instantiate(bossWeapon.PrimaryAmmo);
                    //var ammoStartTransform = new LocalTransform() { Position = bossWeapon.AmmoStartTransform.Position };//use bone mb transform
                    var ammoStartTransform = LocalTransform.FromPosition(bossWeapon.AmmoStartTransform.Position);//use bone mb transform
                    
                    var playerLocalTransform = SystemAPI.GetComponent<LocalTransform>(playerE);
                    var playerLocalPosition = playerLocalTransform.Position;
                    var rotation = bossWeapon.AmmoStartTransform.Rotation;
                    var velocity = new PhysicsVelocity();
                    var forward = math.forward(rotation);
                    
                    if (bossStrategyComponent.AimAtPlayer)
                    {
                        //var bossXZ = new float3(bossLocalTransform.Position.x, bossLocalTransform.Position.y, bossLocalTransform.Position.z);
                        var bossXZ = new float3(ammoStartTransform.Position.x, ammoStartTransform.Position.y, ammoStartTransform.Position.z);
                        var playerXZ = new float3(playerLocalPosition.x, playerLocalPosition.y, playerLocalPosition.z);
                        var direction = math.normalize(playerXZ - bossXZ);
                        forward = direction;
                        direction.y = 0;
                        var targetRotation = quaternion.LookRotationSafe(direction, math.up());//always face player
                        //bossLocalTransform.Rotation = targetRotation;
                        bossLocalTransform.Rotation = math.slerp(bossLocalTransform.Rotation, targetRotation, 0.5f);



                    }

                    velocity.Linear = math.normalize(forward) * strength;

                    bulletManagerComponent.playSound = true;

                    ammoDataComponent.Shooter = entity;
                    
                        
                    commandBuffer.SetComponent(e, ammoDataComponent);
                    ammoStartTransform.Rotation = rotation;
                    commandBuffer.SetComponent(e, new TriggerComponent
                    { Type = (int)TriggerType.Ammo, ParentEntity = entity, Entity = e, Active = true });
                    SystemAPI.SetComponent(entity, bossLocalTransform);
                    ammoStartTransform.Scale = ammoDataComponent.AmmoScale;
                    commandBuffer.SetComponent(e, ammoStartTransform);
                    commandBuffer.SetComponent(e, velocity);
                }

                commandBuffer.SetComponent(entity, bossWeapon);



            }
        ).Schedule(this.Dependency);
        dep.Complete();
        playerEntities.Dispose();


        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();


    }



}
