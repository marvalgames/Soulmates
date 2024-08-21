using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "FS Post Event - Weld Vertices", menuName = "FImpossible Creations/Procedural Generation/PE Weld Vertices Instance", order = 1)]
    public class FSPostEvent_WeldVertices : FieldSpawnerPostEvent_Base
    {
        string useTag = "";
        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            FieldVariable mode = helper.RequestVariable("Weld V2:", false);
            FieldVariable thres = helper.RequestVariable("Threshold:", 0.01f);

            ProceedVeld(generatedRef.CombinedStaticContainer, mode.GetBoolValue(), thres.Float);
            ProceedVeld(generatedRef.CombinedNonStaticContainer, mode.GetBoolValue(), thres.Float);
        }

        void ProceedVeld(GameObject parent, bool v2, float thres)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedVeld(filter, v2, thres);
            }
        }

        void ProceedVeld(MeshFilter filter, bool v2, float thres)
        {
            if (v2)
                filter.sharedMesh = FMeshUtils.Weld2(filter.sharedMesh, filter.sharedMesh.bounds.max.magnitude * thres);
            else
                filter.sharedMesh = FMeshUtils.Weld(filter.sharedMesh, filter.sharedMesh.bounds.max.magnitude * thres);
        }


#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "(EXPERIMENTAL) Trying to weld vertices of combined meshes, beware UV errors!";
            FieldVariable mode = helper.RequestVariable("Weld V2:", false);
            FieldVariable.Editor_DrawTweakableVariable(mode);

            FieldVariable thres = helper.RequestVariable("Threshold:", 0.01f);
            thres.helper.x = 0.001f;
            thres.helper.y = 0.1f;
            FieldVariable.Editor_DrawTweakableVariable(thres);

            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");

            string info = "";
            if (tagg.GetStringValue() == "Untagged") info = "Untagged = Apply To All";
            else info = "Apply To Tagged:";
            tagg.SetValue( UnityEditor.EditorGUILayout.TagField(info, tagg.GetStringValue()));

            //FieldVariable count = helper.RequestVariable("Probes Per Cell", 2);
            //count.helper.x = 1;
            //count.helper.y = 4;
            //FieldVariable.Editor_DrawTweakableVariable(count);
        }
#endif

    }
}