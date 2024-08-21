using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{

    [CustomEditor(typeof(HoleStamp))]
    public class HoleStampEditor : Editor
    {
        public bool preview = false;
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            serializedObject.Update();
            using var changeScope = new EditorGUI.ChangeCheckScope();

            HoleStamp ms = (HoleStamp)target;

            if (ms.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }


            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");
            GUIUtil.DrawFilterSet(ms, ms.filterSet, otherTextureWeight, filters, filtersEnabled, ms.transform, true);

            
            serializedObject.ApplyModifiedProperties();

            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(ms.GetBounds(), MicroVerse.InvalidateType.All); // because tree's can now mod height/splats
            }

        }

        private void OnSceneGUI()
        {
            var stamp = (HoleStamp)target;
            if (stamp.filterSet.falloffFilter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.filterSet.falloffFilter.paintMask, stamp.GetBounds(), stamp.transform);
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
                var hs = (target as HoleStamp);
                if (hs == null) return;
                Color color = Color.clear;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.textureStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.filterSet.falloffFilter, color, overlayTex);
            }
        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var hs = (HoleStamp)target;
                if (hs != null && hs.transform.hasChanged)
                {
                    hs.transform.hasChanged = false;
                    var r = hs.transform.localRotation.eulerAngles;
                    r.z = 0;
                    hs.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(hs.GetBounds(), MicroVerse.InvalidateType.All);
                }
            }
        }
    }
}
