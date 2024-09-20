using Rewired;
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
    public partial class CrosshairRaycastSystem : SystemBase // Change to ISystem
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

        private Camera _cam;
        private float3 mousePosition;
        private readonly float targetRange = 100;
        private float _xMin;
        private float _xMax;
        private float _yMin;
        private float _yMax;

        protected override void OnCreate()
        {
            _cam = Camera.main;
            SetCursorBounds();
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
            Entities.WithoutBurst().ForEach(
                (Entity entity, CrosshairClass crosshairClass, ref CrosshairComponent crosshair) =>
                {
                    if (!crosshair.spawnCrosshair)
                    {
                        var go = GameObject.Instantiate(crosshairClass.crosshairPrefab);
                        crosshair.spawnCrosshair = true;
                        var crosshairInstance = new CrosshairInstance { crosshairInstance = go };
                        ecb.AddComponent(entity, crosshairInstance);
                    }

                    //input
                    //float z;
                    var transform = SystemAPI.GetComponent<LocalTransform>(entity);
                    float3 position = transform.Position;
                    float3 playerScreen = _cam.WorldToScreenPoint(position);
                    var actorEntity = actorWeaponAimEntityList[0];
                    var actorAim = SystemAPI.GetComponent<ActorWeaponAimComponent>(actorEntity);
                    var actorTransform = SystemAPI.GetComponent<LocalTransform>(actorEntity);
                    var input = SystemAPI.GetComponent<InputControllerComponent>(actorEntity);
                    var viewportPct = crosshair.viewportPct;
                    _xMin = Screen.width * (1 - viewportPct / 100);
                    _xMax = Screen.width * viewportPct / 100;
                    _yMin = Screen.height * (1 - viewportPct / 100);
                    _yMax = Screen.height * viewportPct / 100;


                    //var playerToMouseDir = (float3)mousePosition - playerScreen;
                    //_aimCrosshair = Vector3.zero;
                    //var x = Player.GetAxis("RightHorizontal");
                    //if (math.abs(x) < .000001) x = 0;
                    //var y = Player.GetAxis("RightVertical");
                    //if (math.abs(y) < .000001) y = 0;

                    //var aim = new Vector3(
                    //x * SystemAPI.Time.DeltaTime,
                    //y * SystemAPI.Time.DeltaTime,
                    //0
                    //);

                    //aim.Normalize();

                    //_aimCrosshair = aim;

                    //if (gamePad)
                    //{
                    //  mousePosition += new Vector3(_aimCrosshair.x * gamePadSensitivity,
                    //    _aimCrosshair.y * gamePadSensitivity,
                    //  0);
                    //}
                    //else
                    //{
                    mousePosition = new float3(input.mousePosition.x,
                        input.mousePosition.y, 0);
                    //}

                    // Debug.Log("mouse " + mousePosition);

                    mousePosition.z = actorAim.crosshairRaycastTarget.z - _cam.transform.position.z;

                    //_targetPosition = _cam.ScreenToWorldPoint(mousePosition);

                    if (mousePosition.x < _xMin) mousePosition.x = _xMin;
                    if (mousePosition.x > _xMax) mousePosition.x = _xMax / 2;
                    if (mousePosition.y < _yMin) mousePosition.y = _yMin;
                    if (mousePosition.y > _yMax) mousePosition.y = _yMax / 2;

                    //CrosshairClass.transform.position = mousePosition; //*********************
                    transform.Position = mousePosition;
                    SystemAPI.SetComponent(entity, transform);
                    actorAim.mousePosition = mousePosition;
                    actorAim.screenPosition = _cam.WorldToScreenPoint(actorTransform.Position);
                    var ray = _cam.ScreenPointToRay(mousePosition);
                    var start = _cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
                    var rayDirection = ray.direction;
                    //rayDirection.z = 1;
                    var end = ray.origin + Vector3.Normalize(rayDirection) * targetRange;


                    actorAim.rayCastStart = start;
                    actorAim.rayCastEnd = end;


                    start = actorAim.rayCastStart;
                    end = actorAim.rayCastEnd;
                    
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
                            if (hitList.Fraction < hi) 
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
                            actorAim.crosshairRaycastTarget.z = zLength;
                            if (actorAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }

                            Debug.Log("hit enemy position ");
                        }
                        else if (SystemAPI.HasComponent<BreakableComponent>(e))
                        {
                            actorAim.crosshairRaycastTarget.z = zLength;
                            if (actorAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }

                            //Debug.Log("hit breakable position ");
                        }
                        else if (SystemAPI.HasComponent<TriggerComponent>(e))
                        {
                            actorAim.crosshairRaycastTarget.z = zLength;
                            if (actorAim.weaponCamera == CameraTypes.ThirdPerson)
                            {
                                actorAim.crosshairRaycastTarget.y = hitForward.Position.y;
                                actorAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            }

                            //Debug.Log("hit something ");
                        }
                        else
                        {
                            //Debug.Log("hit terrain ");
                            actorAim.crosshairRaycastTarget.y = hitForward.Position.y;
                            actorAim.crosshairRaycastTarget.x = hitForward.Position.x;
                            actorAim.crosshairRaycastTarget.z = zLength;
                        }

                        actorAim.targetPosition = actorAim.crosshairRaycastTarget;
                    }

                    SystemAPI.SetComponent(actorEntity, actorAim);
                }).Run();

            actorWeaponAimEntityList.Dispose();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        bool IsHitPointInScreenBounds(float3 hitWorldPosition)
        {
            float3 screenPoint = _cam.WorldToScreenPoint(hitWorldPosition);
            return screenPoint.x >= 0 && screenPoint.x <= Screen.width && screenPoint.y >= 0 && screenPoint.y <= Screen.height && screenPoint.z > 0;
        }

        private void SetCursorBounds()
        {
            mousePosition = new float3(Screen.width / 2f, Screen.height * .75f, 0);
        }
    }
}

//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
//
// using Sandbox.Player;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using Unity.Transforms;
// using UnityEngine;
// using RaycastHit = Unity.Physics.RaycastHit;
//
// namespace Collisions
// {
//     [UpdateInGroup(typeof(PhysicsSystemGroup))]
//     [RequireMatchingQueriesForUpdate]
//     public partial class CrosshairRaycastSystem : SystemBase
//     {
//         private enum CollisionLayer
//         {
//             Player = 1 << 0,
//             Ground = 1 << 1,
//             Enemy = 1 << 2,
//             WeaponItem = 1 << 3,
//             Obstacle = 1 << 4,
//             Npc = 1 << 5,
//             PowerUp = 1 << 6,
//             Stairs = 1 << 7,
//             Particle = 1 << 8,
//             Camera = 1 << 9,
//             Crosshair = 1 << 10,
//             Breakable = 1 << 11
//         }
//
//
//         protected override void OnUpdate()
//         {
//             var ecb = new EntityCommandBuffer(Allocator.Temp);
//             var actorWeaponAimQuery = GetEntityQuery(ComponentType.ReadOnly<ActorWeaponAimComponent>(),
//                 ComponentType.ReadOnly<PlayerComponent>()); //player 0
//             var actorWeaponAimEntityList = actorWeaponAimQuery.ToEntityArray(Allocator.TempJob);
//             if (actorWeaponAimEntityList.Length == 0)
//             {
//                 actorWeaponAimEntityList.Dispose();
//                 return;
//             }
//
//             var allHits = new NativeList<RaycastHit>(Allocator.Temp);
//             var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//             Entities.WithoutBurst().ForEach((Entity entity, ref CrosshairComponent crosshair) =>
//             {
//                 var actorEntity = actorWeaponAimEntityList[0];
//                 var actorWeaponAim = SystemAPI.GetComponent<ActorWeaponAimComponent>(actorEntity);
//                 var actorTransform = SystemAPI.GetComponent<LocalTransform>(actorEntity);
//                 var start = actorWeaponAim.rayCastStart;
//                 var end = actorWeaponAim.rayCastEnd;
//                 var inputForward = new RaycastInput
//                 {
//                     Start = start,
//                     End = end,
//                     Filter = new CollisionFilter
//                     {
//                         BelongsTo = (uint)CollisionLayer.Crosshair,
//                         CollidesWith = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Breakable |
//                                        (uint)CollisionLayer.Ground
//                                        | (uint)CollisionLayer.Obstacle,
//                         GroupIndex = 0
//                     }
//                 };
//                 Debug.DrawLine(start, end, Color.green, SystemAPI.Time.DeltaTime);
//                 var hasHitPoints = collisionWorld.CastRay(inputForward, ref allHits);
//                 if (hasHitPoints)
//                 {
//                     //code to check if hit point is behind player (facing same dir forward)
//                     var closest = 0;
//                     ;
//                     double hi = 1;
//                     for (var i = 0; i < allHits.Length; i++)
//                     {
//                         var hitList = allHits[i];
//                         var fwd = actorTransform.Forward();
//                         Vector3 worldForward = Camera.main.transform.TransformDirection(fwd);
//                         float dot = Vector3.Dot(worldForward,
//                             math.normalize(hitList.Position - actorTransform.Position));
//                         var facing = dot > 0;
//                         var body = collisionWorld.Bodies[hitList.RigidBodyIndex].Entity;
//                         var enemy = (SystemAPI.HasComponent<EnemyComponent>(body));
//                         if (hitList.Fraction < hi && (facing || enemy))
//                         {
//                             closest = i;
//                             hi = hitList.Fraction;
//                         }
//                     }
//
//                     var hitForward = allHits[closest];
//                     var e = collisionWorld.Bodies[hitForward.RigidBodyIndex].Entity;
//                     var zLength = hitForward.Position.z;
//
//                     if (SystemAPI.HasComponent<EnemyComponent>(e))
//                     {
//                         actorWeaponAim.crosshairRaycastTarget.z = zLength;
//                         actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
//                         actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
//
//                         Debug.Log("hit enemy position ");
//                     }
//                     else if (SystemAPI.HasComponent<BreakableComponent>(e))
//                     {
//                         actorWeaponAim.crosshairRaycastTarget.z = zLength;
//                         actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
//                         actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
//
//                         Debug.Log("hit breakable position ");
//                     }
//                     else if (SystemAPI.HasComponent<TriggerComponent>(e))
//                     {
//                         actorWeaponAim.crosshairRaycastTarget.z = zLength;
//                         actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
//                         actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
//
//                         Debug.Log("hit something ");
//                     }
//                     else
//                     {
//                         Debug.Log("hit terrain ");
//                         actorWeaponAim.crosshairRaycastTarget.y = hitForward.Position.y;
//                         actorWeaponAim.crosshairRaycastTarget.x = hitForward.Position.x;
//                         actorWeaponAim.crosshairRaycastTarget.z = zLength;
//                     }
//
//
//                     crosshair.targetDelayCounter = 0;
//                 }
//
//
//                 SystemAPI.SetComponent(actorEntity, actorWeaponAim);
//             }).Run();
//
//             actorWeaponAimEntityList.Dispose();
//             ecb.Playback(EntityManager);
//             ecb.Dispose();
//         }
//     }
// }