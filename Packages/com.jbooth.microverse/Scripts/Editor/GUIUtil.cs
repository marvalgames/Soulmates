using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using static JBooth.MicroVerseCore.FilterSet;

namespace JBooth.MicroVerseCore
{

    public class GUIUtil
    {
        static string GetPackageVersion()
        {
            List<UnityEditor.PackageManager.PackageInfo> packages = AssetDatabase.FindAssets("package")
                .Select(AssetDatabase.GUIDToAssetPath).Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                .Select(UnityEditor.PackageManager.PackageInfo.FindForAssetPath).ToList();

            foreach (var p in packages)
            {
                if (p != null && p.name == "com.jbooth.microverse")
                {
                    return p.version;
                }
            }
            return "0.0";
        }

        static Texture2D gradient;
        static Texture2D logo;
        static string _version;
        public static void DrawHeaderLogo()
        {
            if (gradient == null)
            {
                gradient = Resources.Load<Texture2D>("microverse_gradient");
                logo = Resources.Load<Texture2D>("microverse_logo");
            }

            if (_version == null)
            {
                _version = GetPackageVersion();
            }

            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(64));
            EditorGUI.DrawPreviewTexture(rect, gradient);
            rect.y -= 6;
            GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit, true);
            rect.y += 30;
            GUI.Label(rect, Application.unityVersion);
            rect.x += rect.width-30;
            GUI.Label(rect, _version);
            rect.y -= 22;
            rect.height = 18;
            rect.width = 18;
            rect.x += 10;
            if (GUI.Button(rect, "?"))
            {
                Application.OpenURL("https://docs.google.com/document/d/1R4Ru7GKdVLLNVmfVwcX7RPRPrvkN0l36LNqm9LIMtno/edit?usp=sharing");
            }
        }

        public static Texture2D FindDefaultTexture(string name)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D microverse_default_");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains(name))
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
        }

        static GUIStyle _boxStyle;

        public static GUIStyle boxStyle
        {
            get
            {
                if (_boxStyle == null)
                {
                    _boxStyle = new GUIStyle(EditorStyles.helpBox);
                    _boxStyle.normal.textColor = GUI.skin.label.normal.textColor;
                    _boxStyle.fontStyle = FontStyle.Bold;
                    _boxStyle.fontSize = 11;
                    _boxStyle.alignment = TextAnchor.UpperLeft;
                }
                return _boxStyle;
            }
        }

        static Texture2D selectedTex = null;
        static Texture2D labelBackgroundTex = null;
        static void SetupSelectionGrid()
        {
            if (selectedTex == null)
            {
                selectedTex = new Texture2D(128, 128, TextureFormat.ARGB32, false);
                for (int x = 0; x < 128; ++x)
                {
                    for (int y = 0; y < 128; ++y)
                    {
                        if (x < 1 || x > 126 || y < 1 || y > 126)
                        {
                            selectedTex.SetPixel(x, y, new Color(0, 0, 128));
                        }
                        else
                        {
                            selectedTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                        }
                    }
                }
                selectedTex.Apply();
            }
            if (labelBackgroundTex == null)
            {
                labelBackgroundTex = new Texture2D(1, 1);
                labelBackgroundTex.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 0.5f));
                labelBackgroundTex.Apply();
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
                    Color color = EditorGUIUtility.isProSkin ? new Color(0.0f, 0.0f, 0.0f, 0.8f) : new Color(1.0f, 1.0f, 1.0f, 0.5f);
                    _labelBackgroundTexture.SetPixel(0, 0, color);
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

        static int DrawSelectionElement(Rect r, int i, int index, Texture2D image, string label)
        {
            SetupSelectionGrid();

            if (GUI.Button(r, "", GUI.skin.box))
            {
                index = i;
            }
            GUI.DrawTexture(r, image != null ? image : Texture2D.blackTexture, ScaleMode.ScaleToFit, false);
            if (i == index && index >= 0)
            {
                GUI.DrawTexture(r, selectedTex, ScaleMode.ScaleToFit, true);
            }

            r.height = SelectionElementLabelStyle.CalcHeight( new GUIContent( label), r.width);

            GUI.DrawTexture(r, labelBackgroundTex, ScaleMode.StretchToFill);
            GUI.Box(r, label, SelectionElementLabelStyle);

            return index;
        }


        public static int DragOffGrid(ref Vector2 scroll, GUIContent[] contents, Rect rect, int imageSize = 64)
        {
            float size = rect.width - 120;
            int maxX = Mathf.FloorToInt(size / imageSize)-1;
            if (maxX < 1)
                maxX = 1;

            float bs = (contents.Length * imageSize / maxX) / rect.height;
            if (bs > 1)
            {
                var scrollRect = rect;
                scrollRect.x = rect.width - 20;
                scrollRect.width -= 20;
                scrollRect.y = 20;
                scrollRect.height -= 20;
                scroll.y = GUI.VerticalScrollbar(scrollRect, scroll.y, 1, 0, bs);
            }
            int startIdx = Mathf.CeilToInt(scroll.y * bs) * maxX;
            EditorGUILayout.BeginHorizontal();
            int index = -1;
            if (contents == null)
                return -1;
            for (int i = startIdx; i < contents.Length; ++i)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(imageSize), GUILayout.Height(imageSize));
                if (Event.current.type == EventType.Repaint &&
                    GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    index = i;
                }
                DrawSelectionElement(r, -1, -1, contents[i].image as Texture2D, contents[i].text);
                if (i % maxX == maxX - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

            }
            
            EditorGUILayout.EndHorizontal();
            return index;
        }

        static GUIStyle toggleButtonStyle;
        public static bool DrawToggleButton(string label, bool b)
        {
            if (toggleButtonStyle == null || toggleButtonStyle.normal.textColor != Color.yellow || toggleButtonStyle.normal.background == null)
            {
                toggleButtonStyle = new GUIStyle(GUI.skin.label);
                toggleButtonStyle.normal.background = new Texture2D(1, 1);
                toggleButtonStyle.normal.background.SetPixel(0, 0, Color.yellow);
                toggleButtonStyle.normal.background.Apply();
                toggleButtonStyle.normal.textColor = Color.black;
            }

            return (GUILayout.Button(label, b ? toggleButtonStyle : GUI.skin.label, GUILayout.Width(14)));
        }

        public static bool DrawToggleButton(GUIContent label, bool b)
        {
            if (toggleButtonStyle == null || toggleButtonStyle.normal.textColor != Color.yellow || toggleButtonStyle.normal.background == null)
            {
                toggleButtonStyle = new GUIStyle(GUI.skin.label);
                toggleButtonStyle.normal.background = new Texture2D(1, 1);
                toggleButtonStyle.normal.background.SetPixel(0, 0, Color.yellow);
                toggleButtonStyle.normal.background.Apply();
                toggleButtonStyle.normal.textColor = Color.black;
            }

            return (GUILayout.Button(label, b ? toggleButtonStyle : GUI.skin.label, GUILayout.Width(14)));
        }

        public static int SelectionGrid(int index, ref Vector2 scroll, GUIContent[] contents, int imageSize = 64, int maxX = 4, bool supportUnselected = false)
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < contents.Length; ++i)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(imageSize), GUILayout.Height(imageSize));
                index = DrawSelectionElement(r, i, index, contents[i].image as Texture2D, contents[i].text);
                if (i % maxX == maxX-1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

            }

            // if unselected is allowed, then the index is -1 which needs to be available for selection
            int minIndex = supportUnselected ? -1 : 0;

            index = Mathf.Clamp(index, minIndex, contents.Length - 1);
            EditorGUILayout.EndHorizontal();
            return index;
        }

        public static void DrawMinMax(GUIContent label, ref float valMin, ref float valMax, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            valMin = EditorGUILayout.FloatField(valMin, GUILayout.Width(38));
            EditorGUILayout.MinMaxSlider(ref valMin, ref valMax, min, max);
            valMax = EditorGUILayout.FloatField(valMax, GUILayout.Width(38));
            EditorGUILayout.EndHorizontal();
        }

        public static Vector2 DrawMinMax(GUIContent label, Vector2 vals, Vector2 limits)
        {
            if (limits == Vector2.zero)
            {
                vals = EditorGUILayout.Vector2Field(label, vals);
            }
            else
            {
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                vals.x = EditorGUILayout.FloatField(vals.x, GUILayout.Width(60));
                float x = vals.x;
                float y = vals.y;
                EditorGUILayout.MinMaxSlider(ref x, ref y, limits.x, limits.y);
                vals.x = x;
                vals.y = y;
                vals.y = EditorGUILayout.FloatField(vals.y, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            return vals;
        }

        public static Vector2 DrawMinMax(string label, Vector2 vals, Vector2 limits)
        {
            if (limits == Vector2.zero)
            {
                vals = EditorGUILayout.Vector2Field(label, vals);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                vals.x = EditorGUILayout.FloatField(vals.x, GUILayout.Width(60));
                float x = vals.x;
                float y = vals.y;
                EditorGUILayout.MinMaxSlider(ref x, ref y, limits.x, limits.y);
                vals.x = x;
                vals.y = y;
                vals.y = EditorGUILayout.FloatField(vals.y, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            return vals;
        }


        static GUIContent CScaleShrink = new GUIContent("S", "Shrink the scale range by adding to X and subtracting from Y");
        static GUIContent CScaleReset = new GUIContent("R", "Reset scale to 1");
        static GUIContent CScaleExpand = new GUIContent("E", "Expand the scale range by subtracting from X and adding to Y");

        /// <summary>
        /// Create a Vector2 input field with quick buttons for scale range editing.
        /// The range can be shrinked or expanded by 0.1 and reset to 1.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="scaleRange"></param>
        /// <returns></returns>
        public static Vector2 ScaleRange(string label, ref Vector2 scaleRange)
        {

            GUILayout.BeginHorizontal();
            {
                scaleRange = EditorGUILayout.Vector2Field(label, scaleRange);

                if (GUILayout.Button(CScaleShrink, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    scaleRange += new Vector2(0.1f, -0.1f);
                }
                if (GUILayout.Button(CScaleReset, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    scaleRange = Vector2.one;
                }
                if (GUILayout.Button(CScaleExpand, EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    scaleRange += new Vector2(-0.1f, 0.1f);
                }
            }
            GUILayout.EndHorizontal();

            return scaleRange;

        }


        static GUIContent CDistanceFromTree = new GUIContent("Distance From Tree", "Min and max distance from all previous tree's to spawn from");
        static GUIContent CDistanceFromObject = new GUIContent("Distance From Object", "Min and max distance from all object's to spawn from");
        static GUIContent CDistanceFromParent = new GUIContent("Distance From Parent", "Min and max distance from the parent stamps objects or tree's to spawn");
        static GUIContent CClamp = new GUIContent("Clamp Distance Filter", "Clamp value to 0 or 1 so weight is not affeted by distance values");
        public static void DoSDFFilter(SerializedObject serializedObject)
        {
            var mindt = serializedObject.FindProperty("minDistanceFromTree").floatValue;
            var maxdt = serializedObject.FindProperty("maxDistanceFromTree").floatValue;
            var mindo = serializedObject.FindProperty("minDistanceFromObject").floatValue;
            var maxdo = serializedObject.FindProperty("maxDistanceFromObject").floatValue;
            var mindp = serializedObject.FindProperty("minDistanceFromParent").floatValue;
            var maxdp = serializedObject.FindProperty("maxDistanceFromParent").floatValue;
            var clamp = serializedObject.FindProperty("sdfClamp").boolValue;
            if (maxdt < mindt) (mindt, maxdt) = (maxdt, mindt);
            if (maxdo < mindo) (mindo, maxdo) = (maxdo, mindo);
            if (maxdp < mindp) (mindp, maxdp) = (maxdp, mindp);
            // clamp to two digits..
            mindt = Mathf.Floor(mindt * 100) / 100;
            mindo = Mathf.Floor(mindo * 100) / 100;
            mindp = Mathf.Floor(mindp * 100) / 100;
            maxdt = Mathf.Floor(maxdt * 100) / 100;
            maxdo = Mathf.Floor(maxdo * 100) / 100;
            maxdp = Mathf.Floor(maxdp * 100) / 100;

            EditorGUI.BeginChangeCheck();
            DrawMinMax(CDistanceFromTree, ref mindt, ref maxdt, 0.0f, 255.0f);
            DrawMinMax(CDistanceFromObject, ref mindo, ref maxdo, 0.0f, 255.0f);
            DrawMinMax(CDistanceFromParent, ref mindp, ref maxdp, 0.0f, 255.0f);
            clamp = EditorGUILayout.Toggle(CClamp, clamp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("minDistanceFromTree").floatValue = mindt;
                serializedObject.FindProperty("maxDistanceFromTree").floatValue = maxdt;
                serializedObject.FindProperty("minDistanceFromObject").floatValue = mindo;
                serializedObject.FindProperty("maxDistanceFromObject").floatValue = maxdo;
                serializedObject.FindProperty("minDistanceFromParent").floatValue = mindp;
                serializedObject.FindProperty("maxDistanceFromParent").floatValue = maxdp;
                serializedObject.FindProperty("sdfClamp").boolValue = clamp;
            }
        }

        static Material noisePreviewMat;
        public static FilterSet.NoiseOp DrawNoise(Object owner, Noise noise, string label = "Noise", FilterSet.NoiseOp noiseOp = FilterSet.NoiseOp.Add, bool secondaryNoises = false, bool allowStampSpace = true)
        {
            if (noisePreviewMat == null)
            {
                noisePreviewMat = new Material(Shader.Find("Hidden/MicroVerse/NoisePreview"));
            }
            List<string> keywords = new List<string>();

            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);

            EditorGUILayout.BeginHorizontal();
            var noiseType = (Noise.NoiseType)EditorGUILayout.EnumPopup(label, noise.noiseType);

            var old = GUI.enabled;
            
            GUI.enabled = old && noiseType != Noise.NoiseType.None;
            if (DrawToggleButton("P", Object.ReferenceEquals(PreviewRenderer.noisePreview, noise)))
            {
                if (Object.ReferenceEquals(PreviewRenderer.noisePreview, noise))
                {
                    PreviewRenderer.noisePreview = null;
                }
                else
                {
                    PreviewRenderer.noisePreview = noise;
                }
            }
            GUI.enabled = old;
            EditorGUILayout.EndHorizontal();

            if (noiseType != noise.noiseType)
            {
                // disable preview if we're turning noise off.
                if (noiseType == Noise.NoiseType.None)
                {
                    if (ReferenceEquals(PreviewRenderer.noisePreview, noise))
                        PreviewRenderer.noisePreview = null;
                }
                Undo.RecordObject(owner, "Adjust Noise");
                noise.noiseType = noiseType;
                EditorUtility.SetDirty(owner);
            }


            if (noise.noiseType != Noise.NoiseType.None && noise.noiseType != Noise.NoiseType.Texture)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(128), GUILayout.Height(128));
                if (noise.noiseType == Noise.NoiseType.Texture)
                {
                    EditorGUI.DrawPreviewTexture(r, noise.texture);
                }
                else
                {
                    noise.EnableKeyword(noisePreviewMat, "_", keywords);
                    noisePreviewMat.SetVector("_Param", noise.GetParamVector());
                    noisePreviewMat.SetVector("_Param2", noise.GetParam2Vector());
                    noisePreviewMat.SetVector("_Remap", new Vector2(-noise.displayGamma, 1 + noise.displayGamma));
                    noisePreviewMat.shaderKeywords = keywords.ToArray();
                    EditorGUI.DrawPreviewTexture(r, Texture2D.blackTexture, noisePreviewMat);
                }
                r =  EditorGUILayout.GetControlRect(GUILayout.Width(128));
                var centeredStyle = GUI.skin.GetStyle("Label");
                centeredStyle.alignment = TextAnchor.UpperCenter;
                noise.displayGamma = GUI.HorizontalSlider(r, noise.displayGamma, 0, 20);
                var guiContent = new GUIContent((-noise.displayGamma).ToString("0.0") + " to " + (noise.displayGamma + 1).ToString("0.0"), "Blue = Values below 0, Red = Values above 1, Gamma remaps the range");
                GUI.Label(r, guiContent, centeredStyle);
                GUI.tooltip = "";
                EditorGUILayout.EndVertical();
                RenderTexture.active = null; // ugh, why? Throws warning about interal Unity render texture
                
                float prevLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;

                EditorGUILayout.BeginVertical();
                EditorGUILayout.GetControlRect();
                if (secondaryNoises && noiseType != Noise.NoiseType.None)
                {
                    EditorGUILayout.BeginHorizontal();
                    noiseOp = (FilterSet.NoiseOp)EditorGUILayout.EnumPopup("Operation", noiseOp);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                Noise.NoiseSpace space = Noise.NoiseSpace.World;
                if (allowStampSpace)
                {
                    space = (Noise.NoiseSpace)EditorGUILayout.EnumPopup("Space", noise.noiseSpace);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                float frequency = EditorGUILayout.FloatField("Frequency", noise.frequency);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                float amplitude = EditorGUILayout.FloatField("Amplitude", noise.amplitude);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                float offset = EditorGUILayout.FloatField("Offset", noise.offset);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                float balance = EditorGUILayout.Slider("Balance", noise.balance, -1f, 1f);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = prevLabelWidth;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Noise");
                    noise.frequency = frequency;
                    noise.amplitude = amplitude;
                    noise.offset = offset;
                    noise.balance = balance;
                    noise.noiseSpace = space;
                    EditorUtility.SetDirty(owner);
                }
            }
            else if (noise.noiseType == Noise.NoiseType.Texture)
            {
                EditorGUI.BeginChangeCheck();
                var texture = (Texture2D)EditorGUILayout.ObjectField("Texture", noise.texture, typeof(Texture2D), false);
                EditorGUILayout.BeginHorizontal();
                Noise.NoiseSpace space = (Noise.NoiseSpace)EditorGUILayout.EnumPopup("Space", noise.noiseSpace);
                EditorGUILayout.EndHorizontal();
                var channel = (FalloffFilter.TextureChannel)EditorGUILayout.EnumPopup("Channel", noise.channel);
                Vector2 scale = new Vector2(noise.textureST.x, noise.textureST.y);
                Vector2 offset = new Vector2(noise.textureST.z, noise.textureST.w);
                scale = EditorGUILayout.Vector2Field("Scale", scale);
                offset = EditorGUILayout.Vector2Field("Offset", offset);
                
                EditorGUILayout.BeginHorizontal();

                float prevLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;

                float amplitude = EditorGUILayout.FloatField("Amplitude", noise.amplitude);
                float balance = EditorGUILayout.Slider("Balance", noise.balance, -1f, 1f);

                EditorGUIUtility.labelWidth = prevLabelWidth;

                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Noise");
                    noise.texture = texture;
                    noise.textureST = new Vector4(scale.x, scale.y, offset.x, offset.y);
                    noise.channel = channel;
                    noise.amplitude = amplitude;
                    noise.balance = balance;
                    noise.noiseSpace = (Noise.NoiseSpace)space;
                    EditorUtility.SetDirty(owner);
                }
            }
            EditorGUILayout.EndVertical();
            return noiseOp;
        }

        static int filterSelection = 0;
        static int filterSelectionWithTexture = 0;
        

        static void DrawFilter(Object owner, FilterSet.Filter filter, Vector2 rangeLimit, PreviewRenderer.FilterSetType type)
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var old = GUI.enabled;
            filter.enabled = EditorGUILayout.Toggle(filter.enabled, GUILayout.Width(20));
            GUI.enabled = filter.enabled;
            filter.weight = EditorGUILayout.Slider("Weight", filter.weight, 0, 1);

            bool active = Object.ReferenceEquals(PreviewRenderer.filter, filter);
            if (DrawToggleButton("P", active ))
            {
                if (Object.ReferenceEquals(PreviewRenderer.filter, filter))
                {
                    PreviewRenderer.filter = null;
                    PreviewRenderer.filterSet = null;
                }
                else
                {
                    PreviewRenderer.filter = filter;
                    PreviewRenderer.filterSetType = type;
                    PreviewRenderer.filterSet = null;
                }
            }
            GUI.enabled = old;


            EditorGUILayout.EndHorizontal();
            if (filter.enabled)
            {
                filter.filterType = (FilterSet.Filter.FilterType)EditorGUILayout.EnumPopup("Filter Type", filter.filterType);
                if (filter.filterType == FilterSet.Filter.FilterType.Simple)
                {
                    filter.range = GUIUtil.DrawMinMax("Range", filter.range, rangeLimit);
                    filter.smoothness = EditorGUILayout.Vector2Field("Smoothing", filter.smoothness);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    filter.curve = EditorGUILayout.CurveField("Curve", filter.curve, Color.white, new Rect(0, 0, 1, 1));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (filter._curveTexture != null)
                        {
                            GameObject.DestroyImmediate(filter._curveTexture);
                            filter._curveTexture = null;
                        }
                    }
                }
                if (type == PreviewRenderer.FilterSetType.Curvature)
                {
                    filter.mipBias = EditorGUILayout.Slider("Mip Bias", filter.mipBias, 0, 6);
                }

                GUIUtil.DrawNoise(owner, filter.noise);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(owner);
            }
        }

        public static void DrawFilterSet(Object owner, FilterSet filterSet,
            SerializedProperty otherTextureWeight,
            SerializedProperty textureWeightsProp,
            SerializedProperty textureFilterEnabledProp,
            Transform trans,
            bool allowGlobalFalloff)
        {
            EditorGUI.BeginChangeCheck();
            DrawFalloffFilter(owner, filterSet.falloffFilter, trans, allowGlobalFalloff);
            
            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);
            filterSet.weight = EditorGUILayout.Slider("Layer Weight", filterSet.weight, 0, 1.0f);
            EditorGUI.indentLevel++;
            GUIUtil.DrawNoise(owner, filterSet.weightNoise);
            filterSet.weight2NoiseOp = GUIUtil.DrawNoise(owner, filterSet.weight2Noise, "Noise2", filterSet.weight2NoiseOp, true);
            filterSet.weight3NoiseOp = GUIUtil.DrawNoise(owner, filterSet.weight3Noise, "Noise3", filterSet.weight3NoiseOp, true);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(owner);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            //GUIUtil.DrawSeparator();
            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);
            EditorGUI.BeginChangeCheck();
            filterSelectionWithTexture = DrawFilterSelector(filterSelectionWithTexture, filterSet, true);
            if (EditorGUI.EndChangeCheck())
            {
                PreviewRenderer.filter = null;
                PreviewRenderer.filterSet = null;
                EditorUtility.SetDirty(owner);
            }
            switch (filterSelectionWithTexture)
            {
                case 0:
                    DrawFilter(owner, filterSet.heightFilter, Vector2.zero, PreviewRenderer.FilterSetType.Height);
                    break;
                case 1:
                    DrawFilter(owner, filterSet.slopeFilter, new Vector2(0, 90), PreviewRenderer.FilterSetType.Slope);
                    break;
                case 2:
                    DrawFilter(owner, filterSet.angleFilter, new Vector2(-360, 360), PreviewRenderer.FilterSetType.Angle);
                    break;
                case 3:
                    DrawFilter(owner, filterSet.curvatureFilter, new Vector2(0, 1), PreviewRenderer.FilterSetType.Curvature);
                    break;
                case 4:
                    DrawFilter(owner, filterSet.flowFilter, new Vector2(0, 1), PreviewRenderer.FilterSetType.Flow);
                    break;
                case 5:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(textureFilterEnabledProp);
                    var old = GUI.enabled;
                    GUI.enabled = textureFilterEnabledProp.boolValue;
                    bool active = Object.ReferenceEquals(PreviewRenderer.filterSet, filterSet) && PreviewRenderer.filter == null;
                    if (DrawToggleButton("P", active))
                    {
                        if (active)
                        {
                            PreviewRenderer.filter = null;
                            PreviewRenderer.filterSet = null;
                        }
                        else
                        {
                            PreviewRenderer.filter = null;
                            PreviewRenderer.filterSetType = PreviewRenderer.FilterSetType.Texture;
                            PreviewRenderer.filterSet = filterSet;
                        }
                    }
                    GUI.enabled = old;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(otherTextureWeight);
                    EditorGUILayout.PropertyField(textureWeightsProp);
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        
        static int DrawFilterSelector(int index, FilterSet filterSet, bool textureLayer)
        {
            EditorGUILayout.BeginHorizontal();
            var old = GUI.backgroundColor;
            Color enabledColor = new Color(0.8f, 0.8f, 1.3f);

            if (filterSet.heightFilter.enabled){GUI.backgroundColor = enabledColor; }
            if (index == 0) GUI.backgroundColor *= 1.5f;
            if (GUILayout.Button("Height", GUI.skin.button))
            {
                index = 0;
            }
            GUI.backgroundColor = old;
            if (filterSet.slopeFilter.enabled) { GUI.backgroundColor = enabledColor; }
            if (index == 1) GUI.backgroundColor *= 1.5f;
            if (GUILayout.Button("Slope", GUI.skin.button))
            {
                index = 1;
            }
            GUI.backgroundColor = old;
            if (filterSet.angleFilter.enabled) { GUI.backgroundColor = enabledColor; }
            if (index == 2) GUI.backgroundColor *= 1.5f;
            if (GUILayout.Button("Angle", GUI.skin.button))
            {
                index = 2;
            }
            GUI.backgroundColor = old;
            if (filterSet.curvatureFilter.enabled) { GUI.backgroundColor = enabledColor; }
            if (index == 3) GUI.backgroundColor *= 1.5f;
            if (GUILayout.Button("Curve", GUI.skin.button))
            {
                index = 3;
            }
            GUI.backgroundColor = old;

            if (filterSet.flowFilter.enabled) { GUI.backgroundColor = enabledColor; }
            if (index == 4) GUI.backgroundColor *= 1.5f;
            if (GUILayout.Button("Flow", GUI.skin.button))
            {
                index = 4;
            }
            GUI.backgroundColor = old;

            if (filterSet.textureFilterEnabled) { GUI.backgroundColor = enabledColor; }
            if (index == 5) GUI.backgroundColor *= 1.5f;
            if (textureLayer && GUILayout.Button("Texture", GUI.skin.button))
            {
                index = 5;
            }
            GUI.backgroundColor = old;
            EditorGUILayout.EndHorizontal();
            return index;
        }

        public static void DrawFilterSet(Object owner, FilterSet filterSet, Transform trans, bool allowGlobalFalloff)
        {
            DrawFalloffFilter(owner, filterSet.falloffFilter, trans, allowGlobalFalloff);
            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);
            EditorGUI.BeginChangeCheck();
            filterSet.weight = EditorGUILayout.Slider("Layer Weight", filterSet.weight, 0, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(owner);
            }
            EditorGUI.indentLevel++;
            GUIUtil.DrawNoise(owner, filterSet.weightNoise);
            filterSet.weight2NoiseOp = GUIUtil.DrawNoise(owner, filterSet.weight2Noise, "Noise2", filterSet.weight2NoiseOp, true);
            filterSet.weight3NoiseOp = GUIUtil.DrawNoise(owner, filterSet.weight3Noise, "Noise3", filterSet.weight3NoiseOp, true);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            GUIUtil.DrawSeparator();
            EditorGUILayout.BeginVertical(GUIUtil.boxStyle);
            EditorGUI.BeginChangeCheck();
            
            filterSelection = DrawFilterSelector(filterSelection, filterSet, false);
            if (EditorGUI.EndChangeCheck())
            {
                PreviewRenderer.filter = null;
                PreviewRenderer.filterSet = null;
            }
            switch (filterSelection)
            {
                case 0:
                    DrawFilter(owner, filterSet.heightFilter, Vector2.zero, PreviewRenderer.FilterSetType.Height);
                    break;
                case 1:
                    DrawFilter(owner, filterSet.slopeFilter, new Vector2(0, 90), PreviewRenderer.FilterSetType.Slope);
                    break;
                case 2:
                    DrawFilter(owner, filterSet.angleFilter, new Vector2(-360, 360), PreviewRenderer.FilterSetType.Angle);
                    break;
                case 3:
                    DrawFilter(owner, filterSet.curvatureFilter, new Vector2(0, 1), PreviewRenderer.FilterSetType.Curvature);
                    break;
                case 4:
                    DrawFilter(owner, filterSet.flowFilter, new Vector2(0, 1), PreviewRenderer.FilterSetType.Flow);
                    break;
            }
            EditorGUILayout.EndVertical();
        }


        static double deltaTime;
        static double lastTime;
        static bool activePainting = false;
        static Vector3 prevNormal = Vector3.up;
        public static void DoPaintSceneView(Stamp owner, SceneView sceneView, FalloffFilter.PaintMask pm, Bounds stampBounds, Transform stampTransform,
            float tiltX = 0, float tiltZ = 0)
        {
            if (!pm.painting)
            {
                activePainting = false;
                return;
            }

            deltaTime = EditorApplication.timeSinceStartup - lastTime;
            lastTime = EditorApplication.timeSinceStartup;
            stampBounds.min = new Vector3(stampBounds.min.x, -1, stampBounds.min.z);
            stampBounds.max = new Vector3(stampBounds.max.x, 1, stampBounds.max.z);
            
            Vector3 mousePosition = Event.current.mousePosition;

            // So, in 5.4, Unity added this value, which is basically a scale to mouse coordinates for retna monitors.
            // Not all monitors, just some of them.
            // What I don't get is why the fuck they don't just pass me the correct fucking value instead. I spent hours
            // finding this, and even the paid Unity support my company pays many thousands of dollars for had no idea
            // after several weeks of back and forth. If your going to fake the coordinates for some reason, please do
            // it everywhere to not just randomly break things everywhere you don't multiply some new value in. 
            float mult = EditorGUIUtility.pixelsPerPoint;

            mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y * mult;
            mousePosition.x *= mult;
            Vector3 fakeMP = mousePosition;
            fakeMP.z = 800;
            Vector3 point = sceneView.camera.ScreenToWorldPoint(fakeMP);
            Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);

            Terrain[] terrains = MicroVerse.instance.terrains;
            bool hitSomething = false;
            float distance = float.MaxValue;
            Vector3 normal = Vector3.up;
            foreach (var terrain in terrains)
            {
                for (int i = 0; i < terrains.Length; ++i)
                {
                    if (terrains[i] == null)
                        continue;
                    // Early out if we're not in the area..
                    var cld = terrains[i].GetComponent<Collider>();
                    Bounds b = cld.bounds;
                    // b.Expand(brushSize * 2);
                    if (!b.IntersectRay(ray))
                    {
                        continue;
                    }
                    RaycastHit hit;
                    
                    if (cld.Raycast(ray, out hit, float.MaxValue))
                    {
                        if (Event.current.shift == false)
                        {
                            if (hit.distance < distance)
                            {
                                point = hit.point;
                                normal = hit.normal;
                                hitSomething = true;
                            }
                        }
                    }
                }
            }
            prevNormal = Vector3.Lerp(prevNormal, normal, (float)deltaTime * 5);
            if (hitSomething)
            {
                Handles.color = Color.white;
                //Handles.SphereHandleCap(0, point, Quaternion.identity, brushSize * 2, EventType.Repaint);
                Handles.DrawWireDisc(point, prevNormal, brushSize * 2, 3);
                Handles.DrawLine(point, point + normal * brushSize, 1);
            }
            else
            {
                Handles.color = Color.gray;
                Handles.DrawWireDisc(point, prevNormal, brushSize * 2, 3);
                //Handles.SphereHandleCap(0, point, Quaternion.identity, brushSize * 2, EventType.Repaint);
            }

            var eventType = Event.current.type;
            if (!activePainting && eventType == EventType.MouseDown && Event.current.button == 0)
            {
                activePainting = true;
                //Event.current.Use();
            }
            if (eventType == EventType.MouseUp && Event.current.button == 0)
            {
                activePainting = false;
                if (pm.updateMode == FalloffFilter.PaintMask.UpdateMode.EndStroke)
                {
                    IModifier mod = (IModifier)owner;
                    if (mod != null)
                        MicroVerse.instance.Invalidate(mod.GetBounds());
                    else
                        MicroVerse.instance.Invalidate(null);
                }
                //Event.current.Use();
            }
            if (eventType == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(stampTransform.GetHashCode(), FocusType.Passive));
            }

            // only paint once per frame
            if (eventType != EventType.Repaint)
            {
                return;
            }

            if (hitSomething && activePainting)
            {
                point = stampTransform.worldToLocalMatrix.MultiplyPoint(point);
                tiltX = Mathf.Clamp01(Mathf.Abs(tiltX));
                tiltZ = Mathf.Clamp01(Mathf.Abs(tiltZ));
                tiltX *= tiltX;
                tiltZ *= tiltZ;
                point.x *= Mathf.Lerp(1, 3.14f, tiltZ);
                point.z *= Mathf.Lerp(1, 3.14f, tiltX);
                point.x += 0.5f;
                point.z += 0.5f;
                if (brushMode == BrushMode.Adjust)
                {
                    pm.Paint(point.x, point.z, brushSize, brushFalloff, brushFlow, targetValue, deltaTime);
                }
                else
                {
                    pm.Smooth(point.x, point.z, brushSize, brushFalloff, brushFlow, targetValue, deltaTime);
                }
                if (pm.updateMode == FalloffFilter.PaintMask.UpdateMode.EveryChange)
                {
                    IModifier mod = (IModifier)owner;
                    if (mod != null)
                        MicroVerse.instance.Invalidate(mod.GetBounds());
                    else
                        MicroVerse.instance.Invalidate(null);
                }
            }
            EditorUtility.SetDirty(owner);

        }
    


        static float targetValue = 0;
        static float brushSize = 20;
        static float brushFalloff = 0.5f;
        static float brushFlow = 20.0f;
        static Material previewBrushMaterial;
        static bool previewMask;
        public enum BrushMode
        {
            Adjust,
            Smooth
        }
        static BrushMode brushMode = BrushMode.Adjust;

        public static void DoPaintGUI(Object owner, FalloffFilter.PaintMask pm)
        {
            if (previewBrushMaterial == null)
            {
                previewBrushMaterial = new Material(Shader.Find("Hidden/MicroVerse/PreviewBrushShader"));
            }
            pm.updateMode = (FalloffFilter.PaintMask.UpdateMode)EditorGUILayout.EnumPopup("Update Mode", pm.updateMode);
            EditorGUILayout.LabelField("Brush Settings");
            using (new GUILayout.VerticalScope(GUI.skin.box))
            { 
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(32));
                var brushPreviewRect = EditorGUILayout.GetControlRect(GUILayout.Width(100), GUILayout.Height(100));
                previewBrushMaterial.SetFloat("_Falloff", brushFalloff);
                previewBrushMaterial.SetFloat("_Size", brushSize / 64.0f);
                EditorGUI.DrawPreviewTexture(brushPreviewRect, Texture2D.whiteTexture, previewBrushMaterial);

                {
                    EditorGUILayout.BeginVertical();
                    brushMode = (BrushMode)EditorGUILayout.EnumPopup("Brush Mode", brushMode);
                    targetValue = EditorGUILayout.Slider("Target Value", targetValue, 0, 1.0f, GUILayout.ExpandWidth(true));
                    brushSize = EditorGUILayout.Slider("Size", brushSize, 1, 64);
                    brushFlow = EditorGUILayout.Slider("Flow", brushFlow, 0.1f, 40.0f);
                    brushFalloff = EditorGUILayout.Slider("Falloff", brushFalloff, 0.1f, 8.0f);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(32));
            if (GUILayout.Button(pm.painting ? "Stop Painting" : "Start Painting"))
            {
                pm.painting = !pm.painting;
            }
            if (GUILayout.Button("Clear"))
            {
                Undo.RecordObject(owner, "Adjust Falloff");
                pm.Clear();
            }
            if (GUILayout.Button("Fill"))
            {
                Undo.RecordObject(owner, "Adjust Falloff");
                pm.Fill((byte)targetValue);
            }

            EditorGUILayout.EndHorizontal();

            if (pm.texture != null)
            {
                previewMask = EditorGUILayout.Foldout(previewMask, "Mask Preview");
                if (previewMask)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(32));
                    EditorGUILayout.Space();
                    var maskPreviewRect = EditorGUILayout.GetControlRect(GUILayout.Width(256), GUILayout.Height(256));
                    EditorGUILayout.Space();
                    EditorGUI.DrawPreviewTexture(maskPreviewRect, pm.texture);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.Space();
            
        }

        // Falloff drawing
        static GUIContent CFilterType = new GUIContent("Falloff Type", "Allows you to choose a shape for the falloff of the stamp");
        static GUIContent CFilterMinMax = new GUIContent("Range", "Start and end of the falloff area, used to smoothly transition weights");
        static GUIContent CFilterTexture = new GUIContent("Texture", "The green channel will control the weight of the effect");
        static GUIContent CFilterSplineArea = new GUIContent("Spline Area", "The spline area to use");
        static GUIContent CFilterPaintArea = new GUIContent("Paint Area", "The falloff paint area to use");
        static GUIContent CTextureSize = new GUIContent("Texture Size", "Size of the backing texture");
        public static void DrawFalloffFilter(Object owner, FalloffFilter f, Transform trans, bool allowGlobal, bool allowPaintMask = true)
        {
            FalloffOverride fo = trans.GetComponentInParent<FalloffOverride>();
            if (fo != null && !(owner is FalloffOverride))
            {
                EditorGUILayout.HelpBox("Falloff is controled by Falloff Override", MessageType.Info);
                return;
            }
            EditorGUI.BeginChangeCheck();
            FalloffFilter.FilterType filterType = f.filterType;

            var old = filterType;
            if (allowPaintMask)
            {
                if (allowGlobal)
                {
                    filterType = (FalloffFilter.FilterType)EditorGUILayout.EnumPopup(CFilterType, f.filterType);
                }
                else
                {
                    FalloffFilter.FilterTypeNoGlobal noGlobal = FalloffFilter.CastEnum(f.filterType, FalloffFilter.FilterTypeNoGlobal.Box);
                    var nog = (FalloffFilter.FilterTypeNoGlobal)EditorGUILayout.EnumPopup(CFilterType, noGlobal);
                    filterType = FalloffFilter.CastEnum(nog, FalloffFilter.FilterType.Box);
                }
            }
            else
            {
                if (allowGlobal)
                {
                    FalloffFilter.FilterTypeNoPaintMask noPaintMask = FalloffFilter.CastEnum(f.filterType, FalloffFilter.FilterTypeNoPaintMask.Box);
                    var nog = (FalloffFilter.FilterTypeNoPaintMask)EditorGUILayout.EnumPopup(CFilterType, noPaintMask);
                    
                    filterType = FalloffFilter.CastEnum(nog, FalloffFilter.FilterType.Box);
                }
                else
                {
                    FalloffFilter.FilterTypeNoPaintMask noGlobalOrPaint = FalloffFilter.CastEnum(f.filterType, FalloffFilter.FilterTypeNoPaintMask.Box);
                    var nog = (FalloffFilter.FilterTypeNoGlobalNoPaintMask)EditorGUILayout.EnumPopup(CFilterType, noGlobalOrPaint);
                    filterType = FalloffFilter.CastEnum(nog, FalloffFilter.FilterType.Box);
                }
                if (filterType != old)
                {
                    if (filterType == FalloffFilter.FilterType.Global ||
                        old == FalloffFilter.FilterType.Global)
                    {
                        EditorUtility.SetDirty(owner);
                        MicroVerse.instance?.Invalidate();
                    }
                }
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(owner, "Adjust Falloff");
                f.filterType = filterType;
            }
            
            EditorGUI.indentLevel++;
            if (f.filterType == FalloffFilter.FilterType.Box)
            {
                EditorGUI.BeginChangeCheck();
                float x = f.falloffRange.x;
                float y = f.falloffRange.y;
                y = EditorGUILayout.Slider(CFilterMinMax, y, 0, 1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Falloff");
                    f.falloffRange = new Vector2(x, y);
                }
            }
            else if (f.filterType == FalloffFilter.FilterType.Range)
            {
                EditorGUI.BeginChangeCheck();
                float x = f.falloffRange.x;
                float y = f.falloffRange.y;
                y = Mathf.Max(x, y);
                EditorGUILayout.MinMaxSlider(CFilterMinMax, ref x, ref y, 0, 1);
                y = Mathf.Max(x, y);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Falloff");
                    f.falloffRange = new Vector2(x, y);
                }
            }
            else if (f.filterType == FalloffFilter.FilterType.Texture)
            {
                EditorGUI.BeginChangeCheck();
                var texture = (Texture2D)EditorGUILayout.ObjectField(CFilterTexture, f.texture, typeof(Texture2D), false);
                var channel = (FalloffFilter.TextureChannel)EditorGUILayout.EnumPopup("Channel", f.textureChannel);
                var amplitude = EditorGUILayout.FloatField("Amplitude", f.textureParams.x);
                var balance = EditorGUILayout.Slider("Balance", f.textureParams.y, -1, 1);
                var clamp = EditorGUILayout.Toggle("Clamp Texture", f.clampTexture);
                var rotation = f.textureRotationScale.x;
                var scale = f.textureRotationScale.y;
                var offset = new Vector2(f.textureRotationScale.z, f.textureRotationScale.w);
                if (!clamp)
                {
                    rotation = EditorGUILayout.Slider("Rotation", f.textureRotationScale.x, -Mathf.PI, Mathf.PI);
                    scale = EditorGUILayout.FloatField("Scale", f.textureRotationScale.y);
                    offset = EditorGUILayout.Vector2Field("Offset", new Vector2(f.textureRotationScale.z, f.textureRotationScale.w));
                }
                
                float x = f.falloffRange.x;
                float y = f.falloffRange.y;
                y = EditorGUILayout.Slider(CFilterMinMax, y, 0, 1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Falloff");
                    f.textureChannel = channel;
                    f.texture = texture;
                    f.textureParams = new Vector2(amplitude, balance);
                    f.textureRotationScale = new Vector4(rotation, scale, offset.x, offset.y);
                    f.clampTexture = clamp;
                    f.falloffRange = new Vector2(x, y);
                }
            }
            else if (f.filterType == FalloffFilter.FilterType.PaintMask)
            {
                EditorGUI.BeginChangeCheck();
                var size = (FalloffFilter.PaintMask.Size)EditorGUILayout.EnumPopup(CTextureSize, f.paintMask.size);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Falloff");
                    f.paintMask.Resize(size);
                }
                DoPaintGUI(owner, f.paintMask);
            }
            else if (f.filterType == FalloffFilter.FilterType.SplineArea)
            {
#if __MICROVERSE_SPLINES__
                EditorGUI.BeginChangeCheck();
                if (f.splineArea == null)
                {
                    var parentArea = (trans.GetComponentInParent<SplineArea>());
                    if (parentArea != null)
                    {
                        f.splineArea = parentArea;
                        EditorUtility.SetDirty(owner);
                    }
                }
                var splineArea = (SplineArea)EditorGUILayout.ObjectField(CFilterSplineArea, f.splineArea, typeof(SplineArea), true);
                var areaFalloff = EditorGUILayout.FloatField("Falloff", f.splineAreaFalloff);
                var boost = EditorGUILayout.FloatField(new GUIContent("Width Boost", "Pushes the falloff area outside of the spline. This lets you use thin, non closed splines, as a area for things like tree's along a path"), f.splineAreaFalloffBoost);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(owner, "Adjust Falloff");
                    f.splineArea = splineArea;
                    f.splineAreaFalloff = areaFalloff;
                    f.splineAreaFalloffBoost = boost;
                }
#else
                EditorGUILayout.HelpBox("The MicroVerse splines module is not installed. Please install it if you want to create spline based areas", MessageType.Error);
#endif
            }

            


            EditorGUI.BeginChangeCheck();
            Easing.BlendShape blend = f.easing.blend;
            if (filterType != FalloffFilter.FilterType.Global)
            {
                blend = (Easing.BlendShape)EditorGUILayout.EnumPopup("Blend", f.easing.blend);
                DrawNoise(owner, f.noise);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(owner, "Adjust Falloff");
                f.easing.blend = blend;
            }

            EditorGUI.BeginChangeCheck();
            var paintArea = (PaintFalloffArea)EditorGUILayout.ObjectField(CFilterPaintArea, f.paintArea, typeof(PaintFalloffArea), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(owner, "Adjust Paint Area");
                f.paintArea = paintArea;
            }

            EditorGUI.indentLevel--;
        }

        static Dictionary<string, bool> rolloutStates = new Dictionary<string, bool>();
        static GUIStyle rolloutStyle;
        public static bool DrawRollup(string text, bool defaultState = true, bool inset = false)
        {
            if (rolloutStyle == null)
            {
                rolloutStyle = new GUIStyle(GUI.skin.box);
                rolloutStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            var oldColor = GUI.contentColor;
            GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            if (inset == true)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.GetControlRect(GUILayout.Width(40));
            }

            if (!rolloutStates.ContainsKey(text))
            {
                rolloutStates[text] = defaultState;
                string key = text;
                if (EditorPrefs.HasKey(key))
                {
                    rolloutStates[text] = EditorPrefs.GetBool(key);
                }
            }
            if (GUILayout.Button(text, rolloutStyle, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(20) }))
            {
                rolloutStates[text] = !rolloutStates[text];
                string key = text;
                EditorPrefs.SetBool(key, rolloutStates[text]);
            }
            if (inset == true)
            {
                EditorGUILayout.GetControlRect(GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }
            GUI.contentColor = oldColor;
            return rolloutStates[text];
        }
        public static void DrawSeparator()
        {
            EditorGUILayout.Separator();
            GUILayout.Box("", boxStyle, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            EditorGUILayout.Separator();
        }
        public static bool DrawRollupToggle(string text, ref bool toggle)
        {
            if (rolloutStyle == null)
            {
                rolloutStyle = new GUIStyle(GUI.skin.box);
                rolloutStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            var oldColor = GUI.contentColor;

            EditorGUILayout.BeginHorizontal(rolloutStyle);
            if (!rolloutStates.ContainsKey(text))
            {
                rolloutStates[text] = true;
                string key = text;
                if (EditorPrefs.HasKey(key))
                {
                    rolloutStates[text] = EditorPrefs.GetBool(key);
                }
            }

            var nt = EditorGUILayout.Toggle(toggle, GUILayout.Width(18));
            if (nt != toggle && nt == true)
            {
                // open when changing toggle state to true
                rolloutStates[text] = true;
                EditorPrefs.SetBool(text, rolloutStates[text]);
            }
            toggle = nt;
            if (GUILayout.Button(text, rolloutStyle, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(20) }))
            {
                rolloutStates[text] = !rolloutStates[text];
                string key = text;
                EditorPrefs.SetBool(key, rolloutStates[text]);
            }
            EditorGUILayout.EndHorizontal();
            GUI.contentColor = oldColor;
            return rolloutStates[text];
        }

        public class PopupTextureLayerSelector : PopupWindowContent
        {
            public override Vector2 GetWindowSize()
            {
                return new Vector2(270, (layers.Count / 4) * 64 + 100);
            }

            List<TerrainLayer> layers = new List<TerrainLayer>();
            GUIContent[] contents;
            public static SerializedProperty currentProperty;
            public override void OnOpen()
            {
                layers.Clear();
                if (MicroVerse.instance == null)
                    return;
                
                MicroVerse.instance.SyncTerrainList();
                var texturing = MicroVerse.instance.GetComponentsInChildren<ITextureModifier>();
                foreach (var t in texturing)
                {
                    foreach (var ter in MicroVerse.instance.terrains)
                    {
                        t.InqTerrainLayers(ter, layers);
                    }
                }
                layers = layers.Distinct().ToList();
                for (int i = 0; i < layers.Count; ++i)
                {
                    if (layers[i] == null)
                    {
                        layers.RemoveAt(i);
                        i--;
                    }
                }
                contents = new GUIContent[layers.Count];
                for (int i = 0; i < layers.Count; ++i)
                {
                    if (layers[i] == currentProperty.objectReferenceValue as TerrainLayer)
                    {
                        selection = i;
                    }
                    contents[i] = new GUIContent(layers[i].name, layers[i].diffuseTexture);
                }
            }

            public override void OnClose()
            {
                layers.Clear();
            }

            Vector2 scroll = new Vector2(0, 0);
            static int selection = 0;
            public static Bounds bounds;
            public override void OnGUI(Rect rect)
            {
                GUILayout.Label("Select Terrain Layer", EditorStyles.boldLabel);
                if (contents.Length > 0)
                {
                    int nselect = SelectionGrid(selection, ref scroll, contents, 64, 4);
                    if (nselect != selection)
                    {
                        selection = nselect;
                        currentProperty.objectReferenceValue = layers[selection];
                        currentProperty.serializedObject.ApplyModifiedProperties();
                        MicroVerse.instance?.Invalidate(bounds, MicroVerse.InvalidateType.Splats);
                    }
                }
            }

        }


        static Vector2 textureSelectionScroll;
        static Rect popupButtonRect;
        public static void DrawTextureLayerSelector(SerializedProperty property, Bounds b, string label = null)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            if (label != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
            else
            {
                EditorGUILayout.PropertyField(property);
            }
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                PopupTextureLayerSelector.bounds = b;
                PopupTextureLayerSelector.currentProperty = property;
                PopupWindow.Show(popupButtonRect, new PopupTextureLayerSelector());
               
            }
            if (Event.current.type == EventType.Repaint)
            {
                popupButtonRect = GUILayoutUtility.GetLastRect();
                popupButtonRect.x -= 256;
            }
            EditorGUILayout.EndHorizontal();
            
            if (MicroVerse.instance == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

#if __MICROSPLAT__
            if (MicroVerse.instance.msConfig == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }
            else
            {
                TerrainLayer l = property.objectReferenceValue as TerrainLayer;
                if (l == null)
                {
                    EditorGUILayout.EndVertical();
                    return;

                }

                if (MicroVerseEditor.GetLayersIfSyncToMS() == null)
                {
                    EditorGUILayout.HelpBox("MicroSplat Texture Arrays are out of sync", MessageType.Warning);
                    if (GUILayout.Button("Sync MicroSplat Texture Arrays"))
                    {
                        MicroVerseEditor.SyncMicroSplat();
                    }
                }
            }
#endif
            EditorGUILayout.EndVertical();

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

        public static Color DropAreaBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);


        /// <summary>
        /// Custom property drawer that allows the selection of the terrain layer via preview
        /// </summary>
        [CustomPropertyDrawer(typeof(TextureFilter))]
        public class TextureFilterPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                {
                    EditorGUI.BeginProperty(position, label, property);
                    EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                    var layerRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                    float buttonWidth = 60f;
                    var layerRectField = new Rect(layerRect);
                    layerRectField.width -= buttonWidth;
                    var layerRectButton = new Rect(layerRect);
                    layerRectButton.x = layerRectButton.x + layerRectButton.width - buttonWidth;
                    layerRectButton.width = buttonWidth;

                    var amountRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width, EditorGUIUtility.singleLineHeight);
                    var unitRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 3, position.width, EditorGUIUtility.singleLineHeight);
                    var nameRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 4, position.width, EditorGUIUtility.singleLineHeight);

                    // Draw fields - pass GUIContent.none to each so they are drawn without labels
                    if (GUI.Button(layerRectButton, "Select"))
                    {
                        //PopupTextureLayerSelector.bounds = layerRect;
                        PopupTextureLayerSelector.currentProperty = property.FindPropertyRelative("layer");
                        PopupWindow.Show(layerRectButton, new PopupTextureLayerSelector());

                    }

                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(layerRectField, property.FindPropertyRelative("layer"), new GUIContent("Layer"));
                    EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("weight"), new GUIContent("Weight"));
                    EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("amplitude"), new GUIContent("Amplitude"));
                    EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("balance"), new GUIContent("Balance"));
                    EditorGUI.indentLevel--;

                }
                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight * 5;
            }
        }

    }
}