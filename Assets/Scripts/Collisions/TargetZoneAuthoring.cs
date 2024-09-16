using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public struct TargetZoneComponent : IComponentData
    {
        public bool validTarget;

        public bool headZoneEnabled;
        public LocalTransform headZone;
        public float3 headZonePosition;

        public bool bodyZoneEnabled;
        public LocalTransform bodyZone;
        public float3 bodyZonePosition;

        public bool leftHandZoneEnabled;
        public LocalTransform leftHandZone;
        public float3 leftHandZonePosition;

        public bool rightHandZoneEnabled;
        public LocalTransform rightHandZone;
        public float3 rightHandZonePosition;

        public bool leftFootZoneEnabled;
        public LocalTransform leftFootZone;
        public float3 leftFootZonePosition;

        public bool rightFootZoneEnabled;
        public LocalTransform rightFootZone;
        public float3 rightFootZonePosition;
    }

    public class TargetZoneAuthoring : MonoBehaviour
    {
        public bool headZoneEnabled = true;
        public bool bodyZoneEnabled = true;
        public bool leftHandZoneEnabled = true;
        public bool rightHandZoneEnabled = true;
        public bool leftFootZoneEnabled = true;
        public bool rightFootZoneEnabled = true;

        private class TargetZoneAuthoringBaker : Baker<TargetZoneAuthoring>
        {
            public override void Bake(TargetZoneAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new TargetZoneComponent
                {
                    headZoneEnabled = authoring.enabled,
                    bodyZoneEnabled = authoring.bodyZoneEnabled,
                    leftHandZoneEnabled = authoring.leftHandZoneEnabled,
                    rightHandZoneEnabled = authoring.rightHandZoneEnabled,
                    leftFootZoneEnabled = authoring.leftFootZoneEnabled,
                    rightFootZoneEnabled = authoring.rightFootZoneEnabled
                });
            }
        }
    }
}