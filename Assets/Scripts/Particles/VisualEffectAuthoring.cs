using Unity.Entities;
using UnityEngine;


public class VisualEffectAuthoring : MonoBehaviour
{
    public bool enemyDamaged = false;
    public bool playerDamaged = true;
    public float spawnTime = 3;
    public float damageAmount = 1;
    public float framesToSkip = 12;
    [Header("Effects Index")]
    public int effectsIndex = 0;
    public int deathBlowEffectsIndex;
    public float destroyCountdown = 2;

    class VisualEffectAuthoringBaker : Baker<VisualEffectAuthoring>
    {
        public override void Bake(VisualEffectAuthoring authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e,

            new VisualEffectEntityComponent
            {
                enemyDamaged = authoring.enemyDamaged,
                playerDamaged = authoring.playerDamaged,
                damageAmount = authoring.damageAmount,
                spawnTime = authoring.spawnTime,
                framesToSkip = authoring.framesToSkip,
                effectsIndex = authoring.effectsIndex,
                deathBlowEffectsIndex = authoring.deathBlowEffectsIndex,
                destroyCountdown = authoring.destroyCountdown
            }
            );

        }
    }



}

