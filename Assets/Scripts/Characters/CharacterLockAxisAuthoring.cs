using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public struct CharacterLockAxisComponent : IComponentData
    {
        public float FixedY;
    }
    public class CharacterLockAxisAuthoring : MonoBehaviour
    {
        public float fixedY = 3;
        private class CharacterLockAxisAuthoringBaker : Baker<CharacterLockAxisAuthoring>
        {
            public override void Bake(CharacterLockAxisAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new CharacterLockAxisComponent { FixedY = authoring.fixedY });
            }
        }
    }
}