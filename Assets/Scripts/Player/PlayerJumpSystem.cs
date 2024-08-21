using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace Sandbox.Player
{
    public struct PlayerJumpComponent : IComponentData
    {
        public float startJumpGravityForce;
        public float gameStartJumpGravityForce;
        public float heightOneFrames;
        public float heightTwoTime;
        public float heightThreeTime;
        public float doubleHeightOneFrames;
        public float doubleHeightTwoTime;
        public float doubleHeightThreeTime;
        public float addedNegativeForce;
        public float jumpDownGravityMultiplier;
        public float jumpY;
        public float airForce;
        public int jumpPoints;
        public bool CancelJump;
        public JumpStages JumpStage;
        public bool disabled;
        public bool doubleJump;
        public bool DoubleJumpStarted;
        public bool DoubleJumpAllowed;
        public float JumpStartFrames;
        public float JumpStartHeightTwoTime;
        public float JumpStartHeightThreeTime;
        public int JumpCount;
        public int frames;
        public bool playJumpAudio;
        public bool playJumpAnimation;
        public bool playVfx;
        public bool stopVfx;
    }


    public enum JumpStages
    {
        Ground,
        JumpStart,
        JumpUp,
        JumpDown,
        JumpEnd
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(InputControllerSystemUpdate))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial class PlayerJumpSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithNone<Pause>().ForEach(
                (
                    ref PhysicsVelocity pv,
                    ref ApplyImpulseComponent applyImpulseComponent,
                    ref PlayerJumpComponent playerJumpComponent,
                    in InputControllerComponent inputController,
                    in LocalToWorld ltw,
                    in Entity e
                ) =>
                {
                    var hasDash = SystemAPI.HasComponent<PlayerDashComponent>(e);
                    if (hasDash)
                    {
                        hasDash = SystemAPI.GetComponent<PlayerDashComponent>(e).active;
                    }

                    var invincible = false;
                    var timeButtonPressed = inputController.buttonTimePressed;
                    var jumpPoints = playerJumpComponent.jumpPoints;

                    var leftStickX = inputController.leftStickX;
                    var leftStickY = inputController.leftStickY;
                    var zModifier = (ltw.Forward * leftStickY).z;
                    var buttonAReleased = inputController.buttonA_Released;
                    var button_a = inputController.buttonA_Pressed;

                    if (button_a)
                    {
                        playerJumpComponent.JumpCount += 1;
                    }

                    if (buttonAReleased)
                    {
                        playerJumpComponent.CancelJump = true;
                    }

                    if (buttonAReleased && playerJumpComponent.DoubleJumpStarted == false &&
                        playerJumpComponent.doubleJump)
                    {
                        playerJumpComponent.DoubleJumpAllowed = true;
                        playerJumpComponent.CancelJump = false;
                    }

                    var velocity = pv.Linear;
                    var originalJumpFrames = playerJumpComponent.JumpStartFrames;
                    var originalJumpPower = playerJumpComponent.startJumpGravityForce;
                    var height_II_timer = timeButtonPressed > 0.08 &&
                                          timeButtonPressed < playerJumpComponent.JumpStartHeightTwoTime &&
                                          jumpPoints >= 2;
                    var height_III_timer = timeButtonPressed >= playerJumpComponent.JumpStartHeightTwoTime
                                           && timeButtonPressed < playerJumpComponent.heightThreeTime &&
                                           jumpPoints == 3;


                    if (applyImpulseComponent.InJump == false) //has touched ground
                    {
                        playerJumpComponent.playVfx = false;
                        playerJumpComponent.frames = 0;
                        playerJumpComponent.JumpStage = JumpStages.Ground;
                        playerJumpComponent.CancelJump = false;
                        playerJumpComponent.DoubleJumpStarted = false;
                        playerJumpComponent.DoubleJumpAllowed = false;
                        playerJumpComponent.JumpCount = 0;
                        //playerJumpComponent.stopVfx = true;
                    }
                    else
                    {
                        playerJumpComponent.playVfx = true;
                    }

                    if (applyImpulseComponent.Falling)
                    {
                        if (hasDash == false)
                        {
                            pv.Linear.y += applyImpulseComponent.inAirNegativeForce;
                        }
                        else
                        {
                            var playerDash = SystemAPI.GetComponent<PlayerDashComponent>(e);
                            if (playerDash.InDash == false)
                            {
                                pv.Linear.y += applyImpulseComponent.inAirNegativeForce;
                            }
                        }

                        return;
                    }

                    if (button_a && playerJumpComponent.frames == 0)
                    {
                        playerJumpComponent.playJumpAudio = true;
                        playerJumpComponent.JumpStartFrames = playerJumpComponent.heightOneFrames;
                        playerJumpComponent.JumpStartHeightTwoTime = playerJumpComponent.heightTwoTime;
                        playerJumpComponent.JumpStartHeightThreeTime = playerJumpComponent.heightThreeTime;
                        applyImpulseComponent.InJump = true;
                        applyImpulseComponent.Grounded = false;
                        applyImpulseComponent.Falling = false;
                        playerJumpComponent.frames = 1;
                        playerJumpComponent.playJumpAnimation = true;
                        playerJumpComponent.playVfx = true;
                        playerJumpComponent.JumpStage = JumpStages.JumpStart;
                        velocity = new float3(pv.Linear.x, originalJumpPower, pv.Linear.z);
                    }
                    else if (button_a && playerJumpComponent.DoubleJumpAllowed)
                    {
                        playerJumpComponent.JumpStartFrames = playerJumpComponent.doubleHeightOneFrames;
                        playerJumpComponent.JumpStartHeightTwoTime = playerJumpComponent.doubleHeightTwoTime;
                        playerJumpComponent.JumpStartHeightThreeTime = playerJumpComponent.doubleHeightThreeTime;
                        playerJumpComponent.DoubleJumpStarted = true;
                        playerJumpComponent.DoubleJumpAllowed = false;
                        applyImpulseComponent.InJump = true;
                        applyImpulseComponent.Grounded = false;
                        applyImpulseComponent.Falling = false;
                        playerJumpComponent.frames = 1;
                        playerJumpComponent.playJumpAnimation = true;
                        playerJumpComponent.JumpStage = JumpStages.JumpStart;
                        velocity = new float3(pv.Linear.x, originalJumpPower * 1, pv.Linear.z); //ADD DBL JUMP FACTOR
                    }
                    else if (playerJumpComponent.frames >= 1 && playerJumpComponent.frames <= originalJumpFrames &&
                             applyImpulseComponent.InJump &&
                             applyImpulseComponent.Grounded == false && applyImpulseComponent.Falling == false)
                    {
                        playerJumpComponent.frames = playerJumpComponent.frames + 1;
                        velocity = new float3(pv.Linear.x, originalJumpPower, zModifier);
                    }
                    else if (playerJumpComponent.frames > originalJumpFrames && height_II_timer &&
                             applyImpulseComponent.InJump &&
                             playerJumpComponent.CancelJump == false &&
                             applyImpulseComponent.Grounded == false && applyImpulseComponent.Falling == false)
                    {
                        playerJumpComponent.frames = playerJumpComponent.frames + 1;
                        velocity = new float3(pv.Linear.x, originalJumpPower, zModifier);
                    }
                    else if (playerJumpComponent.frames > originalJumpFrames && height_III_timer &&
                             applyImpulseComponent.InJump &&
                             playerJumpComponent.CancelJump == false &&
                             applyImpulseComponent.Grounded == false && applyImpulseComponent.Falling == false)
                    {
                        playerJumpComponent.frames = playerJumpComponent.frames + 1;
                        velocity = new float3(pv.Linear.x, originalJumpPower, zModifier);
                    }

                    pv.Linear = new float3(velocity.x, velocity.y, velocity.z);

                    if (playerJumpComponent.JumpStage != JumpStages.Ground)
                    {
                        if (hasDash == false)
                        {
                            pv.Linear.y += applyImpulseComponent.inAirNegativeForce;
                        }
                        else
                        {
                            var playerDash = SystemAPI.GetComponent<PlayerDashComponent>(e);
                            invincible = playerDash.Invincible;
                            if (invincible == false)
                            {
                                pv.Linear.y += applyImpulseComponent.inAirNegativeForce;
                            }
                        }
                    }

                    if (button_a && playerJumpComponent.frames == 1)
                    {
                        if (hasDash) // break dash
                        {
                            var playerDash = SystemAPI.GetComponent<PlayerDashComponent>(e);
                            playerDash.InDash = false;
                            playerDash.DashTimeTicker = 0;
                            playerDash.DelayTimeTicker = 0;
                            SystemAPI.SetComponent(e, playerDash);
                        }
                    }
                }
            ).Schedule();
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerJumpSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerJumpAudioSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach(
                (
                    ref PlayerJumpComponent playerJumpComponent,
                    in ApplyImpulseComponent applyImpulseComponent,
                    in VisualEffectJumpGO goVisualEffect,
                    in AudioPlayerJumpGO goAudioPlayer,
                    in LocalTransform transform
                    //PlayerJumpGameObjectClass playerJump, 
                    //uses audio source created in PlayerJumpGameObjectClass sub-scene authoring 
                ) =>
                {
                    
                    if (goVisualEffect.VisualEffect && applyImpulseComponent.InJump)
                    {
                        goVisualEffect.VisualEffect.transform.position = transform.Position;
                        goVisualEffect.VisualEffect.SetFloat("FlareRate", 100);
                        Debug.Log("VFX Jump");
                    }
                    else if (goVisualEffect.VisualEffect && !applyImpulseComponent.InJump)
                    {
                        goVisualEffect.VisualEffect.transform.position = transform.Position;
                        goVisualEffect.VisualEffect.SetFloat("FlareRate", 0);
                    }
                    
                    
                    var audioSource = goAudioPlayer.AudioSource;
                    if (audioSource && playerJumpComponent.playJumpAudio)
                    {
                        var clip = goAudioPlayer.AudioClip;
                        audioSource.PlayOneShot(audioSource.clip);
                        playerJumpComponent.playJumpAudio = false;
                    }
                }
            ).Run();
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerJumpSystem))]
    public partial class PlayerJumpAnimationSystem : SystemBase
    {
        private static readonly int JumpState = Animator.StringToHash("JumpState");

        protected override void OnUpdate()
        {
            Entities.WithoutBurst().WithNone<Pause>().ForEach(
                (
                    Animator animator,
                    ref PlayerJumpComponent jump //uses animator that is added to entity from main scene Player Object
                ) =>
                {
                    if (jump.playJumpAnimation)
                    {
                        jump.playJumpAnimation = false;
                        animator.SetInteger(JumpState, 1);
                    }
                }
            ).Run();
        }
    }
}