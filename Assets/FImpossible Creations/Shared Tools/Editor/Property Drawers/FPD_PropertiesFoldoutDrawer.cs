using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
    [CustomPropertyDrawer(typeof(FPD_PropertiesFoldoutAttribute))]
    public class FPD_PropertiesFoldout : PropertyDrawer
    {
        FPD_PropertiesFoldoutAttribute Attribute { get { return ((FPD_PropertiesFoldoutAttribute)base.attribute); } }

        string title = "";
        GUIStyle frameStyle = null;
        GUIStyle foldStyle = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Attribute == null) return;

            #region Prepare Title String

            if (title == "")
            {
                title = Attribute.title;
                if (title == "") title = property.displayName + "(" + Attribute.HowManyNextPropertiesToContain + ")";
                title = "  " + title;
            }

            #endregion

            #region Prepare GUI Styles

            if (frameStyle == null)
            {
                if (Attribute.frameStyleID == 0) frameStyle = EditorStyles.helpBox;
                else if (Attribute.frameStyleID == 1) frameStyle = FGUI_Resources.BGInBoxStyle;
                else if (Attribute.frameStyleID == 2) frameStyle = FGUI_Resources.HeaderBoxStyle;
                else if (Attribute.frameStyleID == 3) frameStyle = FGUI_Resources.FrameBoxStyle;
                else if (Attribute.frameStyleID == 4) frameStyle = FGUI_Resources.FoldStyle;
                else if (Attribute.frameStyleID == 5) frameStyle = FGUI_Resources.ViewBoxStyle;
                else if (Attribute.frameStyleID == 6) frameStyle = FGUI_Resources.BGInBoxBlankStyle;

                if (frameStyle == null) if (!string.IsNullOrWhiteSpace(Attribute.frameStyle)) frameStyle = GUI.skin.GetStyle(Attribute.frameStyle);
                if (frameStyle == null) frameStyle = FGUI_Resources.BGInBoxStyle;
            }

            if (foldStyle == null)
            {
                foldStyle = new GUIStyle(EditorStyles.foldout);
                foldStyle.fontStyle = FontStyle.Bold;
            }

            #endregion

            GUILayout.Space(7);
            GUILayout.BeginVertical(frameStyle);

            if (Attribute.indent > 0) EditorGUI.indentLevel += 1;

            Attribute.foldout = EditorGUILayout.Foldout(Attribute.foldout, title, true, foldStyle);

            if (Attribute.foldout)
            {
                if (Attribute.indent == 0) EditorGUI.indentLevel += 1;
                else EditorGUI.indentLevel += Attribute.indent - 1;

                GUILayout.Space(1);
                int toDisplay = Attribute.HowManyNextPropertiesToContain - 1;
                var sp = property.Copy();

                GUILayout.Space(Attribute.extraSpacing);
                EditorGUILayout.PropertyField(sp, true);

                if (sp.Next(false))
                {
                    for (int i = 0; i < toDisplay; i++)
                    {
                        EditorGUILayout.PropertyField(sp, true);
                        if (!sp.Next(false)) break;
                    }
                }

                if (Attribute.indent == 0) EditorGUI.indentLevel -= 1;
                else EditorGUI.indentLevel -= Attribute.indent - 1;

                GUILayout.Space(Attribute.extraSpacing);
            }

            if (Attribute.indent > 0) EditorGUI.indentLevel -= 1;
            GUILayout.EndVertical();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float size = -EditorGUIUtility.singleLineHeight / 5f;
            return size;
        }
    }

}

