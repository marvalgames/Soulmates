using Sandbox.Player;
using UnityEngine;

public class PlayerCombatActionState : StateMachineBehaviour
{
    private static readonly int CombatAction = Animator.StringToHash("CombatAction");
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Enter;
        Debug.Log( "PLAYER STAGE " + animator.GetComponent<ActorEntityTracker>().animationStageTracker);
        ;
        //animator.GetComponent<PlayerCombat>().StartAttackUpdateCheckComponent();
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Update;
        //animator.GetComponent<PlayerCombat>().StateUpdateCheckComponent();
        Debug.Log( "PLAYER STAGE " + animator.GetComponent<ActorEntityTracker>().animationStageTracker);

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("player exit behaviour");
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Exit;
        animator.SetInteger(CombatAction, 0);
        //animator.GetComponent<PlayerCombat>().EndAttack();
        Debug.Log( "PLAYER STAGE " + animator.GetComponent<ActorEntityTracker>().animationStageTracker);

    }

}