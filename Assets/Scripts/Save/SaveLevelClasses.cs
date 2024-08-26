using System;
using Sandbox.Player;

[Serializable]
public class PlayerLevelData
{
    public HealthComponent savedLevelHealth;
    public PlayerComponent savedLevelPlayer;
    public StatsComponent savedLevelStats;
    public ScoreComponent savedLevelScores;
}

[Serializable]
public class SaveLevelPlayers
{
    public PlayerLevelData playerLevelData;

}


