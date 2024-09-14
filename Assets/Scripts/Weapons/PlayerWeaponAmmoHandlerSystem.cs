using Collisions;
using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class PlayerWeaponAmmoHandlerSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem _mEntityCommandBufferSystem;


    protected override void OnCreate()
    {
        _mEntityCommandBufferSystem = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (LevelManager.instance == null) return;
        if (LevelManager.instance.endGame) return;

        var dt = SystemAPI.Time.DeltaTime; //gun duration
        var commandBuffer = _mEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        Entities.WithBurst().WithNone<Pause>().ForEach(
            (
                Entity entity,
                int entityInQueryIndex,
                ref AmmoManagerComponent bulletManagerComponent,
                in ActorWeaponAimComponent actorWeaponAimComponent,
                in DeadComponent dead,
                in PhysicsVelocity playerVelocity,
                //in AttachWeaponComponent attachWeapon,
                in AnimatorWeightsComponent animatorWeightsComponent,
                in TargetZoneComponent targetZone
            ) =>
            {
                if (!SystemAPI.HasComponent<WeaponComponent>(entity) ||
                    SystemAPI.HasComponent<EnemyComponent>(entity)) return;
                var gun = SystemAPI.GetComponent<WeaponComponent>(entity);
                if (gun.roleReversal == RoleReversalMode.Off)
                {
                    if (
                        //attachWeapon.attachedWeaponSlot < 0 ||
                        //attachWeapon.attachWeaponType != (int)WeaponType.Gun &&
                        //attachWeapon.attachSecondaryWeaponType != (int)WeaponType.Gun
                        //|| 
                        !actorWeaponAimComponent.aimMode
                    )
                    {
                        gun.Duration = 0;
                        gun.IsFiring = 0;
                        return;
                    }
                }

                if (dead.isDead) return;
                var primaryAmmoEntity = gun.PrimaryAmmo;
                var ammoDataComponent = SystemAPI.GetComponent<AmmoDataComponent>(primaryAmmoEntity);
                var rate = ammoDataComponent.GameRate;
                var strength = ammoDataComponent.GameStrength;
                if (gun.roleReversal == RoleReversalMode.Off)
                {
                    //change based on game
                    if (gun.ChangeAmmoStats > 0)
                    {
                        strength = strength * (100 - gun.ChangeAmmoStats * 2) / 100;
                        if (strength <= 0) strength = 0;
                    }
                }

                if (gun is { IsFiring: 1, Duration: 0 } && animatorWeightsComponent.aimWeight >= 0)
                    //if (gun is { IsFiring: 1, Duration: 0 } && animatorWeightsComponent.aimWeight >= .98)
                {
                    gun.Duration += dt;
                    if (gun.roleReversal == RoleReversalMode.Off)
                    {
                        var e = commandBuffer.Instantiate(entityInQueryIndex, gun.PrimaryAmmo);
                        var weaponPosition = gun.AmmoStartLocalToWorld.Position; //use bone mb transform
                        var weaponRotation = gun.AmmoStartLocalToWorld.Rotation;
                        //Target experimental
                        weaponPosition = targetZone.headZonePosition;
                        //weaponRotation = targetZone.headZone.Rotation;
                        
                        var playerForward = SystemAPI.GetComponent<LocalTransform>(entity).Forward();
                        var velocity = SystemAPI.GetComponent<PhysicsVelocity>(entity);
                        velocity.Linear.y = 0;
                        var currentLinearVelocity = velocity.Linear;
                        velocity.Linear = actorWeaponAimComponent.aimDirection * strength + currentLinearVelocity;
                        velocity.Angular = math.float3(0, 0, 0);
                        ammoDataComponent.Shooter = entity;
                        commandBuffer.SetComponent(entityInQueryIndex, e, ammoDataComponent);
                        commandBuffer.SetComponent(entityInQueryIndex, e, new TriggerComponent
                            { Type = (int)TriggerType.Ammo, ParentEntity = entity, Entity = e, Active = true });
                        var localTransform = LocalTransform.FromPositionRotation(weaponPosition, weaponRotation);
                        localTransform.Scale = ammoDataComponent.AmmoScale;
                        commandBuffer.SetComponent(entityInQueryIndex, e, localTransform);
                        commandBuffer.SetComponent(entityInQueryIndex, e, velocity);
                    }

                    bulletManagerComponent.playSound = true;
                    bulletManagerComponent.setAnimationLayer = true;
                }
                else if (gun is { IsFiring: 1, Duration: > 0 })
                {
                    gun.Duration += dt;
                    if ((gun.Duration > rate) && (gun.IsFiring == 1))
                    {
                        gun.Duration = 0;
                        gun.IsFiring = 0;
                    }
                }

                commandBuffer.SetComponent(entityInQueryIndex, entity, gun);
            }
        ).ScheduleParallel();
        _mEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}