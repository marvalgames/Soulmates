

using Collisions;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
[UpdateAfter(typeof(PlayerMoveSystem))]
[RequireMatchingQueriesForUpdate]
public partial class BossStrategySystem : SystemBase
{
    private static readonly int Strike = Animator.StringToHash("Strike");


    protected override void OnUpdate()
    {

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var positionBuffer = GetBufferLookup<BossWaypointBufferElement>(true);
        var playerTransformGroup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        
        Entities.WithoutBurst().WithAll<EnemyComponent>().WithNone<Pause>().ForEach
        ((
                Animator animator, 
                EffectsManager effectsManager,
                ref BossMovementComponent bossMovementComponent,
                ref CheckedComponent checkedComponent,
                in Entity enemyE,
                in AudioSource audioSource,
                in  DefensiveStrategyComponent defensiveStrategyComponent) =>
        {

            animator.speed = 1;


            if (SystemAPI.HasComponent<EvadeComponent>(enemyE))
            {
                if (SystemAPI.GetComponent<EvadeComponent>(enemyE).InEvade == true)
                {
                    return;
                }
            }

            var targetPointBuffer = positionBuffer[enemyE];
            int audioIndex = targetPointBuffer[bossMovementComponent.CurrentIndex].audioListIndex;
            if (audioSource != null)
            {
                var hasEffects = SystemAPI.HasComponent<EffectsComponent>(enemyE);
                if (hasEffects)
                {
                    //var effects = SystemAPI.GetComponent<EffectsComponent>(enemyE);
                    if (!audioSource.isPlaying && effectsManager != null)
                    {
                        audioSource.clip = effectsManager.actorEffect[audioIndex].clip;
                        if (audioSource.clip != null)
                        {
                            audioSource.PlayOneShot(audioSource.clip, 1.0f);
                            //Debug.Log("AUDIO MANAGER " + audioSource);
                        }
                    }
                }
            }

            var action = targetPointBuffer[bossMovementComponent.CurrentIndex].wayPointAction;//for show weapon only - the anim is what  triggers whatever ammo may be used


            var animType = 0;


            if (action == (int)WayPointAction.Move)
            {
                animType = 0;
            }
            if (action == (int)WayPointAction.Attack)
            {
                animType = 1;
            }
            if (action == (int)WayPointAction.Fire)
            {
                animType = 2;
            }


            var animStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            

            if (checkedComponent.attackCompleted && animType == 1)
            {
                checkedComponent.attackCompleted = false;
                checkedComponent.hitTriggered = false;
                checkedComponent.anyAttackStarted = true;
            }
            else if(animator.GetInteger(Strike) == 1 && !checkedComponent.attackCompleted
             && animStateInfo.normalizedTime > .85)
            {
                checkedComponent.attackCompleted = true;
            }
            animator.SetInteger(Strike, animType);
            var chase = targetPointBuffer[bossMovementComponent.CurrentIndex].wayPointChase;
            if (targetPointBuffer.Length <= 0)
                return;
            var playerE = defensiveStrategyComponent.closestEnemiesAttackEntity;
            if (playerE == Entity.Null) return;
            var playerMove = SystemAPI.GetComponent<LocalTransform>(playerE);
            var playerForward = SystemAPI.GetComponent<LocalToWorld>(playerE).Forward;
            var bossLocalTransform = SystemAPI.GetComponent<LocalTransform>(enemyE);
            var targetPosition = targetPointBuffer[bossMovementComponent.CurrentIndex].wayPointPosition;
            var actionStopDistance = new float3(0, 1, 5);//may need to add to waypoints

            if (action == (int)WayPointAction.Attack)
            {
                targetPosition = new float3(playerMove.Position.x, playerMove.Position.y + actionStopDistance.y, playerMove.Position.z + actionStopDistance.z);
            }
            else if (chase)
            {
                if (SystemAPI.HasComponent<BossStrategyComponent>(enemyE))
                {
                    var bossStrategyComponent = SystemAPI.GetComponent<BossStrategyComponent>(enemyE);
                    targetPosition = new float3(playerMove.Position.x, targetPosition.y,
                        playerMove.Position.z + bossStrategyComponent.StopDistance); //keep the Y of the waypoint!
                }
            }

            //math.normalize(targetPosition);
            playerForward.y = 0;

            var bossXZ = new float3(bossLocalTransform.Position.x, 0, bossLocalTransform.Position.z);
            var playerXZ = new float3(playerMove.Position.x, 0, playerMove.Position.z);
            var direction = math.normalize(playerXZ - bossXZ);
            var dist = math.distance(bossXZ, playerXZ);
            if (dist < 1) direction = -math.forward();//????????????????? 1
            var targetRotation = quaternion.LookRotationSafe(direction, math.up());//always face player
            var slerpDampTime = bossMovementComponent.RotateSpeed;
            var rotation = SystemAPI.GetComponent<LocalTransform>(enemyE).Rotation;
            rotation = math.slerp(rotation, targetRotation.value, slerpDampTime * SystemAPI.Time.DeltaTime);
            var localTransform = SystemAPI.GetComponent<LocalTransform>(enemyE);
            localTransform.Rotation = rotation;
            SystemAPI.SetComponent(enemyE, localTransform);

            var targetSpeed = targetPointBuffer[bossMovementComponent.CurrentIndex].wayPointSpeed;
            var duration = targetPointBuffer[bossMovementComponent.CurrentIndex].duration;

            bossMovementComponent.CurrentWayPointTimer += SystemAPI.Time.DeltaTime;
            if (bossMovementComponent.CurrentWayPointTimer >= duration)
            {
                bossMovementComponent.CurrentWayPointTimer = 0;
                if (targetPointBuffer.Length > bossMovementComponent.CurrentIndex + 1)
                {
                    bossMovementComponent.CurrentIndex++;
                }
                else
                {
                    bossMovementComponent.CurrentIndex = bossMovementComponent.Repeat ? 0 : bossMovementComponent.CurrentIndex;
                }
                bossMovementComponent.WayPointReached = true;
            }
            else
            {
                bossLocalTransform.Position = bossLocalTransform.Position + math.normalize(targetPosition - bossLocalTransform.Position) * SystemAPI.Time.DeltaTime * bossMovementComponent.Speed * targetSpeed;
                SystemAPI.SetComponent(enemyE, bossLocalTransform);
                bossMovementComponent.WayPointReached = false;
            }


        }

        ).Run();
        
        ecb.Playback(EntityManager);
        ecb.Dispose();
        
    }



}





