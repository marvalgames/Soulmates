using Collisions;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;


public struct AnimatorWeightsComponent : IComponentData
{
    public int setIntegerDead;
    public float speed;
    public float hitWeight;
    public float aimWeight;
    public float deflectWeight;
    public bool firstFrame;
}

[RequireMatchingQueriesForUpdate]
public partial class AnimatorWeightsSystem : SystemBase
{
    private static readonly int HitWeight = Animator.StringToHash("HitWeight");
    private static readonly int AimWeight = Animator.StringToHash("AimWeight");

    protected override void OnUpdate()
    {

      

        Entities.WithoutBurst().ForEach(
            (Animator animator, ref AnimatorWeightsComponent animatorWeightsComponent, in CheckedComponent checkedComponent) =>
            {
                animatorWeightsComponent.hitWeight = animator.GetFloat(HitWeight);
                animatorWeightsComponent.aimWeight = animator.GetFloat(AimWeight);
            }
        ).Run();










    }






}