using UnityEngine;

namespace FIMSpace.Generating
{
    public abstract class TilePostFilterBase : ScriptableObject
    {
        [HideInInspector] public string PostEventInfo = "";

        /// <summary> Initial generating meshes out of defined tile design algorithms </summary>
        public virtual void OnOriginMeshGenerated(TileDesign design, TileMeshSetup meshSetup, TileDesign.PostFilterHelper helper)
        {

        }

        /// <summary> When categorizing each tile mesh instance </summary>
        public virtual void OnTileInstanceAssignment(TileDesign design, TileMeshSetup meshSetup, TileMeshSetup.TileMeshCombineInstance instance, TileDesign.PostFilterHelper helper)
        {

        }

        /// <summary> On combining one of the tiles. Combination is dictated by the material. Before cutting holes. </summary>
        public virtual void OnTilesCombined(TileDesign design, Material combinationMaterial, ref Mesh mesh, TileDesign.PostFilterHelper helper)
        {

        }

        /// <summary> On combining one of the tiles. Combination is dictated by the material. After cutting holes. </summary>
        //public virtual void OnTilesCombinedAfterBoole(TileDesign design, ref Mesh mesh, TileDesign.PostFilterHelper helper)
        //{

        //}

        /// <summary> On final iteration through all generated final meshes, after all operations. </summary>
        public virtual void OnFinalResult(ref Mesh mesh, Material combinationMaterial,TileDesign.PostFilterHelper helper)
        {

        }


#if UNITY_EDITOR
        /// <summary> [ Needs to be inside   !!!   #if UNITY_EDITOR #endif   !!! ] </summary>
        public virtual void Editor_DisplayGUI(TileDesign.PostFilterHelper helper)
        {
        }
#endif

    }
}