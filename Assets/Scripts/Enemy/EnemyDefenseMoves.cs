using Collisions;
using Managers;
using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Enemy
{
    public partial struct EnemyDefenseMoves : ISystem
    {
        private EntityQuery playerQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            builder.WithAll<PlayerComponent>();
            playerQuery = state.GetEntityQuery(builder);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
            var checkedGroup = SystemAPI.GetComponentLookup<CheckedComponent>();
            var job = new EnemyDefenseMovesJob()
            {
                playerEntities = playerEntities,
                checkedGroup = checkedGroup
            };
            job.Schedule();
        }

       
    }

    //[WithAll(typeof(PlayerComponent))]
    [BurstCompile]
    partial struct EnemyDefenseMovesJob : IJobEntity
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> playerEntities;
        [ReadOnly] public ComponentLookup<CheckedComponent> checkedGroup;

        void Execute(Entity entity, ref AnimationManagerComponentData animationComponent)
        {
            //evade strike always
            //change to certain amount of frames (not just attack started) before turning off 
            var checkedComponentEnemy = checkedGroup[entity];
            for (int i = 0; i < playerEntities.Length; i++)
            {
                var playerEntity = playerEntities[i];
                if (checkedGroup.HasComponent(playerEntity))
                {
                    var checkedComponent = checkedGroup[playerEntity];
                    if (checkedComponent.attackFirstFrame && !checkedComponentEnemy.anyAttackStarted)
                    {
                        animationComponent.evadeStrike = true;
                    }
                    else if(!checkedComponent.anyAttackStarted)//not false until player move complete (Block)
                    {
                        animationComponent.evadeStrike = false;
                    }
                }
            }
        }
    }
    
    [UpdateAfter(typeof(EnemyDefenseMoves))]
    public partial struct UpdateCheckComponentSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var checkedComponent in SystemAPI.Query<RefRW<CheckedComponent>>())
            {
                if (checkedComponent.ValueRW.attackFirstFrame)
                {
                    //Debug.Log("FIRST FRAME " + checkedComponent.ValueRW.attackFirstFrame);
                    checkedComponent.ValueRW.attackFirstFrame = false;
                }
            }
        }
    }
    
    
}