using Sandbox.Player;
using Unity.Entities;

namespace Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]

    public partial class PlayerInputComboSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref ActorWeaponAimComponent actorAimComponent,
                in InputControllerComponent inputControllerComponent) =>
            {
                var rightStickPressed = inputControllerComponent.rightStickPressed;
                if (rightStickPressed)
                {
                    actorAimComponent.combatMode = !actorAimComponent.combatMode;
                    if (actorAimComponent.combatMode) actorAimComponent.aimMode = false;
                    //Debug.Log("Combat System " + actorAimComponent.combatMode);
                }
            }).Run();
        }
    }
}