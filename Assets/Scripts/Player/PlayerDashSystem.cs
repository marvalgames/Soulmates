using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PlayerJumpSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerDashSystem : SystemBase
    {
        private static readonly int Dash = Animator.StringToHash("Dash");


        protected override void OnUpdate()
        {
            var dt = SystemAPI.Time.DeltaTime;
            Entities.WithoutBurst().ForEach(
                (
                    Entity e,
                    ActorInstance actor,
                    ref PlayerDashComponent playerDash,
                    in InputControllerComponent inputController,
                    in ApplyImpulseComponent applyImpulse,
                    in LocalToWorld ltw,
                    in DashAudioVideoGO player
                ) =>
                {
                    if (playerDash.active == false) return;
                    var audioSource = player.AudioSource;
                    var visualEffect = player.VisualEffect;
                    var animator = actor.actorPrefabInstance.GetComponent<Animator>();

                    if (playerDash.DelayTimeTicker > 0)
                    {
                        playerDash.DelayTimeTicker -= dt;
                        return;
                    }

                    playerDash.DelayTimeTicker = 0;

                    if (playerDash is { DashTimeTicker: 0, DelayTimeTicker: <= 0 })
                    {
                        var bPressed =
                            inputController.buttonB_Pressed; // put back for general LD 50 change since no jump
                        if (bPressed)
                        {
                            playerDash.DashTimeTicker += dt;
                            if (animator.GetInteger(Dash) == 0)
                            {
                                animator.SetInteger(Dash, 1);
                                playerDash.InDash = true;
                                if (playerDash.uses > 0)
                                {
                                    playerDash.uses -= 1;
                                }
                                else
                                {
                                    playerDash.active = false;
                                }
                            }

                            if (audioSource)
                            {
                                if (player.AudioClip)
                                {
                                    if (audioSource.isPlaying == false)
                                    {
                                        audioSource.clip = player.AudioClip;
                                        audioSource.Play();
                                    }
                                }
                            }


                            if (visualEffect && applyImpulse.InJump == false)
                            {
                                visualEffect.Play();
                            }
                            else
                            {
                                visualEffect.Stop();
                            }
                        }
                    }
                    else if (playerDash.DashTimeTicker < playerDash.dashTime && animator.speed > 0 &&
                             SystemAPI.HasComponent<PhysicsVelocity>(e) && SystemAPI.HasComponent<PhysicsMass>(e))
                    {
                        var pv = SystemAPI.GetComponent<PhysicsVelocity>(e);
                        var pm = SystemAPI.GetComponent<PhysicsMass>(e);
                        playerDash.InDash = true;
                        var force = ltw.Forward * playerDash.power;
                        pv.ApplyLinearImpulse(pm, force);
                        playerDash.DashTimeTicker += dt;
                        SystemAPI.SetComponent(e, pv);
                    }
                    else if (playerDash.DashTimeTicker >= playerDash.dashTime)
                    {
                        playerDash.DashTimeTicker = 0;
                        playerDash.DelayTimeTicker = playerDash.delayTime;
                        animator.SetInteger(Dash, 0);
                        playerDash.InDash = false;
                        if (audioSource != null) audioSource.Stop();
                        //if (player.ps != null) player.ps.Stop();
                    }
                }
            ).Run();
        }
    }
}