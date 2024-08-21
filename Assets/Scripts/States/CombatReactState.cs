using UnityEngine;

public class CombatReactState : StateMachineBehaviour
{
    private static readonly int Dash = Animator.StringToHash("Dash");
    private static readonly int HitReact = Animator.StringToHash("HitReact");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetInteger(HitReact, 0);
        animator.SetInteger(Dash, 0);
        //Debug.Log("hit");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.GetComponent<Rigidbody>() == false) return;
        animator.GetComponent<Rigidbody>().isKinematic = false;
    }

}
