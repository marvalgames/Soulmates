using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;


public struct VisualEffectEntitySpawnerComponent : IComponentData
{
    public Entity entity;
    public bool instantiated;
}

public struct VisualEffectSceneComponent : IComponentData
{
}

public struct VisualEffectEntityComponent : IComponentData
{
    public float damageAmount;
    public bool enemyDamaged;
    public bool playerDamaged;
    public bool instantiated;
    public bool trigger;
    public float currentTime;
    public float spawnTime;
    public bool destroy;
    public float destroyCountdown;
    public float framesToSkip; //timer instead?
    public int frameSkipCounter;
    public int effectsIndex;
    public int deathBlowEffectsIndex;
}

public struct VfxComponentTag : IComponentData
{
}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ParticleRaycastSystem))]
[RequireMatchingQueriesForUpdate]
public partial struct VisualEffectSystem : ISystem //use for visual effect and particle systems
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var ecb = new EntityCommandBuffer(Allocator.Temp);
        var tick = SystemAPI.Time.DeltaTime;


        // Entities.WithoutBurst().ForEach(
        //     (
        //         ref VisualEffectEntityComponent visualEffectComponent,
        //         in Entity entity //,
        //         //in VisualEffect ve
        //     ) =>
        //     {
        //         if (visualEffectComponent.instantiated)
        //         {
        //             //Debug.Log("INSTANT");
        //             visualEffectComponent.currentTime += SystemAPI.Time.DeltaTime;
        //             if (visualEffectComponent.currentTime > visualEffectComponent.spawnTime)
        //             {
        //                 visualEffectComponent.currentTime = 0;
        //                 visualEffectComponent.instantiated = false;
        //                 visualEffectComponent.destroy = true;
        //             }
        //         }
        //         else if (visualEffectComponent.trigger == true)
        //         {
        //             visualEffectComponent.instantiated = true;
        //             visualEffectComponent.trigger = false;
        //         }
        //     }
        // ).Run();
        //ecb.Playback(EntityManager);
       // ecb.Dispose();
        var job = new VisualEffectJob()
        {
            tick = tick
        };
        job.ScheduleParallel();

    }

    [BurstCompile]
    partial struct VisualEffectJob : IJobEntity
    {
        public float tick;
        void Execute(Entity e, ref VisualEffectEntityComponent visualEffectComponent)
        {
            if (visualEffectComponent.instantiated)
            {
                //Debug.Log("INSTANT");
                visualEffectComponent.currentTime += tick;
                if (visualEffectComponent.currentTime > visualEffectComponent.spawnTime)
                {
                    visualEffectComponent.currentTime = 0;
                    visualEffectComponent.instantiated = false;
                    visualEffectComponent.destroy = true;
                }
            }
            else if (visualEffectComponent.trigger == true)
            {
                visualEffectComponent.instantiated = true;
                visualEffectComponent.trigger = false;
            }
            
            
        }
    }
    
    
    
}


[RequireMatchingQueriesForUpdate]
public partial class VisualEffectSceneSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithoutBurst().ForEach
        ((VisualEffect visualEffect, Entity e
            ) =>
            {
                if (SystemAPI.HasComponent<VisualEffectSceneComponent>(e))
                {
                    var parent = SystemAPI.GetComponent<Parent>(e).Value;

                    var hasPlayerJumpComponent = SystemAPI.HasComponent<PlayerJumpComponent>(parent);
                    if (hasPlayerJumpComponent)
                    {
                        var playerJump = SystemAPI.GetComponent<PlayerJumpComponent>(parent);
                        if (playerJump.playVfx)
                        {
                            visualEffect.SetFloat("Spawn Rate", 40);
                        }
                        else
                        {
                            visualEffect.SetFloat("Spawn Rate", 0);
                        }

                        SystemAPI.SetComponent(parent, playerJump);
                    }
                }

                if (SystemAPI.HasComponent<VisualEffectEntityComponent>(e))
                {
                    var visualEffectComponent = SystemAPI.GetComponent<VisualEffectEntityComponent>(e);
                    if (visualEffectComponent.destroy && visualEffectComponent.destroyCountdown > 0)
                    {
                        visualEffect.SetFloat("Spawn Rate", 0);
                        visualEffectComponent.destroyCountdown -= SystemAPI.Time.DeltaTime;
                        ecb.SetComponent(e, visualEffectComponent);
                    }
                    else if (visualEffectComponent is { destroy: true, destroyCountdown: <= 0 })
                    {
                        Debug.Log("destroy");
                        ecb.DestroyEntity(e);
                    }

                    if (SystemAPI.HasComponent<SpawnedItem>(e))
                    {
                        var spawnedItem = SystemAPI.GetComponent<SpawnedItem>(e);
                        {
                            if (!spawnedItem.spawned) return;
                            visualEffect.transform.position = spawnedItem.spawnedLocalTransform.Position;
                            spawnedItem.spawned = false;
                        }
                        ecb.SetComponent(e, spawnedItem);
                    }
                    
                }
                
            }
        ).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}