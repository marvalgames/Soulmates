using Unity.Entities;
using Unity.Rendering;
using UnityEngine;


//skinned mesh not compatible currently
//MonoBehaviour Only
[RequireMatchingQueriesForUpdate]
public partial class ShaderSystem : SystemBase
{
    protected override void OnUpdate()
    {

        Entities.WithoutBurst().ForEach( (Entity e, in RenderMesh renderMesh,  in ShaderComponent shaderComponent) =>
        {
            Debug.Log("renderMesh " + renderMesh);
            Debug.Log("renderMesh " + renderMesh.mesh);
            Debug.Log("renderMesh " + renderMesh.material);

        }
        ).Run();
        
    }













}

