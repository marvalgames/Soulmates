using Rukhanka.Hybrid;
using UnityEditor;
using UnityEngine.UIElements;
    
////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
[CustomPropertyDrawer(typeof(WeightedTransform))]
public class WeightedTransformPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty p)
    {
        var myInspector = new VisualElement();
        var inpectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.rukhanka.animation/Rukhanka.Editor/UXML/WeightedTransformEditor.uxml");
        inpectorXML.CloneTree(myInspector);
        return myInspector;
    }
}
}
