using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;

namespace Enemy
{
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemyActorMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new EnemyActorMovementJob
            {
            };

            job.Schedule();
        }


        [BurstCompile]
        partial struct EnemyActorMovementJob : IJobEntity
        {
            void Execute(Entity e, ref DefensiveStrategyComponent matchup, in EnemyBehaviourComponent enemyBehaviour)
            {
                matchup.botState = BotState.MOVING;
                var closestPlayer = matchup.closestEnemiesAttackEntity;
                var distanceToPlayer = matchup.distanceToOpponent;
                var stopRange = enemyBehaviour.stopRange;
                if (distanceToPlayer < stopRange)
                {
                    matchup.botState = BotState.STOP;
                }



            }
            
            
        }
    }
}