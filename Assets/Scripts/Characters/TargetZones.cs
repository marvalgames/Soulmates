using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct TargetZoneComponent : IComponentData
{
    //public LocalTransform headZone;
    public LocalToWorld HeadZoneLocalToWorld;
    public bool validTarget;
    public float3 headZonePosition;
}
public class TargetZones : MonoBehaviour
{

    public Transform headZone;
    [SerializeField]
    Transform deparentTriggers;
    public Entity Entity;
    public EntityManager Manager;

    void Start()
    {
        if (deparentTriggers != null)
        {
            deparentTriggers.parent = null;
        }

        var animator = GetComponent<Animator>();
        if (!animator) return;
        if (headZone == null)
        {
            headZone = animator.GetBoneTransform(HumanBodyBones.Head);
        }
        
        //TEST
        if (Entity == Entity.Null)
        {
            Entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (Manager == default)
            { 
                Manager = GetComponent<CharacterEntityTracker>().entityManager;
            }
            if(Entity != Entity.Null) Manager.AddComponentData(Entity,
                new TargetZoneComponent());

        }
        
     
        
        


    }
    
    void Update()
    {
        if (Manager == default || Entity == Entity.Null) return;
        
        //if (Manager.HasComponent(Entity, typeof(TargetZoneComponent)) == false) return;

        if (Manager.HasComponent<TargetZoneComponent>(Entity))
        {
            var targetZone = Manager.GetComponentData<TargetZoneComponent>(Entity);
            var position1 = headZone.transform.position;
            var position = position1;
            //position.y = position1.y - transform.position.y;
            targetZone.headZonePosition = position; //local to world transformation needed ???
            Manager.SetComponentData(Entity, targetZone);
        }


    }

    


}
