using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Collisions
{
    public struct ToggleCollisionComponent : IComponentData
    {
        public float Value;
    }

    public struct ToggleFilterComponent : IComponentData
    {
        public CollisionFilter defaultFilter;
        public CollisionFilter dashFilter;
    }


    [InternalBufferCapacity(8)]
    public struct ActorCollisionBufferElement : IBufferElementData
    {
        public bool isPlayer;
        public Entity _parent;
        public Entity _child;
    }

    public class ToggleCollisionAuthoring : MonoBehaviour
    {
        public bool isPlayer = true;
        public GameObject _parent;


        class ToggleCollisionBaker : Baker<ToggleCollisionAuthoring>
        {
            public override void Bake(ToggleCollisionAuthoring authoring)
            {
                var parentEntity = GetEntity(authoring._parent.transform.root.gameObject, TransformUsageFlags.Dynamic);
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                if (authoring.isPlayer)
                {
                    var buffer = AddBuffer<ActorCollisionBufferElement>(entity);
                    var go = GetChildren(authoring.gameObject);
                    for (int i = 0; i < go.Length; i++)
                    {
                        var child = go[i];
                        var childEntity = GetEntity(child, TransformUsageFlags.Dynamic);
                        //Debug.Log("children " + GetEntity(child, TransformUsageFlags.Dynamic));
                        var collisionElement =
                            new ActorCollisionBufferElement
                            {
                                isPlayer = authoring.isPlayer,
                                _parent = parentEntity,
                                _child = childEntity
                            };

                        buffer.Add(collisionElement);
                    }
                }

                AddComponent(entity, new ToggleCollisionComponent());
            }
        }
    }
}