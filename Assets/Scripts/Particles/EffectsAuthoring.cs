using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

public enum EffectType
{
    None,
    Damaged,
    Dead,
    TwoClose,
}

public struct EffectComponent : IComponentData
{
    public bool visualEffect;
    public bool disableEffect;
}

[Serializable]
public class Effect : IComponentData //use in system to spawn members
{
    public EffectType effectType;
    public GameObject psPrefab;
    public GameObject vePrefab;
    public AudioClip clip;
}

[Serializable]
public class EffectClass : IComponentData //use in authoring
{
    //instances could probably changed from Game Object to type and the GetComponent to retrieve
    public AudioClip effectClip;
    public GameObject effectParticleSystem;
    public GameObject effectVisualEffect;
    public GameObject effectParticleSystemInstance;
    public GameObject effectVisualEffectInstance;
    public bool pauseEffect;
    public bool soundPlaying;
    public bool playEffectAllowed;
    public EffectType playEffectType;
    public int effectIndex;
}

public class EffectClassHolder : IComponentData
{
    public List<EffectClass> effectsClassList = new();
    public GameObject effectAudioSourcePrefab;
    public AudioSource effectAudioSourceInstance;
}


[InternalBufferCapacity(8)]
public struct EffectComponentElement : IBufferElementData
{
    public bool pauseEffect;
    public bool soundPlaying;
    public bool playEffectAllowed;
    public EffectType playEffectType;
    public int effectIndex;
}

public class EffectsAuthoring : MonoBehaviour //NOT WORKING
{
    //public bool pauseEffect;
    //[assembly:RegisterGenericComponentType(typeof(List<EffectClass>))]
    //public GameObject effectsManager;
    public List<EffectClass> effectList;
    public bool visualEffect = false;
    public GameObject audioSourceEffectsPrefab;
}

public class EffectsBaker : Baker<EffectsAuthoring>
{
    public override void Bake(EffectsAuthoring authoring)
    {
        var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
        AddComponent(e, new EffectComponent { visualEffect = authoring.visualEffect });

        if (authoring.effectList.Count > 0)
        {

            var buffer = AddBuffer<EffectComponentElement>(e);
            foreach (var effect in authoring.effectList)
            {
                var effectComponentElement = new EffectComponentElement
                {
                    effectIndex = effect.effectIndex,
                    playEffectType = effect.playEffectType
                };
                buffer.Add(effectComponentElement);
            }

            var effectsClassList = new List<EffectClass>();
            for (var i = 0; i < authoring.effectList.Count; i++)
            {
                var effect = authoring.effectList[i];
                var addEffect =
                    new EffectClass
                    {
                        effectVisualEffect = effect.effectVisualEffect,
                        effectParticleSystem = effect.effectParticleSystem,
                        effectClip = effect.effectClip, effectIndex = effect.effectIndex,
                        playEffectType = effect.playEffectType
                    };
                effectsClassList.Add(addEffect);
            }


            var effectClassHolder = new EffectClassHolder
            {
                effectsClassList = effectsClassList,
                effectAudioSourcePrefab = authoring.audioSourceEffectsPrefab
            };
            
            AddComponentObject(e, effectClassHolder);
        }

    }
}


public partial class EffectsManagerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);


        Entities.WithoutBurst().ForEach(
            (
                EffectsPrefabManager effectsPrefabManager,
                in Entity entity
            ) =>
            {
                Debug.Log("EFFECTS PREFAB SYSTEM ENTITY " + entity);
                //Debug.Log("EFFECTS PREFAB SYSTEM PREFAB " + effectsPrefabManager.audioSource);
            }
        ).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}