using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public struct CrosshairComponent : IComponentData
    {
        public float raycastDistance;
        public float targetDelayCounter;
    }

    public class CrosshairComponentAuthoring : MonoBehaviour
    {
        public float raycastDistance = 140;

        EntityManager manager;
        Entity entity;

        void Start()
        {
            //entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            //manager = GetComponent<CharacterEntityTracker>().entityManager;
            var world = World.DefaultGameObjectInjectionWorld;
            manager = world.EntityManager;
            entity = manager.CreateEntity();

            manager.AddComponentObject(entity, this);
            manager.AddComponentData(entity, new CrosshairComponent {raycastDistance = raycastDistance });
            var localTransform = LocalTransform.Identity;
            manager.AddComponentData(entity, localTransform);


        }
        void Update()
        {
            if (manager == default || entity == Entity.Null) return;
            var hasComponent = manager.HasComponent<CrosshairComponent>(entity);
            if (hasComponent == false) return;

            var crosshairComponent = manager.GetComponentData<CrosshairComponent>(entity);
            crosshairComponent.raycastDistance = raycastDistance;
            manager.SetComponentData(entity, crosshairComponent);


        }


  



    }
}