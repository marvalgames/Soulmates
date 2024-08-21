using Sandbox.Player;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;


[System.Serializable]
public struct LevelCompleteComponent : IComponentData
{
    public bool active;
    public bool targetReached;
    public bool checkWinCondition;
    public int dieLevel;
    public int areaIndex;
}

//[UpdateAfter(typeof(WinnerSystems))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class LevelCompleteSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (LevelManager.instance == null) return;


        var currentLevelCompleted = LevelManager.instance.currentLevelCompleted;
        var totalGameLevels = LevelManager.instance.totalLevels;
     
        if (currentLevelCompleted >= totalGameLevels)
            return; //all levels complete before even checking if level complete below than return since game over basically


        var query = GetEntityQuery(typeof(LevelCompleteComponent), typeof(PlayerComponent));
        var requests =
            query.ToEntityArray(Allocator.Temp);


        var levelCompleteCounter = 0; //# reached end that are required to complete the level
        var levelComplete = false;
        var message = "Level Complete";
        if (requests.Length == 0)
        {
            return;
        }

        var levelManager = LevelManager.instance.levelSettings[currentLevelCompleted];
        if (levelManager.levelCompleteScenario ==
            LevelCompleteScenario.TriggerReached)
        {
            var levelCompleteRequired = 1;


            Entities.WithoutBurst().ForEach
            (
                (ref LevelCompleteComponent LevelCompleteComponent, in PlayerComponent player, in Entity entity) =>
                {
                    if (LevelCompleteComponent.targetReached)
                    {
                        LevelCompleteComponent.targetReached = false;
                        levelCompleteCounter += 1;
                    }
                }
            ).Run();


            if (levelCompleteCounter >= levelCompleteRequired)
            {
                levelComplete = true;
                message = "Target Reached";
            }
        }


        if (levelManager.levelCompleteScenario ==
            LevelCompleteScenario.DestroyAll)
        {
            if (levelManager.enemiesDead >= levelManager.potentialLevelTargets)
            {
                levelComplete = true;
                Debug.Log("LEVEL COMPLETE DESTROY");
            }
        }

        if (levelComplete)
        {
            if (LevelManager.instance.currentLevelCompleted < LevelManager.instance.totalLevels - 1)
            {
                Debug.Log("PLAY LEVEL MUSIC");
                LevelManager.instance.PlayLevelMusic(LevelManager.instance.currentLevelCompleted + 1);
                Entities.WithoutBurst().WithStructuralChanges().ForEach
                (
                    (
                        LevelCompleteMenuGroup levelCompleteMenuGroup,
                        ref LevelCompleteMenuComponent LevelCompleteMenuComponent) =>
                    {
                        LevelCompleteMenuComponent.levelLoaded = false; //need?
                        SaveLevelManager.instance.levelMenuShown = true;
                        levelCompleteMenuGroup.ShowMenu(message);
                    }
                ).Run();


                LevelManager.instance.currentLevelCompleted += 1;

                //level bonus
                Entities.WithoutBurst().WithStructuralChanges().ForEach
                (
                    (
                        Entity e,
                        ref ScoreComponent scoreComponent) =>
                    {
                        Debug.Log("SCORE " + scoreComponent.score);
                        var score = (1 + (float) LevelManager.instance.currentLevelCompleted / 20f +
                                     scoreComponent.streak / 100f) * scoreComponent.score;
                        Debug.Log("SCORE " + score);
                        scoreComponent.score = (int) score;
                    }
                ).Run();


                //Debug.Log("LEVEL UP " + LevelManager.instance.endGame);
                //Debug.Log("LEVEL TOTAL " + LevelManager.instance.totalLevels);
            }
        }
    }
}