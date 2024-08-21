using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace JBooth.MicroVerseCore
{
    public enum ContentType
    {
        Height = 0,
        Texture,
        Vegetation,
        Objects,
        Audio,
        Biomes,
        Roads,
        Caves,
        Global
    }

    public class BrowserContent : ScriptableObject
    {
        public enum RenderPipeline
        {
            Standard = 1,
            URP = 2,
            HDRP = 4
        }

        [Tooltip("The content type controls where this shows up in the browser")]
        public ContentType contentType;
        [Tooltip("Person who authored this preset")]
        public string author;
        [Tooltip("The asset package which this works with")]
        public string packName;
        [Tooltip("A unique id, which links the ad to the content and is used to determine which should be displayed")]
        public string id;

        [HideInInspector] public RenderPipeline renderPipeline = 0;

        public override int GetHashCode()
        {
            unchecked
            {
                return GetBCHashCode() + contentType.GetHashCode();
            }
        }

        public int GetBCHashCode()
        {
            unchecked
            {
                if (string.IsNullOrEmpty(author) || string.IsNullOrEmpty(packName) || string.IsNullOrEmpty(id))
                    return 0;
                return author.GetHashCode() + packName.GetHashCode() + id.GetHashCode();
            }
        }

    }

    [System.Serializable]
    public class ContentData : IEquatable<ContentData>
    {
        [Tooltip("Prefab to instantiate when dragged from the browser")]
        public GameObject prefab;
        [Tooltip("This additional prefab will be instantiated and parented to the previous one. This allows you to ship MV stamps which get attached to a prefab of an existing asset")]
        public GameObject childPrefab;
        [Tooltip("Preview image to use for the prefab in the browser")]
        public Texture2D previewImage;
        [Tooltip("Asset to render a preview for instead of using the preview Image")]
        public GameObject previewAsset;
        public string stamp; // only used for height stamps
        [Tooltip("Preview gradient override to use for this height map")]
        public Texture2D previewGradient;
        [Tooltip("Description for content")]
        public string description;

        public bool Equals(ContentData other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 0;
                if (prefab != null) hash += prefab.GetHashCode();
                if (childPrefab != null) hash += childPrefab.GetHashCode();
                if (previewImage != null) hash += previewImage.GetHashCode();
                if (previewAsset != null) hash += previewAsset.GetHashCode();
                if (previewGradient != null) hash += previewGradient.GetHashCode();
                if (description != null) hash += description.GetHashCode();

                if( stamp != null) hash = hash ^ stamp.GetHashCode();

                return hash;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ContentData))]
    public class ContentDataDrawer : PropertyDrawer
    {
        private int previewIconSize = 96;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // move a bit closer to the drag handle
            position.x -= 8;

            var previewImage = property.FindPropertyRelative("previewImage");

            Rect previewTextureRect = new Rect(position.x, position.y, previewIconSize, previewIconSize);
            Texture2D previewTexture = previewImage.objectReferenceValue as Texture2D;
            if (previewTexture)
            {
                EditorGUI.DrawPreviewTexture(previewTextureRect, previewTexture);
            }


            // margin between preview image and other gui elements
            float margin = 6f;

            position.x += previewIconSize;
            position.x += margin;
            position.width -= previewIconSize;
            position.width -= margin;

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            {
                // Using BeginProperty / EndProperty on the parent property means that
                // prefab override logic works on the entire property.
                EditorGUI.BeginProperty(position, label, property);

                var yH = base.GetPropertyHeight(property, label);

                // Draw fields - pass GUIContent.none to each so they are drawn without labels
                position.height /= 10;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("prefab"));
                position.y += yH;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("childPrefab"));
                position.y += yH;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("previewImage"));
                position.y += yH;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("previewAsset"));
                position.y += yH;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("previewGradient"));
                position.y += yH;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("description"));

                position.y += yH;
                EditorGUI.PrefixLabel(position, new GUIContent("Stamp"));
                position.x = position.width + previewIconSize - 20;
                position.width = 64;
                position.height *= 4;
                string guid = property.FindPropertyRelative("stamp").stringValue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = null;
                if (!string.IsNullOrEmpty(path))
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }

                var nt = EditorGUI.ObjectField(position, tex, typeof(Texture2D), false);
                if (nt != tex)
                {
                    if (nt == null)
                    {
                        property.FindPropertyRelative("stamp").stringValue = null;
                    }
                    else
                    {
                        string npath = AssetDatabase.GetAssetPath(nt);
                        string nguid = AssetDatabase.AssetPathToGUID(npath);
                        property.FindPropertyRelative("stamp").stringValue = nguid;
                    }
                }

                EditorGUI.EndProperty();

            }
            EditorGUIUtility.labelWidth = prevLabelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 10;
        }
    }

    

    [CreateAssetMenu(fileName = "Collection", menuName = "MicroVerse/ContentPack")]
    public class ContentCollection : BrowserContent
    {
        static Material stampPreviewMat;
        [Tooltip("Preview gradient used to colorize any height map found in this package")]
        public Texture2D previewGradient;
        public ContentData[] contents;
        [Tooltip("Optional config data object")]
        public ScriptableObject systemConfig;
        public static Dictionary<string, Texture2D> cachedPreviews = new Dictionary<string, Texture2D>();
        static Texture2D defaultGradient;

        public GUIContent GetPreview(ContentData contentData)
        {
            if (contents == null)
                return new GUIContent("missing");

            var content = new GUIContent("missing", Texture2D.blackTexture);

            if (contentData != null)
            {
                if (contentType == ContentType.Height)
                {
                    if (!string.IsNullOrEmpty(contentData.stamp))
                    {
                        bool erased = true;
                        if (cachedPreviews.ContainsKey(contentData.stamp))
                        {
                            var tex = cachedPreviews[contentData.stamp];
                            if (tex != null)
                            {
                                content = new GUIContent(tex.name, tex);
                                erased = false;
                            }
                            else
                            {
                                cachedPreviews.Remove(contentData.stamp);
                            }
                        }
                        if (erased)
                        {
                            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(new GUID(contentData.stamp)));
                            if (tex == null)
                                return content;
                            var dst = RenderTexture.GetTemporary(96, 96, 0, RenderTextureFormat.ARGB32);
                            dst.name = tex.name;

                            if (defaultGradient == null)
                            {
                                defaultGradient = Resources.Load<Texture2D>("microverse_default_previewgradient");
                            }

                            if (stampPreviewMat == null)
                            {
                                stampPreviewMat = new Material(Shader.Find("Hidden/MicroVerse/StampPreview2D"));
                                stampPreviewMat.SetTexture("_Gradient", defaultGradient);
                            }

                            stampPreviewMat.SetTexture("_Stamp", tex);
                            if (contentData.previewGradient != null)
                            {
                                stampPreviewMat.SetTexture("_Gradient", contentData.previewGradient);
                            }
                            else if (previewGradient != null)
                            {
                                stampPreviewMat.SetTexture("_Gradient", previewGradient);
                            }
                            else
                            {
                                stampPreviewMat.SetTexture("_Gradient", defaultGradient);

                            }
                            UnityEngine.Profiling.Profiler.BeginSample("Generate Previews");
                            Graphics.Blit(null, dst, stampPreviewMat);
                            stampPreviewMat.SetTexture("_Stamp", null);

                            var dtex = new Texture2D(96, 96, TextureFormat.ARGB32, false);
                            dtex.name = tex.name;

                            RenderTexture.active = dst;
                            dtex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                            dtex.Apply();
                            RenderTexture.active = null;
                            RenderTexture.ReleaseTemporary(dst);

                            cachedPreviews[contentData.stamp] = dtex;
                            content = new GUIContent(dst.name, dtex);
                            UnityEngine.Profiling.Profiler.EndSample();
                        }
                    }
                    else
                    {
                        if (contentData.prefab != null)
                        {
                            if (contentData.previewImage != null)
                            {
                                content = new GUIContent(contentData.prefab.name, contentData.previewImage);
                            }
                            else if (contentData.previewAsset)
                            {
                                Texture tex = AssetPreview.GetAssetPreview(contentData.previewAsset);
                                content = new GUIContent(contentData.prefab.name, tex);
                            }
                            else
                            {
                                Texture tex = AssetPreview.GetAssetPreview(contentData.prefab);
                                content = new GUIContent(contentData.prefab.name, tex);
                            }
                        }
                    }
                }
                else
                {
                    if (contentData.prefab != null)
                    {
                        if (contentData.previewImage != null)
                        {
                            content = new GUIContent(contentData.prefab.name, contentData.previewImage);
                        }
                        else if (contentData.previewAsset)
                        {
                            Texture tex = AssetPreview.GetAssetPreview(contentData.previewAsset);
                            content = new GUIContent(contentData.prefab.name, tex);
                        }
                        else
                        {
                            Texture tex = AssetPreview.GetAssetPreview(contentData.prefab);
                            content = new GUIContent(contentData.prefab.name, tex);
                        }
                    }
                }
            }
            
            return content;
        }
    }


}