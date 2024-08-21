using Unity.Entities;
using UnityEngine;



//skinned mesh not compatible currently
//MonoBehaviour Only
public struct ShaderComponent : IComponentData
{
    public bool active;
}


public class ShaderComponentAuthoring : MonoBehaviour
{
    public SkinnedMeshRenderer leftEyeMeshRenderer;
    public SkinnedMeshRenderer rightEyeMeshRenderer;
    float offsetValue = 0;
    float direction = 1;
    [SerializeField] float eyeSpeed = .25f;
    [SerializeField] float offsetMin = -1;
    [SerializeField] float offsetMax = 1;
    void Start()
    {
        //material = GetComponent<SkinnedMeshRenderer>().material;
        //mesh = GetComponent<MeshFilter>().mesh;
        //mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;

        //meshRenderer = GetComponent<SkinnedMeshRenderer>();
        //Debug.Log("mesh " + mesh);
        //Debug.Log("mat " + material);
    }


    void Update()
    {
        offsetValue += Time.deltaTime * eyeSpeed * direction;
        if (offsetValue <= offsetMin || offsetValue >= offsetMax)
        {
            direction = -direction;
            offsetValue += Time.deltaTime * eyeSpeed * direction;
        }
        if (leftEyeMeshRenderer != null) leftEyeMeshRenderer.material.SetVector("_Offset", new Vector2(offsetValue, 0));
        if (rightEyeMeshRenderer != null) rightEyeMeshRenderer.material.SetVector("_Offset", new Vector2(offsetValue, 0));

        //material.SetTexture()
        // Animates main texture scale in a funky way!
        //float scaleX = Mathf.Cos(Time.time) * 0.5f + 1;
        //float scaleY = Mathf.Sin(Time.time) * 0.5f + 1;
        //scaleX = 4f;
        //scaleY = 4f;
        //material.SetTextureScale("_BaseMap", new Vector2(scaleX, scaleY));


    }

    //public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    //{
    //  dstManager.AddComponentData(entity, new ShaderComponent() { active = active });
    //        dstManager.SetSharedComponentData(entity, new RenderMesh() { mesh = mesh, material = material });
    //
    //  }
}
