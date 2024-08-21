using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;


public struct ParticleEffectsSpawnerComponent : IComponentData
{
    public Entity entity;
    public bool instantiated;
}

public struct ParticleEffectsEntityComponent : IComponentData
{
    //public Entity entity;
    public float damageAmount;
    public bool enemyDamaged;
    public bool playerDamaged;
    public bool instantiated;
    public bool trigger;
    public float currentTime;
    public float spawnTime;
    public bool destroy;
    public float destroyCountdown;
    public int effectsIndex;
    public bool particleSystemEntitySpawned;
    public Entity particleSystemEntity;
}

public struct ParticleComponentTag : IComponentData
{
}

//not same as spawn powerup or resource particle system - would have been better if all combined
//spawn any particle systems attached or instantiated fron other items ie Ammo
[RequireMatchingQueriesForUpdate]
public partial struct SpawnItemParticleSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        foreach (var (particleEffectComponent, entity)
                 in SystemAPI.Query<RefRW<ParticleEffectsEntityComponent>>().WithEntityAccess())
        {
            if (particleEffectComponent.ValueRW.particleSystemEntitySpawned) return;
            var e = particleEffectComponent.ValueRW.particleSystemEntity;
            var instance = e;
            particleEffectComponent.ValueRW.particleSystemEntitySpawned = true;
            var tr = SystemAPI.GetComponent<LocalTransform>(entity);
            ecb.AddComponent(instance, new SpawnedItem()
            {
                spawned = true,
                itemParent = entity,
                spawnedLocalTransform = tr
            });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}


[UpdateAfter(typeof(SpawnItemParticleSystem))]
public partial class UpdateItemParticleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithoutBurst().ForEach
        ((ParticleSystem particleSystem, Entity spawnedEntity,
                ref SpawnedItem spawnedItem) =>
            {
                var parent = spawnedItem.itemParent; //parent has poweritemcomponent
                if (SystemAPI.HasComponent<ParticleEffectsEntityComponent>(parent))
                {
                    particleSystem.transform.position = SystemAPI.GetComponent<LocalTransform>(parent).Position;
                }

                if (SystemAPI.HasComponent<DestroyComponent>(parent))
                {
                    ecb.DestroyEntity(spawnedEntity);
                    particleSystem.Stop(true);
                }

                if (particleSystem.isPlaying) return;
                particleSystem.Play(true);
                spawnedItem.spawned = false;
            }
        ).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}