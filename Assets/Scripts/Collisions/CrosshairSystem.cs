using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;


//[UpdateAfter(typeof(Unity.Physics.Systems.EndFramePhysicsSystem))]
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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
            NPC = 1 << 5,
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
            var actorWeaponAimQuery = GetEntityQuery(ComponentType.ReadOnly<ActorWeaponAimComponent>(), ComponentType.ReadOnly<PlayerComponent>());//player 0
            var actorWeaponAimEntityList = actorWeaponAimQuery.ToEntityArray(Allocator.TempJob);
            if (actorWeaponAimEntityList.Length == 0)
            {
                actorWeaponAimEntityList.Dispose();
                return;
            }
            //var playerLocalTransform = SystemAPI.GetComponent<LocalTransform>(actorWeaponAimEntityList[0]);
            //var playerRotation = SystemAPI.GetComponent<Rotation>(actorWeaponAimEntityList[0]);
            var allHits = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();



            Entities.WithoutBurst().ForEach((Entity entity, ref CrosshairComponent crosshair) =>
            {
                var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
                var actorEntity = actorWeaponAimEntityList[0];
                var actorWeaponAim = SystemAPI.GetComponent<ActorWeaponAimComponent>(actorEntity);
                var LocalTransform = SystemAPI.GetComponent<LocalTransform>(entity);

                //var mouse = actorWeaponAim.mouseCrosshairWorldPosition;
                var xHairPosition = new float3(LocalTransform.Position.x, LocalTransform.Position.y, actorWeaponAim.crosshairRaycastTarget.z);
                //Debug.Log("mouse " + mouse);
                //actorWeaponAim.crosshairRaycastTarget = mouse;

                var distance = crosshair.raycastDistance;
            
                //actorWeaponAim.closetEnemyWeaponTargetPosition = new float3(0, 0, distance);

                var start = actorWeaponAim.rayCastStart;
                var end = actorWeaponAim.rayCastEnd;
            
            
                var inputForward = new RaycastInput()
                {
                    Start = start,
                    End = end,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Crosshair,
                        CollidesWith = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Breakable  | (uint)CollisionLayer.Ground
                                       | (uint)CollisionLayer.Obstacle,
                        GroupIndex = 0
                    }
                };
                //Debug.DrawLine(start, end, Color.green, SystemAPI.Time.DeltaTime);
                var hasHitPoints = collisionWorld.CastRay(inputForward, ref allHits);
                if (hasHitPoints)
                {
                
                    var closest = 0; ;
                    double hi = 1;
                    for (var i = 0; i < allHits.Length; i++)
                    {
                        var hitList = allHits[i];
                        //Debug.Log("index " + i + " f " + (int)(hitList.Fraction * 100));

                        if (hitList.Fraction < hi)
                        {
                            closest = i;
                            hi = hitList.Fraction;
                        }
                    }
                    var hitForward = allHits[closest];
                    var e = collisionWorld.Bodies[hitForward.RigidBodyIndex].Entity;
                    //Debug.Log("entity " + e);
                    //float zLength = hitForward.Position.z * 1 / math.cos(29);
                    var zLength = hitForward.Position.z;
                    //float zLength = hitForward.Position.z * math.cos(30);


                    if (SystemAPI.HasComponent<EnemyComponent>(e))
                    {
                        // Debug.Log("hit enemy position 0");
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera != CameraTypes.TopDown)
                        {
                            if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }
                            //Debug.Log("hit enemy position ");
                        }
                        else
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                        }

                   
                    }
                    else if (SystemAPI.HasComponent<BreakableComponent>(e))
                    {
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera != CameraTypes.TopDown)
                        {
                            if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }
                            //Debug.Log("hit breakable position ");
                        }
                        else
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                        }

                   
                    }
                    else if (SystemAPI.HasComponent<TriggerComponent>(e))
                    {
                        actorWeaponAim.crosshairRaycastTarget.z = zLength;
                        if (actorWeaponAim.weaponCamera != CameraTypes.TopDown)
                        {
                            if (actorWeaponAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }
                        }
                        else
                        {
                            actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
                        }

                        //Debug.Log("hit something ");
                    }
                    else
                    {
                        //Debug.Log("hit terrain ");
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











