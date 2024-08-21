using System.CodeDom.Compiler;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;


namespace Enemy
{
    [RequireMatchingQueriesForUpdate]
    public partial struct EnemyBehaviorSystem : ISystem
    {
        private Random _random;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _random = new Random();
            _random.InitState(10);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (
                         enemyBasicMovementComponent,
                         enemyWeaponMovementComponent,
                         enemyMeleeMovementComponent,
                         dead
                         ) in
                     SystemAPI
                         .Query<
                             RefRW<EnemyMovementComponent>,
                             RefRW<EnemyWeaponMovementComponent>,
                             RefRW<EnemyMeleeMovementComponent>,
                             RefRO<DeadComponent>
                         >())
            {
                if (!dead.ValueRO.isDead && enemyMeleeMovementComponent.ValueRW.switchUp)
                {
                    if (enemyMeleeMovementComponent.ValueRW.switchUpTimer
                        <= enemyMeleeMovementComponent.ValueRW.currentSwitchUpTime)
                    {
                        enemyMeleeMovementComponent.ValueRW.switchUpTimer += SystemAPI.Time.DeltaTime;
                        //melee timer used for melee AND weapon
                    }
                    else
                    {
                        enemyMeleeMovementComponent.ValueRW.currentSwitchUpTime =
                            _random.NextFloat(enemyMeleeMovementComponent.ValueRW.originalSwitchUpTime * .5f,
                                enemyMeleeMovementComponent.ValueRW.originalSwitchUpTime * 1.5f);
                        
                        enemyMeleeMovementComponent.ValueRW.switchUpTimer = 0;
                        //ignore or turn off basic movement for this
                        enemyBasicMovementComponent.ValueRW.enabled = false;
                        enemyMeleeMovementComponent.ValueRW.enabled = !enemyMeleeMovementComponent.ValueRW.enabled;
                        enemyWeaponMovementComponent.ValueRW.enabled = !enemyWeaponMovementComponent.ValueRW.enabled;
                        
                    }

                }
            }
        }
    }
}