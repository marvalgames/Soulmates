using UnityEngine;
using Unity.Assertions;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class IKSampleConf: MonoBehaviour
{
    public Slider aimIKWeightSlider;
    
    public Slider overrideIKPosWeightSlider;
    public Slider overrideIKRotWeightSlider;
    
    public Slider fabrikLeftLegWeightSlider;
    public Slider fabrikRightHandWeightSlider;
    public Slider fabrikSnakeWeightSlider;
    
    public Slider twoBoneLeftLegWeightSlider;
    public Slider twoBoneRightLegWeightSlider;
    
    public static IKSampleConf Instance { get; private set; }
    
/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
}
}
