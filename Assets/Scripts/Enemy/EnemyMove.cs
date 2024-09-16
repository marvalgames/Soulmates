using System;
using System.Collections.Generic;
using Collisions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

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
    public bool selectMoveUsing;//choose from move list in enemy melee mb
    public MoveStates MoveState;
    public int Zone;
    public CombatStates CombatState;
    //public AttackStages AttackStages;
    public LocalTransform targetZone;
    public bool enemyStrikeAllowed;


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

public struct MeleeComponent : IComponentData
{
    public bool Available;
    public float hitPower;
    public float gameHitPower;
    public bool anyTouchDamage;
    public float3 target;
}



public class EnemyMove : MonoBehaviour
{
    //[HideInInspector]
    private AgentLinkMover agentLinkMover;
    [HideInInspector]
    public NavMeshAgent agent;
    [HideInInspector]
    public Animator anim;
    public List<WayPoint> wayPoints = new List<WayPoint>();
    [SerializeField] public int currentWayPointIndex;
    [SerializeField]
    private WayPoint currentWayPoint;

    public bool randomWayPoints;
    public EnemyRoles enemyRole;
    public float moveSpeed;
    public float rotateSpeed = 1;
    public float blendSpeed = .1f;

    public Vector3 aiTarget;//default chase target but if combat used gets replaced by combat system move target
    public Entity entity;
    private EntityManager manager;
    private EnemyRatings enemyRatings;

    float impulseFactor = 1;


    public float speedMultiple;
    [Header("Backup AI")]
    public bool backup;
    public float backupSpeed = 15f;
    public float backupSeconds = .25f;
    public float backupTimer;
    //public float afterMoveTimer = 0;

    [HideInInspector]
    public Vector3 originalPosition;
    private float3 agentNextPosition;


    [Header("Misc")] public float locoPitch = .5f;
    public AudioSource audioSource;
    public AudioClip clip;
    public ParticleSystem psPrefab;
    [HideInInspector]
    public ParticleSystem psInstance;
    [HideInInspector]
    public ParticleSystem stunEffect;//used by freeze system

    public bool ignoreAgentAI;
    
    [SerializeField]
    float duration = 3.0f;
    float normalizedTime;
    Vector3 startPos;
    Vector3 endPos;
    public AnimationCurve curve = new AnimationCurve();
    bool jumpLanded;
    MoveStates state = MoveStates.Default;
    private static readonly int Zone = Animator.StringToHash("Zone");
    private static readonly int Velz = Animator.StringToHash("velz");
    private static readonly int JumpState = Animator.StringToHash("JumpState");

    void Init()
    {
        //enemyRatings = GetComponent<EnemyRatings>();0
        agent = GetComponent<NavMeshAgent>();
        moveSpeed = 3.5f;
        agentLinkMover = GetComponent<AgentLinkMover>();

        var ratings = manager.GetComponentData<RatingsComponent>(entity);
        if (agent)
        {
            agent.speed = ratings.speed * impulseFactor;
            moveSpeed = agent.speed;
            //Debug.Log("AGENT SPEED " + agent.speed);
        }


        originalPosition = transform.position;
        anim = GetComponent<Animator>();

        if (agent)
        {
            //manager.AddComponent<NavMeshAgentComponent>(entity);
            agent.autoBraking = false;
            agent.updateRotation = false;
            agent.updatePosition = true;
            agent.autoTraverseOffMeshLink = false;
            if (manager.HasComponent<NavMeshAgentComponent>(entity))
            {
                var agentComponent  = manager.GetComponentData<NavMeshAgentComponent>(entity);
                agentComponent.agentSpeed = moveSpeed;
                manager.SetComponentData(entity, agentComponent);
            }
            
        }



    }


    void Start()
    {

        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }
        }


        if (entity != Entity.Null)
        {
            manager.AddComponentObject(entity, this);
            audioSource.clip = clip;
            Init();
        }


        var transform1 = transform;
        var position1 = transform1.position;
        var position = new Vector3(position1.x, position1.y, position1.z);
        if (psPrefab != null)
        {
            psInstance = Instantiate(psPrefab, position, Quaternion.identity);
            psInstance.transform.parent = transform;
        }

        SetWaypoints(randomWayPoints);


    }

    public void SetWaypoints(bool _randomWayPoints)
    {
        //waypoint 0 is initial so no matter where located travels there then never used again
        //waypoint 1 is where it goes next then cycles to last waypoint back to waypoint 1
        if (enemyRole == EnemyRoles.Patrol && wayPoints.Count <= 1)
        {
            enemyRole = EnemyRoles.Chase; //patrol requires 2 min waypoints if not chnage role to chase
        }
    


        for (var i = 0; i < wayPoints.Count; i++)
        {
            var wayPoint = wayPoints[i];
            wayPoint.targetPosition = transform.position + wayPoint.offset;
            wayPoint.action = wayPoints[i].action;
            wayPoints[i] = wayPoint;
        }

        if (wayPoints.Count == 0)
        {
            wayPoints.Add(new WayPoint { targetPosition = transform.position, action = WayPointAction.Idle });
        }

        if (agent == null || agent.enabled == false) return;
        startPos = agent.transform.position;
        currentWayPointIndex = 0;
        var isCurrentWayPointJump = wayPoints[currentWayPointIndex].action == WayPointAction.Jump;
        if (isCurrentWayPointJump)
        {
            anim.SetInteger(JumpState, 1);
            normalizedTime = 0.0f;
            endPos = wayPoints[0].targetPosition + Vector3.up * agent.baseOffset;
        }

        currentWayPointIndex = 0;
        agentNextPosition = agent.nextPosition;


    }

    public void Patrol()
    {
        //Debug.Log("UPDATE MOVE0");
        if (wayPoints.Count == 0 || agent.enabled == false)
            return;
        
        Debug.Log("Nav Patrol");

        if (wayPoints[currentWayPointIndex].action == WayPointAction.Idle)
        {
            agent.speed = 0;
            var state = manager.GetComponentData<EnemyStateComponent>(entity);
            state.MoveState = MoveStates.Idle;
            manager.SetComponentData(entity, state);
            return;
        }

        var isCurrentWayPointJump = wayPoints[currentWayPointIndex].action == WayPointAction.Jump;
        var distance = 1.0f;
        if (agent.pathPending == false && agent.remainingDistance <= distance && isCurrentWayPointJump == false)
        {

            //  jumpTrigger = false;
            currentWayPointIndex++;
            if (currentWayPointIndex >= wayPoints.Count) currentWayPointIndex = 0;
            if (wayPoints[currentWayPointIndex].action == WayPointAction.Jump)
            {
                startPos = agent.transform.position;
                anim.SetInteger(JumpState, 1);
                normalizedTime = 0.0f;
                endPos = wayPoints[currentWayPointIndex].targetPosition + Vector3.up * agent.baseOffset;
            }

        }
        else if (agent.pathPending == false && agent.remainingDistance <= distance && isCurrentWayPointJump
            && jumpLanded)
        {
            anim.SetInteger(JumpState, 0);

            jumpLanded = false;
            currentWayPointIndex++;
            if (currentWayPointIndex >= wayPoints.Count) currentWayPointIndex = 0;
            if (wayPoints[currentWayPointIndex].action == WayPointAction.Jump)
            {
                startPos = agent.transform.position;
                anim.SetInteger(JumpState, 1);
                normalizedTime = 0.0f;
                endPos = wayPoints[currentWayPointIndex].targetPosition + Vector3.up * agent.baseOffset;
            }
        }
    }



    void Curve()
    {

        if (normalizedTime < 1.0f)
        {
            var yOffset = curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            agent.destination = transform.position;
            normalizedTime += Time.deltaTime / duration;
            ignoreAgentAI = true; 

        }
        else
        {
            jumpLanded = true;
            ignoreAgentAI = false;
        }


    }

    void OnDrawGizmos()
    {
        if (agent == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(agent.destination, .095f);
    }



    public void FaceWaypoint()
    {
        if (!agent.enabled) return;
        var lookDir = aiTarget - transform.position;
        lookDir.y = 0;
        if (lookDir.magnitude < .003f) return;
        var rot = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);

    }
    
    public void SetBackup()
    {
        if (agent == null) return;

        if (agent.enabled)
        {
            Debug.Log("Nav Backup");
            backupTimer += Time.deltaTime;
            if (backupTimer >= backupSeconds)
            {
                backupTimer = 0;
            }
        }
    }


    public void Update()
    {
        if (manager.HasComponent<EnemyMovementComponent>(entity))
        {
            var enemyMovementComponent = manager.GetComponentData<EnemyMovementComponent>(entity);
            if (enemyMovementComponent.updateAgent)
            {
                enemyMovementComponent.agentNextPosition = agent.nextPosition;
                manager.SetComponentData(entity, enemyMovementComponent);
            }
        }

    }


    public void AnimationMovement(float3 target)
    {
        if (anim == null || agent.isOnNavMesh == false) return;

        float velz = 0;

        var forward =
            transform.InverseTransformDirection(transform.forward);
        if (agent && manager.HasComponent<Pause>(entity) == false)
        {
            aiTarget = target;
            if (backup == false)
            {
                //agent.updatePosition = true;
            }

            state = manager.GetComponentData<EnemyStateComponent>(entity).MoveState;

            var validTarget = false;
            if (manager.HasComponent<MatchupComponent>(entity))
            {
                validTarget = manager.GetComponentData<MatchupComponent>(entity).validTarget;
            }
            
            var notMoving = false;
            var path = new NavMeshPath();
            if (validTarget && agent.CalculatePath(target, path))
            {
                if (path.status == NavMeshPathStatus.PathPartial)
                {
                    Debug.Log("Partial");
                    notMoving = true;
                }
            }

            var pursuitMode = anim.GetInteger(Zone);
            var speed = pursuitMode >= 2 ? moveSpeed : moveSpeed * 1.5f;
            velz = forward.normalized.z;

            if (state == MoveStates.Idle || state == MoveStates.Stopped ||
                state == MoveStates.Defensive 
                || notMoving)
            {
                speed = 0;
                velz = 0;
            }
            else if (state == MoveStates.Patrol)
            {
                speed = moveSpeed * 1f;
                velz = 1f;
            }


            //if (wayPoints[currentWayPointIndex].action != WayPointAction.Jump)
            if(!ignoreAgentAI && !agentLinkMover.isAgentNavigatingLink)
            {
                //Debug.Log("Nav Forward");
                //agent.updatePosition = true;
                //agent.updateRotation = false;
                agent.destination = target;
                agentNextPosition = agent.nextPosition;
                //transform.position = agent.nextPosition;
                anim.SetInteger(JumpState, 0);
            }

            agent.speed = speed * impulseFactor;
            speedMultiple = 1;
            velz *= speedMultiple * impulseFactor;
            audioSource.pitch = velz * locoPitch;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            
            anim.SetFloat(Velz, velz, blendSpeed, Time.deltaTime);
        }
        else
        {
            agent.speed = 0;
        }
        PlayEffects(velz);
        
    }

    private void PlayEffects(float velZ)
    {
        if (psInstance)
        {
            if (!psInstance.isEmitting && velZ > 0.03f)
            {
                psInstance.Play(true);
            }
            else
            {
                psInstance.Stop(true);
            }
        }

    }


    public void UpdateEnemyMovement()
    {

        if (entity == Entity.Null || agent == null) return;

        if (manager.HasComponent<Pause>(entity))
        {
            agent.speed = 0;
            anim.speed = 0;
            return;
        }


        impulseFactor = 1;
        var impulseActive = manager.HasComponent<ImpulseComponent>(entity);
        if (impulseActive)
        {
            var impulse = manager.GetComponentData<ImpulseComponent>(entity);
            if (impulse.activate)
            {
                impulseFactor = impulse.animSpeedRatio;
            }
            else if (impulse.activateOnReceived)
            {
                impulseFactor = impulse.animSpeedRatioOnReceived;
            }
        }



        if (wayPoints.Count <= currentWayPointIndex) return;
        anim.speed = impulseFactor;
        //agent.updatePosition = true;
        //agent.updateRotation = false;
        

        var isCurrentWayPointJump = wayPoints[currentWayPointIndex].action == WayPointAction.Jump;
        Debug.Log("Nav UpdateEnemyMovement");
        if (isCurrentWayPointJump == false)
        {
            Debug.Log("Nav pos true");
            //agent.updatePosition = true;
        }
        else
        {
            Debug.Log("Nav pos false");
            //agent.updatePosition = false;
            Curve();
        }
    }


}
















