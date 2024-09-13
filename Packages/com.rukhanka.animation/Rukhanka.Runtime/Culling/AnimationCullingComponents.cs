using Unity.Entities;
#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    #if RUKHANKA_WITH_NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    #endif
    public struct CullAnimationsTag: IComponentData, IEnableableComponent { }
}
