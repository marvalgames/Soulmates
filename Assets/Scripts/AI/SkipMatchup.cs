using Unity.Entities;
using UnityEngine;

public struct SkipMatchupComponent : IComponentData

{

}



public class SkipMatchup : MonoBehaviour
{

    class SkipMatchupBaker : Baker<SkipMatchup>
    {
        public override void Bake(SkipMatchup authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new SkipMatchupComponent() ); 
        }
    }
}







