using Sandbox.Player;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup((typeof(PresentationSystemGroup)))]
[RequireMatchingQueriesForUpdate]

public partial class LoadSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SaveManager.instance == null || LevelManager.instance == null) return;


        //if (SaveManager.instance.saveData == null) return;
        //if (SaveManager.instance.saveData.saveGames == null) return;


        if (SaveManager.instance.saveWorld.isSlotSaved[0] == false) return; //deleted / cleared slot
        if (LevelManager.instance.loadGame == false) return;
        LevelManager.instance.loadGame = false;
        Debug.Log("load main scene");
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        if (SaveManager.instance.saveWorld == null) return;
        var savedGames = SaveManager.instance.saveData.saveGames.Count;
        if (savedGames == 0)
        {
            SaveManager.instance.saveData.saveGames.Clear();
            SaveManager.instance.saveData.saveGames.Add(new SaveGames()); //slot 0
            //SaveManager.instance.saveData.saveGames.Add(new SaveGames()); // slot 1
            //SaveManager.instance.saveData.saveGames.Add(new SaveGames()); // slot 2
            return;
        }

        var slot = 0;
        if (SaveManager.instance.saveData.saveGames[slot].savePlayers == null) return;
        if (SaveManager.instance.saveData.saveGames[slot].saveEnemies == null) return;
        if (SaveManager.instance.saveData.saveGames[slot].savePlayers.Count == 0) return;
        if (SaveManager.instance.saveData.saveGames[slot].saveEnemies.Count == 0) return;

        var sg = SaveManager.instance.saveData.saveGames[slot];
        LevelManager.instance.currentLevelCompleted = sg.currentLevel;
        //Debug.Log("SG LEVEL " + sg.currentLevel);

        var playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        var PlayerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
        PlayerEntities.Sort(new IndexComparer());

        var enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>());
        var EnemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
        EnemyEntities.Sort(new IndexComparer());


        for (var i = 0; i < PlayerEntities.Length; i++)
        {
            var e = PlayerEntities[i];
            var pl = SaveManager.instance.saveData.saveGames[slot].savePlayers[i];
            var player = pl.playerData.savedPlayer;
            var health = pl.playerData.savedHealth;
            ecb.SetComponent(e, player);
            ecb.SetComponent(e, health);
            if (SystemAPI.HasComponent<ScoreComponent>(e) == true)
            {
                var score = pl.playerData.savedScore;
                ecb.SetComponent(e, score);
            }

            var pos = new float3
            {
                x = pl.playerData.position[0], y = pl.playerData.position[1], z = pl.playerData.position[2]
            };

            var ps = LocalTransform.FromPosition(pos);
            Debug.Log("ps " + ps);
            ecb.SetComponent(e, ps);
        }


        for (var i = 0; i < EnemyEntities.Length; i++)
        {
            var e = EnemyEntities[i];
            var en = SaveManager.instance.saveData.saveGames[slot].saveEnemies[i];
            var enemy = en.enemyData.savedEnemy;
            var health = en.enemyData.savedHealth;
            ecb.SetComponent(e, enemy);
            ecb.SetComponent(e, health);
            var pos = new float3
            {
                x = en.enemyData.position[0], y = en.enemyData.position[1], z = en.enemyData.position[2]
            };

            var ps = LocalTransform.FromPosition(pos);

            ecb.SetComponent(e, ps);
        }


        ecb.Playback(EntityManager);
        ecb.Dispose();

        PlayerEntities.Dispose();
        EnemyEntities.Dispose();
    }
}


public partial class LoadNextLevelSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (SaveManager.instance == null) return;

        var saveScene = SaveLevelManager.instance.saveScene;
        var loadNextScene = SaveLevelManager.instance.loadNextScene;

        if (loadNextScene == false) return;


        SaveLevelManager.instance.saveScene = false;
        SaveLevelManager.instance.loadNextScene = false;

        var ecb = new EntityCommandBuffer(Allocator.Temp);


        var savedLevelPlayerIndex = 0;
        Entities.WithoutBurst().WithAll<PlayerComponent>().ForEach((ref ScoreComponent scoreComponent, in Entity e) =>
            {
                scoreComponent = SaveLevelManager.instance.saveLevelPlayers[savedLevelPlayerIndex].playerLevelData
                    .savedLevelScores;
                Debug.Log("load next level " + SaveLevelManager.instance.saveLevelPlayers.Count);

                savedLevelPlayerIndex = savedLevelPlayerIndex + 1;
            }
        ).Run();

        savedLevelPlayerIndex = 0;
        Entities.WithoutBurst().WithAll<PlayerComponent>().ForEach((ref HealthComponent healthComponent, in Entity e) =>
            {
                healthComponent = SaveLevelManager.instance.saveLevelPlayers[savedLevelPlayerIndex].playerLevelData
                    .savedLevelHealth;
                //Debug.Log("id1  " + savedLevelPlayerIndex);


                savedLevelPlayerIndex++;
            }
        ).Run();


        //var sceneSwitcher = GetSingleton<SceneSwitcherComponent>();
        //var sceneEntity = GetSingletonEntity<SceneSwitcherComponent>();
        //ecb.DestroyEntity(sceneEntity);


        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}