using Unity.Entities;
using UnityEngine;

public struct DoorComponent : IComponentData
{
    public bool active;
    public int area;
}


public class DoorComponentAuthoring : MonoBehaviour
{
    public bool active = true;
    public int area;


    class DoorComponentBaker : Baker<DoorComponentAuthoring> 
    {
        public override void Bake(DoorComponentAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new DoorComponent 
            {
                active= authoring.active, area = authoring.area 
            }
            );
        }

    }        
    

   
}
