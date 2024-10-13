using System;
using Unity.Assertions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
class ScriptedAnimatorSampleConf: MonoBehaviour
{
    public Slider animationTimeSlider;
    public Slider weight;
    public float animationTime;
    public bool manualTimeControl;
    public int2 animationIndices;
    public bool doBlending;
    
    public static ScriptedAnimatorSampleConf Instance { get; private set; }
    
/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        Assert.IsNull(Instance);
        Instance = this;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void Update()
    {
        if (manualTimeControl)
        {
            animationTime = animationTimeSlider.value;
        }
        else
        {
            animationTime += Time.deltaTime;
            animationTimeSlider.value = math.frac(animationTime);
        }
    }
}
}
