using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[Serializable]
public class MultiPassRendererFeature : ScriptableRendererFeature
{
    public List<string> lightModePasses;
    private MultiPassPass mainPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(mainPass);
    }

    public override void Create()
    {
        mainPass = new MultiPassPass(lightModePasses);
    }
}
