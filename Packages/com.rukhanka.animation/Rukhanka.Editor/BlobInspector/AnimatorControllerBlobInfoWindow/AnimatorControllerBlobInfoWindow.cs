using Rukhanka;
using Rukhanka.Editor;
using Rukhanka.Toolbox;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhaka.Editor
{
public class AnimatorControllerBlobInfoWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset = default;
    [SerializeField]
    private VisualTreeAsset controllerParameterBlobInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset entityRefAsset = default;
    [SerializeField]
    private VisualTreeAsset layerBlobInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset stateBlobInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset transitionBlobInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset conditionBlobInfoAsset = default;
    
    internal static BlobInspector.BlobAssetInfo controllerBlob;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var doc = visualTreeAsset.Instantiate();
        root.Add(doc);
        
        titleContent = new GUIContent("Rukhanka.Animation Controller Blob Info");
        FillBlobInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    unsafe void FillBlobInfo()
    {
        ref var b = ref controllerBlob.blobAsset.Reinterpret<ControllerBlob>().Value;
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
        sizeLabel.text = CommonTools.FormatMemory(controllerBlob.blobAsset.m_data.Header->Length);
        
        ref var blobParameters = ref b.parameters;
        var parametersFoldout = rootVisualElement.Q<Foldout>("parametersFoldout");
        parametersFoldout.text = $"{blobParameters.Length} Controller Parameters";
        
        //  Fill parameters
        for (var i = 0; i < blobParameters.Length; ++i)
        {
            ref var p = ref blobParameters[i];
            var parameterUIEntry = controllerParameterBlobInfoAsset.Instantiate();
            
            if (i % 2 == 0)
                parameterUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            var nameLabelP = parameterUIEntry.Q<Label>("nameLabel");
            var hashLabelP = parameterUIEntry.Q<Label>("hashLabel");
            var typeLabelP = parameterUIEntry.Q<Label>("typeLabel");
            var defaultValueLabelP = parameterUIEntry.Q<Label>("defaultValueLabel");
            
        #if RUKHANKA_DEBUG_INFO
            nameLabelP.text = p.name.ToString();
        #else
            nameLabelP.text = "-";
        #endif
            hashLabelP.text = p.hash.ToString();
            typeLabelP.text = p.type.ToString();
            defaultValueLabelP.text = GetParameterValueAsString(ref p);
            
            parametersFoldout.Add(parameterUIEntry);
        }
        
        //  Referenced entities view
        var relatedEntitiesView = rootVisualElement.Q("relatedEntitiesView");
        var relatedEntitieLabel = rootVisualElement.Q<Label>("relatedEntitiesLabel");
        AnimatorClipBlobInfoWindow.PopulateReferencedEntities(relatedEntitiesView, relatedEntitieLabel, controllerBlob.refEntities, entityRefAsset);
        
        FillControllerLayersList(ref b);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillControllerLayersList(ref ControllerBlob cb)
    {
        var layersFoldout = rootVisualElement.Q<Foldout>("layersFoldout");
        layersFoldout.text = $"{cb.layers.Length} Layers";
        
        for (var i = 0; i < cb.layers.Length; ++i)
        {
            ref var layer = ref cb.layers[i];
            var uiEntry = layerBlobInfoAsset.Instantiate();
            var rootFoldout = uiEntry.Q<Foldout>("layerInfoFoldout");
            var blendModeLabel = uiEntry.Q<Label>("blendModeLabel");
            var defaultStateLabel = uiEntry.Q<Label>("defaultStateLabel");
            var initialWeightLabel = uiEntry.Q<Label>("initialWeightLabel");
            var avatarMaskHashLabel = uiEntry.Q<Label>("avatarMaskHashLabel");
            
            rootFoldout.text = $"[{i}]";
        #if RUKAHKA_DEBUG_INFO
            rootFoldout.text += $"' {layer.name.ToString()}'";
            if (layer.defaultStateIndex >= 0)
                defaultStateLabel.text = layer.states[layer.defaultStateIndex].name.ToString();
        #else
            defaultStateLabel.text = $"[{layer.defaultStateIndex.ToString()}]";
        #endif
            blendModeLabel.text = layer.blendingMode.ToString();
            initialWeightLabel.text = layer.initialWeight.ToString();
            avatarMaskHashLabel.text = layer.avatarMaskBlobHash.ToString();
            
            FillLayerStatesInfo(ref layer, ref cb, uiEntry);
            
            layersFoldout.Add(uiEntry);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillLayerStatesInfo(ref LayerBlob lb, ref ControllerBlob cb, VisualElement root)
    {
        var foldout = root.Q<Foldout>("statesFoldout");
        foldout.text = $"{lb.states.Length} States";
        
        for (var i = 0; i < lb.states.Length; ++i)
        {
            ref var state = ref lb.states[i];
            var uiEntry = stateBlobInfoAsset.Instantiate();
            var rootFoldout = uiEntry.Q<Foldout>("stateInfoFoldout");
            var nameHashLabel = uiEntry.Q<Label>("nameHashLabel");
            var tagLabel = uiEntry.Q<Label>("tagLabel");
            var tagHashLabel = uiEntry.Q<Label>("tagHashLabel");
            var speedLabel = uiEntry.Q<Label>("speedLabel");
            var speedParamIdxLabel = uiEntry.Q<Label>("speedParamIdxLabel");
            var timeParamIdxLabel = uiEntry.Q<Label>("timeParamIdxLabel");
            var cycleOffsetLabel  = uiEntry.Q<Label>("cycleOffsetLabel");
            var cycleOffsetParamIdxLabel  = uiEntry.Q<Label>("cycleOffsetParamIdxLabel");
            
            rootFoldout.text = $"[{i}]";
            nameHashLabel.text = state.hash.ToString();
        #if RUKHANKA_DEBUG_INFO
            rootFoldout.text += $" '{state.name.ToString()}'";
            tagLabel.text = state.tag.ToString();
        #else
            tagLabel.text = "-";
        #endif
            tagHashLabel.text = state.tagHash.ToString();
            speedLabel.text = state.speed.ToString();
            speedParamIdxLabel.text = $"[{state.speedMultiplierParameterIndex}]";
        #if RUKHANKA_DEBUG_INFO
            if (state.speedMultiplierParameterIndex >= 0)
                speedParamIdxLabel.text += $" '{cb.parameters[state.speedMultiplierParameterIndex].name.ToString()}'";
        #endif
            timeParamIdxLabel.text = $"[{state.timeParameterIndex}]";
        #if RUKHANKA_DEBUG_INFO
            if (state.timeParameterIndex >= 0)
                timeParamIdxLabel.text += $" '{cb.parameters[state.timeParameterIndex].name.ToString()}'";
        #endif
            cycleOffsetLabel.text = state.cycleOffset.ToString();
            cycleOffsetParamIdxLabel.text = $"[{state.cycleOffsetParameterIndex}]";
        #if RUKHANKA_DEBUG_INFO
            if (state.cycleOffsetParameterIndex >= 0)
                cycleOffsetParamIdxLabel.text = $" '{cb.parameters[state.cycleOffsetParameterIndex].name.ToString()}'";
        #endif
            
            FillTransitionsInfo(ref state, ref lb, ref cb, uiEntry);
            
            foldout.Add(uiEntry);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillTransitionsInfo(ref StateBlob sb, ref LayerBlob lb, ref ControllerBlob cb, VisualElement root)
    {
        var foldout = root.Q<Foldout>("transitionsFoldout");
        foldout.text = $"{sb.transitions.Length} Transitions";
        
        for (var i = 0; i < sb.transitions.Length; ++i)
        {
            ref var tr = ref sb.transitions[i];
            var uiEntry = transitionBlobInfoAsset.Instantiate();
            var rootFoldout = uiEntry.Q<Foldout>("transitionInfoFoldout");
            var nameHashLabel = uiEntry.Q<Label>("nameHashLabel");
            var targetStateIDLabel = uiEntry.Q<Label>("targetStateIDLabel");
            var durationLabel = uiEntry.Q<Label>("durationLabel");
            var exitTimeLabel = uiEntry.Q<Label>("exitTimeLabel");
            var offsetLabel = uiEntry.Q<Label>("offsetLabel");
            var hasExitTimeCheckbox = uiEntry.Q<Toggle>("hasExitTimeCheckbox");
            var hasFixedDurationCheckbox = uiEntry.Q<Toggle>("hasFixedDurationCheckbox");
            
            nameHashLabel.text = tr.hash.ToString();
            targetStateIDLabel.text = $"[{tr.targetStateId.ToString()}]";
            rootFoldout.text = $"[{i}]";
        #if RUKHANKA_DEBUG_INFO
            rootFoldout.text += $" '{tr.name.ToString()}'";
            if (tr.targetStateId >= 0)
                targetStateIDLabel.text += $" '{lb.states[tr.targetStateId].name.ToString()}'";
        #endif
            durationLabel.text = tr.duration.ToString();
            exitTimeLabel.text = tr.exitTime.ToString();
            offsetLabel.text = tr.offset.ToString();
            hasExitTimeCheckbox.value = tr.hasExitTime;
            hasExitTimeCheckbox.SetEnabled(false);
            hasFixedDurationCheckbox.value = tr.hasFixedDuration;
            hasFixedDurationCheckbox.SetEnabled(false);
            
            FillConditionsInfo(ref tr, ref cb, uiEntry);
            
            foldout.Add(uiEntry);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillConditionsInfo(ref TransitionBlob tb, ref ControllerBlob cb, VisualElement root)
    {
        var foldout = root.Q<Foldout>("conditionsFoldout");
        foldout.text = $"{tb.conditions.Length} Conditions";
        
        for (var i = 0; i < tb.conditions.Length; ++i)
        {
            ref var conditionBlob = ref tb.conditions[i];
            var uiEntry = conditionBlobInfoAsset.Instantiate();
            var conditionLabel = uiEntry.Q<Label>("conditionLabel");
            conditionLabel.text = BuildConditionString(ref conditionBlob, ref cb.parameters);
            
            foldout.Add(uiEntry);
        }
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    string BuildConditionString(ref ConditionBlob cb, ref BlobArray<ParameterBlob> pb)
    {
        ref var param = ref pb[cb.paramIdx];
    #if RUKHANKA_DEBUG_INFO
        var paramString = $"'{param.name.ToString()}'";
    #else
        var paramString = $"'{param.hash.ToString()}'";
    #endif
        
        var opString = "";
        switch (cb.conditionMode)
        {
        case AnimatorConditionMode.Equals: opString = " =="; break;
        case AnimatorConditionMode.Greater: opString = " >"; break;
        case AnimatorConditionMode.If: opString = " == true"; break;
        case AnimatorConditionMode.Less: opString = " <"; break;
        case AnimatorConditionMode.IfNot: opString = " == false"; break;
        case AnimatorConditionMode.NotEqual: opString = " !="; break;
        }
        
        var thresholdString = "";
        switch (param.type)
        {
        case ControllerParameterType.Float: thresholdString = " " + cb.threshold.floatValue; break;
        case ControllerParameterType.Int: thresholdString = " " + cb.threshold.intValue; break;
        }
        
        var rv = paramString + opString + thresholdString;
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    string GetParameterValueAsString(ref ParameterBlob pb) => pb.type switch
    {
        ControllerParameterType.Bool => pb.defaultValue.boolValue ? "true" : "false",
        ControllerParameterType.Trigger => pb.defaultValue.boolValue ? "true" : "false",
        ControllerParameterType.Float => pb.defaultValue.floatValue.ToString(),
        ControllerParameterType.Int => pb.defaultValue.intValue.ToString(),
        _ => ""
    };
}
}