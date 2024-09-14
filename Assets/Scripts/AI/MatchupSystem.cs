using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace AI
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(MatchupSystem))]
    public partial struct MatchupTargetZonesSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var meleeGroup = SystemAPI.GetComponentLookup<MeleeComponent>();
            var targetGroup = SystemAPI.GetComponentLookup<TargetZoneComponent>();
            var job = new MatchupTargetZoneSystemJob()
            {
                meleeGroup = meleeGroup,
                targetGroup = targetGroup,
            };
            job.Schedule();
        }
    }


    [BurstCompile]
    partial struct MatchupTargetZoneSystemJob : IJobEntity
    {
        public ComponentLookup<MeleeComponent> meleeGroup;

        [ReadOnly] public ComponentLookup<TargetZoneComponent> targetGroup;

        void Execute(PlayerComponent playerComponent, DeadComponent deadComponent, Entity player,
            MatchupComponent matchComponent)
        {
            var closestEnemy = matchComponent.closestOpponent;
            matchComponent.validTarget = false;
            if (closestEnemy != Entity.Null && targetGroup.HasComponent(closestEnemy))
            {
                matchComponent.validTarget = true;
                var targetZone = targetGroup[closestEnemy];
                if (meleeGroup.HasComponent(player) &&
                    matchComponent.closestDistance < matchComponent.lookAtDistance)
                {
                    matchComponent.lookAt = true;
                    var melee = meleeGroup[player];
                    melee.target =
                        targetZone.headZonePosition;
                    meleeGroup[player] = melee;
                }
                else
                {
                    matchComponent.lookAt = false;
                }
            }
        }
    }


    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    partial struct MatchupSystem : ISystem
    {
        private EntityQuery enemyQuery;
        private EntityQuery enemiesAttackQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var enemyBuilder = new EntityQueryBuilder(Allocator.Temp);
            enemyBuilder.WithAll<EnemyComponent>();
            enemyQuery = state.GetEntityQuery(enemyBuilder);
            var attacksBuilder = new EntityQueryBuilder(Allocator.Temp);
            attacksBuilder.WithAny<EnemiesAttackComponent, PlayerComponent>();
            enemiesAttackQuery = state.GetEntityQuery(attacksBuilder);
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemyEntityList = enemyQuery.ToEntityArray(Allocator.TempJob);
            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
            var targetZonesGroup = SystemAPI.GetComponentLookup<TargetZoneComponent>();
            var playersGroup = SystemAPI.GetComponentLookup<PlayerComponent>();
            var enemyCount = enemyEntityList.Length;
            var enemiesAttackEntityList = enemiesAttackQuery.ToEntityArray(Allocator.TempJob);
            enemyEntityList.Dispose();
            if (enemyCount == 0)
            {
                return;
            }

            var matchupSystemJob = new MatchupSystemJob()
            {
                transformGroup = transformGroup,
                targetZonesGroup = targetZonesGroup,
                enemiesAttackEntityList = enemiesAttackEntityList,
                playersGroup = playersGroup
            };
            matchupSystemJob.ScheduleParallel();
        }
    }


    [BurstCompile]
    partial struct MatchupSystemJob : IJobEntity
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> enemiesAttackEntityList;
        [ReadOnly] public ComponentLookup<LocalTransform> transformGroup;
        [ReadOnly] public ComponentLookup<TargetZoneComponent> targetZonesGroup;
        [ReadOnly] public ComponentLookup<PlayerComponent> playersGroup;


        void Execute(Entity enemyEntity, DeadComponent deadComponent,
            EnemyComponent enemyComponent,
            in EnemyStateComponent enemyStateComponent,
            in DefensiveStrategyComponent defensiveStrategyComponent,
            ref MatchupComponent matchup)
        {
            if (!transformGroup.HasComponent(enemyEntity)) return;
            var enemyPosition = transformGroup[enemyEntity].Position;
            var enemyRotation = transformGroup[enemyEntity].Rotation;
            var closestDistance = math.INFINITY;
            var closestPlayerEntity = Entity.Null;

            for (var j = 0; j < enemiesAttackEntityList.Length; j++)
            {

                if (enemiesAttackEntityList[j] == enemyEntity || deadComponent.isDead) continue;
                var playerEntity = enemiesAttackEntityList[j];
                if (transformGroup.HasComponent(playerEntity))
                {

                    var playerPosition = transformGroup[playerEntity].Position;
                    var distance = math.distance(playerPosition, enemyPosition);
                    var bothEnemies = !playersGroup.HasComponent(playerEntity) &&
                                      !playersGroup.HasComponent(enemyEntity);
                    if (bothEnemies)
                    {
                        distance *= defensiveStrategyComponent.switchToPlayerMultiplier;
                    }

                    var forwardVector = math.forward(enemyRotation);
                    var vectorToPlayer = playerPosition - enemyPosition;
                    var unitVecToPlayer = math.normalize(vectorToPlayer);
                    var angleRadians = math.INFINITY;
                    var viewDistanceSq = math.INFINITY;
                    var dot = 1.0;
                    var view360 = true;

                    if (defensiveStrategyComponent.currentRole ==
                        DefensiveRoles.None)
                    {
                        angleRadians = matchup.AngleRadians;
                        viewDistanceSq = matchup.ViewDistanceSQ;
                        dot = math.dot(forwardVector, unitVecToPlayer);
                        view360 = matchup.View360 ||
                                  enemyStateComponent.MoveState ==
                                  MoveStates.Chase ||
                                  enemyStateComponent.MoveState ==
                                  MoveStates.Default;
                    }

                    var canSeePlayer = (dot > 0.0f || view360) && // player is in front of us
                                       math.degrees(math.abs(math.acos(dot))) <
                                       angleRadians && // player is within the cone angle bounds
                                       math.length(vectorToPlayer) <
                                       viewDistanceSq; // player is within vision distance (we use Squared Distance to avoid sqrt calculation)
                    
                    
                    Debug.Log("Matchup 1 " + targetZonesGroup.HasComponent(playerEntity));


                    if (distance < closestDistance && canSeePlayer &&
                        targetZonesGroup.HasComponent(playerEntity))
                    {

                        Debug.Log("Matchup 2");

                        closestPlayerEntity = playerEntity;
                        closestDistance = distance;
                    }
                }

                matchup.closestOpponent = closestPlayerEntity;
            }
            
            var closestPlayer = matchup.closestOpponent;
            matchup.validTarget = false;
            if (closestPlayer != Entity.Null)
            {
                matchup.validTarget = true;
                matchup.targetEntity = closestPlayer;
                matchup.isWaypointTarget = false;
            }
            else //no valid player targets
            {
                matchup.targetZone = Vector3.zero;
                matchup.isWaypointTarget = true; //NEED? 
            }
        }
    }
}