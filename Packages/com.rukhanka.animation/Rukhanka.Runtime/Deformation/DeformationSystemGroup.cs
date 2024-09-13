#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Entities;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(RegisterMaterialsAndMeshesSystem))]
[UpdateBefore(typeof(DeformationsInPresentation))]
public partial class RukhankaDeformationSystemGroup: ComponentSystemGroup
{
    protected override void OnCreate()
    {
#if !HYBRID_RENDERER_DISABLED
        if (!EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem())
#endif
        {
            Enabled = false;
            UnityEngine.Debug.Log("No SRP present, no compute shader support, or running with -nographics. Rukhanka Mesh Deformation Systems disabled.");
        }
        base.OnCreate();
    }
}
}

#endif