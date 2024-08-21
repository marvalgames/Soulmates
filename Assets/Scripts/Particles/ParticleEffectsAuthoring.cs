using Unity.Entities;
using UnityEngine;


public class ParticleEffectsAuthoring : MonoBehaviour
{
    class ParticleEffectsAuthoringBaker : Baker<ParticleEffectsAuthoring>
    {
        public override void Bake(ParticleEffectsAuthoring authoring)
        {
            var ps = authoring.GetComponentInChildren<ParticleSystem>();
            var psEntity = GetEntity(ps, TransformUsageFlags.Dynamic);
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            AddComponent(e,
                new ParticleEffectsEntityComponent
                {
                    particleSystemEntity = psEntity
                }
            );
        }
    }
}