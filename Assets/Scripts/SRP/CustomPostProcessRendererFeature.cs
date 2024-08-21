using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class CustomPostProcessRendererFeature : ScriptableRendererFeature
{
    [SerializeField]
    private Shader m_bloomShader;
    [SerializeField]
    private Shader m_compositeShader;
    [SerializeField]
    private Shader m_ssaoShader;

    public Texture2D[] m_BlueNoise256Textures;

    private Material m_bloomMaterial;
    private Material m_compositeMaterial;
    private Material m_ssaoMaterial;

    private CustomPostProcessPass m_customPass;
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(m_customPass);
        }
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_customPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            m_customPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_customPass.SetTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
        }
    }

    public override void Create()
    {
        m_bloomMaterial = CoreUtils.CreateEngineMaterial(m_bloomShader);
        m_compositeMaterial = CoreUtils.CreateEngineMaterial(m_compositeShader);
        m_customPass = new CustomPostProcessPass(m_bloomMaterial, m_compositeMaterial);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_bloomMaterial);
        CoreUtils.Destroy(m_compositeMaterial);
        CoreUtils.Destroy(m_ssaoMaterial);
    }
}
