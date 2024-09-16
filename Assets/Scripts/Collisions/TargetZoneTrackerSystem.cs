using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    public partial struct TargetZoneTrackerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (targetZoneTracker, targetZoneTrackerTransform, entity)
                     in SystemAPI.Query<RefRW<TargetZonesTrackerComponent>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
                if (!state.EntityManager.HasComponent<ActorInstance>(parentEntity)) return;
                var targetZoneType = targetZoneTracker.ValueRW.TriggerType;
                var parentGameObject = state.EntityManager.GetComponentObject<ActorInstance>(parentEntity);
                targetZoneTracker.ValueRW.ParentEntity = parentEntity;
                var targetZone = parentGameObject.actorPrefabInstance.GetComponent<TargetZone>();

                Transform followTransform;
                if (targetZoneType == TriggerType.Head)
                {
                    var zone = targetZone.headZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
                else if (targetZoneType == TriggerType.Body)
                {
                    var zone = targetZone.bodyZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
                else if (targetZoneType == TriggerType.LeftHand)
                {
                    var zone = targetZone.leftHandZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
                else if (targetZoneType == TriggerType.RightHand)
                {
                    var zone = targetZone.rightHandZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
                else if (targetZoneType == TriggerType.LeftFoot)
                {
                    var zone = targetZone.leftFootZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
                else if (targetZoneType == TriggerType.RightFoot)
                {
                    var zone = targetZone.rightFootZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}