using Sandbox.Player;
using Unity.Entities;


public struct FreezeComponent : IComponentData
{

    public float freezeTime;
    public bool isFrozen;

    public bool stopAnimation;
}



////[UpdateAfter(typeof(Unity.Physics.Systems.EndFramePhysicsSystem))]

//public partial class FreezeSystem : SystemBase
//{

//    protected override void OnUpdate()
//    {

//        //Entities.WithoutBurst().WithStructuralChanges().WithNone<Pause>().ForEach((Entity e, EnemyMove move,
//        //    ref FreezeComponent freezeComponent,
//        //    ref EnemyStateComponent enemyStateComponent,
//        //    in RatingsComponent ratingsComponent

//        //) =>
//        //{
//        //    //if (pause.value == 1) return;

//        //    bool hasDamage = EntityManager.HasComponent(e, typeof(DamageComponent));
//        //    if (hasDamage)
//        //    {

//        //        float timeToAdd = EntityManager.GetComponentData<DamageComponent>(e).StunLanded;
//        //        Debug.Log("time " + timeToAdd);

//        //        freezeComponent.freezeTime += timeToAdd;
//        //        freezeComponent.isFrozen = true;
//        //    }
//        //    else
//        //    {
//        //        freezeComponent.freezeTime -= SystemAPI.Time.DeltaTime;
//        //        if (freezeComponent.freezeTime <= 0)
//        //        {
//        //            freezeComponent.freezeTime = 0;
//        //            freezeComponent.isFrozen = false;
//        //            move.agent.speed = move.moveSpeed;
//        //            if (move.stunEffect)
//        //            {
//        //                move.stunEffect.Stop();
//        //            }
//        //        }
//        //    }

//        //    if (freezeComponent.isFrozen == true)
//        //    {
//        //        //    move.moveSpeed = 0;
//        //        move.agent.speed = 0;
//        //        enemyStateComponent.MoveState = MoveStates.Idle;
//        //        if (move.stunEffect && move.ps)
//        //        {
//        //            if (move.ps.isPlaying == false)
//        //            {
//        //                move.stunEffect.transform.SetParent(move.transform);
//        //                move.stunEffect.Play(true);
//        //            }
//        //        }

//        //    }


//        //}).Run();


//        Entities.WithNone<Pause>().ForEach((Entity e,
//            ref FreezeComponent freezeComponent,
//            ref RatingsComponent ratingsComponent,
//            ref PlayerMoveComponent playerMoveComponent,
//            in InputControllerComponent inputController

//        ) =>
//        {
//            //if (pause.value == 1) return;
//            //Debug.Log("stun");
//            var leftBumperPressed = inputController.leftBumperPressed;//freeze
//            if (leftBumperPressed)
//            {
//                freezeComponent.isFrozen = !freezeComponent.isFrozen;
//            }


//            //bool button_b_released = inputController.buttonB_Released;


//            var currentSpeed = ratingsComponent.speed;
//            ratingsComponent.gameSpeed = freezeComponent.isFrozen ? 0 : currentSpeed;

//            //if (freezeComponent.isFrozen)
//            //{
//            //    //playerMoveComponent.currentSpeed = ratingsComponent.speed * 0;//not required or just turbo
//            //    ratingsComponent.gameSpeed = 0;
//            //}
//            //else
//            //{
//            //    //playerMoveComponent.currentSpeed = currentSpeed;
//            //    ratingsComponent.gameSpeed = currentSpeed;
//            //}





//            //bool hasDamage = EntityManager.HasComponent(e, typeof(DamageComponent));
//            //if (hasDamage)
//            //{

//            //    float timeToAdd = EntityManager.GetComponentData<DamageComponent>(e).StunLanded;
//            //    Debug.Log("time " + timeToAdd);

//            //    freezeComponent.freezeTime += timeToAdd;
//            //    freezeComponent.isFrozen = true;
//            //}
//            //else
//            //{
//            //    freezeComponent.freezeTime -= SystemAPI.Time.DeltaTime;
//            //    if (freezeComponent.freezeTime <= 0)
//            //    {
//            //        //Debug.Log("freeze time " + freezeComponent.freezeTime);
//            //        freezeComponent.freezeTime = 0;
//            //        freezeComponent.isFrozen = false;
//            //        // move.agent.speed = move.moveSpeed;
//            //        //if (move.stunEffect)
//            //        //{
//            //            //move.stunEffect.Stop();
//            //        //}
//            //    }
//            //}

//            //if (freezeComponent.isFrozen == true)
//            //{
//            //    //    move.moveSpeed = 0;
//            //    //move.agent.speed = 0;
//            //    //enemyStateComponent.MoveState = MoveStates.Idle;
//            //    //if (move.stunEffect && move.ps)
//            //    //{
//            //       // if (move.ps.isPlaying == false)
//            //        //{
//            //           // move.stunEffect.transform.SetParent(move.transform);
//            //            //move.stunEffect.Play(true);
//            //        //}
//            //    //}

//            //}


//        }).Run();







//    }
//}
