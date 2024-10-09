using Collisions;
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

            var currentLayer = animator.GetLayerIndex("Default");
            var playingLayer0 = animator.IsInTransition(0);
            var playingLayer1 = animator.IsInTransition(1);
            animatorValues.ValueRW.animatorInTransition = playingLayer0;
            if (playingLayer1 == false)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                animatorValues.ValueRW.animatorStateWeight = math.frac(stateInfo.normalizedTime);
                animatorValues.ValueRW.stateName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
            }

            //test
            if (animatorValues.ValueRW.animatorStateWeight > animatorValues.ValueRW.hitWeight)
            {
                animatorValues.ValueRW.hitWeight = animatorValues.ValueRW.animatorStateWeight;
            }


            //Fast-forward to the middle of the animation
            //animator["Walk"].normalizedTime = 0.5f;
        }
    }
}