using System;
using Rukhanka.Hybrid;
using UnityEditor;
using UnityEngine.UIElements;
    
////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[CustomEditor(typeof(AimIKAuthoring))]
public class AimIKAuthoringEditor : UnityEditor.Editor
{
    public VisualTreeAsset inpectorXML;
    
////////////////////////////////////////////////////////////////////////////////////////

    public override VisualElement CreateInspectorGUI()
    {
        var myInspector = new VisualElement(); 
        inpectorXML.CloneTree(myInspector);
        var t = (AimIKAuthoring)target;
        var angleLimitMinSlider = (Slider)myInspector.Q("minAngleSlider");
        var angleLimitMaxSlider = (Slider)myInspector.Q("maxAngleSlider");
        angleLimitMinSlider.RegisterValueChangedCallback((newVal) => { t.angleLimitMax = Math.Max(t.angleLimitMax, newVal.newValue); });
        angleLimitMaxSlider.RegisterValueChangedCallback((newVal) => { t.angleLimitMin = Math.Min(newVal.newValue, t.angleLimitMin); });
        
        return myInspector;
    }
}
}
