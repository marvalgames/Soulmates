using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{


    [CustomEditor(typeof(HeightAreaEffectStamp), true)]
    [CanEditMultipleObjects]
    class HeightAreaEffectStampEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();

            serializedObject.Update();
            var area = (HeightAreaEffectStamp)target;

            if (area.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the hierarchy, will have no effect", MessageType.Warning);
            }
            using var changeScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("effectType"));
            serializedObject.ApplyModifiedProperties();

            switch (area.effectType)
            {
                case HeightAreaEffectStamp.EffectType.Terrace:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("terraceSize"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("terraceStrength"));
                    break;
                case HeightAreaEffectStamp.EffectType.Beach:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("beachDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("beachPower"));
                    break;
                case HeightAreaEffectStamp.EffectType.RemapCurve:
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.CurveField(serializedObject.FindProperty("remapCurve"), Color.blue, new Rect(0, 0, 1, 1));
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (area.remapCurveTex != null)
                        {
                            DestroyImmediate(area.remapCurveTex);
                            area.remapCurveTex = null;
                        }
                    }
                    break;
                case HeightAreaEffectStamp.EffectType.Noise:
                    GUIUtil.DrawNoise(area, area.noise);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("combineMode"));
                    serializedObject.ApplyModifiedProperties();
                    if (area.combineMode == HeightStamp.CombineMode.Blend)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("combineBlend"));
                    }
                    break;

            }


            serializedObject.ApplyModifiedProperties();

            GUIUtil.DrawFalloffFilter(this, area.falloff, area.transform, false);

            if (changeScope.changed)
            {
                EditorUtility.SetDirty(area);
                MicroVerse.instance?.Invalidate(area.GetBounds());
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (HeightAreaEffectStamp)target;
            if (stamp.falloff.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.falloff.paintMask, stamp.GetBounds(), stamp.transform);
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


        //static Texture2D overlayTex = null;
        private void OnSceneRepaint(SceneView sceneView)
        {
            /*
            if (target == null) return;
            RenderTexture.active = sceneView.camera.activeTexture;
            if (MicroVerse.instance != null)
            {
                if (overlayTex == null)
                {
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_height");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as HeightAreaEffectStamp);
                if (hs == null) return;
                Color color = Color.gray;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.heightStampColor;
                }
                PreviewRenderer.DrawStampPreview(hs, terrains, hs.transform, hs.falloff, color, overlayTex);
            }
            */
        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                var heightmapStamp = (HeightAreaEffectStamp)target;
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