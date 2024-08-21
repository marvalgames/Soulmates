using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;





public class ResourceItem : MonoBehaviour
{
    [SerializeField]
    private ResourceScriptableObject resourceScriptableObject;

    public bool active = true;
    public bool immediateUse;
   
    public AudioClip powerEnabledAudioClip;
    public AudioClip powerTriggerAudioClip;
    public AudioSource audioSource;
    public GameObject psPrefab;




    class ResourceBaker : Baker<ResourceItem>
    {
        public override void Bake(ResourceItem authoring)
        {
            //var ps = authoring.GetComponentInChildren<ParticleSystem>();
            var ps = authoring.psPrefab;
            var psEntity = GetEntity(ps, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
          
            AddComponent(entity, new ResourceItemComponent
            {
                //particleSystemEntity = GetEntity(authoring.powerEnabledEffectPrefab),
                particleSystemEntity = psEntity,
                active = authoring.active,
                description = authoring.resourceScriptableObject.resourceItemDescription,
                longDescription = authoring.resourceScriptableObject.resourceItemLongDescription,
                pickupEntity = entity,
                index = entity.Index,
                resourceType = authoring.resourceScriptableObject.resourceType,

                statDescription1 = authoring.resourceScriptableObject.statDescription1,
                statDescription2 = authoring.resourceScriptableObject.statDescription2,
                statDescription3 = authoring.resourceScriptableObject.statDescription3,
                statRating1 = authoring.resourceScriptableObject.statRating1,
                statRating2 = authoring.resourceScriptableObject.statRating2,
                statRating3 = authoring.resourceScriptableObject.statRating3,
                statDescriptionLong1 = authoring.resourceScriptableObject.statLongDescription1,
                statDescriptionLong2 = authoring.resourceScriptableObject.statLongDescription2,
                statDescriptionLong3 = authoring.resourceScriptableObject.statLongDescription3
                    
            });

            if (authoring.resourceScriptableObject.resourceType == ResourceType.Currency)
            {
                AddComponent(entity,
                    new CurrencyComponent()
                    {
                        currencyValue = authoring.resourceScriptableObject.resourceValue
                    });

            }


        }
    }




}


[UpdateAfter(typeof(Collisions.PickupInputPowerUpUseImmediateSystem))]
public partial class SpawnResourceParticleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithoutBurst().ForEach
        (( Entity entity,  ref ResourceItemComponent powerItemComponent) =>
            {
                if(powerItemComponent.particleSystemEntitySpawned) return;
                var e = powerItemComponent.particleSystemEntity;
                var instance = ecb.Instantiate(e);
                //var instance = e;
                //Debug.Log("INSTANCE " + instance);
                powerItemComponent.particleSystemEntitySpawned = true;
                var tr = SystemAPI.GetComponent<LocalTransform>(entity);
                ecb.AddComponent(instance, new SpawnedItem()
                {
                    spawned = true, itemParent = entity, spawnedLocalTransform = tr
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

