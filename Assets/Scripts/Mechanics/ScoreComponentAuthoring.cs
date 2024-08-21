using System;
using Unity.Entities;
using UnityEngine;



[System.Serializable]

public struct ScoreComponent : IComponentData
{
    public bool trackStreak;
    public bool trackCombo;

    public int score;
    public int rank;
    public bool pointsScored;
    public int lastPointValue;
    public int defaultPointsScored;
    public int addBonus;//used for bank shot bonus for example


    public int scoreChecked;
    public float timeSinceLastScore;
    public int streak;
    public int combo;
    public bool lastShotConnected;

    public Entity scoringAmmoEntity;

    [NonSerialized]
    public Entity scoredAgainstEntity;

    public bool zeroPoints;
    public int startShotValue;
}


public class ScoreComponentAuthoring : MonoBehaviour

{

    public int defaultPointsScored = 100;
    [Header("Bonuses")]
    public bool trackStreak;
    public bool trackCombo;


    class ScoreComponentBaker : Baker<ScoreComponentAuthoring>
    {
        public override void Bake(ScoreComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e, new ScoreComponent()
                {
                    defaultPointsScored = authoring.defaultPointsScored,
                    trackStreak = authoring.trackStreak,
                    trackCombo = authoring.trackCombo
                }
            );

        }
    }

   
}
