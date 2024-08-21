using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomPreview(typeof(HeightStamp))]
    public class HeightStampPreview : ObjectPreview
    {
        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var hs = (target as HeightStamp);
            if (hs.stamp != null)
                GUI.DrawTexture(r, hs.stamp, ScaleMode.ScaleToFit);
        }
    }

    [CustomEditor(typeof(HeightStamp), true)]
    [CanEditMultipleObjects]
    class HeightStampEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            
            serializedObject.Update();
            var heightmapStamp = (HeightStamp)target;

            if (heightmapStamp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stamp"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                MicroVerse.instance?.Invalidate(heightmapStamp.GetBounds());
            }
            EditorGUILayout.EndHorizontal();

            // check for single channel import settings, which break in iOS/Andoid
            if (heightmapStamp.stamp != null)
            {
                var path = AssetDatabase.GetAssetPath(heightmapStamp.stamp);
                if (!string.IsNullOrEmpty(path))
                {
                    var ai = AssetImporter.GetAtPath(path);
                    if (ai != null)
                    {
                        TextureImporter ti = ai as TextureImporter;
                        if (ti != null)
                        {
                            if (ti.textureType == TextureImporterType.SingleChannel)
                            {
                                EditorGUILayout.HelpBox("Stamp is set to \"Single Channel\" and may not import correctly when set to iOS or Android. Please set the texture type to Default", MessageType.Warning);
                                if (GUILayout.Button("Fix"))
                                {
                                    ti.textureType = TextureImporterType.Default;
                                    ti.sRGBTexture = false;
                                    ti.SaveAndReimport();
                                }
                            }
                        }
                    }
                }
            }
           
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
            if (heightmapStamp.stamp != null && heightmapStamp.stamp.mipmapCount > 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mipBias"));
            }
            else if (heightmapStamp.stamp != null)
            {
                var old = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mipBias"));
                GUI.enabled = old;
            }
            
            heightmapStamp.scaleOffset = EditorGUILayout.Vector4Field("Scale Offset", heightmapStamp.scaleOffset);
            //heightmapStamp.remapRange = GUIUtil.DrawMinMax("Remap Range", heightmapStamp.remapRange, new Vector2(0, 1));
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                var tscaleX = serializedObject.FindProperty("tiltScaleX");
                var tscaleZ = serializedObject.FindProperty("tiltScaleZ");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tiltX"));
                EditorGUILayout.LabelField("Scale", GUILayout.Width(40));
                bool tsx = EditorGUILayout.Toggle(tscaleX.boolValue, GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tiltZ"));
                EditorGUILayout.LabelField("Scale", GUILayout.Width(40));
                bool tsz = EditorGUILayout.Toggle(tscaleZ.boolValue, GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    tscaleX.boolValue = tsx;
                    tscaleZ.boolValue = tsz;
                }
                if (serializedObject.FindProperty("mode").enumValueIndex == 9)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blend"));
                }
              
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useHeightRemap"));
                var old = GUI.enabled;
                GUI.enabled = serializedObject.FindProperty("useHeightRemap").boolValue;
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("remapCurve"));
                EditorGUI.indentLevel--;
                GUI.enabled = old;
                if (EditorGUI.EndChangeCheck())
                {
                    heightmapStamp.ClearRemapCurve();
                }



                EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("invert"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("twist"));
            
                EditorGUILayout.PropertyField(serializedObject.FindProperty("erosion"));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("erosionSize"));
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();


            
            GUIUtil.DrawFalloffFilter(this, heightmapStamp.falloff, heightmapStamp.transform, false);

            if (changeScope.changed)
            {
                EditorUtility.SetDirty(heightmapStamp);
                MicroVerse.instance?.Invalidate(heightmapStamp.GetBounds());
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (HeightStamp)target;
            if (stamp.falloff.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.falloff.paintMask, stamp.GetBounds(), stamp.transform, stamp.tiltScaleX ? stamp.tiltX : 0, stamp.tiltScaleZ ? stamp.tiltZ : 0);
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            SceneView.duringSceneGui += OnSceneRepaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            SceneView.duringSceneGui -= OnSceneRepaint;
        }

        public Bounds OnGetFrameBounds()
        {
            var transforms = Selection.GetTransforms(SelectionMode.Unfiltered);
            Bounds result = new Bounds(transforms[0].position, transforms[0].lossyScale);
            for (int i = 1; i < transforms.Length; i++)
                result.Encapsulate(new Bounds(transforms[i].position, transforms[i].lossyScale));

            result.extents *= 0.5f;
            return result;
        }

        private bool HasFrameBounds() => Selection.objects.Length > 0;


        static Texture2D overlayTex = null;
        private void OnSceneRepaint(SceneView sceneView)
        {
            if (target == null) return;
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_height");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as HeightStamp);
                if (hs == null) return;
                Color color = Color.gray;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.heightStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.falloff, color, overlayTex);
            }
        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var heightmapStamp = (HeightStamp)target;
                if (heightmapStamp.transform.hasChanged)
                {
                    var r = heightmapStamp.transform.localRotation.eulerAngles;
                    
                    r.x = 0;
                    r.z = 0;
                    heightmapStamp.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(heightmapStamp.GetBounds());
                    heightmapStamp.transform.hasChanged = false;
                }
            }
        }
    }
}