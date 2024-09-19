using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Player
{
    public partial struct PlayerAimSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actorAim, playerAim, entity) in SystemAPI.Query<RefRO<ActorWeaponAimComponent>, RefRW<PlayerAimComponent>>().WithEntityAccess())
            {
                var aimTarget = actorAim.ValueRO.crosshairRaycastTarget;
                var transform = SystemAPI.GetComponent<LocalTransform>(entity);
                //playerAim.ValueRW.aimDirection = math.normalize(aimTarget - playerAim.ValueRW.aimLocation);
                var aimDirection = math.normalize(aimTarget - transform.Position);
                playerAim.ValueRW.aimDirection = aimDirection;
            }

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}