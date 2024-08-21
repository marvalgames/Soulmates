using System.Collections.Generic;


[System.Serializable]
public class SaveGames
{
    public List<SaveEnemies> saveEnemies = new List<SaveEnemies>();
    public List<SavePlayers> savePlayers = new List<SavePlayers>();
    public List<LevelSettings> saveLevelData = new List<LevelSettings>();
    public List<float> scoreList = new List<float>();

    public int currentLevel;



}
