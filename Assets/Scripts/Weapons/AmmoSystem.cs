using Collisions;
using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(ScoreSystem))]
[RequireMatchingQueriesForUpdate]
public partial struct AmmoSystem : ISystem
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
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var dt = SystemAPI.Time.fixedDeltaTime; //bullet duration
        var playerEntities = playerQuery.ToEntityArray(Allocator.Temp);
        if(playerEntities.Length == 0) return;
        var player = playerEntities[0];//P1



        var scoreGroup = SystemAPI.GetComponentLookup<ScoreComponent>(false);
        foreach (var (ammo, entity) in SystemAPI.Query<RefRW<AmmoComponent>>().WithEntityAccess())
        {
            ammo.ValueRW.AmmoTimeCounter += dt;
            if (ammo.ValueRW.AmmoTimeCounter > ammo.ValueRW.AmmoTime)
            {
                var trigger = SystemAPI.GetComponent<TriggerComponent>(entity);
                var shooter = trigger.ParentEntity;
                var isPlayer = SystemAPI.HasComponent<PlayerComponent>(shooter);
                var hasScore = SystemAPI.HasComponent<ScoreComponent>(shooter);
                
                if (hasScore && isPlayer)//always false for GMTK
                {
                    var score = scoreGroup[shooter];
                    score.zeroPoints = false;
                    if (score.lastShotConnected == false)
                    {
                        score.streak = 0;
                    }
                    score.lastShotConnected = false;
                    score.combo = 0;
                    scoreGroup[shooter] = score;
                    ammo.ValueRW.ammoHits = 0;
                }
                ecb.DestroyEntity(entity);
            }
            else // enemy
            {
                if (ammo.ValueRW.DamageCausedPreviously) ammo.ValueRW.frameSkipCounter = ammo.ValueRW.frameSkipCounter + 1;
                var trigger = SystemAPI.GetComponent<TriggerComponent>(entity);
                var shooter = trigger.ParentEntity;
                var hasScore = SystemAPI.HasComponent<ScoreComponent>(shooter);
                if (hasScore)
                {
                    var score = scoreGroup[shooter];
                    if (score.pointsScored && score.scoringAmmoEntity == entity)
                    {
                        ammo.ValueRW.ammoHits += 1;
                        //Debug.Log("ammo hits1 " + ammo.ammoHits);
                        score.combo = ammo.ValueRW.ammoHits;
                        ammo.ValueRW.AmmoTime += ammo.ValueRW.comboTimeAdd;
                    }
                    scoreGroup[shooter] = score;
                }
            }
        }


        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[RequireMatchingQueriesForUpdate]
public partial struct DefenseEvadeSystem : ISystem
{
    private EntityQuery enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<DefensiveStrategyComponent, EvadeComponent>();
        enemyQuery = state.GetEntityQuery(builder);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
        var transformGroup = SystemAPI.GetComponentLookup<LocalTransform>();
        var defensiveStrategyGroup = SystemAPI.GetComponentLookup<DefensiveStrategyComponent>();
        var enemiesAttackGroup = SystemAPI.GetComponentLookup<EnemiesAttackComponent>();

        var job = new DefenseEvadeJob()
        {
            defenseStrategyGroup = defensiveStrategyGroup,
            transformGroup = transformGroup,
            enemyEntities = enemyEntities,
            enemiesAttackGroup = enemiesAttackGroup
        };
        job.Schedule();
    }

    [BurstCompile]
    partial struct DefenseEvadeJob : IJobEntity
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> enemyEntities;
        [ReadOnly] public ComponentLookup<LocalTransform> transformGroup;
        public ComponentLookup<DefensiveStrategyComponent> defenseStrategyGroup;
        [ReadOnly] public ComponentLookup<EnemiesAttackComponent> enemiesAttackGroup;

        void Execute(in AmmoComponent ammoComponent, in LocalTransform enemyLocalTransform, in Entity ammoE)
        {
            for (var i = 0; i < enemyEntities.Length; i++)
            {
                var enemy = enemyEntities[i];
                bool teammate = ammoComponent.playerOwner == false;
                if (enemiesAttackGroup.HasComponent(enemy))
                {
                    if (enemiesAttackGroup.IsComponentEnabled(enemy))
                    {
                        teammate = false;
                    }
                }

                var triggerLocalTransform = transformGroup[enemy];
                var defensiveStrategy = defenseStrategyGroup[enemy];
                var distance = math.distance(triggerLocalTransform.Position, enemyLocalTransform.Position);
                if (teammate == false && distance < defensiveStrategy.breakRouteVisionDistance &&
                    defensiveStrategy.breakRoute == true &&
                    ammoE != defensiveStrategy.closeBulletEntity)
                {
                    //Debug.Log("EVADE SET");
                    defensiveStrategy.closeBulletEntity = ammoE;
                    defensiveStrategy.currentRole = DefensiveRoles.Chase;
                    defensiveStrategy.currentRole = DefensiveRoles.Evade;
                    defensiveStrategy.currentRoleTimer = 0;
                    defenseStrategyGroup[enemy] = defensiveStrategy;
                }
            }
        }
        
        
        
    }
}