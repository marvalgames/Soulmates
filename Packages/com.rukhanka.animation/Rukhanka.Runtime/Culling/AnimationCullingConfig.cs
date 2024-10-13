using System;
using UnityEngine;
using UnityEngine.Rendering;

#if HDRP_10_0_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#elif URP_10_0_0_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public class AnimationCullingConfig: MonoBehaviour
{
    [Tooltip("Add all cameras that should be used for visibility calculation")]
    public Camera[] cullingCameras;
    [Tooltip("Add all shadow casting lights for proper shadow occlusion calculation")]
    public Light[] shadowCastingLights;
    [Tooltip("Use editor scene view as culling camera")]
    public bool addEditorSceneCamera = true;
    
#if HDRP_10_0_0_OR_NEWER
    [HideInInspector]
    public HDShadowSettings shadowSettings;
#elif URP_10_0_0_OR_NEWER
    [HideInInspector]
    public UniversalRenderPipelineAsset urpAsset;
#endif
    
    public bool drawCullingVolumes;
    public Color cullingVolumeColor = Color.blue;
    public bool drawSceneBoundingBoxes;
    public Color visibleChunkColor = Color.green;
    public Color invisibleChunkColor = Color.red;
    public Color visibleRendererColor = Color.white;
    public Color invisibleRendererColor = Color.red;
    
    public static AnimationCullingConfig Instance { get; private set; }
    
/////////////////////////////////////////////////////////////////////////////////

    void Awake()
    {
        if (Instance != null)
            throw new Exception($"There is more then single AnimationCullingConfig in scene!");

        foreach (var l in shadowCastingLights)
        {
            if (l == null) continue;
            
            if (l.shadows == LightShadows.None)
                Debug.LogWarning($"Animation Culling Config: Light '{l.name}' does not casting shadows. It is meaningless to account it for culling calculation.");
        }
        
    #if HDRP_10_0_0_OR_NEWER
        var volumes = FindObjectsOfType<Volume>();
        foreach (var v in volumes)
        {
            if (!v.isGlobal) continue;
            if (v.sharedProfile.TryGet(out shadowSettings))
            {
                break;
            }
        }
    #elif URP_10_0_0_OR_NEWER
        urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
    #endif
        
        Instance = this;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void Update()
    {
        float shadowDistance = 0;
        
    #if HDRP_10_0_0_OR_NEWER
        if (shadowSettings == null)
            return;
        shadowDistance = shadowSettings.maxShadowDistance.value;
    #elif URP_10_0_0_OR_NEWER
        if (urpAsset == null)
            return;
        shadowDistance = urpAsset.shadowDistance;
    #endif
        
        foreach (var l in shadowCastingLights)
        {
            if (l == null || l.type != LightType.Directional) continue;
            //  Need to update shadow range according to shadow settings
            l.range = shadowDistance;
        }
    }

/////////////////////////////////////////////////////////////////////////////////

    void OnDestroy()
    {
        Instance = null;
    }
}
}
