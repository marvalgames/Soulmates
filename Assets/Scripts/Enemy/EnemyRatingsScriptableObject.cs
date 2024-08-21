using UnityEngine;

[CreateAssetMenu(fileName = "Enemy1", menuName = "Enemy Ratings")]
public class EnemyRatingsScriptableObject : ScriptableObject
{
    public float speed = 3.5f;
    public float combatStrikeDistanceZoneBegin = 1.8f;
    public float combatStrikeDistanceZoneEnd = 3.6f;
    public float combatRangeDistance = 6.4f;
    public float shootRangeDistance = 8.0f;
    public float chaseRange = 18.4f;
    public float stopRange = 1; 
    public float aggression = 50;
    public float maxHealth = 100;
    public float hitPower = 10;

}



