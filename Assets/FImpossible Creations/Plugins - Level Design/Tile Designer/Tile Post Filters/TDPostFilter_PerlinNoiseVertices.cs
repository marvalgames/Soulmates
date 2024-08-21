using FIMSpace.FGenerating;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "TD Post Filter - Perlin Offset Vertices", menuName = "FImpossible Creations/Procedural Generation/TD Perlin Offset Vertices Instance", order = 1)]
    public class TDPostFilter_PerlinNoiseVertices : TilePostFilterBase
    {
        //string useID = "";

        public override void OnTilesCombined(TileDesign design, Material combinationMaterial, ref Mesh mesh, TileDesign.PostFilterHelper helper)
        {
            //FUniversalVariable tagg = helper.RequestVariable("ID", "");
            //useID = tagg.GetStringValue();

            //if (string.IsNullOrWhiteSpace(useID))
            //{
            //    if (meshSetup.CustomID != useID) return;
            //}

            FUniversalVariable powerV = helper.RequestVariable("Power:", 0.05f);
            FUniversalVariable scaleV = helper.RequestVariable("Noise Scale:", 0.4f);
            FUniversalVariable offsetAxisV = helper.RequestVariable("Offsets:", new Vector3(1, 1, 1));

            float power = powerV.GetFloat();
            float scale = scaleV.GetFloat();
            Vector3 offsetAxis = offsetAxisV.GetVector3();

            Vector3 randomOffset = FVectorMethods.RandomVector(-1000f, 1000f);

            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vPos = vertices[i];

                float x = vPos.x * scale + randomOffset.x;
                float y = vPos.y * scale + randomOffset.y;
                float z = vPos.z * scale + randomOffset.z;

                float blend = Mathf.PerlinNoise(x + z, y) * power;
                //float blend = FEngineering.PerlinNoise3D(x, y, z) * power;
                vPos.x += blend * offsetAxis.x;
                vPos.y += blend * offsetAxis.y;
                vPos.z += blend * offsetAxis.z;

                //FUniversalVariable mode = helper.RequestVariable("Weld V2:", false);
                //FUniversalVariable thres = helper.RequestVariable("Threshold:", 0.01f);
                //ProceedVeld(ref generated, mode.GetBoolValue(), thres.Float);

                vertices[i] = vPos;
            }

            mesh.SetVertices(vertices);
        }



#if UNITY_EDITOR
        public override void Editor_DisplayGUI(TileDesign.PostFilterHelper helper)
        {
            PostEventInfo = "";
            //FUniversalVariable mode = helper.RequestVariable("Weld V2:", false);
            //FUniversalVariable.Editor_DrawTweakableVariable(mode);

            FUniversalVariable power = helper.RequestVariable("Power:", 0.05f);
            power.Editor_DisplayVariableGUI();

            FUniversalVariable scale = helper.RequestVariable("Noise Scale:", 0.05f);
            scale.Editor_DisplayVariableGUI();

            GUILayout.Space(3);
            FUniversalVariable offsetAxis = helper.RequestVariable("Offsets:", new Vector3(1,1,1));
            offsetAxis.Editor_DisplayVariableGUI();

            //FUniversalVariable tagg = helper.RequestVariable("ID", "");

            //string info = "";
            //if (tagg.GetStringValue() == "") info = "No ID = Apply To All";
            //else info = "Apply To ID:";
            //tagg.SetValue( UnityEditor.EditorGUILayout.TextField(info, tagg.GetStringValue()));
        }
#endif

    }
}