﻿using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct DefensiveStrategyComponent : IComponentData
{
    public bool breakRoute;
    public float breakRouteVisionDistance;
    public DefensiveRoles currentRole;
    public float currentRoleMaxTime;
    public float currentRoleTimer;
    public Entity closeBulletEntity;
    //public Entity closestEnemiesAttackEntity;
    public float switchToPlayerMultiplier;
    public float botSpeed;
    //public float distanceToOpponent;
    public BotState botState;

}

public struct EnemyBehaviourComponent : IComponentData
{
    public float speedMultiple;
    public float speed;
    public bool useDistanceFromStation;
    public float chaseRange;
    public float stopRange;
    public float aggression;
    public float maxHealth;
  
}

public struct EnemyWeaponMovementComponent : IComponentData
{
    public Vector3 originalPosition;
    public bool enabled; //true if currently active movement state
    public float shootRangeDistance;
}

public struct EnemyMeleeMovementComponent : IComponentData
{
    public float combatStrikeDistanceZoneBegin;
    public float combatStrikeDistanceZoneEnd;
    public float combatRangeDistance;
    public bool backup;
    public Vector3 originalPosition;
    public bool enabled; //true if currently active movement state

    //change to enemy behavior component ??
    public bool switchUp; //if true enemy will change states when tracking
    public float switchUpTimer;
    public float originalSwitchUpTime;
    public float currentSwitchUpTime;
}

public struct EnemyMovementComponent : IComponentData
{
    public bool AgentBackupMovement;
    public bool AgentPatrolMovement;
    public bool AgentAnimationMovement;
    public bool backup;
    public float speedMultiple;
    public bool enabled; //true if currently active movement state
    public float3 agentNextPosition;
    public bool updateAgent;
    public float enemyBackupSpeed;
    public float backupTimer;
    public float3 originalPosition;
    public bool nearEdge;

}

public class EnemyBehaviorManager : MonoBehaviour
{
    [Header("Melee Switch Options")] public bool switchUp; //if true enemy will change states when tracking
    public float switchUpTime = 6.0f;
    public float backupSpeed = 2.0f;
    [Header("Route Attributes")] public NavigationStates navigationStates;

    [Tooltip("If true use distance from original station for chasing player instead of distance from player")]
    public bool useDistanceFromStation; //if true distance from original station is used to decide chase or not

    [SerializeField] private float currentRoleMaxTime = 3;

    [Header("Break Route")] [SerializeField]
    private bool breakRoute = true;

    [SerializeField] [Tooltip("If enabled distance")]
    private float breakRouteVisionDistance;

    [SerializeField] [Tooltip("Higher is closer range enemy looks for player")]
    public float switchToPlayerMultiplier = 6;
    

    [Header("Mechanics")] [SerializeField] private bool canFreeze;
    [SerializeField]
    private float botSpeed = 5.0f;


    class EnemyBehaviourBaker : Baker<EnemyBehaviorManager>
    {
        public override void Bake(EnemyBehaviorManager authoring)
        {
            var basicMovement = false;
            var meleeMovement = false;
            var weaponMovement = false;
            if (authoring.navigationStates == NavigationStates.Movement) basicMovement = true;
            else if (authoring.navigationStates == NavigationStates.Melee) meleeMovement = true;
            else if (authoring.navigationStates == NavigationStates.Weapon) weaponMovement = true;

            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);


            AddComponent(e, 
                new EnemyBehaviourComponent
                {
                    useDistanceFromStation = authoring.useDistanceFromStation,
                    chaseRange = GetComponent<EnemyRatings>().Ratings.chaseRange,
                    stopRange = GetComponent<EnemyRatings>().Ratings.stopRange,
                    speedMultiple = 1.0f,
                    aggression = GetComponent<EnemyRatings>().Ratings.aggression
                    
                });
            AddComponent(e, 
                new DefensiveStrategyComponent
                {
                    breakRoute = authoring.breakRoute,
                    breakRouteVisionDistance = authoring.breakRouteVisionDistance,
                    currentRole = DefensiveRoles.None,
                    currentRoleMaxTime = authoring.currentRoleMaxTime,
                    currentRoleTimer = 0,
                    switchToPlayerMultiplier = authoring.switchToPlayerMultiplier,
                    botSpeed = authoring.botSpeed
                });

            var position = authoring.transform.position;
            AddComponent(e, 
                new EnemyMovementComponent
                    { originalPosition = position, enabled = basicMovement, updateAgent = true, enemyBackupSpeed = authoring.backupSpeed});

            AddComponent(e, 
                new EnemyMeleeMovementComponent
                {
                    combatStrikeDistanceZoneBegin = GetComponent<EnemyRatings>().Ratings.combatStrikeDistanceZoneBegin,
                    combatStrikeDistanceZoneEnd = GetComponent<EnemyRatings>().Ratings.combatStrikeDistanceZoneEnd,
                    originalPosition = position,
                    switchUp = authoring.switchUp,
                    originalSwitchUpTime = authoring.switchUpTime,
                    currentSwitchUpTime = authoring.switchUpTime,
                    combatRangeDistance = GetComponent<EnemyRatings>().Ratings.combatRangeDistance,
                    enabled = meleeMovement
                });

            AddComponent(e, 
                new EnemyWeaponMovementComponent
                {
                    originalPosition = position,
                    shootRangeDistance = GetComponent<EnemyRatings>().Ratings.shootRangeDistance,
                    enabled = weaponMovement
                });
            if (authoring.canFreeze) AddComponent(e, new FreezeComponent());
        }
    }
}