using System.Collections.Generic;
using Rukhanka.Toolbox;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class AnimatorClipBlobInfoWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset = default;
    [SerializeField]
    private VisualTreeAsset boneClipBlobInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset animationEventInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset entityRefAsset = default;
    
    internal static BlobInspector.BlobAssetInfo animationClipBlob;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var doc = visualTreeAsset.Instantiate();
        root.Add(doc);
        
        titleContent = new GUIContent("Rukhanka.Animation Clip Blob Info");
        FillBlobInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    unsafe void FillBlobInfo()
    {
        ref var b = ref animationClipBlob.blobAsset.Reinterpret<AnimationClipBlob>().Value;
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
        
        var lengthLabel = rootVisualElement.Q<Label>("lengthLabel");
        lengthLabel.text = $"{b.length:F3} sec";
        
        var cycleOffsetLabel = rootVisualElement.Q<Label>("cycleOffsetLabel");
        cycleOffsetLabel.text = $"{b.cycleOffset}";
        
        var additivePoseTimeLabel = rootVisualElement.Q<Label>("additivePoseTimeLabel");
        additivePoseTimeLabel.text = $"{b.additiveReferencePoseTime}";
        
        var loopedCheckBox = rootVisualElement.Q<Toggle>("loopedCheckBox");
        loopedCheckBox.SetEnabled(false);
        loopedCheckBox.value = b.looped;
        
        var loopPoseBlendCheckBox = rootVisualElement.Q<Toggle>("loopPoseBlendCheckBox");
        loopPoseBlendCheckBox.SetEnabled(false);
        loopPoseBlendCheckBox.value = b.loopPoseBlend;
        
        
        var sizeLabel = rootVisualElement.Q<Label>("sizeLabel");
        sizeLabel.text = CommonTools.FormatMemory(animationClipBlob.blobAsset.m_data.Header->Length);
        
        FillBoneClipFoldout("boneClipInfoFoldout", "Bone Clips", ref b.bones);
        FillBoneClipFoldout("userCurvesInfoFoldout", "User Curves", ref b.curves);
        
        ref var events = ref b.events;
        var animationEventsInfoFoldout = rootVisualElement.Q<Foldout>("animationEventsInfoFoldout");
        animationEventsInfoFoldout.text = $"{events.Length} Animation Events";
        animationEventsInfoFoldout.Add(animationEventInfoAsset.Instantiate());
        for (var i = 0; i < events.Length; ++i)
        {
            var eventInfo = animationEventInfoAsset.Instantiate();
            
            if (i % 2 == 0)
                eventInfo.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            ref var evt = ref events[i];
            var hashLabelE = eventInfo.Q<Label>("hashLabel");
            var nameLabelE = eventInfo.Q<Label>("nameLabel");
            var timeLabelE = eventInfo.Q<Label>("timeLabel");
            var floatParamLabelE = eventInfo.Q<Label>("floatParamLabel");
            var intParamLabelE = eventInfo.Q<Label>("intParamLabel");
            var stringParamTabelE = eventInfo.Q<Label>("stringParamLabel");
            var stringHashParamTabelE = eventInfo.Q<Label>("stringHashParamLabel");
        
        #if RUKHANKA_DEBUG_INFO
            nameLabelE.text = evt.name.ToString();
            stringParamTabelE.text = evt.stringParam.ToString();
        #else
            nameLabelE.text = "-";
            stringParamTabelE.text = "-";
        #endif
            hashLabelE.text = evt.nameHash.ToString();
            timeLabelE.text = evt.time.ToString();
            floatParamLabelE.text = evt.floatParam.ToString();
            intParamLabelE.text = evt.intParam.ToString();
            stringHashParamTabelE.text = evt.stringParamHash.ToString();
            animationEventsInfoFoldout.Add(eventInfo);
        }
        
        var relatedEntitiesView = rootVisualElement.Q("relatedEntitiesView");
        var relatedEntitieLabel = rootVisualElement.Q<Label>("relatedEntitiesLabel");
        PopulateReferencedEntities(relatedEntitiesView, relatedEntitieLabel, animationClipBlob.refEntities, entityRefAsset);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    internal static void PopulateReferencedEntities(VisualElement container, Label containerLabel, List<Entity> entities, VisualTreeAsset entryAsset)
    {
        containerLabel.text = $"{entities.Count} Related Entities";
        for (var i = 0; i < entities.Count; ++i)
        {
            var e = entities[i];
            var element = entryAsset.Instantiate();
            var entityIDLabel = element.Q<Label>("entityIDLabel");
            var nameLabel = element.Q<Label>("nameLabel");
            
            entityIDLabel.text = e.ToString();
            if (BlobInspector.currentWorld != null)
                nameLabel.text = BlobInspector.currentWorld.EntityManager.GetName(e);
            
            container.Add(element);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    void FillBoneClipFoldout(string foldoutName, string caption, ref BlobArray<BoneClipBlob> bones)
    {
        var boneClipInfoFoldout = rootVisualElement.Q<Foldout>(foldoutName);
        boneClipInfoFoldout.text = $"{bones.Length} {caption}";
        
        for (int i = 0; i < bones.Length; ++i)
        {
            ref var bone = ref bones[i];
            var clipInfo = boneClipBlobInfoAsset.Instantiate();
            
            if (i % 2 == 0)
                clipInfo.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            var hashLabelCI = clipInfo.Q<Label>("hashLabel");
            var nameLabelCI = clipInfo.Q<Label>("nameLabel");
            var curveInfoLabelCI = clipInfo.Q<Label>("curveInfoLabel");
            
            hashLabelCI.text = $"{bone.hash}";
        #if RUKHANKA_DEBUG_INFO
            nameLabelCI.text = $"{bone.name.ToString()}";
        #else
            nameLabelCI.text = "-";
        #endif
            
            var totalKeyframes = 0;
            for (var k = 0; k < bone.animationCurves.Length; ++k)
            {
                totalKeyframes += bone.animationCurves[k].keyFrames.Length;
            }
            
            curveInfoLabelCI.text = $"{bone.animationCurves.Length} curves, {totalKeyframes} keyframes";
            
            boneClipInfoFoldout.Add(clipInfo);
        }
    }
}
}
