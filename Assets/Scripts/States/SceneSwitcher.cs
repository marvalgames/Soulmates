using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

public struct SceneSwitcherComponent : IComponentData
{
    public bool delete;
    public bool saveScene;
}

public class SceneSwitcher : MonoBehaviour
{
    public int CurrentSceneIndex = 0;
    private EntityManager manager;
    private Entity e;

    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        e = manager.CreateEntity();
        manager.AddComponentData(e, new SceneSwitcherComponent()
        {
        });
        manager.AddComponentObject(e, this);

        CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (LevelManager.instance.currentLevelCompleted >= LevelManager.instance.totalLevels) return;

        if (CurrentSceneIndex > 1 && LevelManager.instance.audioSourceMenu)
        {
            LevelManager.instance.audioSourceMenu.Stop();
            LevelManager.instance.PlayLevelMusic(0); //scene 0 is loader and scene 1  is menu - has own play
        }
        else if (CurrentSceneIndex == 1 && LevelManager.instance.audioSourceGame)
        {
            LevelManager.instance.audioSourceGame.Stop();
            LevelManager.instance.PlayMenuMusic(); //scene 0 is loader and scene 1  is menu - has own play
        }
    }


    void OnEnable()
    {
        PauseMenuGroup.SaveExitClickedEvent += OnButtonSaveAndExitClicked;
        PauseMenuGroup.ExitClickedEvent += OnButtonExitClicked;
        LevelCompleteMenuGroup.LevelCompleteEvent += SetupAndLoadNextScene;
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }


    void OnDisable()
    {
        PauseMenuGroup.SaveExitClickedEvent -= OnButtonSaveAndExitClicked;
        PauseMenuGroup.ExitClickedEvent -= OnButtonExitClicked;
        LevelCompleteMenuGroup.LevelCompleteEvent -= SetupAndLoadNextScene;
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }


    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        var sceneIndex =  SceneManager.GetActiveScene().buildIndex;
        Debug.Log("level loaded " + sceneIndex);

    

        LevelManager.instance.InitGameData();
    }


    public void OnButtonResetGameClicked()
    {
        //bool resetLevel = SaveManager.instance.saveWorld.isSlotSaved[1] == false;
        LevelManager.instance.ClearGameData();
        LevelManager.instance.resetLevel = true;
        LevelManager.instance.currentLevelCompleted = 0;
        Debug.Log("LEVEL RESET " + LevelManager.instance.currentLevelCompleted);

        SaveLevelManager.instance.saveScene = false;
        SaveManager.instance.saveMainGame = false;
        LevelManager.instance.loadGame = false;
        LevelManager.instance.gameResult = GameResult.None;
        SaveManager.instance.SaveCurrentLevelCompleted(0);
        //GameInterface.instance.Paused = GameInterface.instance.startPaused;
        //GameInterface.instance.StateChange = true;
        DialogueManager.ResetDatabase(DatabaseResetOptions.KeepAllLoaded);
        LoadYourScene(2);
    }


    public void
        OnButtonSaveAndExitClicked() //called from game pause menu and end game menu - if new project will need to be added to those possibly
    {
        LevelManager.instance.ClearGameData();
        SaveManager.instance.saveMainGame = true;
        SaveManager.instance.saveWorld.isSlotSaved[0] = true;
        SaveManager.instance.SaveWorldSettings();
        SaveManager.instance.SaveCurrentLevelCompleted(LevelManager.instance.currentLevelCompleted);
        SaveLevelManager.instance.saveScene = true;
        LoadYourScene(1);
    }


    public void
        OnButtonExitClicked() //called from game pause menu and end game menu - if new project will need to be added to those possibly
    {
        LevelManager.instance.ClearGameData();
        LevelManager.instance.resetLevel = true;
        LoadYourScene(1);
    }


    void LoadYourScene(int sceneIndex)
    {
        if (sceneIndex > 1)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var entities = entityManager.GetAllEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                string ename = entityManager.GetName(entities[i]);
                bool isSubscene = entityManager.HasComponent<SubScene>(entities[i]);
                bool isSceneSection = entityManager.HasComponent<SceneSection>(entities[i]);
                //Debug.Log("ENTITY I " + " " + ename);
                //Runtime generated entities requires manual deletion, 
                //but we need to skip for some specific entities otherwise there will be spamming error
                if (ename != "SceneSectionStreamingSingleton" 
                    && ename != "Unity.Physics.Systems.PhysicsAnalyticsSingleton"
                    && !isSubscene && !isSceneSection 
                    &&
                    !ename.Contains("GameObject Scene:")
                    )
                {
                    if (entityManager.HasComponent<SimulationSingleton>(entities[i]) == false &&
                        entityManager.HasComponent<PhysicsWorldSingleton>(entities[i]) == false 
                        )
                    {
                        entityManager.DestroyEntity(entities[i]);
                    }
                }
            }

        }
        StartCoroutine(Pause(sceneIndex, 2.0f));

    }

    public void EnableLoadGame() //called only by load slot from load menu
    {
        LevelManager.instance.loadMenuContinueClicked = true;
    }

    public void SetupAndLoadNextScene()
    {
        SaveLevelManager.instance.saveScene = true;
        SaveManager.instance.SaveCurrentLevelCompleted(LevelManager.instance.currentLevelCompleted);
        Debug.Log("setup and load next");
        LoadNextScene();
    }

    public void LoadQuickPlayScene()
    {
        Debug.Log("load quick scene");
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        LevelManager.instance.currentLevelCompleted = 0;
        SaveManager.instance.SaveCurrentLevelCompleted(0);
        var nextSceneIndex = 2;
        LoadYourScene(nextSceneIndex);
        Debug.Log("load next scene complete " + nextSceneIndex);
    }


    public void LoadClickScene()
    {
        Debug.Log("load click scene");
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        var level = SaveManager.instance.saveData.saveGames[0].currentLevel;
        if (LevelManager.instance.newGame == true)
        {
            level = 0;
            LevelManager.instance.newGame = false;
            LevelManager.instance.currentLevelCompleted = level;
            SaveManager.instance.SaveCurrentLevelCompleted(level);
        }

        var nextSceneIndex = level + 2;
        if (nextSceneIndex < 2) nextSceneIndex = 2; //0 is loader 1 is menu
        if (nextSceneIndex >= sceneCount)
        {
            Quit();
            return;
        }


        //Debug.Log("load next scene complete MENU" + nextSceneIndex);
    }

    public void LoadNextScene()
    {
        Debug.Log("load next scene");
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        var level = LevelManager.instance.currentLevelCompleted;
        SaveManager.instance
            .SaveCurrentLevelCompleted(
                level); //need to save  because enableload called after then loadsystem will load to entities created when loading next level
        var nextSceneIndex = level + 2;
        if (nextSceneIndex < 2) nextSceneIndex = 2; //0 is loader 1 is menu
        if (nextSceneIndex > sceneCount)
        {
            Quit();
            return;
        }

        //StartCoroutine(LoadYourAsyncScene(nextSceneIndex));

        Debug.Log("load next scene complete " + nextSceneIndex);
    }


    public void Quit()
    {
        SaveManager.instance.SaveWorldSettings();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator Pause(int buildIndex, float secs)
    {
        yield return new WaitForSeconds(secs);
        SceneManager.LoadScene(buildIndex);

    }
}

[RequireMatchingQueriesForUpdate]
public partial class ResetLevelSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SaveManager.instance == null || LevelManager.instance == null) return;


        if (LevelManager.instance.resetLevel == false) return;
        LevelManager.instance.resetLevel = false;

        Debug.Log("destroying entities");

        var ecb = new EntityCommandBuffer(Allocator.Persistent);
        //GameInterface.Paused = false;
        //GameInterface.StateChange = false;


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}


public partial class SetupNextLevelSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SaveManager.instance == null || LevelManager.instance == null) return;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var saveScene = SaveLevelManager.instance.saveScene;
        if (saveScene == false) return;
        SaveLevelManager.instance.saveScene = false;
        SaveLevelManager.instance.loadNextScene = true;
        var playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        var playerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
        SaveLevelManager.instance.saveLevelPlayers.Clear();

        for (var i = 0; i < playerEntities.Length; i++)
        {
            var playerEntity = playerEntities[i];
            var savedPlayer = SystemAPI.GetComponent<PlayerComponent>(playerEntity); //required
            var savedHealth = new HealthComponent();
            if (SystemAPI.HasComponent<HealthComponent>(playerEntity))
                savedHealth = SystemAPI.GetComponent<HealthComponent>(playerEntity);
            var savedStats = new StatsComponent();
            if (SystemAPI.HasComponent<StatsComponent>(playerEntity))
                savedStats = SystemAPI.GetComponent<StatsComponent>(playerEntity);
            var savedScores = new ScoreComponent();
            if (SystemAPI.HasComponent<ScoreComponent>(playerEntity))
                savedScores = SystemAPI.GetComponent<ScoreComponent>(playerEntity);


            var levelPlayers = new SaveLevelPlayers()
            {
                playerLevelData = new PlayerLevelData()
                {
                    savedLevelHealth = savedHealth,
                    savedLevelPlayer = savedPlayer,
                    savedLevelStats = savedStats,
                    savedLevelScores = savedScores
                }
            };
            SaveLevelManager.instance.saveLevelPlayers.Add(levelPlayers);
            Debug.Log("setup level  ");
        }

        Debug.Log("deleting");
        playerEntities.Dispose();


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}