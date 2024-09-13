using Rukhanka.Hybrid;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
    
////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[CustomEditor(typeof(RigDefinitionAuthoring))]
public class RigDefinitionAuthoringEditor : UnityEditor.Editor
{
    public VisualTreeAsset inspectorXML;
    VisualElement boneStrippingMaskElement;
    VisualElement rigUserConfigGroupElement;
    VisualElement inspector;
    
////////////////////////////////////////////////////////////////////////////////////////

    public override VisualElement CreateInspectorGUI()
    {
        inspector = new VisualElement(); 
        inspectorXML.CloneTree(inspector);
        
        boneStrippingMaskElement = inspector.Q("boneStrippingMask");
        boneStrippingMaskElement.TrackSerializedObjectValue(serializedObject, ShowOrHideBoneStrippingMask);
        ShowOrHideBoneStrippingMask(serializedObject);
        
        rigUserConfigGroupElement = inspector.Q("rigUserConfigGroup");
        rigUserConfigGroupElement.TrackSerializedObjectValue(serializedObject, ShowOrHideRigUserConfig);
        ShowOrHideRigUserConfig(serializedObject);
        return inspector;
    }
    
////////////////////////////////////////////////////////////////////////////////////////

    void ShowOrHideBoneStrippingMask(SerializedObject so)
    {
        //  Hide bone stripping mask for "None" and "Automatic" modes
        var strippingMode = serializedObject.FindProperty("boneEntityStrippingMode");
        var showBoneStrippingMask = (RigDefinitionAuthoring.BoneEntityStrippingMode)strippingMode.enumValueIndex == RigDefinitionAuthoring.BoneEntityStrippingMode.Manual;
        boneStrippingMaskElement.style.display = showBoneStrippingMask ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
////////////////////////////////////////////////////////////////////////////////////////

    void ShowOrHideRigUserConfig(SerializedObject so)
    {
        var m = serializedObject.FindProperty("rigConfigSource");
        var showUserConfig = (RigDefinitionAuthoring.RigConfigSource)m.enumValueIndex == RigDefinitionAuthoring.RigConfigSource.UserDefined;
        rigUserConfigGroupElement.style.display = showUserConfig ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
