using System.Collections;
using System.Collections.Generic;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public partial struct EnemiesAttackEnableableComponentSystem : ISystem
{
    private EntityQuery playerQuery;


    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        var playerBuilder = new EntityQueryBuilder(Allocator.Temp);
        playerBuilder.WithAll<PlayerComponent, WeaponComponent>();
        playerQuery = state.GetEntityQuery(playerBuilder);
    }

    public void OnUpdate(ref SystemState system)
    {
        if (LevelManager.instance == null) return;
        //Debug.Log("Rev0");

        
        if (LevelManager.instance.endGame ||
            LevelManager.instance.currentLevelCompleted >= LevelManager.instance.totalLevels) return;
        //Debug.Log("Rev1");

        var playerEntityList = playerQuery.ToEntityArray(Allocator.TempJob);
        if (playerEntityList.Length == 0) return;
        var enemiesAttackComponentGroup = SystemAPI.GetComponentLookup<EnemiesAttackComponent>();
        var roleReversalMode = LevelManager.instance.levelSettings[LevelManager.instance.currentLevelCompleted]
            .roleReversalMode != RoleReversalMode.Off; //if toggle or on then true

        var roleReversal = SystemAPI.GetComponent<WeaponComponent>(playerEntityList[0]).roleReversal !=
                           RoleReversalMode.Off; //p1 shoots normal and enemies do not attack each other

        var reverseMode = roleReversal;
        if (roleReversalMode) //if no role reverse mechanic then reverseMode set skipped;
        {
            //Debug.Log("Rev2");

            var job = new EnemiesAttackEnableableJob()
            {
                enemiesAttackComponentGroup = enemiesAttackComponentGroup,
                reverseMode = true
            };

            job.Schedule();
        }
        
    }
}

partial struct EnemiesAttackEnableableJob : IJobEntity
{
    //[DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> playerEntityList;
    public ComponentLookup<EnemiesAttackComponent> enemiesAttackComponentGroup;
    public bool reverseMode;

    void Execute(Entity e, EnemyComponent enemyComponent)
    {
        if (enemiesAttackComponentGroup.HasComponent(e))
        {
            enemiesAttackComponentGroup.SetComponentEnabled(e, reverseMode);
            Debug.Log("Rev " + reverseMode);

        }
    }
}