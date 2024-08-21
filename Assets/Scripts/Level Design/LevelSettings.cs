using UnityEngine;
[System.Serializable]
public enum LevelCompleteScenario
{
    DestroyAll,
    DestroyLeader,
    TriggerReached
}

[System.Serializable]

public class LevelSettings
{

    // Use this for initialization
    [System.NonSerialized]
    public AudioClip levelMusic = null;
    public string levelName;
    public LevelCompleteScenario levelCompleteScenario = LevelCompleteScenario.DestroyAll;
    
    public int potentialLevelTargets;//in some games max of something ie potential saved robots
    
    public int lives = 1;
    public int points;

    public int playersDead;
    public int enemiesDead;
    public int NpcDead;

    public int playersSaved;
    public int enemiesSaved;
    public int NpcSaved;


    public RoleReversalMode roleReversalMode = RoleReversalMode.On;





}


