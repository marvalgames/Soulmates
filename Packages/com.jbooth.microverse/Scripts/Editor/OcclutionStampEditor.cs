using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(OcclusionStamp))]
    public class OcclusionStampEditor : Editor
    {
        
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            OcclusionStamp stamp = (OcclusionStamp)target;

            if (stamp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }
            using var changeScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeHeightWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeTextureWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeTreeWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeDetailWeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeObjectWeight"));

            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");

            GUIUtil.DrawFilterSet(stamp, stamp.filterSet, otherTextureWeight, filters, filtersEnabled, stamp.transform, true);

            serializedObject.ApplyModifiedProperties();

            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.All);
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


        static Texture2D overlayTex = null;
        private void OnSceneRepaint(SceneView sceneView)
        {
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_occlusion");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as OcclusionStamp);
                if (hs == null) return;
                Color color = Color.magenta;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.occluderStampColor;
                }

                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.filterSet.falloffFilter, color, overlayTex);
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (OcclusionStamp)target;
            if (stamp.filterSet.falloffFilter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.filterSet.falloffFilter.paintMask, stamp.GetBounds(), stamp.transform);
            }
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

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var stamp = (OcclusionStamp)target;
                if (stamp != null && stamp.transform.hasChanged)
                {
                    stamp.transform.hasChanged = false;
                    var r = stamp.transform.localRotation.eulerAngles;
                    r.z = 0;
                    stamp.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(stamp.GetBounds(), MicroVerse.InvalidateType.Tree);
                }
            }
        }
    }
}
