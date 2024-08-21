using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Sandbox.Player;

[RequireMatchingQueriesForUpdate]
public partial class SaveSystem : SystemBase
{
    [DeallocateOnJobCompletion]
    public NativeArray<Entity> PlayerEntities;
    [DeallocateOnJobCompletion]
    public NativeArray<Entity> EnemyEntities;


    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;



    protected override void OnCreate()
    {
    }


    protected override void OnUpdate()
    {

        if (SaveManager.instance == null || LevelManager.instance == null) return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var scoreGroup = GetComponentLookup<ScoreComponent>(false);


        if (SaveManager.instance.saveMainGame == false) return;
        SaveManager.instance.saveMainGame = false;
        Debug.Log("saving main");

        playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyComponent>());

        PlayerEntities = playerQuery.ToEntityArray(Allocator.TempJob);
        EnemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);

        var slot = 0;
        var savedGames = SaveManager.instance.saveData.saveGames.Count;
        if (savedGames == 0 || savedGames > 1)//added > savegames 1 to clean up save since we only save one slot now
        {
            SaveManager.instance.saveData.saveGames.Clear();
            SaveManager.instance.saveData.saveGames.Add(new SaveGames()); //slot 0
            //SaveManager.instance.saveData.saveGames.Add(new SaveGames()); // slot 1
            //SaveManager.instance.saveData.saveGames.Add(new SaveGames()); // slot 2
        }

        SaveManager.instance.saveData.saveGames[slot].savePlayers.Clear();
        SaveManager.instance.saveData.saveGames[slot].saveEnemies.Clear();
        SaveManager.instance.saveData.saveGames[slot].saveLevelData.Clear();

        var level = LevelManager.instance.currentLevelCompleted;
        for (var i = 0; i <= level; i++)
        {
            SaveManager.instance.saveData.saveGames[slot].saveLevelData.Add(new LevelSettings());
        }

        SaveManager.instance.SaveCurrentLevelCompleted(level);
        Debug.Log("save current level " + level);

        for (var i = 0; i < PlayerEntities.Length; i++)
        {

            var e = PlayerEntities[i];
            var player = EntityManager.GetComponentData<PlayerComponent>(e);
            var health = EntityManager.GetComponentData<HealthComponent>(e);
            var ps = EntityManager.GetComponentData<LocalTransform>(e);
            var pl = new SavePlayers
            {
                playerData = new PlayerData
                {
                    savedPlayer = player,
                    savedHealth = health,
                    position =
                    {
                        [0] = ps.Position.x,
                        [1] = ps.Position.y,
                        [2] = ps.Position.z
                    }
                }
            };

            if (SystemAPI.HasComponent<ScoreComponent>(e))
            {
                pl.playerData.savedScore = scoreGroup[e];
            }

            SaveManager.instance.saveData.saveGames[slot].savePlayers.Add(pl);
        }


        for (var i = 0; i < EnemyEntities.Length; i++)
        {
            var e = EnemyEntities[i];
            var enemy = EntityManager.GetComponentData<EnemyComponent>(e);
            var health = EntityManager.GetComponentData<HealthComponent>(e);
            var ps = EntityManager.GetComponentData<LocalTransform>(e);

            var en = new SaveEnemies
            {
                enemyData = new EnemyData
                {
                    savedEnemy = enemy,
                    savedHealth = health,
                    position =
                    {
                        [0] = ps.Position.x,
                        [1] = ps.Position.y,
                        [2] = ps.Position.z
                    }
                }
            };
            SaveManager.instance.saveData.saveGames[slot].saveEnemies.Add(en);
        }



        PlayerEntities.Dispose();
        EnemyEntities.Dispose();

        ecb.Playback(EntityManager);
        ecb.Dispose();

        SaveManager.instance.SaveGameData();

    }


}

public struct IndexComparer : IComparer<Entity>
{
    public int Compare(Entity a, Entity b)
    {
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (manager.HasComponent<CharacterSaveComponent>(a) == false) return 0;
        if (manager.HasComponent<CharacterSaveComponent>(b) == false) return 0;

        var a_index = manager.GetComponentData<CharacterSaveComponent>(a).saveIndex;
        var b_index = manager.GetComponentData<CharacterSaveComponent>(b).saveIndex;
        if (a_index > b_index)
            return 1;
        else if (a_index < b_index)
            return -1;
        else
            return 0;

    }
}





