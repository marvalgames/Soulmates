using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public struct MeleeComponent : IComponentData
{
    public bool Available;
    public float hitPower;
    public float gameHitPower;
    public bool anyTouchDamage;
    public float3 target;
    public bool instantiated;
    public float verticalSpeed;//speed check before combat move
    public int comboAnimationPlayed;
    public int combatAction;
    public int selectMove;
}
public class CharacterBehaviorManager : MonoBehaviour
{
    [SerializeField] private float currentRoleMaxTime = 3;

    [Header("Break Route")] [SerializeField]
    private bool breakRoute = true;

    [SerializeField] [Tooltip("If enabled distance")]
    private float breakRouteVisionDistance;

    [SerializeField] [Tooltip("Higher is closer range enemy looks for player")]
    public float switchToPlayerMultiplier = 6;

    [SerializeField] private float botSpeed = 5.0f;

    class CharacterBehaviourBaker : Baker<CharacterBehaviorManager>
    {
        public override void Bake(CharacterBehaviorManager authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new DefensiveStrategyComponent
                {
                    breakRoute = authoring.breakRoute,
                    breakRouteVisionDistance = authoring.breakRouteVisionDistance,
                    currentRole = DefensiveRoles.None,
                    currentRoleMaxTime = authoring.currentRoleMaxTime,
                    currentRoleTimer = 0,
                    switchToPlayerMultiplier = authoring.switchToPlayerMultiplier,
                    botSpeed = authoring.botSpeed
                });
        }
    }
}