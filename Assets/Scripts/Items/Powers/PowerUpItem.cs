using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[System.Serializable]
public struct HealthPower : IComponentData
{
    public LocalTransform LocalTransform;
    public Entity psAttached;
    public Entity pickedUpActor;
    public Entity itemEntity;
    public bool enabled;
    public float healthMultiplier;
    public int count;
    public bool slowDown;
}

[System.Serializable]
public struct DashPower : IComponentData
{
    public Entity psAttached;
    public Entity pickedUpActor;
    public Entity itemEntity;
    public bool enabled;
    public int count;
    public int useIncrease;
}

[System.Serializable]
public struct Speed : IComponentData
{

    public Entity psAttached;
    public Entity pickedUpActor;
    public Entity itemEntity;
    public bool triggered;
    public bool enabled;
    public bool startTimer;
    public float timer;
    public float timeOn;
    public float originalSpeed;
    public float multiplier;
    public int count;
}





public struct SpawnedItem : IComponentData
{
    public bool spawned;
    public Entity itemParent;
    public LocalTransform spawnedLocalTransform;
}

public class PowerUpItem : MonoBehaviour
{
    public PowerUpScriptableObject powerUpScriptableObject;

    public bool active = true;
    public bool immediateUse;

    public AudioClip powerEnabledAudioClip;
    public AudioClip powerTriggerAudioClip;
    public AudioSource audioSource;
    public GameObject psPrefab;


    class PowerUpBaker : Baker<PowerUpItem>
    {
        public override void Bake(PowerUpItem authoring)
        {
            //var ps = authoring.GetComponentInChildren<ParticleSystem>();
            var ps = authoring.psPrefab;
            var psEntity = GetEntity(ps, TransformUsageFlags.Dynamic);
            var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            //var instance = Instantiate(authoring.powerEnabledEffectPrefab);
            //Debug.Log("PS " + ps);
            //AddComponentObject(ps);
            AddComponent(entity, new PowerItemComponent
            {
                //particleSystemEntity = GetEntity(authoring.powerEnabledEffectPrefab),
                particleSystemEntity = psEntity,
                active = authoring.active,
                description = authoring.powerUpScriptableObject.powerItemDescription,
                longDescription = authoring.powerUpScriptableObject.powerItemLongDescription,
                pickupEntity = entity,
                index = entity.Index,
                pickupType = authoring.powerUpScriptableObject.pickupType,

                statDescription1 = authoring.powerUpScriptableObject.statDescription1,
                statDescription2 = authoring.powerUpScriptableObject.statDescription2,
                statDescription3 = authoring.powerUpScriptableObject.statDescription3,
                statRating1 = authoring.powerUpScriptableObject.statRating1,
                statRating2 = authoring.powerUpScriptableObject.statRating2,
                statRating3 = authoring.powerUpScriptableObject.statRating3,
                statDescriptionLong1 = authoring.powerUpScriptableObject.statLongDescription1,
                statDescriptionLong2 = authoring.powerUpScriptableObject.statLongDescription2,
                statDescriptionLong3 = authoring.powerUpScriptableObject.statLongDescription3
            });
            if (authoring.powerUpScriptableObject.pickupType == PickupType.Speed)
            {
                AddComponent(entity, 
                    new Speed
                    {
                        enabled = false,
                        timeOn = authoring.powerUpScriptableObject.powerTimeOn,
                        multiplier = authoring.powerUpScriptableObject.powerMultiplier
                    });
            }
            else if (authoring.powerUpScriptableObject.pickupType == PickupType.Health)
            {
                AddComponent(entity, 
                    new HealthPower()
                        { enabled = false, healthMultiplier = authoring.powerUpScriptableObject.powerMultiplier });
            }
            else if (authoring.powerUpScriptableObject.pickupType == PickupType.Dash)
            {
                AddComponent(entity,
                    new DashPower()
                        { enabled = false, useIncrease = authoring.powerUpScriptableObject.powerUseIncrease });
            }
            else if (authoring.powerUpScriptableObject.pickupType == PickupType.HealthRate)
            {
                AddComponent(entity,
                    new HealthPower()
                    {
                        enabled = false, slowDown = authoring.powerUpScriptableObject.powerSlowDown,
                        healthMultiplier = authoring.powerUpScriptableObject.powerMultiplier
                    });
            }

            if (authoring.immediateUse)
            {
                AddComponent(entity, new ImmediateUseComponent());
            }
        }
    }
}


[UpdateAfter(typeof(Collisions.PickupInputPowerUpUseImmediateSystem))]
public partial class SpawnPowerUpParticleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithoutBurst().ForEach
        (( Entity entity,  ref PowerItemComponent powerItemComponent) =>
            {
                if(powerItemComponent.particleSystemEntitySpawned) return;
                var e = powerItemComponent.particleSystemEntity;
                var instance = ecb.Instantiate(e);
                //Debug.Log("INSTANCE " + instance);
                powerItemComponent.particleSystemEntitySpawned = true;
                var tr = SystemAPI.GetComponent<LocalTransform>(entity);
                ecb.AddComponent(instance, new SpawnedItem()
                {
                    spawned = true, itemParent = entity,
                    spawnedLocalTransform = tr
                });
                ecb.SetComponent(instance, tr);
                //particleSystem.Stop(true);
            }
        ).Run();
        
        //ecb.Playback();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}


[UpdateAfter(typeof(SpawnPowerUpParticleSystem))]
public partial class UpdatePowerUpParticleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithoutBurst().ForEach
        ((ParticleSystem particleSystem, Entity spawnedEntity, ref LocalTransform spawnedLocalTransform,
                ref SpawnedItem spawnedPowerUpItem) =>
            {
                var parent = spawnedPowerUpItem.itemParent;//parent has poweritemcomponent
                bool pickedUp = false;
                if (SystemAPI.HasComponent<PowerItemComponent>(parent))
                {
                    pickedUp = SystemAPI.GetComponent<PowerItemComponent>(parent).itemPickedUp;
                    //Debug.Log("PICKED UP " + pickedUp);
                }
                else if (SystemAPI.HasComponent<ResourceItemComponent>(parent))
                {
                    pickedUp = SystemAPI.GetComponent<ResourceItemComponent>(parent).itemPickedUp;
                    //Debug.Log("PICKED UP " + pickedUp);
                }
                
                if (SystemAPI.HasComponent<DestroyComponent>(parent) || pickedUp)
                {
                    ecb.DestroyEntity(spawnedEntity);
                    particleSystem.Stop(true);
                }
                
                if (particleSystem.isPlaying) return;
                //Debug.Log("PS POS  " + spawnedLocalTransform.Value);
                particleSystem.Play(true);
                particleSystem.transform.position = spawnedLocalTransform.Position;
                spawnedPowerUpItem.spawned = false;
                
            }
        ).Run();
        
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}