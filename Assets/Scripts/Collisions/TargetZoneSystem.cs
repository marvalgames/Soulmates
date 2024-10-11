using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Collision
{
    public partial struct TargetZoneSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (zone, actor, entityTransform, entity) 
                     in SystemAPI.Query<RefRW<TargetZoneComponent>, ActorInstance, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                zone.ValueRW.headZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().headZone.position;
                zone.ValueRW.bodyZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().bodyZone.position;
                zone.ValueRW.leftHandZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().leftHandZone.position;
                zone.ValueRW.rightHandZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().rightHandZone.position;
                zone.ValueRW.leftFootZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().leftFootZone.position;
                zone.ValueRW.rightFootZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().rightFootZone.position;
                zone.ValueRW.weapon1ZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().weapon1Zone.position;
                
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}