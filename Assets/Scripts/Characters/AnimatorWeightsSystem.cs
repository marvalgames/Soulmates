using Collisions;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

public struct AnimatorWeightsComponent : IComponentData
{
    public float hitWeight;
    public float aimWeight;
    public float animSpeed;
    public float impulseSpeed;
    public bool useImpulseSpeed;
    public bool resetSpeed;
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
                in ActorInstance actor, in DeadComponent dead
            ) =>
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                animatorWeightsComponent.hitWeight = animator.GetFloat(HitWeight);
                animatorWeightsComponent.aimWeight = animator.GetFloat(AimWeight);
                animatorWeightsComponent.animSpeed = animator.speed;
                if (animatorWeightsComponent.useImpulseSpeed && dead.isDead == false)
                {
                    animator.speed = animatorWeightsComponent.impulseSpeed;
                }
                else if (animatorWeightsComponent.resetSpeed)
                {
                    animator.speed = 1;
                    animatorWeightsComponent.resetSpeed = false;
                }
                
            }
        ).Run();
    }
}