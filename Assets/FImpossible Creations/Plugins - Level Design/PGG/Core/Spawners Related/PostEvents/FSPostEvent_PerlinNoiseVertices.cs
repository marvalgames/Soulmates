using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "FS Post Event - Perlin Offset Vertices", menuName = "Perlin Offset Vertices Instance", order = 1)]
    public class FSPostEvent_PerlinNoiseVertices : FieldSpawnerPostEvent_Base
    {
        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            string useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            ProceedPerlinNoise(generatedRef.CombinedNonStaticContainer, useTag, helper);
            ProceedPerlinNoise(generatedRef.CombinedStaticContainer, useTag, helper);
        }

        void ProceedPerlinNoise(GameObject parent, string useTag, FieldSetup.CustomPostEventHelper helper)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedPerlinNoise(filter.sharedMesh, helper );
            }
        }

        void ProceedPerlinNoise(Mesh mesh, FieldSetup.CustomPostEventHelper helper)
        {
            FieldVariable powerV = helper.RequestVariable("Power:", 0.05f);
            FieldVariable scaleV = helper.RequestVariable("Noise Scale:", 0.4f);
            FieldVariable offsetAxisV = helper.RequestVariable("Offsets:", new Vector3(1, 1, 1));

            float power = powerV.GetFloatValue();
            float scale = scaleV.GetFloatValue();
            Vector3 offsetAxis = offsetAxisV.GetVector3Value();

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
                vPos.x += blend * offsetAxis.x;
                vPos.y += blend * offsetAxis.y;
                vPos.z += blend * offsetAxis.z;

                vertices[i] = vPos;
            }

            mesh.SetVertices(vertices);
        }



#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "";

            FieldVariable power = helper.RequestVariable("Power:", 0.05f);
            FieldVariable.Editor_DrawTweakableVariable(power);

            FieldVariable scale = helper.RequestVariable("Noise Scale:", 0.05f);
            FieldVariable.Editor_DrawTweakableVariable(scale);

            GUILayout.Space(3);
            FieldVariable offsetAxis = helper.RequestVariable("Offsets:", new Vector3(1,1,1));
            FieldVariable.Editor_DrawTweakableVariable(offsetAxis);


            GUILayout.Space(4);
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");

            string info = "";
            if (tagg.GetStringValue() == "Untagged") info = "Untagged = Apply To NONE";
            else info = "Apply To Tagged:";
            tagg.SetValue(UnityEditor.EditorGUILayout.TagField(info, tagg.GetStringValue()));

            GUILayout.Space(2);
        }
#endif

    }
}