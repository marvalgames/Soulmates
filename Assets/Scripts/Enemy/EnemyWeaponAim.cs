using Sandbox.Player;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class EnemyWeaponAim : MonoBehaviour
{
    [SerializeField]
    private Transform aimTarget;
    private Animator animator;
    private Vector3 aimPos;
    private Quaternion aimRot;
    public Rig rig;
    public RigBuilder rigBuilder;
    public MultiAimConstraint handConstraint;
    public MultiAimConstraint headConstraint;
    private NavMeshAgent agent;
    //target cleaner if we did transform target in inspector then read position and pass to below
    public bool weaponRaised;
    public CameraTypes weaponCamera;

    Entity entity;
    EntityManager manager;
    private static readonly int Aim = Animator.StringToHash("Aim");
    public float blendValue;
    public float blendSpeed = 0.5f;


    void Start()
    {
        rigBuilder = GetComponent<RigBuilder>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animator.SetLayerWeight(0, 1);
        animator.SetLayerWeight(1, 0);

        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            if (entity != Entity.Null)
            {
                manager.AddComponentData(entity,
                    new ActorWeaponAimComponent {weaponCamera = weaponCamera});
                manager.AddComponentObject(entity, this);
            }
        }
    }


    public void SetAim()
    {
        if (!agent.enabled) return;
        if (weaponRaised)
        {
            blendValue = Mathf.Lerp(blendValue, 1, Time.deltaTime * blendSpeed);
            animator.SetLayerWeight(0, 1 - blendValue);
            animator.SetLayerWeight(1, blendValue); //1 is weapon layer
            animator.SetBool(Aim, true);
            if (rig) rig.weight = 1;
        }
        else
        {
            blendValue = Mathf.Lerp(blendValue, 0, Time.deltaTime * blendSpeed);
            animator.SetLayerWeight(0, 1 - blendValue);
            animator.SetLayerWeight(1, blendValue);
            animator.SetBool(Aim, false);
            if (rig) rig.weight = 0;
        }
    }

    void Update()
    {
            UpdateAimTarget();
    }
    void UpdateAimTarget()
    {
        if (manager.HasComponent<MatchupComponent>(entity) && 
            manager.HasComponent<ActorWeaponAimComponent>(entity))
        {
             var matchComponent = manager.GetComponentData<MatchupComponent>(entity);
             var aimTargetEntity = matchComponent.targetEntity;
             if(aimTargetEntity == Entity.Null) return;
             //var aimTargetGo = manager.GetComponentObject<GameObject>(aimTargetEntity);
             var source1 = handConstraint.data.sourceObjects;
             var source2 = headConstraint.data.sourceObjects;
             //aimTarget.position = aimTargetGo.transform.position;
             
             if (manager.HasComponent<PlayerComponent>(aimTargetEntity) && weaponRaised)
             {
                 source1.SetWeight(0,1);
                 source2.SetWeight(0,1); 
                 handConstraint.data.sourceObjects = source1;
                 headConstraint.data.sourceObjects = source2;
             }
             else
             {
                 source1.SetWeight(0,0);
                 source2.SetWeight(0,0);
                 handConstraint.data.sourceObjects = source1;
                 headConstraint.data.sourceObjects = source2;
             }

        }
    }
    
    
    public void LateUpdateSystem()
    {
        SetAim();
    }
}