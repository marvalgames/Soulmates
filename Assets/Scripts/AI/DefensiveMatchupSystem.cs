// using Sandbox.Player;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
//
// namespace AI
// {
// //this is same as above but without enemiesAttack (enemy vs enemy) may be deprecated
//     [RequireMatchingQueriesForUpdate]
//     public partial struct ClosestPlayerMatchupSystem : ISystem
//     {
//         private EntityQuery playerQuery;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             var playerBuilder = new EntityQueryBuilder(Allocator.Temp);
//             playerBuilder.WithAll<PlayerComponent>();
//             playerQuery = state.GetEntityQuery(playerBuilder);
//         }
//
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
//             var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
//
//             var job = new ClosestPlayerMatchUpJob()
//             {
//                 PlayerEntities = playerEntities,
//                 TransformGroup = transformGroup
//             };
//
//             job.Schedule();
//         }
//
//         [BurstCompile]
//         partial struct ClosestPlayerMatchUpJob : IJobEntity
//         {
//             [ReadOnly][DeallocateOnJobCompletion] public NativeArray<Entity> PlayerEntities;
//             [ReadOnly] public ComponentLookup<LocalTransform> TransformGroup;
//
//             void Execute(Entity enemyE, ref MatchupComponent matchupComponent)
//             {
//                 var closestDistance = math.INFINITY;
//                 var closestPlayer = Entity.Null;
//                 var players = PlayerEntities.Length;
//                 for (var i = 0; i < players; i++)
//                 {
//                     var playerE = PlayerEntities[i];
//                     if (TransformGroup.HasComponent(playerE) && TransformGroup.HasComponent(enemyE) && playerE != enemyE)
//                     {
//                         var playerTransform = TransformGroup[playerE];
//                         var enemyTransform = TransformGroup[enemyE];
//                         var distance = math.distance(playerTransform.Position, enemyTransform.Position);
//                         if (distance < closestDistance)
//                         {
//                             closestPlayer = playerE;
//                             closestDistance = distance;
//                         }
//                     }
//                 }
//                 matchupComponent.closestPlayerEntity = closestPlayer;
//             }
//         }
//     }
//
//
//
//
//
//
//
//
// }