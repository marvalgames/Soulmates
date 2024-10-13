using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct FABRIKComponent: IComponentData, IEnableableComponent
{
    public Entity tip, target;
    public int numIterations;
    public float weight, threshold;
}
}
