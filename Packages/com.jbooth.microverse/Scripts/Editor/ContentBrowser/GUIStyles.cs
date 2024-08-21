using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore.Browser
{
    public static class GUIStyles
    {
        private static GUIStyle _separatorStyle;
        public static GUIStyle SeparatorStyle
        {
            get
            {
                if (_separatorStyle == null)
                {
                    Color color = EditorStyles.label.normal.textColor;
                    color.a = 0.4f;

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
    }
}