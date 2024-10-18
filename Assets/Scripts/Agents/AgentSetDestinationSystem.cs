using ProjectDawn.Navigation;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Sandbox.Agents
{
// Sets agents destination
    partial struct AgentSetDestinationSystem : ISystem
    {
        public void OnUpdate(ref SystemState systemState)
        {
            foreach (var (enemyMovement,defensiveStrategy, matchup, locomotion, body) in SystemAPI
                         .Query<RefRO<EnemyMovementComponent>, RefRO<DefensiveStrategyComponent>, RefRO<MatchupComponent>, RefRW<AgentLocomotion>, RefRW<AgentBody>>())
            {
                //var botState = defensiveStrategy.ValueRO.botState;
                var match = matchup.ValueRO.closestOpponentEntity;
                if (!SystemAPI.HasComponent<LocalTransform>(match)) continue;
                var position = SystemAPI.GetComponent<LocalTransform>(match).Position;
                if (!enemyMovement.ValueRO.updateAgent)
                {
                    position = enemyMovement.ValueRO.agentNextPosition;
                }

                if (enemyMovement.ValueRO.forwardVelocity == 0)
                {
                    Debug.Log("forward velocity");
                    locomotion.ValueRW.Speed = 0;
                }
                else
                {
                    body.ValueRW.SetDestination(position);
                    
                }
                //locomotion.ValueRW.Speed = botState == BotState.STOP ? 0 : defensiveStrategy.ValueRO.botSpeed;
            }
        }
    }
}