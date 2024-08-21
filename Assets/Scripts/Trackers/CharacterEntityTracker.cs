using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public struct EntityFollow : IComponentData
{
    //public EntityManager linkedManager;
    public Entity linkedEntity;
    public int index;
}

public class CharacterEntityTracker : MonoBehaviour
{
    
    public Entity linkedEntity;
    public EntityManager entityManager;
    [Tooltip("Match with entity link index in Sub Scene")]
    public int index;
    private NavMeshAgent _agent;
    private Animator _animator;
    [Tooltip("Follow entity movement (True for player)")]
    public bool followLocalTransform;
    [Tooltip("Set followLocalTransform from script if true")]
    public bool followPlayerCharacter;

    private void Awake()
    {
        gameObject.SetActive(false);

    }

    void Start()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        var entities = entityManager.GetAllEntities();

        for (int i = 0; i < entities.Length; i++)
        {
            bool isFollow = entityManager.HasComponent<EntityFollow>(entities[i]);
            if (isFollow)
            {
                if (entityManager.GetComponentData<EntityFollow>(entities[i]).index == index)//this MB index must match subscene entity index (index is designated in entityLinkAuthoring)
                {
                    if(linkedEntity == Entity.Null) linkedEntity = entities[i];
                    //Debug.Log("linked entity " + linkedEntity.Index);
                }
            }
        }

        if (linkedEntity != Entity.Null)
        {
            entityManager.AddComponentObject(linkedEntity, _animator);
            if (_agent)
            {
                entityManager.AddComponentObject(linkedEntity, _agent);
            }
            entityManager.AddComponentObject(linkedEntity, gameObject);
        }

    }

    void LateUpdate()
    {


        if (linkedEntity == Entity.Null || entityManager == default ) return;
        var paused = entityManager.HasComponent<Pause>(linkedEntity);
        var hasTransform = entityManager.HasComponent<LocalTransform>(linkedEntity);


        if (hasTransform && !paused)
        {
            var eLocalPosition = entityManager.GetComponentData<LocalTransform>(linkedEntity).Position;
            var eLocalRotation = entityManager.GetComponentData<LocalTransform>(linkedEntity).Rotation;

            if (followPlayerCharacter)
            {
                followLocalTransform = !entityManager.HasComponent<NpcMovementComponent>(linkedEntity);
            }

            if (followLocalTransform)
            {
                transform.position = eLocalPosition;
                transform.rotation = eLocalRotation;
            }
            else
            {
                var localTransform = LocalTransform.FromPositionRotation(transform.position, transform.rotation);
                entityManager.SetComponentData(linkedEntity, localTransform);
            }
        }

    }

}
