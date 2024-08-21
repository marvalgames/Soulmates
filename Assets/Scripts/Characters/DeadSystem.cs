using Sandbox.Player;
using Unity.Entities;
//using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collisions;

[System.Serializable]
public struct DeadComponent : IComponentData
{
    public bool isDead;

    public bool playDeadEffects;

    //public bool deadEffectsCompleted;
    public float deadEffectsFrames;
    public int tag;
    public bool checkLossCondition;
    public int effectsIndex;
}


[UpdateInGroup(typeof(SimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class DeadSystem : SystemBase //really game over system currently
{

    protected override void OnUpdate()
    {
        if (LevelManager.instance == null) return;

        var currentLevel = LevelManager.instance.currentLevelCompleted;
        int totalLevels = LevelManager.instance.totalLevels;
        if (currentLevel >= totalLevels) return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var enemiesAlive = new NativeArray<int>(1, Allocator.TempJob);
        var enemiesDead = new NativeArray<int>(1, Allocator.TempJob);
        var playersDead = new NativeArray<int>(1, Allocator.TempJob);

        playersDead[0] = LevelManager.instance.levelSettings[currentLevel].playersDead;

        var dep0 = Entities.ForEach
        (
            (in DeadComponent deadComponent, in Entity entity, in PlayerComponent playerComponent) =>
            {
                if (deadComponent.isDead) //player
                {
                    playersDead[0] += 1;
                }
            }
        ).Schedule(this.Dependency);
        dep0.Complete();
        LevelManager.instance.levelSettings[currentLevel].playersDead = playersDead[0];

        //int enemiesDead = LevelManager.instance.levelSettings[currentLevel].enemiesDead;


        var dep1 = Entities.WithAll<EnemyComponent>().ForEach
        (
            (in DeadComponent deadComponent,
                in Entity entity) =>
            {
                if (deadComponent.isDead)
                {
                    enemiesDead[0] += 1;
                    Debug.Log("basic dead system enemy");

                    if (SystemAPI.HasComponent<WinnerComponent>(entity))
                    {
                        var winnerComponent = SystemAPI.GetComponent<WinnerComponent>(entity);
                        if (winnerComponent
                            .checkWinCondition) //this  (and all with this true) enemy must be defeated to win the game
                        {
                            winnerComponent.endGameReached = true;
                            SystemAPI.SetComponent(entity, winnerComponent);
                            //Debug.Log("basic dead system enemy player wins");
                        }
                    }
                }
                else
                {
                    enemiesAlive[0] += 1;
                }
            }
        ).Schedule(this.Dependency);
        dep1.Complete();

        enemiesAlive.Dispose();
        enemiesDead.Dispose();
        playersDead.Dispose();

        //LevelManager.instance.levelSettings[currentLevel].enemiesDead = enemiesDead[0];
        //LevelManager.instance.allEnemiesDestroyed = enemiesAlive[0] == 0;
        //Debug.Log("ENEMIES DEAD " + enemiesDead);


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

[UpdateAfter(typeof(DeadSystem))]
public partial class PostDeadSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if(LevelManager.instance == null) return;
        if (LevelManager.instance.enemyDestroyed)
        {
            Entities.WithoutBurst().WithStructuralChanges().ForEach(
                (ref ShowMessageMenuComponent messageMenuComponent, in ShowMessageMenuGroup messageMenu) =>
                {
                    messageMenu.messageString = "... Destroyed ... ";
                    messageMenu.ShowMenu();
                    messageMenuComponent.show = false;
                    LevelManager.instance.enemyDestroyed = false;
                }
            ).Run();
        }

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var time = SystemAPI.Time.DeltaTime;

        Entities.WithoutBurst().ForEach(
            (HealthBar character, Entity e, ref DeadComponent deadComponent, ref LocalTransform localTransform) =>
            {
                if (deadComponent.isDead)
                {
                    if (deadComponent.deadEffectsFrames > 5.0)
                    {
                        character.gameObject.SetActive(false);
                        var transform = character.transform;
                        var pos = transform.position;
                        pos.y = -2000;
                        localTransform.Position.y = -2000;
                        transform.position = pos;

                        ecb.RemoveComponent(e, typeof(DeadComponent));
                        ecb.RemoveComponent(e, typeof(NpcMovementComponent));
                        //ecb.DestroyEntity(e);
                        
                    }
                    else
                    {
                        deadComponent.deadEffectsFrames += 1 * time;
                    }
                }
            }
        ).Run();



        Entities.WithoutBurst().WithStructuralChanges().ForEach(
            (ref TriggerComponent trigger, ref Entity triggerEntity) =>
            {
                var e = trigger.ParentEntity;
                if (SystemAPI.HasComponent<DeadComponent>(e))
                {
                    var dead = SystemAPI.GetComponent<DeadComponent>(e).isDead;
                    if (dead && e != triggerEntity)
                    {
                        EntityManager.DestroyEntity(triggerEntity);
                    }
                }

            }
        ).Run();




        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}