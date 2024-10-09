using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;

namespace Mechanics
{
    [RequireMatchingQueriesForUpdate]
    public partial struct SlashSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (melee, slashComponent, input) in SystemAPI
                         .Query<RefRW<MeleeComponent>, RefRW<SlashComponent>, RefRO<InputControllerComponent>>()
                         .WithAll<PlayerComponent>())
            {
                if (slashComponent.ValueRW.slashActive == false) return;
                if (input.ValueRO.buttonY_Tap || input.ValueRO.buttonY_Press)
                {
                    melee.ValueRW.selectMove = 3;
                }
            }
        }
    }
}