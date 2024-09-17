using ProjectDawn.Navigation;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Sandbox.Agents
{
// Sets agents destination
    partial struct AgentSetDestinationSystem : ISystem
    {
        public void OnUpdate(ref SystemState systemState)
        {
            foreach (var (defensiveStrategy, matchup, locomotion, body) in SystemAPI
                         .Query<RefRO<DefensiveStrategyComponent>, RefRO<MatchupComponent>, RefRW<AgentLocomotion>, RefRW<AgentBody>>())
            {
                //var botState = defensiveStrategy.ValueRO.botState;
                var match = matchup.ValueRO.closestOpponentEntity;
                if (!SystemAPI.HasComponent<LocalTransform>(match)) continue;
                var position = SystemAPI.GetComponent<LocalTransform>(match).Position;
                body.ValueRW.SetDestination(position);
                //locomotion.ValueRW.Speed = botState == BotState.STOP ? 0 : defensiveStrategy.ValueRO.botSpeed;
            }
        }
    }
}