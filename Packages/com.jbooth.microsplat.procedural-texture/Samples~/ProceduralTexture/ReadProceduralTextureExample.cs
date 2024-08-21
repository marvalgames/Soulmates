using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if __MICROSPLAT_PROCTEX__
[ExecuteInEditMode]
public class ReadProceduralTextureExample : MonoBehaviour
{
   public JBooth.MicroSplat.MicroSplatProceduralTextureConfig proceduralConfig;
   public JBooth.MicroSplat.MicroSplatKeywords keywords;


   int lastHit = -1;

   void Update ()
   {
      if (proceduralConfig == null)
      {
         return;
      }
      RaycastHit hit;
      Ray ray = new Ray (transform.position, Vector3.down);

      if (Physics.Raycast(ray, out hit))
      {
         var terrain = hit.collider.GetComponent<Terrain> ();
         if (terrain == null)
            return;

         var mat = terrain.materialTemplate;
         if (mat == null)
            return;

         Vector2 uv = hit.textureCoord;
         Vector3 worldPos = hit.point;
         Vector3 worldNormal = hit.normal;
         Vector3 up = Vector3.up;
         var noiseUVMode = JBooth.MicroSplat.MicroSplatProceduralTextureUtil.NoiseUVMode.World;
         if (keywords.IsKeywordEnabled("_PCNOISEUV"))
         {
            noiseUVMode = JBooth.MicroSplat.MicroSplatProceduralTextureUtil.NoiseUVMode.UV;
         }
         else if (keywords.IsKeywordEnabled("_PCNOISETRIPLANAR"))
         {
            noiseUVMode = JBooth.MicroSplat.MicroSplatProceduralTextureUtil.NoiseUVMode.Triplanar;
         }

         // you'd need to grab this from the keywords file..
         Vector4 weights;
         JBooth.MicroSplat.MicroSplatProceduralTextureUtil.Int4 indexes;

         JBooth.MicroSplat.MicroSplatProceduralTextureUtil.Sample (
            uv,
            worldPos,
            worldNormal,
            up,
            noiseUVMode,
            mat,
            proceduralConfig,
            out weights,
            out indexes);

         if (indexes.x != lastHit)
         {
            Debug.Log ("PC Texture Index : (" + indexes.x + ", " + indexes.y + ", " + indexes.z + ", " + indexes.z + ")      " + weights);
            lastHit = indexes.x;
         }
         
      }
	}
}
#endif
