using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct CharacterSpawnComponent : IComponentData
{
    public Entity entityPrefab;
    public float3 entityPosition;
    public bool instantiated;
    public int instanceCount;
    public bool lockY;
}


public class CharacterSpawnGameObject : IComponentData
{
    public GameObject Prefab;
    public bool instantiated;
}


public class PresentationGO : IComponentData
{
    public GameObject Prefab;
}
public class TransformGO : ICleanupComponentData
{
    public Transform Transform;
}

public class AnimatorGO : IComponentData
{
    public Animator Animator;
}


[InternalBufferCapacity(8)]
public struct CharacterPositionListBuffer : IBufferElementData
{
    public LocalTransform localTransform;
}

public class CharacterSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject prefabPresentation;
    [SerializeField] private int instanceCount;
    [SerializeField] private bool lockY = true;
    [SerializeField] List<Transform> spawnPoints;

    private class CharacterSpawnerAuthoringBaker : Baker<CharacterSpawnerAuthoring>
    {
        public override void Bake(CharacterSpawnerAuthoring authoring)
        {
            var entityPrefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);
            CharacterSpawnComponent characterSpawnComponent = new CharacterSpawnComponent
            {
                entityPrefab = entityPrefab, entityPosition = authoring.transform.position,
                instanceCount = authoring.instanceCount, lockY = authoring.lockY
            };
            
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, characterSpawnComponent);
            
            var buffer = AddBuffer<CharacterPositionListBuffer>(e);
            for (var i = 0; i < authoring.spawnPoints.Count; ++i)
            {
                var spawnPoint = new CharacterPositionListBuffer
                {
                    localTransform = new LocalTransform
                    {
                        Position = authoring.spawnPoints[i].position, Rotation = authoring.spawnPoints[i].rotation,
                    }
                    
                };
                buffer.Add(spawnPoint);
            }
            AddComponentObject(e, new CharacterSpawnGameObject {Prefab = authoring.prefabPresentation});

            
            
        }
    }
}