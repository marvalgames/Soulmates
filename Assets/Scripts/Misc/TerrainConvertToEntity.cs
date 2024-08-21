using Unity.Entities;
using UnityEngine;

public struct TerrainComponent : IComponentData
{
    public bool convert;
}

public class TerrainConvertToEntity : MonoBehaviour
{
    public bool convert;
}


public class TerrainBaker : Baker<TerrainConvertToEntity>
{
    public override void Bake(TerrainConvertToEntity authoring)
    {
        var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
        AddComponent(e, new TerrainComponent()
        {
            convert = authoring.convert
        });
    }
}




