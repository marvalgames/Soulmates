
using Unity.Entities;
using Unity.Transforms;

#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
[UpdateBefore(typeof(TransformSystemGroup))]
#if RUKHANKA_WITH_NETCODE
[UpdateAfter(typeof(PredictedSimulationSystemGroup))]
#else
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
#endif
public partial class RukhankaAnimationSystemGroup: ComponentSystemGroup { }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if RUKHANKA_WITH_NETCODE
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class RukhankaPredictedAnimationSystemGroup: ComponentSystemGroup { }
#endif
}
