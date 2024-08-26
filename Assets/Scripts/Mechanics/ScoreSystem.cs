using System.Collections.Generic;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(EndGameSystem))]
[RequireMatchingQueriesForUpdate]
public partial class ScoreSystem : SystemBase
{
    protected override void OnUpdate()
    {
        //var flingGroup = GetComponentLookup<FlingMechanicComponent>(false);
        //var damageGroup = GetComponentLookup<DamageComponent>(false);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
     
        var dep0 = Entities.ForEach((ref ScoreComponent score, in Entity e
            ) =>
            {
                if (SystemAPI.HasComponent<PlayerComponent>(e))
                {
                    if (SystemAPI.HasComponent<DamageComponent>(e)) //should always be but leave anyway
                    {
                        var damageComponent = SystemAPI.GetComponent<DamageComponent>(e);
                        var damage = damageComponent.DamageReceived;
                        if (damage > .001 && damageComponent.LosingDamage == false)
                        {
                            score.streak = 0;
                        }
                    }
                }


                if (score.pointsScored)
                {
                    Debug.Log("SCORE");
                    if (score is { trackStreak: true, trackCombo: true, combo: 1 })
                    {
                        score.streak += 1;
                    }
                    else if (score is { trackStreak: true, trackCombo: false })
                    {
                        score.streak += 1;
                    }

                    score.lastShotConnected = true;
                    float defaultScore = score.defaultPointsScored;
                    var timeBonus = (5 - score.timeSinceLastScore) * defaultScore;
                    timeBonus = math.clamp(timeBonus, -.5f * defaultScore, 2f * defaultScore);

                    var streakBonus = math.pow(score.streak * defaultScore, 2) / 500;

                    var comboBonus = score.combo > 1 ? math.pow(score.combo * defaultScore, 2) / 200 : 0;
                    //Debug.Log("combo Bonus " + comboBonus);

                    score.lastPointValue = score.defaultPointsScored + (int)timeBonus + (int)streakBonus +
                                           (int)comboBonus + score.addBonus;
                    score.score = score.score + score.lastPointValue;
                    //score.startShotValue = score.score;
                    if (SystemAPI.HasComponent<HealthComponent>(e))
                    {
                        var he = SystemAPI.GetComponent<HealthComponent>(e);
                        var bonus = streakBonus / 10;
                        if (he.losingHealth == false || score.streak <= 1)
                        {
                            bonus = 0;
                        }

                        he.totalDamageReceived = he.totalDamageReceived - bonus;
                        if (he.totalDamageReceived < 0)
                        {
                            he.totalDamageReceived = 0;
                        }

                        SystemAPI.SetComponent(e, he);
                    }

                    if (SystemAPI.HasComponent<DamageComponent>(score.scoredAgainstEntity))
                    {
                        //Debug.Log("against " + score.scoredAgainstEntity);
                        var damage = SystemAPI.GetComponent<DamageComponent>(score.scoredAgainstEntity);
                        damage.ScorePointsReceived = score.lastPointValue;
                        SystemAPI.SetComponent(score.scoredAgainstEntity, damage);
                    }

                    score.pointsScored = false;
                    //score.scoringAmmoEntity = Entity.Null;
                    score.timeSinceLastScore = 0;
                    //score.lastPointValue = 0;
                }
                else
                {
                    score.timeSinceLastScore += SystemAPI.Time.DeltaTime;
                }
            }
        ).Schedule(Dependency);
        dep0.Complete();

        var currentScore = 0;
        var scoreChecked = 0;


        Entities.WithoutBurst().ForEach((in ScoreShow showScore, in ScoreComponent score) =>
            {
                scoreChecked = score.scoreChecked;
                currentScore = score.score;
                //Debug.Log("SCORE " + currentScore);

                showScore.ShowLabelScore(score.score);
                showScore.ShowLabelStreak(score.streak);
                showScore.ShowLabelCombo(score.combo);
                showScore.ShowLabelLevel(LevelManager.instance.currentLevelCompleted + 1);
            }
        ).Run();


        if (SaveManager.instance == null) return;

        var updateScoreForMenu = SaveManager.instance.updateScore;
        //run rank score loop only when endgame
        if (LevelManager.instance.endGame && scoreChecked == 0 ||
            updateScoreForMenu) //update score if clicked from menu
        {
            SaveManager.instance.updateScore = false;
            //int slot = SaveManager.instance.saveWorld.lastLoadedSlot - 1;
            var slot = 0;
            var scores = SaveManager.instance.saveData.saveGames[slot].scoreList;
            //Debug.Log("cs " + currentScore);
            if (updateScoreForMenu == false)
            {
                scores.Add(currentScore);
                scoreChecked = 1;
            }

            scores.Sort();
            SaveManager.instance.saveData.saveGames[slot].scoreList = scores;
            SaveManager.instance.SaveGameData();
            var rank = 1;
            var count = scores.Count;
            for (var i = 0; i < count; i++)
            {
                if (scores[i] > currentScore)
                {
                    rank += 1;
                }
            }

            Entities.ForEach
            (
                (ref ScoreComponent scoreComponent) =>
                {
                    scoreComponent.rank = rank;
                    scoreComponent.scoreChecked = scoreChecked;
                }
            ).Schedule();

            //Debug.Log("RANK " + rank);

            Entities.WithoutBurst().ForEach
            (
                (ref ScoresMenuComponent scoresMenuComponent) =>
                {
                    CalcScoreMenuLabels(scores, ref scoresMenuComponent);
                }
            ).Run();
        }


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }


    void CalcScoreMenuLabels(List<float> scores, ref ScoresMenuComponent scoresMenuComponent)
    {
        var slot = scores.Count;
        if (scores.Count > 0)
        {
            scoresMenuComponent.hi1 = (int)scores[slot - 1];
        }

        if (scores.Count > 1)
        {
            scoresMenuComponent.hi2 = (int)scores[slot - 2];
        }

        if (scores.Count > 2)
        {
            scoresMenuComponent.hi3 = (int)scores[slot - 3];
        }

        if (scores.Count > 3)
        {
            scoresMenuComponent.hi4 = (int)scores[slot - 4];
        }

        if (scores.Count > 4)
        {
            scoresMenuComponent.hi5 = (int)scores[slot - 5];
        }
    }
}