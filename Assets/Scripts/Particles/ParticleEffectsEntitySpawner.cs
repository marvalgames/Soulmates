using Unity.Entities;
using UnityEngine;


public class ParticleEffectsEntitySpawner : MonoBehaviour
{
    public GameObject ParticleEffectsPrefab;

    class ParticleEffectEntitySpawnerBaker : Baker<ParticleEffectsEntitySpawner>
    {
        public override void Bake(ParticleEffectsEntitySpawner authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new ParticleEffectsSpawnerComponent()
                {
                    entity = GetEntity(authoring.ParticleEffectsPrefab, TransformUsageFlags.Dynamic)
                }
            );
        }
    }
}