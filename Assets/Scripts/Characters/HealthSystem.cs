using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class LosingHealthSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        Entities.WithoutBurst().ForEach((
                ref HealthComponent healthComponent, in PlayerComponent playerComponent,
                in Entity entity) =>
            {
                if (healthComponent.losingHealth)
                {
                    var damage = healthComponent.losingHealthRate * SystemAPI.Time.DeltaTime;
                    ecb.AddComponent(entity, new DamageComponent { DamageReceived = damage, LosingDamage = true });
                }
            }
        ).Run();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class HealthSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecbSystem;
    private static readonly int Dead = Animator.StringToHash("Dead");


    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var anyEnemyDamaged = false;
        var anyPlayerDamaged = false;
        float allPlayerDamageTotal = 0;

        Entities.WithoutBurst().ForEach((
                //Animator animator,
                ref DeadComponent deadComponent,
                ref HealthComponent healthComponent, ref DamageComponent damageComponent,
                ref RatingsComponent ratingsComponent,
                in Entity entity) =>
            {
                var entityCausingDamage = damageComponent.EntityCausingDamage;
                healthComponent.showDamage = false;
                if (EntityManager.HasComponent(entity, typeof(EnemyComponent)))
                {
                    if (SystemAPI.GetComponent<EnemyComponent>(entity).invincible)
                    {
                        damageComponent.DamageReceived = 0;
                    }
                }

                if (damageComponent.DamageReceived > healthComponent.showDamageMin)
                {
                    healthComponent.showDamage = true;
                }

                Debug.Log("Damaged " + damageComponent.DamageReceived);

                healthComponent.totalDamageReceived += damageComponent.DamageReceived;


                allPlayerDamageTotal = allPlayerDamageTotal + healthComponent.totalDamageReceived;
                var dead = EntityManager.GetComponentData<DeadComponent>(entity);
                if (damageComponent.DamageReceived > 0)
                {
                    if (SystemAPI.HasComponent<EnemyComponent>(entity))
                    {
                        anyEnemyDamaged = true;
                    }
                    else if (SystemAPI.HasComponent<PlayerComponent>(entity))
                    {
                        anyPlayerDamaged = true;
                    }
                }

                if (healthComponent.totalDamageReceived >= ratingsComponent.maxHealth && dead.isDead == false)
                {
                    if (SystemAPI.HasComponent<LevelCompleteComponent>(entity))
                    {
                        var levelCompleteComponent = SystemAPI.GetComponent<LevelCompleteComponent>(entity);
                        levelCompleteComponent.dieLevel = LevelManager.instance.currentLevelCompleted;
                        SystemAPI.SetComponent(entity, levelCompleteComponent);
                    }

                    LevelManager.instance.levelSettings[LevelManager.instance.currentLevelCompleted].enemiesDead += 1;
                    ;
                    dead.isDead = true;
                    dead.playDeadEffects = true;
                    //animator.speed = 1;
                    LevelManager.instance.enemyDestroyed = true;
                    Debug.Log("Destroyed " + LevelManager.instance.enemyDestroyed);
                    var isEnemy = SystemAPI.HasComponent<EnemyComponent>(entity);
                    var isPlayer = SystemAPI.HasComponent<PlayerComponent>(entity);
                
                    if (SystemAPI.HasComponent<AmmoComponent>(entityCausingDamage))
                    {
                        var ammo = SystemAPI.GetComponent<AmmoComponent>(entityCausingDamage);
                        dead.effectsIndex = ammo.deathBlowEffectsIndex;
                    }
                    // else if (SystemAPI.HasComponent<VisualEffectEntityComponent>(entityCausingDamage))
                    // {
                    //     var ve = SystemAPI.GetComponent<VisualEffectEntityComponent>(entityCausingDamage);
                    //     dead.effectsIndex = ve.deathBlowEffectsIndex;
                    // }

                    ecb.SetComponent(entity, dead);
                }
            }
        ).Run();


        if (anyEnemyDamaged == false && anyPlayerDamaged == false) return;


        Entities.WithoutBurst().ForEach((ref HealthComponent healthComponent) =>
            {
                if (healthComponent.combineDamage)
                {
                    healthComponent.totalDamageReceived = allPlayerDamageTotal;
                }
            }
        ).Run();


        Entities.WithoutBurst().ForEach(
            (HealthBar healthUI, in HealthComponent healthComponent, in DamageComponent damage) =>
            {
                if (healthComponent.showText3D == ShowText3D.HitScore && healthComponent.showDamage)
                {
                    healthUI.ShowText3dValue((int)damage.ScorePointsReceived);
                }
                else if (healthComponent.showText3D == ShowText3D.HitDamage && healthComponent.showDamage)
                {
                    healthUI.ShowText3dValue((int)damage.DamageReceived);
                }

                healthUI.HealthChange();
            }).Run();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateInGroup(typeof(TransformSystemGroup))]
public partial struct HealthBarManagedSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (actor, guiClass, healthBarComponent, entity)
                 in SystemAPI.Query<ActorInstance, HealthBarClass, RefRW<HealthBarComponent>>().WithEntityAccess())
        {
            if (!healthBarComponent.ValueRW.instantiated)
            {
                var go = GameObject.Instantiate(guiClass.actorGuiPrefab);
                if (SystemAPI.HasComponent<EnemyComponent>(entity))
                {
                    //enemy gets parent
                    go.transform.parent = actor.actorPrefabInstance.transform;
                    go.transform.localPosition = new Vector3(0, 3.5f, 0);
                }
                    
                
                commandBuffer.AddComponent(entity,
                  new HealthBarInstance {actorGuiInstance = go});
                healthBarComponent.ValueRW.instantiated = true;
            }
        }

        foreach (var (actorGui, health, healthBar, entity) in SystemAPI
                     .Query<HealthBarInstance, RefRO<HealthComponent>, RefRO<HealthBarComponent>>()
                     .WithEntityAccess())
        {
            var img = actorGui.actorGuiInstance.GetComponent<HealthBar>()._healthBar;
            var maxHealth = 100f;
            if (SystemAPI.HasComponent<RatingsComponent>(entity))
            {
                maxHealth = SystemAPI.GetComponent<RatingsComponent>(entity).maxHealth;
            }

            var damage = health.ValueRO.totalDamageReceived;
            var pct = 1.00f - (damage / maxHealth);
            if (pct < 0)
            {
                pct = 0;
            }
            img.rectTransform.localScale = new Vector3(pct, 1, 1);
        }
    }
}