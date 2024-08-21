using Unity.Entities;
using UnityEngine;


public class EnemyMeleeAuthoring : MonoBehaviour
{
 
    public bool active = true;
    public float hitPower = 10f;
    public bool allEnemyCollisionsCauseDamage;

    class EnemyMeleeBaker : Baker<EnemyMeleeAuthoring>
    {
        public override void Bake(EnemyMeleeAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e, new MeleeComponent
            {
                Available = authoring.active,
                hitPower = authoring.hitPower,
                gameHitPower = authoring.hitPower,
                anyTouchDamage = authoring.allEnemyCollisionsCauseDamage
            });
            
        }
    }

    
}