using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Collisions
{
    //[UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateInGroup(typeof(PresentationSystemGroup))]
    //[UpdateAfter(typeof(ParentSystem))]
    public partial struct TargetZoneTrackerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (targetZoneTracker, targetLocalToWorld, targetZoneTrackerTransform, entity)
                     in SystemAPI
                         .Query<RefRW<TargetZonesTrackerComponent>, RefRW<LocalToWorld>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                //if(SystemAPI.HasComponent<Parent>(entity) == false) continue;
                //var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
                var parentEntity = targetZoneTracker.ValueRW.ParentEntity;
                //Debug.Log("parent " + parentEntity);
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
                    // targetLocalToWorld.ValueRW.Value = float4x4.TRS(
                    //     zone.position, // Position
                    //     zone.rotation, // Rotation
                    //     new float3(1f, 1f, 1f)); // Scale (uniform)

                }
                else if (targetZoneType == TriggerType.LeftHand)
                {
                    var zone = targetZone.leftHandZone;
                    targetZoneTrackerTransform.ValueRW.Position = zone.position;
                    targetZoneTrackerTransform.ValueRW.Rotation = zone.rotation;

                    // targetLocalToWorld.ValueRW.Value = float4x4.TRS(
                    //     zone.position, // Position
                    //     zone.rotation, // Rotation
                    //     new float3(1f, 1f, 1f)); // Scale (uniform)
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