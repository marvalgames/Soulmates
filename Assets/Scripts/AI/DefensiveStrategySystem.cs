using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
public partial struct DefensiveStrategySystem : ISystem
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
        var players = playerEntities.Length;
        foreach (var (defensiveStrategyComponent, enemyE) in SystemAPI.Query<RefRW<DefensiveStrategyComponent>>()
                     .WithAny<EnemyComponent>().WithEntityAccess())
        {
            for (var i = 0; i < players; i++)
            {
                if (defensiveStrategyComponent.ValueRW.currentRole == DefensiveRoles.Chase)
                {
                    if (defensiveStrategyComponent.ValueRW.currentRoleTimer <
                        defensiveStrategyComponent.ValueRW.currentRoleMaxTime)
                    {
                        defensiveStrategyComponent.ValueRW.currentRoleTimer += SystemAPI.Time.DeltaTime;
                    }
                    else
                    {
                        defensiveStrategyComponent.ValueRW.currentRole = DefensiveRoles.None;
                        defensiveStrategyComponent.ValueRW.currentRoleTimer = 0;
                    }
                }
            }
        }

        playerEntities.Dispose();

    }
}