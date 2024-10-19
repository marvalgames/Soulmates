﻿using AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(MatchupSystem))]
public partial struct ClosestEnemyMatchupSystem : ISystem
    {
        private EntityQuery enemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var enemyBuilder = new EntityQueryBuilder(Allocator.Temp);
            enemyBuilder.WithAll<EnemyComponent>();
            enemyQuery = state.GetEntityQuery(enemyBuilder);
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
            var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();

            var job = new ClosestEnemyMatchUpJob
            {
                EnemyEntities = enemyEntities,
                TransformGroup = transformGroup
            };

            job.Schedule();
        }

        [BurstCompile]
        partial struct ClosestEnemyMatchUpJob : IJobEntity
        {
            [ReadOnly][DeallocateOnJobCompletion] public NativeArray<Entity> EnemyEntities;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformGroup;

            void Execute(Entity playerE, ref MatchupComponent matchupComponent)
            {
                var closestDistance = math.INFINITY;
                var closestEnemy = Entity.Null;
                var enemies = EnemyEntities.Length;
                for (var i = 0; i < enemies; i++)
                {
                    var enemyE = EnemyEntities[i];
                    if (TransformGroup.HasComponent(enemyE) && TransformGroup.HasComponent(playerE) && enemyE != playerE)
                    {
                        var playerTransform = TransformGroup[playerE];
                        var enemyTransform = TransformGroup[enemyE];
                        var distance = math.distance(playerTransform.Position, enemyTransform.Position);
                        if (distance < closestDistance)
                        {
                            closestEnemy = enemyE;
                            closestDistance = distance;
                        }
                    }
                }
                matchupComponent.closestEnemyEntity = closestEnemy;
                matchupComponent.closestDistance = closestDistance;

            }
        }
    }



