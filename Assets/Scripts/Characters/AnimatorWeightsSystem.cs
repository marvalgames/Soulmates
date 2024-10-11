﻿using Collisions;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct AnimatorWeightsComponent : IComponentData
{
    public float hitWeight;
    public float aimWeight;
    public float animSpeed;
    public float impulseSpeed;
    public bool useImpulseSpeed;
    public bool resetSpeed;
    public float animatorStateWeight;
    public bool animatorInTransition;
    public FixedString64Bytes currentStateName;
    public FixedString64Bytes stateName;
    public FixedString64Bytes defaultStateName;
}

[RequireMatchingQueriesForUpdate]
public partial struct AnimatorWeightsSystem : ISystem
{
    private static readonly int HitWeight = Animator.StringToHash("HitWeight");
    private static readonly int AimWeight = Animator.StringToHash("AimWeight");

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (animatorValues, checkedComponent, actor, dead) in
                 SystemAPI
                     .Query<RefRW<AnimatorWeightsComponent>, RefRO<CheckedComponent>, ActorInstance,
                         RefRO<DeadComponent>>())
        {
            var animator = actor.actorPrefabInstance.GetComponent<Animator>();
            if (animator.GetCurrentAnimatorClipInfo(0).Length == 0) continue;
            animatorValues.ValueRW.hitWeight = animator.GetFloat(HitWeight);
            animatorValues.ValueRW.aimWeight = animator.GetFloat(AimWeight);
            animatorValues.ValueRW.animSpeed = animator.speed;
            if (animatorValues.ValueRW.useImpulseSpeed && dead.ValueRO.isDead == false)
            {
                animator.speed = animatorValues.ValueRW.impulseSpeed;
            }
            else if (animatorValues.ValueRW.resetSpeed)
            {
                animator.speed = 1;
                animatorValues.ValueRW.resetSpeed = false;
            }

            var playingLayer = animator.IsInTransition(0);
            animatorValues.ValueRW.animatorInTransition = playingLayer;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            //Debug.Log("Default State YES " + animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            animatorValues.ValueRW.animatorStateWeight = math.frac(stateInfo.normalizedTime);
            animatorValues.ValueRW.stateName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;

            //test
            if (animatorValues.ValueRW.animatorStateWeight > animatorValues.ValueRW.hitWeight)
            {
                //animatorValues.ValueRW.hitWeight = animatorValues.ValueRW.animatorStateWeight;
            }


            //Fast-forward to the middle of the animation
            //animator["Walk"].normalizedTime = 0.5f;
        }
    }
}