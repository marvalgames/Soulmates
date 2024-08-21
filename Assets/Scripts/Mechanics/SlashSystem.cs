using Collisions;
using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

namespace Mechanics
{
    [RequireMatchingQueriesForUpdate]
    public partial class SlashSystem : SystemBase
    {
        private static readonly int SlashState = Animator.StringToHash("SlashState");

        protected override void OnUpdate()
        {
            Entities.ForEach((ref SlashComponent slashComponent, in InputControllerComponent input, in Entity e) =>
                {
                    if (slashComponent.slashActive == false) return;
                    slashComponent.slashState = (int) SlashStates.None;
                    //if ((input.buttonX_Pressed == true) && slashComponent.slashState == (int)SlashStates.None)//why are triggers backward? // LD50
                    if ((input.buttonY_Tap || input.buttonY_Press) &&
                        slashComponent.slashState == (int) SlashStates.None)
                    {
                        slashComponent.slashState = (int) SlashStates.Started;
                        if (slashComponent.animate == false)
                        {
                            slashComponent.animate = true;
                        }
                    }
                }
            ).Run();


            Entities.WithoutBurst().WithStructuralChanges().ForEach((
                    Animator animator, PlayerCombat playerCombat,
                    ref SlashComponent slashComponent, in CheckedComponent checkedComponent) =>
                {
       
                    if (slashComponent.slashActive == false) return;

                    if (slashComponent.animate == true && animator.GetInteger(SlashState) == 0)
                    {
                        playerCombat.SelectMove(3);
                        slashComponent.animate = false;
                    }
                }
            ).Run();
        }
    }
}