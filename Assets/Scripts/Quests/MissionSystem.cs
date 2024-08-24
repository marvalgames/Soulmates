//using Dialogue.Scripts;
using PixelCrushers.DialogueSystem;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public struct MissionComponent : IComponentData
{
    public bool questUpdateEnemiesDestroyed;
    public int questEnemiesDestroyed;
    public bool questUpdatePointsScored;
    public int questPointsScored;
    //public FixedString32Bytes variable;
    //public string enemiesDestroyed;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(HealthSystem))]
//[UpdateBefore(typeof(DeadSystem))]
[UpdateBefore(typeof(CleanupSystem))]
[RequireMatchingQueriesForUpdate]
public partial class MissionSystem : SystemBase
{
    protected override void OnUpdate()
    {

            Entities.WithoutBurst().ForEach((Entity e, ref MissionComponent missionComponent) =>
                {
                    // if (QuestLog.GetQuestState("Enemy Attack") == QuestState.Unassigned)
                    // {
                    //     Debug.Log("quest state unassigned");
                    //     return;
                    // }
                    
                    int currentLevel = LevelManager.instance.currentLevelCompleted;
                    int totalLevels = LevelManager.instance.levelSettings.Count;
                    if(currentLevel >= totalLevels) return;
                    missionComponent.questUpdateEnemiesDestroyed = LevelManager.instance.enemyDestroyed;
                    var dead = LevelManager.instance.levelSettings[currentLevel].enemiesDead;
                    //Debug.Log("mission " + missionComponent.questUpdateEnemiesDestroyed + " " + dead);
                    missionComponent.questEnemiesDestroyed = dead;
                    if (SystemAPI.HasComponent<ScoreComponent>(e))
                    {
                        var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(e);
                        if (scoreComponent.lastPointValue > 0)
                        {
                            missionComponent.questPointsScored = scoreComponent.score;
                            missionComponent.questUpdatePointsScored = true;
                        }
                    }
                   
                }
            ).Run();
    }
}
