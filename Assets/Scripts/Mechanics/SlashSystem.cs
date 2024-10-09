using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Mechanics
{
    [RequireMatchingQueriesForUpdate]
    public partial struct SlashSystemManaged : ISystem
    {
        private static readonly int SlashState = Animator.StringToHash("SlashState");
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (actor, slash, melee) in SystemAPI.Query<ActorInstance, RefRW<SlashComponent>, RefRW<MeleeComponent>>().WithAll<PlayerComponent>())
            {
                var animator = actor.actorPrefabInstance.GetComponent<Animator>();
                if (slash.ValueRW.slashActive == false) continue;

                if (slash.ValueRW.animate)
                {
                    melee.ValueRW.selectMove = 3;
                    //playerCombat.SelectMove(3);
                    slash.ValueRW.animate = false;
                }
            }
        }
    }
    
    [RequireMatchingQueriesForUpdate]
    public partial struct SlashSystem : ISystem 
    {

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (slashComponent, input) in SystemAPI.Query<RefRW<SlashComponent>, RefRO<InputControllerComponent>>().WithAll<PlayerComponent>())
            {
                if (slashComponent.ValueRW.slashActive == false) return;
                slashComponent.ValueRW.slashState = (int)SlashStates.None;
                //if ((input.buttonX_Pressed == true) && slashComponent.slashState == (int)SlashStates.None)//why are triggers backward? // LD50
                if ((input.ValueRO.buttonY_Tap || input.ValueRO.buttonY_Press) &&
                    slashComponent.ValueRW.slashState == (int)SlashStates.None)
                {
                    slashComponent.ValueRW.slashState = (int)SlashStates.Started;
                    if (slashComponent.ValueRW.animate == false)
                    {
                        slashComponent.ValueRW.animate = true;
                    }
                }
                
            }
            
        }
    }
    
}