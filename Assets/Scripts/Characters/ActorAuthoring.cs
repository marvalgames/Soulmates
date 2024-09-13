using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public struct ActorComponent : IComponentData
    {
        public bool instantiated;
    }
    public class ActorClass : IComponentData
    {
        public GameObject actorPrefab;
    }

    public class ActorInstance : IComponentData
    {
        public GameObject actorPrefabInstance;
        public Entity linkedEntity;
    }
    
    public class ActorAuthoring : MonoBehaviour
    {
        public GameObject actorPrefab;
        private class ActorAuthoringBaker : Baker<ActorAuthoring>
        {
            public override void Bake(ActorAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new ActorComponent());
                AddComponentObject(e, new ActorClass {actorPrefab = authoring.actorPrefab});
            }
        }
    }
}