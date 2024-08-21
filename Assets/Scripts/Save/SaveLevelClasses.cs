using Sandbox.Player;

[System.Serializable]
public class PlayerLevelData
{
    public HealthComponent savedLevelHealth;
    public PlayerComponent savedLevelPlayer;
    public StatsComponent savedLevelStats;
    public ScoreComponent savedLevelScores;
}

[System.Serializable]
public class SaveLevelPlayers
{
    public PlayerLevelData playerLevelData;

}


