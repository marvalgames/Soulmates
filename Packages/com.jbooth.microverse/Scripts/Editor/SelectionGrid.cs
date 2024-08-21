using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

namespace JBooth.MicroVerseCore
{
    /// <summary>
    /// A replacement for Unity's selection grid.
    /// This one supports an active checkbox and multiple selection
    /// </summary>
    public class SelectionGrid
    {
        /// <summary>
        /// Data structure which is used in the selection grid
        /// </summary>
        public class Selectable
        {
            public string text;
            public string tooltip;
            public Texture image;
            public bool active;

            /// <summary>
            /// In multiselection mode this classifies the one that was last selected
            /// </summary>
            public bool focused;
        }

        /// <summary>
        /// Show the selection grid in the inspector
        /// </summary>
        /// <param name="selectedIndexes">The selected indexes</param>
        /// <param name="selectables">Objects used for the cells of the selection grid</param>
        /// <param name="cellSize">The size of the cells</param>
        /// <param name="dynamicResize">Whether the cells should become smaller with increasing items</param>
        /// <param name="title">Optional title</param>
        /// <returns></returns>
        public static bool ShowSelectionGrid(List<int> selectedIndexes, Selectable[] selectables, int cellSize, bool dynamicResize = false, string title = null, bool missingDataTextVisible = false)
        {
            // title
            if (!string.IsNullOrEmpty(title))
            {
                GUIContent terrainLayers = EditorGUIUtility.TrTextContent(title);
                GUILayout.Label(terrainLayers, EditorStyles.boldLabel);
            }

            if (selectables.Length > 0)
            {
                EditorGUILayout.HelpBox("Selection: Click = Single, Control+Click = Add, Shift+Click = Range", MessageType.None);
            }

            bool changed = false;

            if (selectables.Length == 0)
            {
                if (missingDataTextVisible)
                {
                    EditorGUILayout.HelpBox("No objects found", MessageType.Info);
                }
                return changed;
            }

            // preselect first item
            if (selectedIndexes.Count == 0 && selectables.Length > 0)
            {
                selectedIndexes.Add(0);
                changed = true;
            }

            if ( dynamicResize)
            {
                // with more than 10 textures the texture size is reduced by a given percentage
                // ie the more items, the smaller the thumbnails
                if (selectables.Length > 10)
                {
                    cellSize = (int)(cellSize * ( 1 - 1f/cellSize));
                }

            }

            // calculate number of columns and rows               
            int columns = (int)(EditorGUIUtility.currentViewWidth - cellSize) / cellSize + 1;
            int rows = (int)Mathf.Ceil((selectables.Length + columns - 2) / (float)columns);

            int selectedIndex = -1;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Active:");

            if(GUILayout.Button( "All", EditorStyles.miniButton))
            {
                foreach (Selectable selectable in selectables)
                    selectable.active = true;

                changed = true;
            }

            if (GUILayout.Button("None", EditorStyles.miniButton))
            {
                foreach (Selectable selectable in selectables)
                    selectable.active = false;

                changed = true;
            }

            if (GUILayout.Button("Invert", EditorStyles.miniButton))
            {
                foreach (Selectable selectable in selectables)
                    selectable.active = !selectable.active;

                changed = true;
            }

            EditorGUILayout.EndHorizontal();

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    for (int col = 0; col < columns; col++)
                    {
                        selectedIndex++;

                        if (selectedIndex < selectables.Length)
                        {
                            Texture previewTexture = selectables[selectedIndex].image;
                            string label = selectables[selectedIndex].text;
                            bool active = selectables[selectedIndex].active;

                            // check if the cell is selected
                            bool isCellSelected = selectedIndexes.Contains(selectedIndex);

                            // visualization style
                            GUIStyle style;

                            if (isCellSelected)
                                style = GUIStylesSelectionGrid.TextureSelectionStyleInclude;
                            else
                                style = GUIStylesSelectionGrid.TextureSelectionStyleUnselected;

                            // show background image
                            GUILayout.Label(previewTexture, style, GUILayout.Width(cellSize), GUILayout.Height(cellSize));

                            // get clickable area of cell
                            Rect previewTextureRect = GUILayoutUtility.GetLastRect();
                            bool previewTextureClicked = Event.current.rawType == EventType.MouseDown && previewTextureRect.Contains(Event.current.mousePosition);

                            if (previewTextureClicked)
                            {
                                // add selection to list
                                // multi edit mode is activated with shift key: you can select multiple cells this way
                                bool isMultiEditMode = Event.current.control || Event.current.shift;

                                // add selection to list
                                if (isMultiEditMode)
                                {
                                    // toggle single selection
                                    if (Event.current.control)
                                    {
                                        if (selectedIndexes.Contains(selectedIndex) && selectedIndexes.Count > 1)
                                            selectedIndexes.Remove(selectedIndex);
                                        else
                                            selectedIndexes.Add(selectedIndex);
                                    }

                                    // toggle range selection
                                    if (Event.current.shift)
                                    {
                                        int startIndex = selectedIndex;
                                        int endIndex = Array.IndexOf(selectables, Array.Find(selectables, x => x.focused));

                                        selectedIndexes.Clear();

                                        // flip range if necessary
                                        if (startIndex > endIndex)
                                        {
                                            int tmp = startIndex;
                                            startIndex = endIndex;
                                            endIndex = tmp;

                                        }

                                        // set selection range
                                        selectedIndexes.Clear();
                                        for (int i = startIndex; i <= endIndex; i++)
                                        {
                                            selectedIndexes.Add(i);
                                        }
                                    }


                                }
                                else
                                {
                                    selectedIndexes.Clear();
                                    selectedIndexes.Add(selectedIndex);

                                }

                                // invalidate focused on all
                                Array.ForEach(selectables, x => x.focused = false);
                                
                                // set new focused
                                selectables[selectedIndex].focused = true;

                                changed = true;
                            }

                            // margin for the selection                            
                            int margin = 3;
                            
                            // get image rect
                            Rect labelRect = GUILayoutUtility.GetLastRect();

                            // reduce image rect by margin
                            labelRect.x += margin;
                            labelRect.y += margin;
                            labelRect.width -= margin * 2;
                            labelRect.height -= margin * 2;

                            // rect of the toggle, depends on the label rect
                            Rect toggleRect = new Rect(labelRect);

                            // label rect
                            labelRect.height = GUIStylesSelectionGrid.SelectionElementLabelStyle.CalcHeight(new GUIContent(label), labelRect.width);

                            // label background
                            GUI.DrawTexture(labelRect, GUIStylesSelectionGrid.LabelBackgroundTexture, ScaleMode.StretchToFill);
                            GUI.Box(labelRect, label, GUIStylesSelectionGrid.SelectionElementLabelStyle);

                            // unselected: make the cell appear grey-ish, ie inaccessible
                            if (!active)
                            {
                                Color overlay = Color.grey;
                                overlay.a = 0.5f;

                                EditorGUI.DrawRect(previewTextureRect, overlay);
                            }

                            // active toggle
                            float lineHeight = GUI.skin.label.lineHeight;

                            // reduce the checkbox area, otherwise outside of the box we'd get a mouse cursor change on hovering where a label would be
                            toggleRect.x += margin;
                            toggleRect.y += toggleRect.height - lineHeight - margin;
                            toggleRect.height = lineHeight;
                            toggleRect.width = GUI.skin.toggle.CalcHeight( GUIContent.none, toggleRect.width); 

                            selectables[selectedIndex].active = GUI.Toggle(toggleRect, active, "");

                            // detect change
                            if (active != selectables[selectedIndex].active)
                            {
                                changed = true;
                            }


                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            return changed;
        }
    }

    public class GUIStylesSelectionGrid
    {

        /// <summary>
        /// Used for include and unselected texture border
        /// </summary>
        private static GUIStyle _textureSelectionStyleUnselected;
        public static GUIStyle TextureSelectionStyleUnselected
        {
            get
            {
                if (_textureSelectionStyleUnselected == null || _textureSelectionStyleUnselected.normal.background == null) // background check because it's null when a scene reloads
                {
                    _textureSelectionStyleUnselected = new GUIStyle(GUI.skin.label);
                    _textureSelectionStyleUnselected.normal.background = GUI.skin.box.normal.background;
                    _textureSelectionStyleUnselected.stretchWidth = true;
                    _textureSelectionStyleUnselected.border = new RectOffset(0, 0, 0, 0);
                    _textureSelectionStyleUnselected.margin = new RectOffset(2, 2, 2, 2);
                    _textureSelectionStyleUnselected.padding = new RectOffset(2, 2, 2, 2);

                }
                return _textureSelectionStyleUnselected;
            }
        }

        private static GUIStyle _textureSelectionStyleInclude;
        public static GUIStyle TextureSelectionStyleInclude
        {
            get
            {
                if (_textureSelectionStyleInclude == null || _textureSelectionStyleInclude.normal.background == null) // background check because it's null when a scene reloads
                {
                    _textureSelectionStyleInclude = new GUIStyle(TextureSelectionStyleUnselected);
                    _textureSelectionStyleInclude.normal.background = CreateColorPixel(Color.green * 0.8f); // or use MV color new Color(0, 0, 128)
                }
                return _textureSelectionStyleInclude;
            }
        }

        private static GUIStyle _textureSelectionStyleExclude;
        public static GUIStyle TextureSelectionStyleExclude
        {
            get
            {
                if (_textureSelectionStyleExclude == null || _textureSelectionStyleExclude.normal.background == null) // background check because it's null when a scene reloads
                {
                    _textureSelectionStyleExclude = new GUIStyle(TextureSelectionStyleUnselected);
                    _textureSelectionStyleExclude.normal.background = CreateColorPixel(Color.red);
                }
                return _textureSelectionStyleExclude;
            }
        }

        static Texture2D _labelBackgroundTexture;
        public static Texture2D LabelBackgroundTexture
        {
            get
            {
                if (_labelBackgroundTexture == null)
                {
                    _labelBackgroundTexture = new Texture2D(1, 1);
                    _labelBackgroundTexture.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 0.5f));
                    _labelBackgroundTexture.Apply();
                }

                return _labelBackgroundTexture;
            }
        }
        static GUIStyle _selectionElementLabelStyle;

        public static GUIStyle SelectionElementLabelStyle
        {
            get
            {
                if (_selectionElementLabelStyle == null)
                {
                    _selectionElementLabelStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                    _selectionElementLabelStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                    _selectionElementLabelStyle.fontStyle = FontStyle.Bold;
                    _selectionElementLabelStyle.alignment = TextAnchor.UpperCenter;
                }
                return _selectionElementLabelStyle;
            }
        }

        /// <summary>
        /// Creates a 1x1 texture
        /// </summary>
        /// <param name="Background">Color of the texture</param>
        /// <returns></returns>
        public static Texture2D CreateColorPixel(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}