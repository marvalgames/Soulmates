using ProjectDawn.Navigation;
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
            foreach (var (defensiveStrategy, locomotion, body) in SystemAPI
                         .Query<RefRO<DefensiveStrategyComponent>, RefRW<AgentLocomotion>, RefRW<AgentBody>>())
            {
                var match = defensiveStrategy.ValueRO.closestEnemiesAttackEntity;
                if (!SystemAPI.HasComponent<LocalTransform>(match)) continue;
                var position = SystemAPI.GetComponent<LocalTransform>(match).Position;
                body.ValueRW.SetDestination(position);
                locomotion.ValueRW.Speed = defensiveStrategy.ValueRO.botSpeed;
            }
        }
    }
}