using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Player
{
    struct PlayerAimComponent : IComponentData
    {
        public float3 aimDirection;
        public float3 aimLocation;
        public float3 crosshairDirection;
    }

    public class PlayerAimAuthoring : MonoBehaviour
    {
        public Transform aimLocation;//doesn't work because needs to update first in system
        private class PlayerAimAuthoringBaker : Baker<PlayerAimAuthoring>
        {
            public override void Bake(PlayerAimAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new PlayerAimComponent { aimLocation = authoring.aimLocation.position });

            }
        }
    }
}