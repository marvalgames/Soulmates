using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomPreview(typeof(MeshStamp))]
    public class MeshStampPreview : ObjectPreview
    {
        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var hs = (target as MeshStamp);
            if (hs.targetDepthTexture != null)
                GUI.DrawTexture(r, hs.targetDepthTexture, ScaleMode.ScaleToFit);
        }
    }

    [CustomEditor(typeof(MeshStamp), true)]
    [CanEditMultipleObjects]
    class MeshStampEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();

            serializedObject.Update();
            var meshStamp = (MeshStamp)target;

            if (meshStamp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetObject"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hideRenderers"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (meshStamp.hideRenderers == false)
                {
                    meshStamp.SetHideRenderers(meshStamp.targetObject, true);
                }
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blendMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("heightScale"));
            EditorGUILayout.MinMaxSlider(new GUIContent("Height Clamp", "Clamp the height map range"), ref meshStamp.heightClamp.x, ref meshStamp.heightClamp.y, 0, 1);
           
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resolution"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blur"));

            if (EditorGUI.EndChangeCheck())
            {
                if (meshStamp.targetDepthTexture != null)
                    DestroyImmediate(meshStamp.targetDepthTexture);
                meshStamp.targetDepthTexture = null;
                serializedObject.ApplyModifiedProperties();
                MicroVerse.instance?.Invalidate(meshStamp.GetBounds());
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            GUIUtil.DrawFalloffFilter(this, meshStamp.falloff, meshStamp.transform, false);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(meshStamp);
                MicroVerse.instance?.Invalidate(meshStamp.GetBounds());
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (MeshStamp)target;
            if (stamp.falloff.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.falloff.paintMask, stamp.GetBounds(), stamp.transform);
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneRepaint;
        }

        private void OnDisable()
        { 
            SceneView.duringSceneGui -= OnSceneRepaint;
        }
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
                var hs = (target as MeshStamp);
                if (hs == null) return;
                Color color = Color.gray;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.heightStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.falloff, color, overlayTex);
            }
        }

        
    }
}