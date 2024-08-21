using Sandbox.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

public struct NpcComponent : IComponentData
{
    public bool active;
    public float switchDistance;
    public float switchSpeedMultiplier;
}

public struct NpcMovementComponent : IComponentData
{
    public Entity targetEntity;
}

public class NpcAgentClass : IComponentData
{
    public NavMeshAgent agent;
    public float rotateSpeed;
    public float moveSpeed;
    public float switchSpeedMultiplier;
}


public class NpcAgentAi : MonoBehaviour
{
    [Tooltip("distance from player to switch from clone movement to nav agent movement. Needs to be greater than distance between to start")]
    public float switchDistance = 10;
    [Tooltip("nav agent move speed multiplier")]
    public float switchSpeedMultiplier = 1.2f;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Animator anim;
    [HideInInspector] public float moveSpeed;
    [HideInInspector]
    public float rotateSpeed = 1;
    private Entity _linkedEntity;
    private EntityManager _entityManager;
    private PlayerRatings _playerRatings;

    void Init()
    {
        _playerRatings = GetComponent<PlayerRatings>();
        agent = GetComponent<NavMeshAgent>();
        moveSpeed = 3.5f;

        var ratings = _entityManager.GetComponentData<RatingsComponent>(_linkedEntity);
        if (_playerRatings)
        {
            if (agent)
            {
                agent.speed = ratings.speed;
                moveSpeed = agent.speed;
            }
        }

        anim = GetComponent<Animator>();
    }


    void Start()
    {
        if (_linkedEntity == Entity.Null)
        {
            _linkedEntity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (_entityManager == default)
            {
                _entityManager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            _entityManager.AddComponentData(_linkedEntity, new NpcComponent()
            {
                active = true,
                switchDistance = switchDistance,
                switchSpeedMultiplier = switchSpeedMultiplier
            });
            _entityManager.AddComponentData(_linkedEntity, new NpcMovementComponent { targetEntity = _linkedEntity });
            Init();
            _entityManager.AddComponentObject(
                _linkedEntity,
                new NpcAgentClass
                {
                    agent = agent, rotateSpeed = rotateSpeed, moveSpeed = moveSpeed,
                    switchSpeedMultiplier = switchSpeedMultiplier
                }
            );
        }
    }


  
}