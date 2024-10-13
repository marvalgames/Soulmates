using Rukhanka;
using Rukhanka.Editor;
using Rukhanka.Toolbox;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhaka.Editor
{
public class RigBlobInfoWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset = default;
    
    [SerializeField]
    private VisualTreeAsset boneInfoAsset = default;
    
    [SerializeField]
    private VisualTreeAsset humanBoneInfoAsset = default;
    
    [SerializeField]
    private VisualTreeAsset entityRefAsset = default;

    internal static BlobInspector.BlobAssetInfo rigBlob;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var doc = visualTreeAsset.Instantiate();
        root.Add(doc);
        
        titleContent = new GUIContent("Rukhanka.Animation Rig Info");
        FillRigInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    unsafe void FillRigInfo()
    {
        ref var b = ref rigBlob.blobAsset.Reinterpret<RigDefinitionBlob>().Value;
        var hashLabel = rootVisualElement.Q<Label>("hashLabel");
        hashLabel.text = b.hash.ToString();
        
        var nameLabel = rootVisualElement.Q<Label>("nameLabel");
        var bakingTimeLabel = rootVisualElement.Q<Label>("bakingTimeLabel");
    #if RUKHANKA_DEBUG_INFO
        nameLabel.text = b.name.ToString();
        bakingTimeLabel.text = $"{b.bakingTime:F3} sec";
    #else
        nameLabel.text = "-";
        bakingTimeLabel.text = "-";
    #endif
        
        var sizeLabel = rootVisualElement.Q<Label>("sizeLabel");
        sizeLabel.text = CommonTools.FormatMemory(rigBlob.blobAsset.m_data.Header->Length);
        
        ref var bones = ref b.bones;
        var rootBoneIndexLabel = rootVisualElement.Q<Label>("rootBoneIndexLabel");
        rootBoneIndexLabel.text = $"[{b.rootBoneIndex.ToString()}]";
    #if RUKHANKA_DEBUG_INFO
        if (b.rootBoneIndex >= 0)
            rootBoneIndexLabel.text += $" {bones[b.rootBoneIndex].name.ToString()}";
    #endif
        
        //  Fill bone data
        var bonesFoldout = rootVisualElement.Q<Foldout>("bonesFoldout");
        bonesFoldout.text = $"{bones.Length} Bones";
        
        bonesFoldout.Add(boneInfoAsset.Instantiate());
        for (var i = 0; i < bones.Length; ++i)
        {
            ref var bone = ref bones[i];
            var boneInfoUIEntry = boneInfoAsset.Instantiate();
            if (i % 2 == 0)
                boneInfoUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            var nameHashLabelB = boneInfoUIEntry.Q<Label>("nameHashLabel");
            var nameLabelB = boneInfoUIEntry.Q<Label>("nameLabel");
            var parentBoneIndexLabelB = boneInfoUIEntry.Q<Label>("parentBoneIndexLabel");
            //var refPoseLabelB = boneInfoUIEntry.Q<Label>("refPoseLabel");
            var humanBodyPartLabelB = boneInfoUIEntry.Q<Label>("humanBodyPartLabel");
            
            nameHashLabelB.text = bone.hash.ToString();
            parentBoneIndexLabelB.text = $"[{bone.parentBoneIndex}]";
        #if RUKHANKA_DEBUG_INFO
            nameLabelB.text = bone.name.ToString();
            if (bone.parentBoneIndex >= 0)
                parentBoneIndexLabelB.text += $" {bones[bone.parentBoneIndex].name.ToString()}";
        #else
            nameLabelB.text = "-";
        #endif
            //refPoseLabelB.text = GetFormattedBoneTransformString(bone.refPose);
            humanBodyPartLabelB.text = bone.humanBodyPart.ToString();
            
            bonesFoldout.Add(boneInfoUIEntry);
        }
        
        // Fill human data
        var humanDataFoldout = rootVisualElement.Q<Foldout>("humanBodyPartsFoldout");
        humanDataFoldout.text = "0 Human Bones";
        if (b.humanData.IsValid)
        {
            humanDataFoldout.text = $"{b.humanData.Value.humanBoneToSkeletonBoneIndices.Length} Human Bones";
            
            ref var hbi = ref b.humanData.Value.humanBoneToSkeletonBoneIndices;
            ref var hrd = ref b.humanData.Value.humanRotData;
            humanDataFoldout.Add(humanBoneInfoAsset.Instantiate());
            for (var i = 0; i < hbi.Length; ++i)
            {
                var rigBoneIndex = hbi[i];
                var boneInfoUIEntry = humanBoneInfoAsset.Instantiate();
                if (i % 2 == 0)
                    boneInfoUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
                
                var humanBodyPartLabelB = boneInfoUIEntry.Q<Label>("humanBodyPartLabel");
                var rigBoneIndexLabelB = boneInfoUIEntry.Q<Label>("rigBoneIndexLabel");
                var minMuscleAnglesLabelB = boneInfoUIEntry.Q<Label>("minMuscleAnglesLabel");
                var maxMuscleAnglesLabelB = boneInfoUIEntry.Q<Label>("maxMuscleAnglesLabel");
                
                humanBodyPartLabelB.text = ((HumanBodyBones)i).ToString();
                rigBoneIndexLabelB.text = $"[{rigBoneIndex}]";
                if (rigBoneIndex >= 0)
                {
                #if RUKHANKA_DEBUG_INFO
                    rigBoneIndexLabelB.text += $" {bones[rigBoneIndex].name.ToString()}";
                #endif
                    minMuscleAnglesLabelB.text = GetFormattedHumanRotAngles(hrd[rigBoneIndex].minMuscleAngles);
                    maxMuscleAnglesLabelB.text = GetFormattedHumanRotAngles(hrd[rigBoneIndex].maxMuscleAngles);
                }
                else
                {
                    minMuscleAnglesLabelB.text = "-";
                    maxMuscleAnglesLabelB.text = "-";
                }
                
                humanDataFoldout.Add(boneInfoUIEntry);
            }
        }
        
        //  Referenced entities view
        var relatedEntitiesView = rootVisualElement.Q("relatedEntitiesView");
        var relatedEntitieLabel = rootVisualElement.Q<Label>("relatedEntitiesLabel");
        AnimatorClipBlobInfoWindow.PopulateReferencedEntities(relatedEntitiesView, relatedEntitieLabel, rigBlob.refEntities, entityRefAsset);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    string GetFormattedHumanRotAngles(float3 angles)
    {
        var rv = $"{math.degrees(angles.x):F1}; {math.degrees(angles.y):F1}; {math.degrees(angles.z):F1}";
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    string GetFormattedBoneTransformString(BoneTransform bt)
    {
        var rv = $"T: {bt.pos}, R: {bt.rot}, S: {bt.scale}";
        return rv;
    }
}
}