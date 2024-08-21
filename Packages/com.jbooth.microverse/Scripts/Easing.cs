using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Easing
{
    public enum BlendShape
    {
        Linear,
        Smoothstep,
        EaseIn,
        EaseOut,
        EaseInOut
    }
    public BlendShape blend = BlendShape.Linear;

    public void PrepareMaterial(Material mat, string key, List<string> keywords)
    {
        switch (blend)
        {
            case BlendShape.Smoothstep:
                keywords.Add(key + "SMOOTHSTEP");
                break;
            case BlendShape.EaseIn:
                keywords.Add(key + "EASEIN");
                break;
            case BlendShape.EaseOut:
                keywords.Add(key + "EASEOUT");
                break;
            case BlendShape.EaseInOut:
                keywords.Add(key + "EASEINOUT");
                break;

        }
    }
}
