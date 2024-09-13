
using Rukhanka.Hybrid;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public class ScriptedAnimatorSampleUsedAnimationsAuthoring: MonoBehaviour { }

//=================================================================================================================//

struct ScriptedAnimatorSampleUsedAnimationsComponent: IComponentData
{
    public FixedList512Bytes<Hash128> clips;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if UNITY_EDITOR
class ScriptedAnimatorSampleUsedAnimationsBaker: Baker<ScriptedAnimatorSampleUsedAnimationsAuthoring>
{
    public override void Bake(ScriptedAnimatorSampleUsedAnimationsAuthoring a)
    {
        var rigDef = a.GetComponent<RigDefinitionAuthoring>();
        var avatar = rigDef.GetAvatar();
        var e = GetEntity(a, TransformUsageFlags.None);
        var anms = a.GetComponent<AnimationAssetSetAuthoring>();
        var c = new ScriptedAnimatorSampleUsedAnimationsComponent();
        for (var i = 0; i < anms.animationClips.Length; ++i)
        {
            c.clips.Add(BakingUtils.ComputeAnimationHash(anms.animationClips[i], avatar));
        }
        AddComponent(e, c);
    }
}
#endif

}

