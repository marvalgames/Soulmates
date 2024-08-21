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
    public partial class SpawnTriggeredVfxSpawn : SystemBase
    {
        protected override void OnUpdate()//Not Used???
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithoutBurst().WithAny<TriggeredComponentTag>().ForEach((
                    ref Entity entity, ref TriggerComponent triggerComponent, ref LocalTransform LocalTransform) =>
                {
                    if (triggerComponent.TriggeredVfxEntity != Entity.Null && triggerComponent.VfxSpawned < 1)
                    {
                        triggerComponent.VfxSpawned += 1;
                        ecb.RemoveComponent<TriggeredComponentTag>(triggerComponent.TriggeredVfxEntity);
                        var e = ecb.Instantiate(triggerComponent.TriggeredVfxEntity);
                        ecb.SetComponent(e, LocalTransform);
                    }
                }
            ).Run();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }


    public enum CollisionLayer
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
        Trigger = 1 << 12,
        Platform = 1 << 13
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public partial class RaycastSystem : SystemBase
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


            var inputDeps0 = Entities.ForEach((Entity entity, ref ApplyImpulseComponent applyImpulse,
                ref LocalTransform localTransform, ref PhysicsVelocity pv, in LocalToWorld localToWorld,
                in PlayerComponent playerComponent) =>
            {


                applyImpulse.Grounded = true;
                applyImpulse.ApproachingStairs = false;

                var start = localTransform.Position + new float3(0f, 0f, 0);
                var direction = new float3(0, 0, 0);
                var radius = 2f;
                var distance = 1.0f;
                var end = start + direction * distance;


                var pointDistanceInput = new PointDistanceInput
                {
                    Position = start,
                    MaxDistance = distance,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Player,
                        CollidesWith = (uint)CollisionLayer.Stairs,
                        GroupIndex = 0
                    }
                };

                NativeList<DistanceHit> pointHits = new NativeList<DistanceHit>(Allocator.Temp);
                var hasPointHit = collisionWorld.OverlapSphere(start, radius, ref pointHits, pointDistanceInput.Filter);
                if (hasPointHit && applyImpulse.InJump == false)
                {
                    //Debug.Log("STAIRS");
                    applyImpulse.ApproachingStairs = true;

                }


                start = localTransform.Position + new float3(0, applyImpulse.checkGroundStartY, 0);
                direction = new float3(0, -1, 0);
                radius = applyImpulse.checkRadius;
                distance = applyImpulse.checkGroundDistance;
                end = start + direction * distance;

                var inputDown = new PointDistanceInput
                {
                    Position = start,
                    MaxDistance = distance,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Player,
                        CollidesWith = (uint)CollisionLayer.Ground,
                        //CollidesWith = (uint)CollisionLayer.Platform,
                        GroupIndex = 0
                    }
                };


                Debug.DrawRay(start, direction, Color.white, distance);

                var hasPointHitDown = collisionWorld.SphereCast(start, radius, direction,
                    distance, out var hitDown, inputDown.Filter);


                if (hasPointHitDown)
                {
                    applyImpulse.groundPosition = hitDown.Position;
                    var e = collisionWorld.Bodies[hitDown.RigidBodyIndex].Entity; //grounded
                    if (applyImpulse.InJump == true)
                    {
                        applyImpulse.InJump = false;
                        applyImpulse.Grounded = true;
                    }
                    else
                    {
                        localTransform.Position.y = hitDown.Position.y + applyImpulse.checkGroundDistance;
                    }
                    applyImpulse.fallingFramesCounter = 0;
                    applyImpulse.Falling = false;
                }
                else
                {
                    if (applyImpulse.InJump == false)
                    {
                        if (applyImpulse.fallingFramesCounter > applyImpulse.fallingFramesMaximum)
                        {
                            applyImpulse.Falling = true;
                            applyImpulse.fallingFramesCounter = 0;
                        }
                        else if (!applyImpulse.ApproachingStairs)
                        {
                            applyImpulse.Falling = false;
                            applyImpulse.fallingFramesCounter++;
                        }
                    }

                    applyImpulse.Grounded = false;
                }

                if (applyImpulse.GroundCollision)
                {
                    applyImpulse.GroundCollision = false;
                    applyImpulse.Grounded = true;
                    applyImpulse.fallingFramesCounter = 0;
                }
            }).Schedule(this.Dependency);

            inputDeps0.Complete();


            var inputDeps1 = Entities.WithAny<EnemyComponent, PlayerComponent>().ForEach((Entity entity,
                ref LocalTransform localTransform, ref PhysicsVelocity pv, ref PhysicsCollider collider,
                in LocalToWorld localToWorld) =>
            {
                var position = localTransform.Position;
                var offset = localToWorld.Forward * 2 + new float3(0, 1, 0);
                var start = position + offset;
                //start ray out before pointing down because we are checking the ground a little bit in front
                var direction = new float3(0, -1, 0); //down
                var distance = 2f;
                var end = start + direction * distance;

                var inputDownOut = new RaycastInput()
                {
                    Start = start,
                    End = end,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Player,
                        CollidesWith = (uint)CollisionLayer.Platform,
                        GroupIndex = 0
                    }
                };

                bool hasPointHitDownOut = collisionWorld.CastRay(inputDownOut, out _);
                float3 _start = new float3();
                float3 _offset = new float3();
                float3 _end = new float3();

                for (int i = 0; i < 3; i++)
                {
                    if (!hasPointHitDownOut) break;


                    switch (i)
                    {
                        case 0:
                        {
                            _offset = localToWorld.Forward * -2 + new float3(0, 1, 0);
                            _start = position + _offset;
                            _end = _start + direction * distance;
                            break;
                        }
                        case 1:
                        {
                            _offset = localToWorld.Right * 2 + new float3(0, 1, 0);
                            _start = position + _offset;
                            _end = _start + direction * distance;
                            break;
                        }
                        case 2:
                        {
                            _offset = localToWorld.Right * -2 + new float3(0, 1, 0);
                            _start = position + _offset;
                            _end = _start + direction * distance;
                            break;
                        }
                    }

                    inputDownOut.Start = _start;
                    inputDownOut.End = _end;
                    hasPointHitDownOut = collisionWorld.CastRay(inputDownOut, out _);
                }


                //ray shoots to -negative value of current y  so if it hits half way it is hitting the ground

                start = localTransform.Position;
                start = start + new float3(0, 0, 0);
                direction = new float3(0, -1, 0);
                //distance = startRayY * 2f;
                distance = 1.0f;
                end = start + direction * distance;
                var inputDown = new RaycastInput()
                {
                    Start = start,
                    End = end,
                    Filter = new CollisionFilter()
                    {
                        BelongsTo = (uint)CollisionLayer.Enemy | (uint)CollisionLayer.Player,
                        CollidesWith = (uint)CollisionLayer.Platform,
                        GroupIndex = 0
                    }
                };


                var hasPointHitDown = collisionWorld.CastRay(inputDown, out _);
                var hasApply = SystemAPI.HasComponent<ApplyImpulseComponent>(entity);
                var hasEnemyMovement = SystemAPI.HasComponent<EnemyMovementComponent>(entity);

                //should make both applyimpulse?
                var nearEdgePlayer = false;
                var nearEdgeEnemy = false;

                if (hasPointHitDownOut == false && hasPointHitDown)
                {
                    if (hasApply)
                    {
                        nearEdgePlayer = true;
                    }
                    else if (hasEnemyMovement)
                    {
                        nearEdgeEnemy = true;
                    }
                }

                if (hasApply)
                {
                    var apply = SystemAPI.GetComponent<ApplyImpulseComponent>(entity);
                    apply.nearEdge = nearEdgePlayer;
                    SystemAPI.SetComponent(entity, apply);
                }
                else if (hasEnemyMovement)
                {
                    var enemyMove = SystemAPI.GetComponent<EnemyMovementComponent>(entity);
                    enemyMove.nearEdge = nearEdgeEnemy;
                    SystemAPI.SetComponent(entity, enemyMove);
                }

            }).Schedule(this.Dependency);
            inputDeps1.Complete();



            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}