using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class TileDesign
    {
        bool UsePostFilters = false;
        void PostFilters_PreGenerateCheck()
        {
            if (PostFilters == null) { UsePostFilters = false; return; }
            FGenerators.CheckForNulls(PostFilters);
            if (PostFilters.Count == 0) { UsePostFilters = false; return; }
            UsePostFilters = true;
        }

        void PostFilters_AfterGeneratingOriginMesh(TileDesign tileDesign, TileMeshSetup tile)
        {
            if (!UsePostFilters) return;
            for (int i = 0; i < PostFilters.Count; i++)
            {
                if (PostFilters[i].PostFilter == null) continue;
                if (PostFilters[i].Enabled == false) continue;
                PostFilters[i].PostFilter.OnOriginMeshGenerated(tileDesign, tile, PostFilters[i]);
            }
        }

        void PostFilters_OnTileInstanceAssignment(TileDesign tileDesign, TileMeshSetup tile, TileMeshSetup.TileMeshCombineInstance inst)
        {
            if (!UsePostFilters) return;
            for (int i = 0; i < PostFilters.Count; i++)
            {
                if (PostFilters[i].PostFilter == null) continue;
                if (PostFilters[i].Enabled == false) continue;
                PostFilters[i].PostFilter.OnTileInstanceAssignment(tileDesign, tile, inst, PostFilters[i]);
            }
        }

        void PostFilters_OnTilesCombined(TileDesign tileDesign, Material mat, ref Mesh mesh)
        {
            if (!UsePostFilters) return;
            for (int i = 0; i < PostFilters.Count; i++)
            {
                if (PostFilters[i].PostFilter == null) continue;
                if (PostFilters[i].Enabled == false) continue;
                PostFilters[i].PostFilter.OnTilesCombined(tileDesign, mat, ref mesh, PostFilters[i]);
            }
        }

        //void PostFilters_OnTilesCombinedAfterBoole(TileDesign tileDesign,  ref Mesh mesh)
        //{
        //    if (!UsePostFilters) return;
        //    for (int i = 0; i < PostFilters.Count; i++)
        //    {
        //        if (PostFilters[i].PostFilter == null) continue;
        //        PostFilters[i].PostFilter.OnTilesCombinedAfterBoole(tileDesign,  ref mesh, PostFilters[i]);
        //    }
        //}

        void PostFilters_OnFinalResult(List<Mesh> meshes, List<Material> meshesMaterials)
        {
            if (!UsePostFilters) return;

            for (int i = 0; i < PostFilters.Count; i++)
            {
                if (PostFilters[i].PostFilter == null) continue;
                if (PostFilters[i].Enabled == false) continue;

                for (int m = 0; m < meshes.Count; m++)
                {
                    Mesh mesh = meshes[m];
                    PostFilters[i].PostFilter.OnFinalResult(ref mesh, meshesMaterials[m], PostFilters[i]);
                    meshes[m] = mesh;
                }

            }
        }

    }
}