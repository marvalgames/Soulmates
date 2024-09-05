using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Sandbox.Player
{
    [System.Serializable]
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

            foreach (var (pv, transform, applyImpulseComponent, inputController, ratingsComponent, checkedComponent,
                         entity) in
                     SystemAPI.Query<RefRW<PhysicsVelocity>,
                             RefRW<LocalTransform>,
                             RefRW<ApplyImpulseComponent>,
                             RefRO<InputControllerComponent>,
                             RefRO<RatingsComponent>,
                             RefRO<CheckedComponent>>
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

                if (playerMoveComponent.move2d)
                {
                    leftStickY = 0;
                }

                if (playerMoveComponent.inputDisabled)
                {
                    leftStickX = 0;
                    leftStickY = 0;
                }

                var aimMode = false;
                if (actorWeapon)
                {
                    var aimComponent = SystemAPI.GetComponent<ActorWeaponAimComponent>(entity);
                    var distanceFromTarget = math.distance(transform.ValueRO.Position, aimComponent.targetPosition);
                    aimComponent.distanceFromTarget = distanceFromTarget;
                    if (aimComponent.aimMode)
                    {
                        aimMode = true;
                    }

                    SystemAPI.SetComponent(entity, aimComponent);
                }

                applyImpulseComponent.ValueRW.playerMoving = false;
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
                    bool hasImpulse = SystemAPI.HasComponent<ImpulseComponent>(entity);

                    if (hasImpulse)
                    {
                        var impulse = SystemAPI.GetComponent<ImpulseComponent>(entity);
                        if (impulse.activate) impulseFactor = impulse.animSpeedRatio;
                        else if (impulse.activateOnReceived) impulseFactor = impulse.animSpeedRatioOnReceived;
                        
                    }

                    if (aimMode)
                    {
                        pv.ValueRW.Linear.x = inputDirection.x * currentSpeed * impulseFactor;
                        pv.ValueRW.Linear.z = inputDirection.z * currentSpeed * impulseFactor;
                    }
                    else
                    {
                        pv.ValueRW.Linear = fwd * stickSpeed * currentSpeed;
                    }
                }
                pv.ValueRW.Linear.y += applyImpulseComponent.ValueRW.OnGroundNegativeForce;
                applyImpulseComponent.ValueRW.forwardSpeed = forwardSpeed;
                //transform.ValueRW.Scale = checkedComponent.ValueRO.scaleFactor;
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PlayerMoveAudioVfxSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach(
                (
                    in LocalTransform transform,
                    in VisualEffectGO goVisualEffect,
                    in AudioPlayerGO goAudioPlayer,
                    in PlayerMoveComponent playerMoveComponent,
                    in ApplyImpulseComponent applyImpulseComponent) =>
                {
                    var stickSpeed = applyImpulseComponent.animatorStickSpeed;


                    if (math.abs(stickSpeed) >= .0001f && applyImpulseComponent.Grounded)
                    {
                        if (goAudioPlayer.AudioSource)
                        {
                            var audioSource = goAudioPlayer.AudioSource;
                            var pitch = stickSpeed * playerMoveComponent.stepRate;
                            // Debug.Log("audio source  " + audioSource);
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
                            goVisualEffect.VisualEffect.SetFloat("FlareRate", 40);
                            // Debug.Log("Flare Rate ");
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
                            goVisualEffect.VisualEffect.SetFloat("FlareRate", 0);
                        }
                    }
                }
            ).Run();
        }
    }
    
    
    
    
}