using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(DetailPrototypeSettings))]
    public class DetailSettingsEditor : Editor
    {
        GUIContent detailIcon;
        void LoadDetailIcon(DetailPrototypeSerializable dp)
        {
            detailIcon = new GUIContent();
            if (dp.prototype == null)
            {
                detailIcon.text = "Missing";
            }
            else if (dp.usePrototypeMesh)
            {
                Texture tex = AssetPreview.GetAssetPreview(dp.prototype);
                detailIcon.image = tex != null ? tex : null;
                detailIcon.text = detailIcon.tooltip = dp.prototype != null ? dp.prototype.name : "Missing";
            }
            else
            {
                detailIcon.image = dp.prototypeTexture;
                if (dp.prototypeTexture == null)
                {
                    detailIcon.text = "Missing";
                }
            }
        }
        Texture2D barTex;

        public override void OnInspectorGUI()
        {
            var settings = (DetailPrototypeSettings)target;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            if (settings.prototype == null)
                settings.prototype = new DetailPrototypeSerializable();
            LoadDetailIcon(settings.prototype);
            if (barTex == null)
            {
                barTex = new Texture2D(1, 1);
                barTex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                barTex.Apply();
            }

            if (settings.prototype.usePrototypeMesh)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(128), GUILayout.Height(128));
                GUI.DrawTexture(r, detailIcon.image == null ? Texture2D.blackTexture : detailIcon.image);
                r.height /= 5;
                r.y += r.height * 4;
                GUI.DrawTexture(r, barTex);
                EditorGUI.LabelField(r, detailIcon.text);
                //EditorGUILayout.LabelField(detailIcon, GUILayout.Width(128), GUILayout.Height(128));
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical();
            TerrainDetailMeshWizard.DrawInspector(settings.prototype);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                MicroVerse.instance?.Invalidate(null, MicroVerse.InvalidateType.Tree);
            }
        }
    }
}
