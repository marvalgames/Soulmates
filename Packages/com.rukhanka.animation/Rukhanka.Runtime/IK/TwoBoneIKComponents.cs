using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct TwoBoneIKComponent: IComponentData, IEnableableComponent
{
    public Entity mid, tip, target, midBentHint;
    public float weight;
}
}
