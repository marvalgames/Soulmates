using System;
using Collisions;
using Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
    [Serializable]
    public struct ApplyImpulseComponent : IComponentData
    {
        public float animatorStickSpeed;
        public float stickX;
        public float stickY;
        public bool Falling;
        public bool Grounded;
        public bool GroundCollision;
        public bool NearGrounded;
        public bool Ceiling;
        public bool ApproachingStairs;
        public bool InJump;
        public float Force;
        public float3 Direction;
        public float3 Velocity;
        public float inAirNegativeForce;
        public float OnGroundNegativeForce;
        public float ApproachStairBoost;
        public LocalTransform LocalTransform;
        public float3 LastPositionLand;
        public bool hiJump;
        public float fallingFramesCounter;
        public float fallingFramesMaximum;
        public float checkGroundDistance;
        public float checkGroundStartY;
        public float checkRadius;
        public float checkNearGroundMultiplier;
        public int frames;
        public bool playerMoving;
        public bool crossHairOn;
        public float forwardSpeed;
        public bool movingBackStarted;
        public bool movingReversed;
        public bool checkMoveBackwards;
        public bool setCrosshairToPlayerForward;
        public float3 targetPosition;
        public float3 groundPosition;
        public bool nearEdge;
    }

    public struct PlayerMoveComponent : IComponentData
    {
        //public float currentSpeed;
        public float rotateSpeed;
        public float combatRotateSpeed;
        public bool snapRotation;
        public float dampTime;
        public bool move2d;
        public float3 startPosition;
        public bool inputDisabled;
        public float rotationVelocity;
        public float stepRate;
        public bool npc;
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateAfter(typeof(PlayerCombatSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraControlsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.Time.DeltaTime;
            var camTransform = SystemAPI.GetSingleton<CameraControlsComponent>();

            foreach (var (pv, transform, applyImpulseComponent, inputController, ratingsComponent, entity) in
                     SystemAPI.Query<RefRW<PhysicsVelocity>,
                             RefRW<LocalTransform>,
                             RefRW<ApplyImpulseComponent>,
                             RefRO<InputControllerComponent>,
                             RefRO<RatingsComponent>>
                         ().WithEntityAccess())
            {
                
                var forwardSpeed = 1;
                var currentSpeed = ratingsComponent.ValueRO.gameSpeed;
                pv.ValueRW.Linear = float3.zero;
                var leftStickX = inputController.ValueRO.leftStickX;
                var leftStickY = inputController.ValueRO.leftStickY;
                var stickInput = new Vector3(leftStickX, 0, leftStickY);
                stickInput.Normalize();
                float stickSpeed = stickInput.sqrMagnitude;

                var playerMoveComponent = SystemAPI.GetComponent<PlayerMoveComponent>(entity);
                var actorWeapon = SystemAPI.HasComponent<ActorWeaponAimComponent>(entity);

                var forwardAdjustment = 1;
                applyImpulseComponent.ValueRW.checkMoveBackwards = false;


                if (playerMoveComponent.move2d)
                {
                    leftStickY = 0;
                }

                if (playerMoveComponent.inputDisabled)
                {
                    leftStickX = 0;
                    leftStickY = 0;
                }

            

                var combatMode = false;
                var aimMode = false;
                if (actorWeapon)
                {
                    var aimComponent = SystemAPI.GetComponent<ActorWeaponAimComponent>(entity);
                    var playerAim = SystemAPI.GetComponent<PlayerAimComponent>(entity);//redundant
                    var playerPosition = transform.ValueRW.Position;
                    var distanceFromTarget = math.distance(playerPosition, aimComponent.targetPosition);
                    var crosshairPosition = aimComponent.targetPosition;
                    crosshairPosition.y = 0;
                    
                    var crosshairDirection = math.normalize(crosshairPosition-playerPosition);
                    aimComponent.distanceFromTarget = distanceFromTarget;
                    aimComponent.
                    combatMode = aimComponent.combatMode;
                    if (aimComponent.aimMode)
                    {
                        aimMode = true;
                        combatMode = false;
                        forwardAdjustment = 2;
                    }
                    playerAim.crosshairDirection = crosshairDirection;
                    SystemAPI.SetComponent(entity, aimComponent);//combine these two as one component
                    SystemAPI.SetComponent(entity, playerAim);
                }

                applyImpulseComponent.ValueRW.playerMoving = false;
                if (combatMode) currentSpeed = ratingsComponent.ValueRO.gameCombatSpeed;
                if (SystemAPI.HasComponent<MeleeComponent>(entity))
                {
                    var meleeComponent = SystemAPI.GetComponent<MeleeComponent>(entity);
                    if (meleeComponent.cancelMovement > 0)
                    {
                        currentSpeed *= (1 - meleeComponent.cancelMovement);
                    }
                }
                
                
                if (currentSpeed == 0) stickSpeed = 0;
                var targetDirection = (leftStickX * camTransform.right * forwardSpeed +
                                       leftStickY * camTransform.forward * forwardSpeed);
                var inputDirection = new Vector3(targetDirection.x, 0.0f, targetDirection.z).normalized;


                applyImpulseComponent.ValueRW.animatorStickSpeed = stickSpeed;
                var ltw = SystemAPI.GetComponent<LocalToWorld>(entity);
                float3 fwd = ltw.Forward * forwardSpeed;
                float3 right = -ltw.Right * forwardSpeed;
                fwd.y = 0;
                right.y = 0;
                fwd = math.normalize(fwd);
                right = math.normalize(right);
                if (math.abs(stickSpeed) > .0000f)
                {
                    applyImpulseComponent.ValueRW.playerMoving = true; //moving forward
                    float impulseFactor = 1;
                    if (SystemAPI.HasComponent<ImpulseComponent>(entity))
                    {
                        var impulse = SystemAPI.GetComponent<ImpulseComponent>(entity);
                        if (impulse.activate)
                        {
                            impulseFactor = impulse.animSpeedRatio;
                        }
                        else if (impulse.activateOnReceived)
                        {
                            impulseFactor = impulse.animSpeedRatioOnReceived;
                        }
                    }


                    if (aimMode || combatMode)
                    {
                        pv.ValueRW.Linear.x = inputDirection.x * currentSpeed * impulseFactor;
                        pv.ValueRW.Linear.z = inputDirection.z * currentSpeed * impulseFactor;
                    }
                    else
                    {
                        pv.ValueRW.Linear = fwd * stickSpeed * currentSpeed;
                    }

                    if (applyImpulseComponent.ValueRW.ApproachingStairs)
                    {
                        pv.ValueRW.Linear.y += applyImpulseComponent.ValueRW.ApproachStairBoost;
                    }
                }

                if (!applyImpulseComponent.ValueRW.ApproachingStairs)
                {
                    pv.ValueRW.Linear.y += applyImpulseComponent.ValueRW.OnGroundNegativeForce;
                }


                if (playerMoveComponent.move2d)
                {
                    var tr = transform.ValueRW.Position;
                    tr.z = playerMoveComponent.startPosition.z;
                    transform.ValueRW.Position = tr;
                }

                var inDash = false;
                if (SystemAPI.HasComponent<PlayerDashComponent>(entity))
                {
                    var playerDashComponent = SystemAPI.GetComponent<PlayerDashComponent>(entity);
                    inDash = playerDashComponent.InDash;
                }


                var range = 5f;
                float3 direction = default;
                if (SystemAPI.HasComponent<MatchupComponent>(entity)) ;
                {
                    var matchupComponent = SystemAPI.GetComponent<MatchupComponent>(entity);
                    var targetEntity = matchupComponent.closestEnemyEntity;
                    var playerPosition = transform.ValueRW.Position;
                    if (SystemAPI.HasComponent<LocalTransform>(targetEntity))
                    {
                        var targetPosition = SystemAPI.GetComponent<LocalTransform>(targetEntity).Position;
                        range = math.distance(targetPosition, playerPosition);
                        direction = math.normalize(targetPosition - playerPosition);
                        direction.y = 0;
                    }
                }
                applyImpulseComponent.ValueRW.forwardSpeed = forwardSpeed;
                if (combatMode && !inDash && range < 5 && !aimMode)
                {
                    var slerpDampTime = playerMoveComponent.combatRotateSpeed;
                    var targetRotation = quaternion.LookRotationSafe(direction, math.up()); //always face player
                    var playerRotation = SystemAPI.GetComponent<LocalTransform>(entity).Rotation;
                    playerRotation = math.slerp(playerRotation, targetRotation.value,
                        slerpDampTime * SystemAPI.Time.DeltaTime);
                    transform.ValueRW.Rotation = playerRotation;
                }
                else if (math.length(targetDirection) > 0 && !aimMode)
                {
                    quaternion targetRotation = Quaternion.LookRotation(inputDirection, math.up());
                    transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation,
                        playerMoveComponent.rotateSpeed * time / forwardAdjustment);
                }
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial class PlayerMoveAnimatorSystem : SystemBase
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int Grounded = Animator.StringToHash("Grounded");


        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach(
                (ActorInstance actorInstance, Entity e,
                    in PlayerMoveComponent playerMove, in ApplyImpulseComponent applyImpulse) =>
                {
                    var animator = actorInstance.actorPrefabInstance.GetComponent<Animator>();
                    var animStickSpeed = applyImpulse.animatorStickSpeed;
                    if (SystemAPI.HasComponent<PlayerDashComponent>(e))
                    {
                        var playerDashComponent = SystemAPI.GetComponent<PlayerDashComponent>(e);
                        animStickSpeed = applyImpulse.Grounded || playerDashComponent.InDash
                                                               || applyImpulse.ApproachingStairs
                            ? applyImpulse.animatorStickSpeed
                            : 0;
                    }

                    var dampTime =
                        animStickSpeed < .003 ? 0 : playerMove.dampTime; //if stick not moved (stopping) then no damp

                    animator.SetFloat(Vertical, animStickSpeed, dampTime, SystemAPI.Time.DeltaTime);
                    animator.SetBool(Grounded, applyImpulse.Grounded);
                }
            ).Run();
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMoveAnimatorSystem))]
    public partial class PlayerMoveAudioVfxSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach(
                (
                    in VisualEffectGO goVisualEffect,
                    in AudioPlayerGO goAudioPlayer,
                    in LocalTransform transform,
                    in PlayerMoveComponent playerMoveComponent,
                    in ApplyImpulseComponent applyImpulseComponent) =>
                {
                    // var audioSource = playerMove.audioSource;
                    // Debug.Log("AS " + goAudioPlayer.AudioSource);
                    // Debug.Log("AC " + goAudioPlayer.AudioClip);
                    var stickSpeed = applyImpulseComponent.animatorStickSpeed;


                    if (math.abs(stickSpeed) >= .0001f && applyImpulseComponent.Grounded)
                    {
                        if (goAudioPlayer.AudioSource)
                        {
                            var audioSource = goAudioPlayer.AudioSource;
                            var pitch = stickSpeed * playerMoveComponent.stepRate;
                            Debug.Log("audio source  " + audioSource);
                            if (audioSource.isPlaying == false)
                            {
                                audioSource.pitch = pitch;
                                audioSource.clip = goAudioPlayer.AudioClip;
                                audioSource.Play();
                            }
                        }

                        if (goVisualEffect.VisualEffect)
                        {
                            goVisualEffect.VisualEffect.transform.position = transform.Position;
                            //goVisualEffect.VisualEffect.SetFloat("FlareRate", 40);
                            goVisualEffect.VisualEffect.Play();
                            Debug.Log("Flare Rate ");
                        }
                    }
                    else
                    {
                        var audioSource = goAudioPlayer.AudioSource;
                        if (audioSource != null)
                        {
                            // Debug.Log("STOP");
                            audioSource.pitch = 0;
                            audioSource.Stop();
                        }


                        if (goVisualEffect.VisualEffect != null)
                        {
                            goVisualEffect.VisualEffect.transform.position = transform.Position;
                            //goVisualEffect.VisualEffect.SetFloat("FlareRate", 0);
                            goVisualEffect.VisualEffect.Stop();
                        }
                    }
                }
            ).Run();
        }
    }
}