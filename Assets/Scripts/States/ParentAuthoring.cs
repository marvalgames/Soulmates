using Unity.Entities;
using UnityEngine;

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
     

        // var parentEffect = new EffectClass
        // {
        //     psEntity = GetEntity(authoring.parentEffect.psPrefab, TransformUsageFlags.Dynamic),
        //     clip = authoring.parentEffect.clip
        // };
        // if (parentEffect.transformGameObject == null)
        // {
        //     parentEffect.transformGameObject = authoring.gameObject;
        // }
        // var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
        // AddComponentObject(e, parentEffect);
    }
}