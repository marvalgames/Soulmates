using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser.CollectionWizard
{
    public class GUIStyles
    {
        private static GUIStyle _appTitleBoxStyle;
        public static GUIStyle AppTitleBoxStyle
        {
            get
            {
                if (_appTitleBoxStyle == null)
                {
                    _appTitleBoxStyle = new GUIStyle("helpBox");
                    _appTitleBoxStyle.fontStyle = FontStyle.Bold;
                    _appTitleBoxStyle.fontSize = 16;
                    _appTitleBoxStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _appTitleBoxStyle;
            }
        }

        private static GUIStyle _boxTitleStyle;
        public static GUIStyle BoxTitleStyle
        {
            get
            {
                if (_boxTitleStyle == null)
                {
                    _boxTitleStyle = new GUIStyle("Label");
                    _boxTitleStyle.fontStyle = FontStyle.BoldAndItalic;
                }
                return _boxTitleStyle;
            }
        }

        private static GUIStyle _helpBoxStyle;
        public static GUIStyle HelpBoxStyle
        {
            get
            {
                if (_helpBoxStyle == null)
                {
                    _helpBoxStyle = new GUIStyle("helpBox");
                    _helpBoxStyle.fontStyle = FontStyle.Bold;
                }
                return _helpBoxStyle;
            }
        }

        private static GUIStyle _groupTitleStyle;
        public static GUIStyle GroupTitleStyle
        {
            get
            {
                if (_groupTitleStyle == null)
                {
                    _groupTitleStyle = new GUIStyle("Label");
                    _groupTitleStyle.fontStyle = FontStyle.Bold;
                }
                return _groupTitleStyle;
            }
        }

        private static GUIStyle _groupBoxStyle;
        public static GUIStyle GroupBoxStyle
        {
            get
            {
                if (_groupBoxStyle == null)
                {
                    _groupBoxStyle = new GUIStyle("helpBox");
                }
                return _groupBoxStyle;
            }
        }

        private static GUIStyle _previewLabelStyle;
        public static GUIStyle PreviewLabelStyle
        {
            get
            {
                if (_previewLabelStyle == null)
                {
                    _previewLabelStyle = new GUIStyle(GUI.skin.label);
                    _previewLabelStyle.alignment = TextAnchor.LowerCenter;
                    _previewLabelStyle.fontStyle = FontStyle.Bold;
                }
                return _previewLabelStyle;
            }
        }

        private static GUIStyle _dropAreaStyle;
        public static GUIStyle DropAreaStyle
        {
            get
            {
                if (_dropAreaStyle == null)
                {
                    _dropAreaStyle = new GUIStyle("box");
                    _dropAreaStyle.fontStyle = FontStyle.Italic;
                    _dropAreaStyle.alignment = TextAnchor.MiddleCenter;
                    _dropAreaStyle.normal.textColor = GUI.skin.label.normal.textColor;
                    
                }
                return _dropAreaStyle;
            }
        }

        private static GUIStyle _separatorStyle;
        public static GUIStyle SeparatorStyle
        {
            get
            {
                if (_separatorStyle == null)
                {
                    Color color = EditorGUIUtility.isProSkin ? Color.black : Color.grey;

                    _separatorStyle = new GUIStyle();
                    _separatorStyle.normal.background = CreateColorPixel(color);
                    _separatorStyle.stretchWidth = true;
                    _separatorStyle.margin = new RectOffset(5, 5, 0, 0);
                    _separatorStyle.fixedHeight = 0.5f;

                }
                return _separatorStyle;
            }
        }

        public static Texture2D CreateColorPixel(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        public static void DrawSeparator()
        {
            GUILayout.Space(10f);
            GUILayout.Box(GUIContent.none, GUIStyles.SeparatorStyle);
            GUILayout.Space(10f);
        }

        public static Color DefaultBackgroundColor = GUI.backgroundColor;
        public static Color ErrorBackgroundColor = new Color(1f, 0f, 0f, 0.7f); // red tone

        public static Color DropAreaBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f); // gray tone

        public static float BigButtonSize = 40f;

    }
}