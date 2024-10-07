using Sandbox.Player;
using UnityEngine;

public class CombatActionState : StateMachineBehaviour
{
    private static readonly int State = Animator.StringToHash("State");

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {
        animator.SetInteger(State, 1);
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Enter;
        //Debug.Log("STRIKE START " + animator.IsInTransition(0));


    }
    
   
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        animator.SetInteger(State, 2);
        //Debug.Log("STRIKE UPDATE");
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Update;

    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        //Debug.Log("Exited state machine: " + stateMachinePathHash);
    }

    
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(State, 3);
        animator.GetComponent<ActorEntityTracker>().animationStageTracker = AnimationStage.Exit;
        //Debug.Log("STRIKE END " + animator.IsInTransition(0));
        animator.GetComponent<ActorEntityTracker>().debugCounter += 1;

    }

  
}
