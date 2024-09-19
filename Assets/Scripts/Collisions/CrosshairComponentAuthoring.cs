using Unity.Entities;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Collisions
{
    public struct CrosshairComponent : IComponentData
    {
        public float raycastDistance;
        public float targetDelayCounter;
        public bool spawnCrosshair;
        public float gamePadSensitivity;
        public float viewportPct;
    }

    public class CrosshairClass : IComponentData
    {
        public GameObject crosshairPrefab;
    }

    public class CrosshairInstance : IComponentData
    {
        public GameObject crosshairInstance;
    }


    public class CrosshairComponentAuthoring : MonoBehaviour
    {
        public float raycastDistance = 140;
        public GameObject crosshairPrefab;
        [SerializeField] [Range(0.0f, 100.0f)] private float gamePadSensitivity = 20;
        [Range(80f, 100.0f)] [SerializeField] private float viewportPct = 100;

        void Start()
        {
            //var localTransform = LocalTransform.Identity;
            //manager.AddComponentData(entity, localTransform);
        }

        private class CrosshairComponentAuthoringBaker : Baker<CrosshairComponentAuthoring>
        {
            public override void Bake(CrosshairComponentAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new CrosshairComponent
                    {
                        raycastDistance = authoring.raycastDistance, gamePadSensitivity = authoring.gamePadSensitivity,
                        viewportPct = authoring.viewportPct
                    });
                AddComponentObject(entity, new CrosshairClass { crosshairPrefab = authoring.crosshairPrefab });
            }
        }
    }
}

//
// using Unity.Entities;
// using Unity.Transforms;
// using UnityEngine;
//
// namespace Collisions
// {
//     public struct CrosshairComponent : IComponentData
//     {
//         public float raycastDistance;
//         public float targetDelayCounter;
//     }
//
//     public class CrosshairComponentAuthoring : MonoBehaviour
//     {
//         public float raycastDistance = 140;
//
//         EntityManager manager;
//         Entity entity;
//
//         void Start()
//         {
//             //entity = GetComponent<CharacterEntityTracker>().linkedEntity;
//             //manager = GetComponent<CharacterEntityTracker>().entityManager;
//             var world = World.DefaultGameObjectInjectionWorld;
//             manager = world.EntityManager;
//             entity = manager.CreateEntity();
//
//             manager.AddComponentObject(entity, this);
//             manager.AddComponentData(entity, new CrosshairComponent {raycastDistance = raycastDistance });
//             var localTransform = LocalTransform.Identity;
//             manager.AddComponentData(entity, localTransform);
//
//
//         }
//         void Update()
//         {
//             if (manager == default || entity == Entity.Null) return;
//             var hasComponent = manager.HasComponent<CrosshairComponent>(entity);
//             if (hasComponent == false) return;
//
//             var crosshairComponent = manager.GetComponentData<CrosshairComponent>(entity);
//             crosshairComponent.raycastDistance = raycastDistance;
//             manager.SetComponentData(entity, crosshairComponent);
//
//
//         }
//
//
//   
//
//
//
//     }
// }