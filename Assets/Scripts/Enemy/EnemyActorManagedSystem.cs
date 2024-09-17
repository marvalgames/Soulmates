using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Enemy
{
    public partial struct EnemyActorManagedSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ActorComponent>();
        }

        // Because this update accesses managed objects, it cannot be Burst compiled,
        // so we do not add the [BurstCompile] attribute.
        public void OnUpdate(ref SystemState state)
        {

            var zone = Animator.StringToHash("Zone");
            var velz = Animator.StringToHash("velz");


            foreach (var (enemyState, enemyMove, actor, entity) in
                     SystemAPI.Query<RefRO<EnemyStateComponent>, RefRO<EnemyMovementComponent>, ActorInstance>().WithEntityAccess())
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                animator.speed = enemyMove.ValueRO.animatorSpeed;
                animator.SetInteger(zone, enemyState.ValueRO.Zone);
                animator.SetFloat(velz, enemyMove.ValueRO.forwardVelocity, enemyMove.ValueRO.blendSpeed, SystemAPI.Time.DeltaTime);
            }
        }

       
        
    }
}