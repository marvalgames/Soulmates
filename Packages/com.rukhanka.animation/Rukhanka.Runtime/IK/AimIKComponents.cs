using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct AimIKAffectedBoneComponent : IBufferElementData
{
    public Entity boneEntity;
    public float weight;
}
    
/////////////////////////////////////////////////////////////////////////////////

public struct AimIKComponent: IComponentData, IEnableableComponent
{
    public Entity target;
    public float2 angleLimits;
    public float3 forwardVector;
    public float weight;
}
}
