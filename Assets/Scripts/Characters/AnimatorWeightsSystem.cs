﻿using Collisions;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

public struct AnimatorWeightsComponent : IComponentData
{
    public float hitWeight;
    public float aimWeight;
}

[RequireMatchingQueriesForUpdate]
public partial class AnimatorWeightsSystem : SystemBase
{
    private static readonly int HitWeight = Animator.StringToHash("HitWeight");
    private static readonly int AimWeight = Animator.StringToHash("AimWeight");

    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach(
            (
                //Animator animator, 
                ref AnimatorWeightsComponent animatorWeightsComponent, in CheckedComponent checkedComponent,
                in ActorInstance actor
            ) =>
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                animatorWeightsComponent.hitWeight = animator.GetFloat(HitWeight);
                animatorWeightsComponent.aimWeight = animator.GetFloat(AimWeight);
            }
        ).Run();
    }
}