using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;


public struct EffectsComponent : IComponentData
{
    public bool pauseEffect;
    public bool soundPlaying;
    public bool playEffectAllowed;
    public EffectType playEffectType;
    public int effectIndex;
}



public class EffectsManager : MonoBehaviour
{

    private Entity entity;
    private EntityManager manager;
 
    public List<EffectClass> actorEffect;
    public AudioSource audioSource;
    
    void Start()
    {
        
        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            if (entity != Entity.Null)
            {
                manager.AddComponentObject(entity, this);
                manager.AddComponentObject(entity, audioSource);
                
            }

        }

        for (var i = 0; i < actorEffect.Count; i++)
        {
            if (actorEffect[i] == null) continue;
            if (actorEffect[i].psPrefab != null)
            {
                var ps = Instantiate(actorEffect[i].psPrefab, transform);
                actorEffect[i].psInstance = ps;
            }

            if (actorEffect[i].vePrefab != null)
            {
                var ve = Instantiate(actorEffect[i].vePrefab, transform);
                Debug.Log("LOADED " + actorEffect[i].effectType);
                actorEffect[i].veInstance = ve;
                ve.Stop();
            }

        }
    }



}
