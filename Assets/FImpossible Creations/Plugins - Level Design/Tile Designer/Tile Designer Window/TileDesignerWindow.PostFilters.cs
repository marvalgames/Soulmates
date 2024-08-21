#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FIMSpace.FEditor;
using System.Collections.Generic;

namespace FIMSpace.Generating
{
    public partial class TileDesignerWindow
    {
        bool _foldout_postFilters = false;

        void DiplayPostFilters()
        {
            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            _foldout_postFilters = EditorGUILayout.Foldout(_foldout_postFilters, " Post Filters:", true);

            if (GUILayout.Button("", EditorStyles.label)) { _foldout_postFilters = !_foldout_postFilters; }

            if (GUILayout.Button("+", FGUI_Resources.ButtonStyle, GUILayout.Height(17), GUILayout.Width(28)))
            {
                _foldout_postFilters = true;
                EditedDesign.PostFilters.Add(new TileDesign.PostFilterHelper());
            }

            EditorGUILayout.EndHorizontal();

            if (_foldout_postFilters)
            {
                if (EditedDesign.PostFilters.Count == 0)
                    EditorGUILayout.LabelField("No Post Filters Added Yet", EditorStyles.centeredGreyMiniLabel);
                else
                    GUILayout.Space(4);

                int toRemove = -1;
                for (int i = 0; i < EditedDesign.PostFilters.Count; i++)
                {
                    var ev = EditedDesign.PostFilters[i];

                    EditorGUILayout.BeginVertical(FGUI_Resources.BGBoxStyle);

                    EditorGUILayout.BeginHorizontal();

                    ev.Enabled = EditorGUILayout.Toggle(ev.Enabled, GUILayout.Width(16));

                    GUI.color = new Color(1f, 1f, 1f, 0.7f);
                    if (GUILayout.Button(new GUIContent(ev.Foldout ? FGUI_Resources.Tex_DownFold : FGUI_Resources.Tex_RightFold), EditorStyles.label, GUILayout.Width(20), GUILayout.Height(18)))
                    {
                        ev.Foldout = !ev.Foldout;
                    }

                    GUI.color = Color.white;

                    ev.PostFilter = (TilePostFilterBase)EditorGUILayout.ObjectField(ev.PostFilter, typeof(TilePostFilterBase), false);

                    GUI.backgroundColor = new Color(1f, 0.65f, 0.65f, 0.9f);
                    if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(23))) toRemove = i;
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();

                    if (ev.Foldout)
                        DisplayPostFilter(EditedDesign.PostFilters[i]);

                    EditorGUILayout.EndVertical();
                }


                if (toRemove > -1) EditedDesign.PostFilters.RemoveAt(toRemove);
            }

            if (EditorGUI.EndChangeCheck()) if (ToDirty) EditorUtility.SetDirty(ToDirty);

            GUILayout.Space(4);

        }

        void DisplayPostFilter(TileDesign.PostFilterHelper helper)
        {
            if (helper.PostFilter == null) return;

            if (string.IsNullOrWhiteSpace(helper.PostFilter.PostEventInfo) == false)
                EditorGUILayout.HelpBox(helper.PostFilter.PostEventInfo, MessageType.None);

            helper.PostFilter.Editor_DisplayGUI(helper);
        }
  
    }
}
#endif