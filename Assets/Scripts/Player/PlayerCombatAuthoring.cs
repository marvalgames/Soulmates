using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerCombatAuthoring : MonoBehaviour
    {

        [SerializeField]
        private bool active = true;
        [SerializeField]
        private float hitPower = 100;

        class PlayerCombatBaker : Baker<PlayerCombatAuthoring>
        {
            public override void Bake(PlayerCombatAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(e, new MeleeComponent
                {
                    Available = authoring.active,
                    hitPower = authoring.hitPower,
                    gameHitPower = authoring.hitPower
                });
            }
        }
    }
}