using Unity.Assertions;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class AnimCullingSampleConf: MonoBehaviour
{
    public Toggle enableAnimationCullingToggle;
    public Toggle enableRendererBBoxRecalculation;
    public Camera cullingCamera;
    
    public static AnimCullingSampleConf Instance { get; private set; }
    
/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
}
