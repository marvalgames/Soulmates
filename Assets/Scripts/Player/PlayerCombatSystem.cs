using Collisions;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

namespace Sandbox.Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerCombatSystem : SystemBase
    {
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int CombatAction = Animator.StringToHash("CombatAction");
        private static readonly int ComboAnimationPlayed = Animator.StringToHash("ComboAnimationPlayed");


        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach(
                (
                    PlayerCombat playerCombat,
                    Animator animator,
                    ref CheckedComponent checkedComponent,
                    ref LocalTransform localTransform,
                    ref InputControllerComponent inputController,
                    in LocalToWorld ltw,
                    in ApplyImpulseComponent applyImpulse
                ) =>
                {
                    var buttonXpressed = inputController.buttonX_Press; //kick types
                    var buttonXtap = inputController.buttonX_Tap; //punch types
                    var leftBumperPressed = inputController.leftBumperPressed;
                    var leftBumperUp = inputController.leftBumperReleased;
                    var allowKick = buttonXpressed == true &&
                                    (math.abs(animator.GetFloat(Vertical)) < 2 || applyImpulse.Grounded == false);
                    var buttonXunPressed = inputController.buttonTimeX_UnPressed;
                    var comboBufferTimeMax = inputController.comboBufferTimeMax;

                    if ((buttonXtap &&
                         checkedComponent is { comboIndexPlaying: 0, AttackStages: AttackStages.End }) ||
                        checkedComponent.AttackStages == AttackStages.No)
                    {
                        checkedComponent.comboIndexPlaying = 1;
                        inputController.comboBufferTimeStart = 0;
                        inputController.comboBufferTimeEnd = 0;
                        playerCombat.SelectMove(1);
                        animator.SetInteger(ComboAnimationPlayed, 1);
                    }
                    else if (buttonXtap && checkedComponent.AttackStages == AttackStages.Action &&
                             checkedComponent.comboIndexPlaying >= 1)
                    {
                        checkedComponent.comboButtonClicked = true;
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

                    if (checkedComponent.AttackStages == AttackStages.End && checkedComponent.comboButtonClicked)
                    {
                        checkedComponent.comboIndexPlaying += 1;
                        animator.SetInteger(ComboAnimationPlayed, checkedComponent.comboIndexPlaying);
                        playerCombat.SelectMove(1);
                        inputController.comboBufferTimeStart = 0;
                        checkedComponent.comboButtonClicked = false;
                    }

                    if (checkedComponent.AttackStages == AttackStages.End)
                    {
                        if (inputController.comboBufferTimeStart == 0)
                        {
                            inputController.comboBufferTimeStart = buttonXunPressed;
                        }

                        inputController.comboBufferTimeEnd = buttonXunPressed;
                        var timeSincePressed =
                            inputController.comboBufferTimeEnd - inputController.comboBufferTimeStart;
                        if (timeSincePressed > comboBufferTimeMax || timeSincePressed < 0)
                        {
                            checkedComponent.comboIndexPlaying = 0;
                            animator.SetInteger(ComboAnimationPlayed, 0);
                            checkedComponent.comboButtonClicked = false;
                        }
                    }
                }
            ).Run();
        }
    }
}