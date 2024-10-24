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
        public float3 rightHandZoneEntityPosition;
        public Entity rightHandZoneEntity;

        public bool leftFootZoneEnabled;
        public LocalTransform leftFootZone;
        public float3 leftFootZonePosition;

        public bool rightFootZoneEnabled;
        public LocalTransform rightFootZone;
        public float3 rightFootZonePosition;
        
        public bool weapon1ZoneEnabled;
        public LocalTransform weapon1Zone;
        public float3 weapon1ZonePosition;

        public bool weapon2ZoneEnabled;
        public LocalTransform weapon2Zone;
        public float3 weapon2ZonePosition;

        
    }

    public class TargetZoneAuthoring : MonoBehaviour
    {
        public bool headZoneEnabled = true;
        public bool bodyZoneEnabled = true;
        public bool leftHandZoneEnabled = true;
        public bool rightHandZoneEnabled = true;
        public bool leftFootZoneEnabled = true;
        public bool rightFootZoneEnabled = true;
        public bool weapon1ZoneEnabled = true;
        public bool weapon2ZoneEnabled = true;
        
        public GameObject rightHandZoneEntityGameObject;

        private class TargetZoneAuthoringBaker : Baker<TargetZoneAuthoring>
        {
            public override void Bake(TargetZoneAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new TargetZoneComponent
                {
                    
                    rightHandZoneEntityPosition = authoring.rightHandZoneEntityGameObject.transform.position,
                    rightHandZoneEntity = GetEntity(authoring.rightHandZoneEntityGameObject, TransformUsageFlags.Dynamic),
                    headZoneEnabled = authoring.enabled,
                    bodyZoneEnabled = authoring.bodyZoneEnabled,
                    leftHandZoneEnabled = authoring.leftHandZoneEnabled,
                    rightHandZoneEnabled = authoring.rightHandZoneEnabled,
                    leftFootZoneEnabled = authoring.leftFootZoneEnabled,
                    rightFootZoneEnabled = authoring.rightFootZoneEnabled,
                    weapon1ZoneEnabled = authoring.weapon1ZoneEnabled,
                    weapon2ZoneEnabled = authoring.weapon2ZoneEnabled
                });
            }
        }
    }
}