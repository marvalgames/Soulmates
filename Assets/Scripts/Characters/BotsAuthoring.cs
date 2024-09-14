using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Sandbox.Player
{
    public class BotAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BotAuthoring>
        {
            public override void Bake(BotAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<Bot>(entity);
                AddComponent(entity, new CharacterIndexComponent());
                AddComponent(entity, new RaycastComponent());
            }
            
        }
    }

    public struct Bot : IComponentData
    {
        public BotState State;
        public float3 TargetPos; // Where the bot is moving to.
        
        public Entity Item; // The item that the bot is carrying.
        public bool IsCarrying; // True if carrying

        public readonly bool IsMoving()
        {
            return !(State == BotState.IDLE
                     || State == BotState.STOP);
        }
    }
    
    public struct RaycastComponent : IComponentData
    {
        public float3 HitPosition; // Where the ray hit something
        public bool HasHit;        // Whether the ray hit anything
    }

    public enum BotState
    {
        IDLE,
        MOVING,
        STOP,
    }
}