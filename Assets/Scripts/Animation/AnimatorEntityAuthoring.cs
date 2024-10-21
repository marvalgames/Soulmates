using Unity.Entities;
using UnityEngine;

namespace Animate
{
    public struct AnimatorEntityComponent : IComponentData
    {
        public int playerCombatStateID;
        public int enemyCombatStateID;
    }

    public class AnimatorEntityAuthoring : MonoBehaviour
    {
        private class AnimatorEntityAuthoringBaker : Baker<AnimatorEntityAuthoring>
        {
            public override void Bake(AnimatorEntityAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new AnimatorEntityComponent());
            }
        }
    }
}