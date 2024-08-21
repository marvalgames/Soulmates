using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{

    [CustomEditor(typeof(TextureStamp))]
    public class TextureStampEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            using var changeScope = new EditorGUI.ChangeCheckScope();
            TextureStamp sf = (TextureStamp)target;
            if (sf.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("layer"), sf.GetBounds());
            serializedObject.ApplyModifiedProperties();
            if (sf.layer != null)
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(96), GUILayout.Height(96));
                GUI.DrawTexture(rect, (sf.layer.diffuseTexture != null) ? sf.layer.diffuseTexture : Texture2D.blackTexture, ScaleMode.ScaleToFit, false);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreOcclusion"));
            serializedObject.ApplyModifiedProperties();
            GUIUtil.DrawFilterSet(sf, sf.filterSet, sf.transform, true);

            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(sf.GetBounds(), MicroVerse.InvalidateType.Splats);
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

        private bool HasFrameBounds() => Selection.objects.Length > 0;

        public Bounds OnGetFrameBounds()
        {
            var transforms = Selection.GetTransforms(SelectionMode.Unfiltered);
            Bounds result = new Bounds(transforms[0].position, transforms[0].lossyScale);
            for (int i = 1; i < transforms.Length; i++)
                result.Encapsulate(new Bounds(transforms[i].position, transforms[i].lossyScale));
            result.extents *= 0.5f;
            return result;
        }

        static Texture2D overlayTex = null;
        private void OnSceneRepaint(SceneView sceneView)
        {
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_texture");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as TextureStamp);
                if (hs == null) return;
                Color color = Color.clear;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.textureStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.filterSet.falloffFilter, color, overlayTex);
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (TextureStamp)target;
            if (stamp.filterSet.falloffFilter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.filterSet.falloffFilter.paintMask, stamp.GetBounds(), stamp.transform);
            }
        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var splatFilter = (TextureStamp)target;
                if (splatFilter != null && splatFilter.transform.hasChanged)
                {
                    var r = splatFilter.transform.localRotation.eulerAngles;
                    r.x = 0;
                    r.z = 0;
                    splatFilter.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(splatFilter.GetBounds(), MicroVerse.InvalidateType.Splats);
                    splatFilter.transform.hasChanged = false;
                }
            }
        }
    }
}
