using Unity.Entities;
using UnityEngine;

namespace Managers
{
    
    
    public class AnimationManagerAuthoring : MonoBehaviour
    {
        public class AnimationManagerAuthoringBaker : Baker<AnimationManagerAuthoring>
        {
            public override void Bake(AnimationManagerAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new AnimationManagerComponentData());
            }
        }
    }

    public struct AnimationManagerComponentData : IComponentData
    {
        public bool evadeStrike;
    }
    
    
}