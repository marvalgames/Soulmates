using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class DestructibleClass : IComponentData
{
    public EffectClass ParentEffect;
    //public Transform Transform;
}

public class ParentAuthoring : MonoBehaviour
{
    public EffectClass parentEffect;

}


public class DestructibleBaker : Baker<ParentAuthoring>
{
    public override void Bake(ParentAuthoring authoring)
    {
        //var entity = GetEntity(authoring.gameObject);
        //Debug.Log("AUDIOBAKE " + entity.Index);

        //if (authoring.parentEffect == null) return;
        //var parentEffect = authoring.parentEffect;

        var parentEffect = new EffectClass
        {
            psEntity = GetEntity(authoring.parentEffect.psPrefab, TransformUsageFlags.Dynamic),
            clip = authoring.parentEffect.clip
        };
        if (parentEffect.transformGameObject == null)
        {
            parentEffect.transformGameObject = authoring.gameObject;
        }
        var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
        AddComponentObject(e, parentEffect);
    }
}