using Unity.Entities;
using UnityEngine;

public class BossIKScript : MonoBehaviour
{
    [Tooltip("Reference to the LookAt component (only used for the head in this instance).")]
    public float lookAtWeight = 1;
    public Transform lookAtTarget;

    public float aimIkWeight = 1;
    public Transform aimIkTarget;
    public Entity linkedEntity;
    public EntityManager entityManager;

    //int count = 0;
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        
        if (linkedEntity == Entity.Null)
        {
            linkedEntity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (entityManager == default)
            {
                entityManager = GetComponent<CharacterEntityTracker>().entityManager;
            }
            //Debug.Log("LINKED " + linkedEntity);
        }
        
        animator = GetComponent<Animator>();
    }

 
    public void StartStrike()//any animation 
    {
        
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //var e = linkedEntity;
        if (linkedEntity == Entity.Null)//try again
        {
            linkedEntity = GetComponent<CharacterEntityTracker>().linkedEntity;
        }
        
        if (linkedEntity != Entity.Null)
        {
            if (manager.HasComponent<BossWeaponComponent>(linkedEntity) == false)
            {
                Debug.Log("NO WEAPON");
                return;
            }
            var bossComponent = manager.GetComponentData<BossWeaponComponent>(linkedEntity);
            bossComponent.IsFiring = 1;
            manager.SetComponentData(linkedEntity, bossComponent);
        }
    }
    
    public void LateUpdateSystem()
    {


    }
}
