using Sandbox.Player;
using Unity.Entities;
//using Unity.Burst;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;


[UpdateAfter(typeof(ScoreSystem))]
[RequireMatchingQueriesForUpdate]

public partial class ShowMenuSystem : SystemBase
{

    int score = 0;
    int rank = 0;
    bool showScoresOnMenu = false;

    protected override void OnCreate()
    {
    }


    protected override void OnUpdate()
    {


        if(LevelManager.instance == null) return;


        if (LevelManager.instance.gameResult == GameResult.Winner ||
            LevelManager.instance.gameResult == GameResult.Loser)
        {

            //grab "last" player score if any
            Entities.WithoutBurst().WithAll<PlayerComponent>().ForEach
            ((in ScoreComponent scoreComponent) =>
            {
                //Debug.Log("SHOW SCORE " + scoreComponent.score);
                score = scoreComponent.score;
                rank = scoreComponent.rank;
                showScoresOnMenu = true;
            }
            ).Run();


            LevelManager.instance.StopAudioSources();//level manager not an entity so cant use for each to stop audio sources so use this

        }

        if (LevelManager.instance.gameResult == GameResult.Winner)
        {


            Entities.WithoutBurst().ForEach
            (
                (ref WinnerMenuComponent winnerMenuComponent, in WinnerMenuGroup winnerMenuGroup) =>
                {
                    if (winnerMenuComponent.hide == true)
                    {
                        Debug.Log("show winner menu");
                        winnerMenuGroup.showScoreboard = showScoresOnMenu;
                        winnerMenuGroup.score = score;
                        winnerMenuGroup.rank = rank;
                        winnerMenuGroup.showMenu = true;
                        winnerMenuComponent.hide = false;
                    }
                }
            ).Run();


        }
        else if (LevelManager.instance.gameResult == GameResult.Loser)
        {


             
            Entities.WithoutBurst().ForEach
            (
                (ref DeadMenuComponent deadMenuComponent, in DeadMenuGroup deadMenuGroup) =>
                {
                    if (deadMenuComponent.hide == true)
                    {
                        Debug.Log("show loser menu");
                        deadMenuGroup.showScoreboard = showScoresOnMenu;
                        deadMenuGroup.score = score;
                        deadMenuGroup.rank = rank;
                        deadMenuGroup.showMenu = true;
                        deadMenuComponent.hide = false;
                    }
                }
            ).Run();



        }
    }
}




