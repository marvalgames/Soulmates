using UnityEngine;

public class CombatActionState : StateMachineBehaviour
{
    private static readonly int State = Animator.StringToHash("State");

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
    {
        //change file to be like player combat where using checkComponent
        //animator.GetComponent<EnemyMelee>().attackStarted = true;
        // animator.GetComponent<EnemyMelee>().StartAttackUpdateCheckComponent();
        // animator.GetComponent<EnemyMelee>().StopAgent();
        // animator.GetComponent<EnemyMelee>().StartAimIK();
        // animator.GetComponent<EnemyMelee>().StartIK();
        //
        animator.SetInteger(State, 0);


    }
    
   
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Implement code that processes and affects root motion
        //animator.GetComponent<EnemyMelee>().StartMotionUpdateCheckComponent();
        animator.SetInteger(State, 1);

    }

    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }


    
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // animator.GetComponent<EnemyMelee>().StartAgent();
        // animator.GetComponent<EnemyMelee>().StopAimIK();
        // animator.GetComponent<EnemyMelee>().StopIK();
        // animator.GetComponent<EnemyMelee>().EndAttack();
        animator.SetInteger(State, 2);
        Debug.Log("STRIKE END");


    }

  
}
