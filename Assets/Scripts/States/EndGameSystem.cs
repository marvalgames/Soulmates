using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DeadSystem))]
public partial class BasicWinnerSystem : SystemBase
{

    protected override void OnUpdate()
    {
        if (LevelManager.instance == null) return;

        var currentLevelCompleted = LevelManager.instance.currentLevelCompleted;
        var totalGameLevels = LevelManager.instance.totalLevels;
        
        var query = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>());
        var enemyEntities = query.ToEntityArray(Allocator.TempJob);
        if (enemyEntities.Length < 1)
        {
            enemyEntities.Dispose();
            return;
        }
      
        var winner = true;
        //Debug.Log("WINNER");

        Entities.WithAll<EnemyComponent>().WithoutBurst().ForEach
        (
            (in Entity e, in DeadComponent deadComponent) =>
            {
                if (winner == false) return;
                var okAlive = false;
                if(SystemAPI.HasComponent<LevelCompleteComponent>(e))
                {
                    var levelComponent = SystemAPI.GetComponent<LevelCompleteComponent>(e);
                    okAlive = levelComponent.areaIndex > LevelManager.instance.totalLevels;
                }
                
                if(!deadComponent.isDead && okAlive == false)
                {
                    winner = false;
                }
  
            }
        ).Run();

        //winner = false;//REMEMBER
        enemyEntities.Dispose();

        if (currentLevelCompleted >= totalGameLevels) winner = true;
        if (winner == false) return;
        Debug.Log("basic winner system");
        LevelManager.instance.endGame = true;
        LevelManager.instance.gameResult = GameResult.Winner;
        
        

    }
}




//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BasicWinnerSystem))]


public partial class BasicLoserSystem : SystemBase
{

    protected override void OnUpdate()
    {
        if(LevelManager.instance == null) return;

        if (LevelManager.instance.gameResult == GameResult.Loser) return;


        var loser = false;


        Entities.WithAll<PlayerComponent>().WithStructuralChanges().WithoutBurst().ForEach
        (
            (Entity e, in DeadComponent dead) =>
            {
                if (dead.isDead == true)
                {
                    loser = true;
                    EntityManager.RemoveComponent<PlayerComponent>(e);
                    EntityManager.RemoveComponent<PhysicsVelocity>(e);
                    Debug.Log("basic loser system");
                    //EntityManager.RemoveComponent<InputControllerComponent>(e);

                }
            }
        ).Run();

   
        if (loser == false) return;

        LevelManager.instance.endGame = true;
        LevelManager.instance.gameResult = GameResult.Loser;

    }
}


//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BasicLoserSystem))]
[RequireMatchingQueriesForUpdate]

public partial class EndGameSystem : SystemBase
{

   

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        if(LevelManager.instance == null) return;

        if (LevelManager.instance.gameResult == GameResult.Winner || LevelManager.instance.gameResult == GameResult.Loser)
        {
            var win = (LevelManager.instance.gameResult == GameResult.Winner);
            var dep = Entities.WithAny<PlayerComponent, EnemyComponent>().ForEach
            ((in Entity e) =>
                {

                    var velocity = SystemAPI.GetComponent<PhysicsVelocity>(e);
                    if (SystemAPI.HasComponent<PhysicsVelocity>(e) && win == true &&
                        SystemAPI.HasComponent<EnemyComponent>(e))
                    {
                        velocity.Linear = new float3(0, 0, 0);
                        ecb.RemoveComponent<EnemyComponent>(e);
                    }
                    else
                    {
                        velocity.Linear = new float3(0, 0, 0);
                    }

                    if (SystemAPI.HasComponent<Pause>(e))
                    {
                        ecb.RemoveComponent<Pause>(e);
                    }

                    ecb.SetComponent(e, velocity);

                }
            ).Schedule(this.Dependency);


            dep.Complete();
        }
        

        ecb.Playback(EntityManager);
        ecb.Dispose();


    }
}
