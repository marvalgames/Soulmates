using System;
using Collisions;
using Enemy;
using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public enum WayPointAction
{
    Move,
    Jump,
    Idle,
    Attack,
    Fire,
    N_A//strike
}

public enum WayPointAnimation
{
    Move,
    Jump,
    Idle,
    Attack,
    Strike,
    N_A
}

public enum AnimationStage
{
    None,
    Enter,
    Update,
    Exit
    
}

[Serializable]
public class WayPoint
{
    public Vector3 targetPosition;
    public Vector3 offset;
    public WayPointAction action;
    //public WayPointAnimation animation;
    //public WeaponType wayPointWeaponType;//used for strike type
    public float duration = 1;
    public float speed = 1;
    public bool chase;
    public int weaponListIndex;// from weapon manager
    public int ammoListIndex;//from ammo manager
    public int audioListIndex;

}


public struct EnemyStateComponent : IComponentData
{
    public float currentStateTimer;
    public float currentStateRequiredTime;
    public MoveStates LastState;
    public bool selectMove;
    public MoveStates MoveState;
    public int Zone;
    public CombatStates CombatState;
    //public AttackStages AttackStages;
    public LocalTransform targetZone;
    public bool enemyStrikeAllowed;
    public int animationFrameCounter;
    public bool firstFrame;
    public bool lastFrame;
    public bool isAnimating;
    //public bool selectMoveUsing;//choose from move list in enemy melee mb
    public AnimationType animationIndex;
    public bool startMove;
    public TriggerType triggerType;
    public int combatAction;
    public float normalizedTime;
    //public AnimationStage animationStage;
    public int lastCombatAction;
}


public enum MoveStates
{
    Default,
    Idle,
    Patrol,
    Chase,
    Defensive,
    Stopped
}

public enum CombatStates
{
    Default,
    Idle,
    Chase,
    Stance,
    Aim
}

public enum EnemyRoles
{
    None,
    Chase,
    Patrol,
    Security,//removes all but first waypoint
    Evade,
    Random
}

public enum DefensiveRoles
{
    None,
    Chase,
    Patrol,
    Evade,
    Random
}

public enum NavigationStates
{
    Default,
    Movement,
    Melee,
    Weapon
}



public enum AttackStages
{
    No,
    Start,
    Action,
    End
}

public struct NavMeshAgentComponent : IComponentData
{
    public float agentSpeed;
    public bool hasPath;
    public bool isStopped;

}





[Serializable]
public class EnemyClass : IComponentData
{
    public GameObject go;
}

[Serializable]
public struct EnemyComponent : IComponentData
{
    [NonSerialized] public Entity e;

    public bool invincible;

    public float3 startPosition;

    //public bool humanoid;
    public int areaIndex;
}

public struct EnemiesAttackComponent : IComponentData, IEnableableComponent
{
    public bool enemiesAttack;
}


public class EnemyComponentAuthoring : MonoBehaviour
{
    // public Entity enemyEntity;
    // public EntityManager manager;

    public bool enemiesAttack;

    public bool isNavMeshAgent = true;

    [SerializeField] private bool checkLossCondition;

    [SerializeField] bool checkWinCondition;

    [SerializeField] bool invincible;

    [SerializeField] int saveIndex;

    [SerializeField] bool paused;

    [SerializeField] int areaIndex;


    void LateUpdate()
    {
        // if (manager == default) return;
        // if (!manager.HasComponent(enemyEntity, typeof(LocalTransform))) return;
        //
        // manager.SetComponentData(enemyEntity, new LocalTransform { Value = transform.position });
    }

    class EnemyComponentBaker : Baker<EnemyComponentAuthoring>
    {
        public override void Bake(EnemyComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new EnemyComponent
                {
                    e = e,
                    invincible = authoring.invincible,
                    startPosition = authoring.transform.position
                }
            );
            AddComponent(e, new EnemiesAttackComponent  {enemiesAttack = authoring.enemiesAttack} );
            if (authoring.isNavMeshAgent)
            {
                AddComponent(e, new NavMeshAgentComponent());
            }
            SetComponentEnabled<EnemiesAttackComponent>(e, authoring.enemiesAttack);
            
            if (authoring.paused)
            {
                AddComponent(e, new Pause());
            }


            AddComponent(e, new StatsComponent
                {
                    shotsFired = 0,
                    shotsLanded = 0
                }
            );


            AddComponent(e, new SkillTreeComponent
                {
                    e = e,
                    availablePoints = 0,
                    SpeedPts = 0,
                    PowerPts = 0,
                    ChinPts = 0,
                    baseSpeed = 0,
                    CurrentLevel = 1,
                    CurrentLevelXp = 0,
                    PointsNextLevel = 10
                }
            );


            AddComponent(e, new WinnerComponent
                {
                    active = true,
                    goalCounter = 0,
                    goalCounterTarget = 0, //ie how many players you have to save - usually zero
                    targetReached = false,
                    endGameReached = false,
                    checkWinCondition = authoring.checkWinCondition
                }
            );

            AddComponent(e,
                new LevelCompleteComponent
                {
                    active = true,
                    targetReached = false,
                    checkWinCondition = authoring.checkWinCondition,
                    areaIndex = authoring.areaIndex
                }
            );


            AddComponent(e, new DeadComponent
                {
                    tag = 2,
                    isDead = false,
                    checkLossCondition = authoring.checkLossCondition
                }
            );
            var a = authoring.name;
            var b = string.Empty;
            var val = 0;

            for (var i = 0; i < a.Length; i++)
            {
                if (Char.IsDigit(a[i]))
                    b += a[i];
            }

            if (b.Length > 0)
                val = int.Parse(b);

            //int index = Int32.Parse(str);//fix
            var index = val;

            //Debug.Log("go " + a + " val " + index);

            AddComponent(e, new CharacterSaveComponent { saveIndex = index });

            AddComponent(e, new CheckedComponent());

            AddComponent(e,
                new EnemyStateComponent { enemyStrikeAllowed = true, MoveState = MoveStates.Default, CombatState = CombatStates.Default});

            //AddComponent(new EnemyClass(){go = authoring.gameObject});
        }
    }
}