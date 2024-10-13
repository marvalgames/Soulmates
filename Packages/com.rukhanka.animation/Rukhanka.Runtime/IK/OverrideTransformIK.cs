using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct OverrideTransformIKComponent: IComponentData, IEnableableComponent
{
    public Entity target;
    public float positionWeight;
    public float rotationWeight;
}
}
