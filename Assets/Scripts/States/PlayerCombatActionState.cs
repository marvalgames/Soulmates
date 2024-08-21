using Sandbox.Player;
using UnityEngine;

public class PlayerCombatActionState : StateMachineBehaviour
{
    private static readonly int CombatAction = Animator.StringToHash("CombatAction");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<PlayerCombat>().StartAttackUpdateCheckComponent();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.SetInteger("CombatAction", 0);
        animator.GetComponent<PlayerCombat>().StateUpdateCheckComponent();
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("player exit behaviour");
        animator.SetInteger(CombatAction, 0);
        //animator.SetLayerWeight(1, 0);

        animator.GetComponent<PlayerCombat>().EndAttack();
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Implement code that processes and affects root motion
        animator.GetComponent<PlayerCombat>().StartMotionUpdateCheckComponent();

    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}