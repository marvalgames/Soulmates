
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[HelpURL("https://docs.rukhanka.com/getting_started#rig-definition")]
public class RigDefinitionAuthoring: MonoBehaviour
{
    public enum BoneEntityStrippingMode
    {
        None,
        Automatic,
        Manual
    }
    
    public enum RigConfigSource
    {
        FromAnimator,
        UserDefined
    }

    public RigConfigSource rigConfigSource;
    public Avatar avatar;
    public bool applyRootMotion;
    public bool animationCulling;
    
    [Tooltip("<color=Cyan><b>None</b></color> - keep all skeleton bone entities.\n<color=Cyan><b>Automatic</b></color> - automatically strip unreferenced bone entities.\n<color=Cyan><b>Manual</b></color> - included and stripped bone entities will be taken from specified avatar mask. This mode will make 'flat' bone hierarchy.")]
    public BoneEntityStrippingMode boneEntityStrippingMode;
    public AvatarMask boneStrippingMask;
    public bool hasAnimationEvents;
    public bool hasAnimatorControllerEvents;
    
////////////////////////////////////////////////////////////////////////////////////////

    public Avatar GetAvatar()
    {
        var rv = avatar;
        if (rigConfigSource == RigConfigSource.FromAnimator)
        {
            var anm = GetComponent<Animator>();
            if (anm)
                rv = anm.avatar;
        }
        return rv;
    }
    
}
}
