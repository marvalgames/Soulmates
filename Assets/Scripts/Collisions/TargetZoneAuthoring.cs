using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public struct TargetZoneComponent : IComponentData
    {
        public LocalTransform headZone;
        public bool headZoneEnabled;
        public bool validTarget;
        public float3 headZonePosition;
    }
    public class TargetZoneAuthoring : MonoBehaviour
    {
        public bool headZoneEnabled = true;
        private class TargetZoneAuthoringBaker : Baker<TargetZoneAuthoring>
        {
            public override void Bake(TargetZoneAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new TargetZoneComponent{headZoneEnabled = authoring.enabled} );
            }
        }
    }
}