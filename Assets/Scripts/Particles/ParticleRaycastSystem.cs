using Collisions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;


//[UpdateAfter(typeof(Unity.Physics.Systems.AfterPhysicsSystemGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial struct ParticleRaycastSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }


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
        Particle = 1 << 8
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        //var physicsWorldSystem =  World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();



        //var dep0 = Entities.ForEach((Entity entity,
        //    ref PhysicsCollider collider, in AmmoComponent ammoComponent) =>
        foreach (var(collider, ammoComponent, entity) 
                 in SystemAPI.Query<RefRW<PhysicsCollider>, AmmoComponent>().
                     WithEntityAccess())
        {

            var LocalTransform = SystemAPI.GetComponent<LocalTransform>(entity);
            var parentEntity = SystemAPI.GetComponent<TriggerComponent>(entity).ParentEntity;
            var start = LocalTransform.Position + new float3(0f, 0, 0);
            var direction = new float3(0, 0, 0);
            var distance = .4f;
            var end = start + direction * distance;

            var pointDistanceInput = new PointDistanceInput
            {
                Position = start,
                MaxDistance = distance,
                Filter = new CollisionFilter()
                {
                    BelongsTo = (uint)CollisionLayer.Particle,
                    CollidesWith = (uint)CollisionLayer.Ground,
                    GroupIndex = 0
                }
            };

            var hasPointHit = collisionWorld.CalculateDistance(pointDistanceInput, out var pointHit);//bump left / right n/a

            start = LocalTransform.Position + new float3(0, 0, 0);
            direction = new float3(0, -1, 0);
            distance = 1.1f;
            end = start + direction * distance;
            var inputDown = new RaycastInput()
            {
                Start = start,
                End = end,
                //Filter = CollisionFilter.Default
                Filter = new CollisionFilter()
                {
                    BelongsTo = (uint)CollisionLayer.Particle,
                    CollidesWith = (uint)CollisionLayer.Ground,
                    GroupIndex = 0
                }
            };
            var hitDown = new Unity.Physics.RaycastHit();
            //Debug.DrawRay(inputDown.Start, direction, Color.white, distance);

            var hasPointHitDown = collisionWorld.CastRay(inputDown, out hitDown);


            start = LocalTransform.Position + new float3(0, 1f, 0);
            direction = new float3(0, 1f, 0);
            distance = .20f;
            end = start + direction * distance;

            var inputUp = new RaycastInput()
            {
                Start = start,
                End = end,
                Filter = new CollisionFilter()
                {
                    BelongsTo = (uint)CollisionLayer.Particle,
                    CollidesWith = (uint)CollisionLayer.Ground,
                    GroupIndex = 0
                }

            };

            var hitUp = new Unity.Physics.RaycastHit();

            var hasPointHitUp = collisionWorld.CastRay(inputUp, out hitUp);


            if (hasPointHit)
            {
                var e = collisionWorld.Bodies[pointHit.RigidBodyIndex].Entity;

            }
            else if (hasPointHitDown)
            {

                var e = collisionWorld.Bodies[hitDown.RigidBodyIndex].Entity;
                if (SystemAPI.HasComponent<VisualEffectEntitySpawnerComponent>(entity))
                {

                    var visualEffectComponentSpawner =
                        SystemAPI.GetComponent<VisualEffectEntitySpawnerComponent>(entity);
                    if (visualEffectComponentSpawner.instantiated == false)
                    {
                        visualEffectComponentSpawner.instantiated = true;
                        SystemAPI.SetComponent(entity, visualEffectComponentSpawner);
                        var spawn = ecb.Instantiate(visualEffectComponentSpawner.entity);
                        var localTransform = LocalTransform.FromPosition(LocalTransform.Position);
                        ecb.SetComponent(spawn, localTransform); //spawn visual effect component entity 

                        ecb.AddComponent(spawn, new SpawnedItem()
                        {
                            spawned = true,
                            itemParent = entity,
                            spawnedLocalTransform = LocalTransform
                        });

                        ecb.SetComponent(spawn, new TriggerComponent
                        {
                            Type = (int)TriggerType.Particle,
                            ParentEntity = parentEntity,
                            Entity = spawn,
                            Active = true
                        });

                        //Debug.Log("POSITION " + spawn);
                    

                    }

                }

            }
            else if (hasPointHitUp)
            {
                var e = collisionWorld.Bodies[hitUp.RigidBodyIndex].Entity;

            }




        }
        var visualEffectTriggerJob = new VisualEffectTriggerJob()
        {

        };

        visualEffectTriggerJob.Schedule();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

    }
    
    partial struct ParticleRaycastJob : IJobEntity
    {
        void Execute()
        {
            
        }
    }

    [BurstCompile]
    partial struct VisualEffectTriggerJob : IJobEntity
    {
        void Execute(ref VisualEffectEntityComponent visualEffectEntityComponent)
        {
            visualEffectEntityComponent.trigger = true;

        }
    }
    
    
}











