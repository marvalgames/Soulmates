using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public class EffectsAuthoring : MonoBehaviour//NOT WORKING
{
    //public bool pauseEffect;
    //[assembly:RegisterGenericComponentType(typeof(List<EffectClass>))]
    public GameObject effectsManager;
    

}

public class EffectsBaker : Baker<EffectsAuthoring>
{
    public override void Bake(EffectsAuthoring authoring)
    {
        //AddComponentObject(authoring.effectsManager.GetComponent<EffectsPrefabManager>());
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

