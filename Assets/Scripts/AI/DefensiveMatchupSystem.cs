using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace AI
{
    [RequireMatchingQueriesForUpdate]
    public partial struct DefensiveMatchupSystem : ISystem
    {
        private EntityQuery playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var playerBuilder = new EntityQueryBuilder(Allocator.Temp);
            playerBuilder.WithAll<EnemiesAttackComponent>();
            playerQuery = state.GetEntityQuery(playerBuilder);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
            var enemiesGroup = SystemAPI.GetComponentLookup<EnemyComponent>();


            var job = new DefensiveMatchUpJob
            {
                PlayerEntities = playerEntities,
                TransformGroup = transformGroup,
                EnemiesGroup = enemiesGroup
                
            };

            job.Schedule();
        }

        [BurstCompile]
        partial struct DefensiveMatchUpJob : IJobEntity
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> PlayerEntities;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformGroup;
            [ReadOnly] public ComponentLookup<EnemyComponent> EnemiesGroup;

            void Execute(Entity enemyE, ref DefensiveStrategyComponent defensiveStrategyComponent)
            {
                var closestDistance = math.INFINITY;
                var closestPlayer = Entity.Null;
                var players = PlayerEntities.Length;

                for (var i = 0; i < players; i++)
                {
                    var playerE = PlayerEntities[i];
                    if (TransformGroup.HasComponent(playerE) && TransformGroup.HasComponent(enemyE) && playerE != enemyE)
                    {
                        var playerTransform = TransformGroup[playerE];
                        var enemyTransform = TransformGroup[enemyE];
                        var distance = math.distance(playerTransform.Position, enemyTransform.Position);
                        if (EnemiesGroup.HasComponent(enemyE) && EnemiesGroup.HasComponent(playerE))
                        {
                            distance *= defensiveStrategyComponent.switchToPlayerMultiplier;

                        }
                        if (distance < closestDistance)
                        {
                            closestPlayer = playerE;
                            closestDistance = distance;
                        }
                    }
                }

                //Debug.Log("DEF MATCH " + closestPlayer);

                defensiveStrategyComponent.closestEnemiesAttackEntity = closestPlayer;
            }
        }
    }






    public partial struct ClosestPlayerMatchupSystem : ISystem
    {
        private EntityQuery playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var playerBuilder = new EntityQueryBuilder(Allocator.Temp);
            playerBuilder.WithAll<PlayerComponent>();
            playerQuery = state.GetEntityQuery(playerBuilder);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();

            var job = new ClosestPlayerMatchUpJob
            {
                PlayerEntities = playerEntities,
                TransformGroup = transformGroup
            };

            job.Schedule();
        }

        [BurstCompile]
        partial struct ClosestPlayerMatchUpJob : IJobEntity
        {
            [ReadOnly][DeallocateOnJobCompletion] public NativeArray<Entity> PlayerEntities;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformGroup;

            void Execute(Entity enemyE, ref MatchupComponent matchupComponent)
            {
                var closestDistance = math.INFINITY;
                var closestPlayer = Entity.Null;
                var players = PlayerEntities.Length;
                for (var i = 0; i < players; i++)
                {
                    var playerE = PlayerEntities[i];
                    if (TransformGroup.HasComponent(playerE) && TransformGroup.HasComponent(enemyE) && playerE != enemyE)
                    {
                        var playerTransform = TransformGroup[playerE];
                        var enemyTransform = TransformGroup[enemyE];
                        var distance = math.distance(playerTransform.Position, enemyTransform.Position);
                        if (distance < closestDistance)
                        {
                            closestPlayer = playerE;
                            closestDistance = distance;
                        }
                    }
                }
                matchupComponent.closestPlayerEntity = closestPlayer;
            }
        }
    }








}