using Rukhanka;
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
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyActorMovementSystem))]
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
            var Aim = Animator.StringToHash("Aim");
            var combatAction = Animator.StringToHash("CombatAction");
            //var zone = new FastAnimatorParameter("Zone");
            //var velz = new FastAnimatorParameter("velz");


            foreach (var (anim, enemyState, enemyMove, actor, entity) in
                     SystemAPI
                         .Query<AnimatorParametersAspect, RefRO<EnemyStateComponent>, RefRO<EnemyMovementComponent>,
                             ActorInstance>()
                         .WithEntityAccess())
            {
                //var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                //animator.speed = enemyMove.ValueRO.animatorSpeed;
                //animator.SetInteger(zone, enemyState.ValueRO.Zone);
                //anim.SetIntParameter(zone, enemyState.ValueRO.Zone);
                //animator.SetFloat(velz, enemyMove.ValueRO.forwardVelocity, enemyMove.ValueRO.blendSpeed, SystemAPI.Time.DeltaTime);
                //anim.SetFloatParameter(velz, enemyMove.ValueRO.forwardVelocity);
            }

            foreach (var (actorAim, enemyMove, actor, entity) in
                     SystemAPI.Query<RefRO<ActorWeaponAimComponent>, RefRW<EnemyMovementComponent>, ActorInstance>()
                         .WithEntityAccess())
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                var weaponRaised = actorAim.ValueRO.weaponRaised == WeaponMotion.Started;
                var blendValue = enemyMove.ValueRW.aimBlendValue;
                var blendSpeed = enemyMove.ValueRW.aimBlendSpeed;
                var deltaTime = SystemAPI.Time.DeltaTime;
                //weaponRaised = true;
                if (weaponRaised)
                {
                    blendValue = Mathf.Lerp(blendValue, 1, deltaTime * blendSpeed);
                    animator.SetLayerWeight(0, 1 - blendValue);
                    animator.SetLayerWeight(1, blendValue); //1 is weapon layer
                    animator.SetBool(Aim, true);
                    //if (rig) rig.weight = 1;
                }
                else
                {
                    blendValue = Mathf.Lerp(blendValue, 0, deltaTime * blendSpeed);
                    animator.SetLayerWeight(0, 1 - blendValue);
                    animator.SetLayerWeight(1, blendValue);
                    animator.SetBool(Aim, false);
                    //if (rig) rig.weight = 0;
                }

                enemyMove.ValueRW.aimBlendValue = blendValue;
            }
        }
    }
}