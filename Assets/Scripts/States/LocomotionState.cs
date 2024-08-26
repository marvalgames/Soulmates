using Sandbox.Player;
using UnityEngine;

public class LocomotionState : StateMachineBehaviour
{
    public AnimationType animationType;
    private static readonly int WeaponRaised = Animator.StringToHash("WeaponRaised");
    private static readonly int JumpState = Animator.StringToHash("JumpState");
    private static readonly int Dash = Animator.StringToHash("Dash");


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (animationType == AnimationType.Aim)
        {
            animator.SetInteger(WeaponRaised, (int)WeaponMotion.Raised);
        }
        else if (animationType == AnimationType.Lowering)
        {
            animator.SetInteger(WeaponRaised, (int)WeaponMotion.Raised);
        }
        else if (animationType == AnimationType.DashRoll)
        {
            animator.SetInteger(Dash, 0);
        }



    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        switch (animationType)
        {
            case AnimationType.Lowering:
                animator.SetInteger(WeaponRaised, (int)WeaponMotion.Lowering);
                break;
            case AnimationType.JumpStart:
                animator.SetInteger(JumpState, 0);
                break;
            case AnimationType.DashRoll:
                animator.SetInteger(Dash, 0);
                break;
        }


    }

}