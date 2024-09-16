using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public struct TargetZonesTrackerComponent : IComponentData
    {
        public Entity ParentEntity;
        public TriggerType TriggerType;
    }
    public class TargetZoneTrackerAuthoring : MonoBehaviour
    {
        public TriggerType triggerType;
        private class TargetZonesTrackerBaker : Baker<TargetZoneTrackerAuthoring>
        {
            public override void Bake(TargetZoneTrackerAuthoring authoring)
            {
                var  e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new TargetZonesTrackerComponent {TriggerType = authoring.triggerType});
                
            }
        }
    }
}