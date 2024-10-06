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

            foreach (var (actor, checkedComponent, inputController, applyImpulse,e  ) in SystemAPI.Query<ActorInstance, RefRW<CheckedComponent>,
                     RefRW<InputControllerComponent>, RefRW<ApplyImpulseComponent>>().WithEntityAccess())
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                var playerCombat = actor.actorPrefabInstance.GetComponent<PlayerCombat>();
                playerCombat.meleeEntity = e;
                playerCombat.entityManager = state.EntityManager;
                 var buttonXpressed = inputController.ValueRW.buttonX_Press; //kick types
                    var buttonXtap = inputController.ValueRW.buttonX_Tap; //punch types
                    var leftBumperPressed = inputController.ValueRW.leftBumperPressed;
                    var leftBumperUp = inputController.ValueRW.leftBumperReleased;
                    var allowKick = buttonXpressed &&
                                    (math.abs(animator.GetFloat(Vertical)) < 2 || applyImpulse.ValueRO.Grounded == false);
                    var buttonXunPressed = inputController.ValueRW.buttonTimeX_UnPressed;
                    var comboBufferTimeMax = inputController.ValueRW.comboBufferTimeMax;

                    if ((buttonXtap &&
                         checkedComponent.ValueRW is { comboIndexPlaying: 0, AttackStages: AttackStages.End } ) ||
                        checkedComponent.ValueRW.AttackStages == AttackStages.No)
                    {
                        checkedComponent.ValueRW.comboIndexPlaying = 1;
                        inputController.ValueRW.comboBufferTimeStart = 0;
                        inputController.ValueRW.comboBufferTimeEnd = 0;
                        playerCombat.SelectMove(1);
                        animator.SetInteger(ComboAnimationPlayed, 1);
                    }
                    else if (buttonXtap && checkedComponent.ValueRW.AttackStages == AttackStages.Action &&
                             checkedComponent.ValueRW.comboIndexPlaying >= 1)
                    {
                        checkedComponent.ValueRW.comboButtonClicked = true;
                    }
                    else if (allowKick) //kick
                    {
                        playerCombat.SelectMove(2);
                    }
                    else if (leftBumperPressed)
                    {
                        playerCombat.SelectMove(10);
                    }
                    else if (leftBumperUp)
                    {
                        animator.SetInteger(CombatAction, 0);
                    }

                    if (checkedComponent.ValueRW.AttackStages == AttackStages.End && checkedComponent.ValueRW.comboButtonClicked)
                    {
                        checkedComponent.ValueRW.comboIndexPlaying += 1;
                        animator.SetInteger(ComboAnimationPlayed, checkedComponent.ValueRW.comboIndexPlaying);
                        playerCombat.SelectMove(1);
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
                            animator.SetInteger(ComboAnimationPlayed, 0);
                            checkedComponent.ValueRW.comboButtonClicked = false;
                        }
                    }
                    
                
            }
        }
        
        
    }
}