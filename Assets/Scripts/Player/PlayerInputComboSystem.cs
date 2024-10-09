using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

namespace Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct PlayerInputComboSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actorAimComponent, inputControllerComponent) in SystemAPI
                         .Query<RefRW<ActorWeaponAimComponent>, RefRO<InputControllerComponent>>())
            {
                var rightStickPressed = inputControllerComponent.ValueRO.rightStickPressed;
                if (rightStickPressed)
                {
                    actorAimComponent.ValueRW.combatMode = !actorAimComponent.ValueRW.combatMode;
                    if (actorAimComponent.ValueRW.combatMode) actorAimComponent.ValueRW.aimMode = false;
                    //Debug.Log("Combat System " + actorAimComponent.combatMode);
                }
            }
        }
    }
}