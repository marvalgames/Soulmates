
using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.DebugDrawer
{

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial class RukhankaDebugDrawerFrameStartSystem : SystemBase
{
    private DrawerManagedSingleton ds;
    protected override void OnCreate()
    {
        ds = new DrawerManagedSingleton();
        if (ds.IsValid())
        {
            var e = EntityManager.CreateSingleton(ds, new FixedString64Bytes("Rukhanka.DebugDrawer.Singleton"));
            var dw = Drawer.Create(ds);
            EntityManager.AddComponentData(e, dw);
        }
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.ManagedAPI.TryGetSingleton<DrawerManagedSingleton>(out var ds))
            return;
        ds.BeginFrame();
        var dw = Drawer.Create(ds);
        SystemAPI.SetSingleton(dw);
    }
    
    protected override void OnDestroy()
    {
        if (SystemAPI.ManagedAPI.TryGetSingleton<DrawerManagedSingleton>(out var ds))
            ds.Dispose();
    }
}

///===============================================================================================================///

[UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
public partial class RukhankaDebugDrawerFrameEndSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Dependency.Complete();
        if (SystemAPI.ManagedAPI.TryGetSingleton<DrawerManagedSingleton>(out var ds))
        {
            ds.EndFrame();
            SystemAPI.SetSingleton(new Drawer());
        }
    }
}
}
