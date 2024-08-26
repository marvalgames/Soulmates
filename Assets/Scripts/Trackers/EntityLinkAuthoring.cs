using Unity.Entities;
using UnityEngine;

namespace Trackers
{

    public class EntityLinkAuthoring : MonoBehaviour
    {
        [Tooltip("Match with skin mesh character index in Main Scene")]
        public int index;

    
        class EntityLinkBaker : Baker<EntityLinkAuthoring>
        {

            public override void Bake(EntityLinkAuthoring authoring)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                //var entityManager = world.EntityManager;

                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(e, new EntityFollow {linkedEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic), 
                    index = authoring.index});
                
                //Debug.Log("ADD LINK " + GetEntity(authoring.gameObject));
            
            }
        }
    
    }
}
