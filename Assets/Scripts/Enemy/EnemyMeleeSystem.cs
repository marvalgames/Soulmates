using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Enemy
{
    //[UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyMeleeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (enemyAttackComponent, checkedComponent, 
                         matchupComponent,
                         enemyLocalTransform, entity)
                     in SystemAPI
                         .Query<RefRW<EnemyStateComponent>, RefRW<CheckedComponent>, RefRO<MatchupComponent>, RefRO<LocalTransform>>()
                         .WithEntityAccess().WithAll<EnemyComponent, MeleeComponent, DeadComponent>())
            {
                var enemyPosition = enemyLocalTransform.ValueRO.Position;
                var playerPosition = matchupComponent.ValueRO.wayPointTargetPosition;
                var dist = math.distance(playerPosition, enemyPosition);
                if (enemyAttackComponent.ValueRW.selectMove)
                {
                    enemyAttackComponent.ValueRW.selectMoveUsing = true;
                }
                else if (checkedComponent.ValueRW.AttackStages == AttackStages.End)
                {
                    if (!checkedComponent.ValueRW
                            .anyAttackStarted) //AttackStarted set from animation - false at end so reset
                    {
                        checkedComponent.ValueRW.totalAttempts += 1; //totalHits in AttackerSystem
                        checkedComponent.ValueRW.hitReceived = false;
                        checkedComponent.ValueRW.anyAttackStarted = false;
                        checkedComponent.ValueRW.AttackStages = AttackStages.No;
                    }
                }
            }
        }
    }
    
    [UpdateAfter(typeof(EnemyMeleeSystem))]
    [RequireMatchingQueriesForUpdate]
    public partial class MeleeManagedSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithoutBurst().ForEach((ActorInstance actorInstance, in EnemyStateComponent enemyStateComponent )=>
            {
                var enemyMelee = actorInstance.actorPrefabInstance.GetComponent<EnemyMelee>();
                if (enemyStateComponent.selectMoveUsing)
                {
                    enemyMelee.SelectMoveUsing();
                    // if (entityManager.HasComponent<CheckedComponent>(meleeEntity))
                    // {
                    //     var defense = animationIndex == (int)AnimationType.Deflect;
                    //     var checkedComponent = entityManager.GetComponentData<CheckedComponent>(meleeEntity);
                    //     checkedComponent.anyDefenseStarted = defense;
                    //     checkedComponent.primaryTrigger = primaryTrigger;
                    //     checkedComponent.animationIndex = animationIndex;
                    //     entityManager.SetComponentData(meleeEntity, checkedComponent);
                    //     StartMove(animationIndex);
                    // }
                    
                    
                }
                
            }).Run();
        }
    }
}