using Collisions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerCombatSystem : ISystem
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");


        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actor, melee, checkedComponent, inputController, applyImpulse, e) in SystemAPI
                         .Query<ActorInstance, RefRW<MeleeComponent>, RefRW<CheckedComponent>,
                             RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess())
            {
                //var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                //var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                //playerCombat.meleeEntity = e;
                //playerCombat.entityManager = state.EntityManager;
                var buttonXpressed = inputController.ValueRW.buttonX_Press; //kick types
                var buttonXtap = inputController.ValueRW.buttonX_Tap; //punch types
                var leftBumperPressed = inputController.ValueRW.leftBumperPressed;
                var leftBumperUp = inputController.ValueRW.leftBumperReleased;
                var allowKick = buttonXpressed &&
                                (melee.ValueRW.verticalSpeed < 2 || applyImpulse.ValueRO.Grounded == false);
                var buttonXunPressed = inputController.ValueRW.buttonTimeX_UnPressed;
                var comboBufferTimeMax = inputController.ValueRW.comboBufferTimeMax;
                //melee.ValueRW.selectMove = 0;

                Debug.Log("xtap " + buttonXtap);
                
                if (buttonXtap &&
                     (checkedComponent.ValueRW is { comboIndexPlaying: 0, AttackStages: AttackStages.End } ||
                    checkedComponent.ValueRW.AttackStages == AttackStages.No))
                {
                    checkedComponent.ValueRW.comboIndexPlaying = 1;
                    inputController.ValueRW.comboBufferTimeStart = 0;
                    inputController.ValueRW.comboBufferTimeEnd = 0;
                    melee.ValueRW.selectMove = 1;
                    Debug.Log("Player Select xtap ");
                    //playerCombat.SelectMove(1);
                    //animator.SetInteger(ComboAnimationPlayed, 1);
                    melee.ValueRW.comboAnimationPlayed = 1;
                }
                else if (buttonXtap && checkedComponent.ValueRW is { AttackStages: AttackStages.Action, comboIndexPlaying: >= 1 })
                {
                    checkedComponent.ValueRW.comboButtonClicked = true;
                }
                else if (allowKick) //kick
                {
                    melee.ValueRW.selectMove = 2;
                    //playerCombat.SelectMove(2);
                }
                else if (leftBumperPressed)
                {
                    melee.ValueRW.selectMove = 10;
                    //playerCombat.SelectMove(10);
                }
                else if (leftBumperUp)
                {
                    //animator.SetInteger(CombatAction, 0);
                    melee.ValueRW.combatAction = 0;
                }

                if (checkedComponent.ValueRW is { AttackStages: AttackStages.End, comboButtonClicked: true })
                {
                    checkedComponent.ValueRW.comboIndexPlaying += 1;
                    //animator.SetInteger(ComboAnimationPlayed, checkedComponent.ValueRW.comboIndexPlaying);
                    melee.ValueRW.comboAnimationPlayed = checkedComponent.ValueRW.comboIndexPlaying;
                    melee.ValueRW.selectMove = 1;
                    //playerCombat.SelectMove(1);
                    inputController.ValueRW.comboBufferTimeStart = 0;
                    checkedComponent.ValueRW.comboButtonClicked = false;
                }

                if (checkedComponent.ValueRW.AttackStages == AttackStages.End)
                {
                    if (inputController.ValueRW.comboBufferTimeStart == 0)
                    {
                        inputController.ValueRW.comboBufferTimeStart = buttonXunPressed;
                    }

                    inputController.ValueRW.comboBufferTimeEnd = buttonXunPressed;
                    var timeSincePressed =
                        inputController.ValueRW.comboBufferTimeEnd - inputController.ValueRW.comboBufferTimeStart;
                    if (timeSincePressed > comboBufferTimeMax || timeSincePressed < 0)
                    {
                        checkedComponent.ValueRW.comboIndexPlaying = 0;
                        //animator.SetInteger(ComboAnimationPlayed, 0);
                        melee.ValueRW.comboAnimationPlayed = 0;
                        checkedComponent.ValueRW.comboButtonClicked = false;
                    }
                }
            }
        }
    }


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerCombatManagedSystem : ISystem
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");


        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actor, melee, checkedComponent, inputController, applyImpulse, e) in SystemAPI
                         .Query<ActorInstance, RefRW<MeleeComponent>, RefRW<CheckedComponent>,
                             RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess())
            {
                var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                playerCombat.meleeEntity = e;
                playerCombat.entityManager = state.EntityManager;

                if (melee.ValueRW.selectMove > 0)
                {
                    playerCombat.SelectMove(melee.ValueRW.selectMove);
                    melee.ValueRW.selectMove = 0;
                    var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                    melee.ValueRW.verticalSpeed = animator.GetFloat(Vertical);
                    //melee.ValueRW.comboAnimationPlayed = 1;
                    //melee.ValueRW.selectMove = 1;
                    animator.SetInteger(ComboAnimationPlayed, melee.ValueRW.comboAnimationPlayed);
                    //animator.SetInteger(CombatAction, melee.ValueRW.combatAction);
                }
            }
        }
        
        
    }
}