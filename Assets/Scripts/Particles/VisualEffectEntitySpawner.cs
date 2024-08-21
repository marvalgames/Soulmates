using Unity.Entities;
using UnityEngine;


public class VisualEffectEntitySpawner : MonoBehaviour
{
    public GameObject VisualEffectPrefab;

    class VisualEffectEntitySpawnerBaker : Baker<VisualEffectEntitySpawner>
    {
        public override void Bake(VisualEffectEntitySpawner authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            if (authoring.VisualEffectPrefab)
            {
                AddComponent(e,
                    new VisualEffectEntitySpawnerComponent()
                    {
                        entity = GetEntity(authoring.VisualEffectPrefab, TransformUsageFlags.Dynamic)
                    }
                );
            }
        }
    }
}