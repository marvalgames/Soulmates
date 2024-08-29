using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Collisions
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class CrosshairRaycastSystem : SystemBase
    {
        private enum CollisionLayer
        {
            Player = 1 << 0,
            Ground = 1 << 1,
            Enemy = 1 << 2,
            WeaponItem = 1 << 3,
            Obstacle = 1 << 4,
            Npc = 1 << 5,
            PowerUp = 1 << 6,
            Stairs = 1 << 7,
            Particle = 1 << 8,
            Camera = 1 << 9,
            Crosshair = 1 << 10,
            Breakable = 1 << 11
        }


        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var actorWeaponAimQuery = GetEntityQuery(ComponentType.ReadOnly<ActorWeaponAimComponent>(),
                ComponentType.ReadOnly<PlayerComponent>()); //player 0
            var actorWeaponAimEntityList = actorWeaponAimQuery.ToEntityArray(Allocator.TempJob);
            if (actorWeaponAimEntityList.Length == 0)
            {
                actorWeaponAimEntityList.Dispose();
                return;
            }

            var allHits = new NativeList<RaycastHit>(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            Entities.WithoutBurst().ForEach((Entity entity, ref CrosshairComponent crosshair) =>
            {
                var physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
                var actorEntity = actorWeaponAimEntityList[0];
                var actorWeaponAim = SystemAPI.GetComponent<ActorWeaponAimComponent>(actorEntity);
                var actorTransform = SystemAPI.GetComponent<LocalTransform>(actorEntity);
                var start = actorWeaponAim.rayCastStart;
                var end = actorWeaponAim.rayCastEnd;
                var inputForward = new RaycastInput
                {
                    Start = start,
                    End = end,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = (uint)CollisionLayer.Crosshair,
                        CollidesWith = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Breakable |
                                       (uint)CollisionLayer.Ground
                                       | (uint)CollisionLayer.Obstacle,
                        GroupIndex = 0
                    }
                };
                Debug.DrawLine(start, end, Color.green, SystemAPI.Time.DeltaTime);
                var hasHitPoints = collisionWorld.CastRay(inputForward, ref allHits);
                if (hasHitPoints)
                {
                    //code to check if hit point is behind player (facing same dir forward)
                    var closest = 0;
                    ;
                    double hi = 1;
                    for (var i = 0; i < allHits.Length; i++)
                    {
                        var hitList = allHits[i];
                        var fwd = actorTransform.Forward();
                        //var invFwd = actorTransform.TransformDirection(math.forward());
                        Vector3 worldForward = Camera.main.transform.TransformDirection(fwd);
                        float dot = Vector3.Dot(worldForward,
                            math.normalize(hitList.Position - actorTransform.Position));

                        //dot = math.sign(worldForward.z) * dot;
                        //Debug.Log("Fwd " + worldForward);
                        var facing = dot > 0;
                        var body = collisionWorld.Bodies[hitList.RigidBodyIndex].Entity;
                        var enemy = (SystemAPI.HasComponent<EnemyComponent>(body));
                        if (hitList.Fraction < hi && (facing || enemy))
                        //if (hitList.Fraction < hi)
                        {
                            closest = i;
                            hi = hitList.Fraction;
                        }
                    }

                    var hitForward = allHits[closest];
                    var e = collisionWorld.Bodies[hitForward.RigidBodyIndex].Entity;
                    var zLength = hitForward.Position.z;

                    if (SystemAPI.HasComponent<EnemyComponent>(e))
                    {
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                            actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                        }

                        Debug.Log("hit enemy position ");
                    }
                    else if (SystemAPI.HasComponent<BreakableComponent>(e))
                    {
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                            actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                        }

                        Debug.Log("hit breakable position ");
                    }
                    else if (SystemAPI.HasComponent<TriggerComponent>(e))
                    {
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                            actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                        }

                        Debug.Log("hit something ");
                    }
                    else
                    {
                        Debug.Log("hit terrain ");
                        actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                        actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                    }


                    crosshair.targetDelayCounter = 0;
                }


                SystemAPI.SetComponent(actorEntity, actorWeaponAim);
            }).Run();

            actorWeaponAimEntityList.Dispose();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}