using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public struct BlockComponent : IComponentData // also use for chargingComponent currently
    {
        public bool blocked;
    }
    public class Block : MonoBehaviour
    {
        class BlockBaker : Baker<Block>
        {
            public override void Bake(Block authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new BlockComponent { blocked = false});
            }
        }

    }
}