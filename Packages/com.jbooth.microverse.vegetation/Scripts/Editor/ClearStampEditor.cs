using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{

    [CustomEditor(typeof(ClearStamp))]
    public class ClearStampEditor : Editor
    {
        static List<ISpawner> spawners = new List<ISpawner>(2);
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            ClearStamp tp = (ClearStamp)target;
            tp.transform.GetComponents<ISpawner>(spawners);
            if (spawners.Count > 1)
            {
                EditorGUILayout.HelpBox("Clear Stamp should be on it's own game object, otherwise SDF fields can be incorrectly computed", MessageType.Error);
            }

            serializedObject.Update();
            
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearTrees"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearDetails"));
#if __MICROVERSE_OBJECTS__
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clearObjects"));
#endif
            

            

            if (tp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the heriarchy, will have no effect", MessageType.Warning);
            }


            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");
            GUIUtil.DrawFilterSet(tp, tp.filterSet, otherTextureWeight, filters, filtersEnabled, tp.transform, true);

            serializedObject.ApplyModifiedProperties();

            if (changeScope.changed)
            {
                MicroVerse.instance?.Invalidate(tp.GetBounds(), MicroVerse.InvalidateType.All); // because tree's can now mod height/splats
            }

        }

        private void OnSceneGUI()
        {
            var stamp = (ClearStamp)target;
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

        static Texture2D overlayTex;
        private void OnSceneRepaint(SceneView sceneView)
        {
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_tree");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as ClearStamp);
                if (hs == null) return;
                Color color = Color.green;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.treeStampColor;
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
                var clear = (ClearStamp)target;
                if (clear != null && clear.transform.hasChanged)
                {
                    clear.transform.hasChanged = false;
                    var r = clear.transform.localRotation.eulerAngles;
                    r.z = 0;
                    clear.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(clear.GetBounds(), MicroVerse.InvalidateType.All);
                }
            }
        }
    }
}
