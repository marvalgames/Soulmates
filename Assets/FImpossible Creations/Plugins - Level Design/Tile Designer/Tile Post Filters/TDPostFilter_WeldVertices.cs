using FIMSpace.FGenerating;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "TD Post Filter - Weld Vertices", menuName = "FImpossible Creations/Procedural Generation/TD Weld Vertices Instance", order = 1)]
    public class TDPostFilter_WeldVertices : TilePostFilterBase
    {
        string useID = "";

        public override void OnOriginMeshGenerated(TileDesign design, TileMeshSetup meshSetup, TileDesign.PostFilterHelper helper)
        {
            FUniversalVariable tagg = helper.RequestVariable("ID", "");
            useID = tagg.GetString();

            if (string.IsNullOrWhiteSpace(useID))
            {
                if (meshSetup.CustomID != useID) return;
            }
            FUniversalVariable mode = helper.RequestVariable("Weld V2:", false);
            FUniversalVariable thres = helper.RequestVariable("Threshold:", 0.01f);

            Mesh mesh = meshSetup.LatestGeneratedMesh;
            ProceedVeld(ref mesh, mode.GetBool(), thres.GetFloat());
            meshSetup.LatestGeneratedMesh = mesh;
        }

        void ProceedVeld(ref Mesh filter, bool v2, float thres)
        {
            if (v2)
                filter = FMeshUtils.Weld2(filter, filter.bounds.max.magnitude * thres);
            else
                filter = FMeshUtils.Weld(filter, filter.bounds.max.magnitude * thres);
        }


#if UNITY_EDITOR
        public override void Editor_DisplayGUI(TileDesign.PostFilterHelper helper)
        {
            PostEventInfo = "(EXPERIMENTAL) Trying to weld vertices of meshe, beware UV errors!";
            FUniversalVariable mode = helper.RequestVariable("Weld V2:", false);
            mode.Editor_DisplayVariableGUI();

            FUniversalVariable thres = helper.RequestVariable("Threshold:", 0.01f);
            thres.SetMinMaxSlider(0.001f, 0.1f);
            thres.Editor_DisplayVariableGUI();

            FUniversalVariable tagg = helper.RequestVariable("ID", "");

            string info = "";
            if (tagg.GetString() == "") info = "No ID = Apply To All";
            else info = "Apply To ID:";
            tagg.SetValue( UnityEditor.EditorGUILayout.TextField(info, tagg.GetString()));

        }
#endif

    }
}