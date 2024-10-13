using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 
[DisableAutoCreation]
[UpdateAfter(typeof(AnimationProcessSystem))]
public partial class RukhankaAnimationInjectionSystemGroup: ComponentSystemGroup { }
}

