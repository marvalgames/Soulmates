using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class UIController_AvatarMask: MonoBehaviour
{
    SystemHandle sysHandle;
    
/////////////////////////////////////////////////////////////////////////////////

    void Start()
    {
        sysHandle = World.DefaultGameObjectInjectionWorld.CreateSystem<OverrideAvatarMaskSystem>();
        var sysGroup = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<RukhankaAnimationSystemGroup>();
        sysGroup.AddSystemToUpdateList(sysHandle);
        EntityWorldSwitcher(true);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    public void UseOverrideAnimationChange(bool on)
    {
        EntityWorldSwitcher(on);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void EntityWorldSwitcher(bool on)
    {
        var wu = World.DefaultGameObjectInjectionWorld.Unmanaged;
        var sys = wu.GetExistingUnmanagedSystem<OverrideAvatarMaskSystem>();
        if (sys != SystemHandle.Null)
            wu.ResolveSystemStateRef(sys).Enabled = !on;
    }
}

//================================================================================//

[DisableAutoCreation]
[UpdateAfter(typeof(FillAnimationsFromControllerSystem))]
[UpdateBefore(typeof(AnimationProcessSystem))]
partial struct OverrideAvatarMaskSystem: ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        ss.Dependency.Complete();
        var atpBufLookup = SystemAPI.GetBufferLookup<AnimationToProcessComponent>();
        foreach (var (_, e) in SystemAPI.Query<RigDefinitionComponent>().WithAll<AnimationToProcessComponent>().WithEntityAccess())
        {
            var atps = atpBufLookup[e];
            for (var i = 0; i < atps.Length; ++i)
            {
                var atp = atps[i];
                atp.avatarMask = BlobAssetReference<AvatarMaskBlob>.Null;
                atps[i] = atp;
            }
        }
    }
}
}

