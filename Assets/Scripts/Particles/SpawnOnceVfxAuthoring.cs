using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpawnOnceVfxComponent : IComponentData
{
    public Entity spawnOnceVfxEntity;
    public float3 spawnPosition;
    public int spawned;
}
public class SpawnOnceVfxAuthoring : MonoBehaviour//Deprecated
{
    [SerializeField] private GameObject spawnOnceVfxPrefab;
    [SerializeField] private Vector3 spawnPosition;



    class SpawnOnceVfxBaker : Baker<SpawnOnceVfxAuthoring>
    {
        public override void Bake(SpawnOnceVfxAuthoring authoring)
        {
            var position = authoring.transform.position + authoring.spawnPosition;
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(e,
                 new SpawnOnceVfxComponent
                 {         
                     spawnOnceVfxEntity =
                        GetEntity(authoring.spawnOnceVfxPrefab, TransformUsageFlags.Dynamic),
                     spawnPosition = position
                 }
           );

        }
    }



}
