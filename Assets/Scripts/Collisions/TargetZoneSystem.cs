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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (zone, actor, entityTransform, entity) 
                     in SystemAPI.Query<RefRW<TargetZoneComponent>, ActorInstance, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                zone.ValueRW.headZonePosition = actor.actorPrefabInstance.GetComponent<TargetZone>().headZone.position;
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}