#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif
using UnityEditor;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
[DisableAutoCreation]
[UpdateAfter(typeof(FillAnimationsFromControllerSystem))]
public partial class AnimationCullingContextUpdateSystem: SystemBase
{
    internal struct CullingCameraData
    {
        public float3 pos;
        public float3 viewDir;
        public float4x4 cullingMatrix;
        public float fieldOfView;
        public bool isOrthographic;
        public float orthographicSize;
    }
    
    internal struct LightData
    {
        public float4 posOrDir;
        public float range;
    }
    
/////////////////////////////////////////////////////////////////////////////////

    protected override void OnCreate()
    {
    #if RUKHANKA_WITH_NETCODE
		bool isServer =  World.IsServer();
        if (isServer)
            Enabled = false;
    #endif
    }

/////////////////////////////////////////////////////////////////////////////////

    protected override void OnUpdate()
    {
        var acc = AnimationCullingConfig.Instance;
        if (acc == null)
            return;
        
        if (!SystemAPI.TryGetSingletonRW<AnimationCullingContext>(out var actx))
        {
            var newContext = new AnimationCullingContext();
            newContext.cullingPlanes = new (32, Allocator.Persistent);
            newContext.cullingVolumePlaneRanges = new (32, Allocator.Persistent);
            newContext.lodAffectors = new (32, Allocator.Persistent);
            EntityManager.CreateSingleton(newContext, "Rukhanka.AnimationCullingContextSingleton");
            actx = SystemAPI.GetSingletonRW<AnimationCullingContext>();
        }
        
        var cullingCameras = new NativeList<CullingCameraData>(acc.cullingCameras.Length, CheckedStateRef.WorldUpdateAllocator);
        foreach (var cullingCam in acc.cullingCameras)
        {
            AddCullingCamera(cullingCam, ref cullingCameras);
        }

        var shadowCastingLights = new NativeList<LightData>(acc.shadowCastingLights.Length, CheckedStateRef.WorldUpdateAllocator);
        foreach (var l in acc.shadowCastingLights)
        {
            AddShadowCastingLight(l, ref shadowCastingLights);
        }
        
    #if UNITY_EDITOR
        if (acc.addEditorSceneCamera)
        {
            var sceneCam = SceneView.lastActiveSceneView?.camera;
            AddCullingCamera(sceneCam, ref cullingCameras);
        }
    #endif
        
        Dependency = BuildCullingContext(cullingCameras, shadowCastingLights, ref actx.ValueRW, acc, Dependency);
    }
    
/////////////////////////////////////////////////////////////////////////////////

    void AddShadowCastingLight(Light l, ref NativeList<LightData> shadowCastingLights)
    {
        if (l == null || l.shadows == LightShadows.None || !l.isActiveAndEnabled) return;
        
        var lt = l.transform;
        var ld = new LightData()
        {
            posOrDir = l.type == LightType.Directional ? new float4(-lt.forward, 0) : new float4(lt.position, 1),
            range = l.range,
        };
        shadowCastingLights.Add(ld);
    }

/////////////////////////////////////////////////////////////////////////////////

    void AddCullingCamera(Camera cam, ref NativeList<CullingCameraData> cullingCameras)
    {
        if (cam == null) return;
        
        var camTransform = cam.transform;
        var ccd = new CullingCameraData()
        {
            pos = camTransform.position,
            viewDir = camTransform.forward,
            cullingMatrix = cam.cullingMatrix,
            fieldOfView = cam.fieldOfView,
            isOrthographic = cam.orthographic,
            orthographicSize = cam.orthographicSize
        };
        cullingCameras.Add(ccd);
    }

/////////////////////////////////////////////////////////////////////////////////

    JobHandle BuildCullingContext(NativeList<CullingCameraData> cullingCameras, NativeList<LightData> shadowCastingLights, ref AnimationCullingContext actx, AnimationCullingConfig acc, JobHandle dependsOn)
	{
    #if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
        SystemAPI.TryGetSingletonRW<Drawer>(out var dd);
    #endif
        
	    var buildCullingContextJob = new BuildCullingContextJob()
        {
            cullingCameras = cullingCameras.AsArray(),
            shadowCastingLights = shadowCastingLights.AsArray(),
            lodBias = QualitySettings.lodBias,
            actx = actx,
    #if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
            dd = dd.ValueRW,
    #endif
        };
        
        var rv = buildCullingContextJob.Schedule(dependsOn);
        
    #if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
        actx.invisibleChunkColor = Drawer.ColorToUINT(acc.invisibleChunkColor);
        actx.visibleChunkColor = Drawer.ColorToUINT(acc.visibleChunkColor);
        actx.invisibleRendererColor = Drawer.ColorToUINT(acc.invisibleRendererColor);
        actx.visibleRendererColor = Drawer.ColorToUINT(acc.visibleRendererColor);
        actx.drawSceneBoundingBoxes = acc.drawSceneBoundingBoxes;
        
        actx.drawCullingVolumes = acc.drawCullingVolumes;
        actx.cullingVolumeColor = Drawer.ColorToUINT(acc.cullingVolumeColor);
    #endif
        return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////

    protected override void OnDestroy()
    {
        if (SystemAPI.TryGetSingleton<AnimationCullingContext>(out var actx))
        {
            actx.Dispose();
			EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<AnimationCullingContext>());
        }
    }
}
}
