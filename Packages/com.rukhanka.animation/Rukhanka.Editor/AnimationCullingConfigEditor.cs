using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
    
////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[CustomEditor(typeof(AnimationCullingConfig))]
public class AnimationCullingConfigEditor : UnityEditor.Editor
{
    public VisualTreeAsset inpectorXML;
    
////////////////////////////////////////////////////////////////////////////////////////

    public override VisualElement CreateInspectorGUI()
    {
        var myInspector = new VisualElement(); 
        inpectorXML.CloneTree(myInspector);
        
        var t = (AnimationCullingConfig)target;
        
    #if !RUKHANKA_DEBUG_INFO
        var debugAndVisualization = myInspector.Q<Foldout>("debugAndVisualization");
        debugAndVisualization.text += " (available only with 'RUKHANKA_DEBUG_INFO' defined)";
        debugAndVisualization.SetEnabled(false);
    #endif
        
        var drawCullingVolumesToggle = myInspector.Q<PropertyField>("drawCullingVolumes");
        var drawCullingVolumesChildren = myInspector.Q("drawCullingVolumesChildren");
        drawCullingVolumesToggle.RegisterValueChangeCallback((newVal) => { drawCullingVolumesChildren.SetEnabled(newVal.changedProperty.boolValue); });
        drawCullingVolumesChildren.SetEnabled(t.drawCullingVolumes);
        
        var drawBBTogle = myInspector.Q<PropertyField>("drawSceneBoundingBoxes");
        var drawBBChildren = myInspector.Q("drawSceneBoundingBoxesChildren");
        drawBBTogle.RegisterValueChangeCallback((newVal) => { drawBBChildren.SetEnabled(newVal.changedProperty.boolValue); });
        drawBBChildren.SetEnabled(t.drawSceneBoundingBoxes);
        
        return myInspector;
    }
}
}
