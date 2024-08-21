using System.Collections.Generic;
using Collisions;
using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public enum CharacterType
{
    Player,
    Enemy,
    Boss,
    NPC
}

[System.Serializable]
public class RigContainer
{
    public TriggerType triggerType;
    public Transform transform;
}

public class RigEntityContainer
{
    public Entity entity;
    public Transform transform;
}



public class RigEntityTracker : MonoBehaviour
{

    public Entity linkedEntity;
    public List<RigEntityContainer> rigEntities = new List<RigEntityContainer>();
    public EntityManager entityManager;
    private Animator _animator;
    public CharacterType characterType;
    public RigContainer[] rigContainers;
    [Header("Mesh Size Reference Only")]
    public Vector3 size;
    public SkinnedMeshRenderer renderer;
    void Start()
    {

        if (renderer)
        {
            size = renderer.bounds.size;
        }
        
        if (linkedEntity == Entity.Null)
        {
            linkedEntity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (entityManager == default)
            {
                entityManager = GetComponent<CharacterEntityTracker>().entityManager;
            }
        }
        
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        var entities = entityManager.GetAllEntities();
        
        for (int i = 0; i < entities.Length; i++)
        {
            var e = entities[i];
            bool isTrigger = entityManager.HasComponent<TriggerComponent>(e);
            if (isTrigger)
            {
                var trigger = entityManager.GetComponentData<TriggerComponent>(e);
                bool isSameCharacterType = false;
                bool isSameEntity = false;
                if (trigger.ParentEntity != Entity.Null)
                {
                    isSameEntity = trigger.ParentEntity == linkedEntity;

                    isSameCharacterType = characterType == CharacterType.Player &&
                                          entityManager.HasComponent<PlayerComponent>(trigger.ParentEntity)
                                          ||
                                          characterType == CharacterType.Enemy &&
                                          entityManager.HasComponent<EnemyComponent>(trigger.ParentEntity)
                                          ||
                                          characterType == CharacterType.Boss &&
                                          entityManager.HasComponent<BossComponent>(trigger.ParentEntity);

                }

                if (!isSameCharacterType || !isSameEntity) continue;
                for (int j = 0; j < rigContainers.Length; j++)
                {
                    var isTypeMatch = trigger.Type == (int)rigContainers[j].triggerType;
                    if(!isTypeMatch) continue;
                    bool addEntity = false;
                    
                    var rigEntity = new RigEntityContainer
                    {
                        transform = rigContainers[j].transform,
                        entity = e
                    };


                    if (trigger.Type is (int)TriggerType.Body
                        or (int)TriggerType.LeftHand
                        or (int)TriggerType.RightHand
                        or (int)TriggerType.LeftFoot
                        or (int)TriggerType.RightFoot
                        or (int)TriggerType.Base
                        or (int)TriggerType.Head
                        or (int)TriggerType.Tail
                        or (int)TriggerType.Melee
                        )
                    {
                        addEntity = true;
                    }

                    if (addEntity)
                    {
                        rigEntities.Add(rigEntity);
                    }
                    
                }

            }

        }

    }

    void LateUpdate()
    {
        if (rigEntities == null || entityManager == default) return;
        foreach (var bone in rigEntities)
        {
            
            var rigEntity = bone.entity;
            var hasTransform = entityManager.HasComponent<LocalTransform>(rigEntity) &&
                               entityManager.HasComponent<LocalTransform>(rigEntity);


            if (hasTransform)
            {
                var tr = bone.transform;
                quaternion eRotation = tr.rotation;
                var localTransform = LocalTransform.FromPositionRotation(tr.position, eRotation);
                entityManager.SetComponentData(rigEntity, localTransform);
            }
        }


    }


}
