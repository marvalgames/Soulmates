using Unity.Entities;
using UnityEngine;


public class VisualEffectSceneAuthoring : MonoBehaviour
{
    //public GameObject parentGameObject;

    class VisualEffectSceneAuthoringBaker : Baker<VisualEffectSceneAuthoring>
    {
        public override void Bake(VisualEffectSceneAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            //var parentEntity = GetEntity(authoring.parentGameObject, TransformUsageFlags.Dynamic);

            AddComponent(e,
                new VisualEffectSceneComponent
                {
                    //parentEntity = parentEntity
                }
            );
        }
    }
}