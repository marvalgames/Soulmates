using UnityEngine;

public class DeadAnimationState : StateMachineBehaviour
{
    private static readonly int dead = Animator.StringToHash("Dead");
    private static readonly int zone = Animator.StringToHash("Zone");
    private static readonly int aim = Animator.StringToHash("Aim");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        Debug.Log("animator dead");     
        animator.SetInteger(dead, -1);
        animator.SetBool(aim, false);

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 0;

    }
    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //// Implement code that processes and affects root motion
        //Vector3 velocity = animator.deltaPosition / SystemAPI.Time.DeltaTime * (float)animator.GetComponent<PlayerMove>().currentSpeed;
        //velocity.y = animator.GetComponent<Rigidbody>().velocity.y;//pass y from rigibody since rigidbody when on controls y force 
        //animator.GetComponent<Rigidbody>().velocity = velocity;

    }

   
}
