using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2021_3_OR_NEWER

using UnityEngine.Splines;
using UnityEditor.Splines;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(SplinePath), true)]
    [CanEditMultipleObjects]
    class SplinePathEditor : Editor
    {
        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            EditorSplineUtility.RegisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
            Spline.Changed += OnSplineChanged;
        }

        private void OnSplineChanged(Spline spline, int arg2, SplineModification arg3)
        {
            var path = target as SplinePath;
            if (path == null) return;
            if (MicroVerse.instance == null) return;
            if (!MicroVerse.instance.enabled) return;
            if (!path.enabled) return;

            if (path.spline != null)
            {
                foreach (var s in path.spline.Splines)
                {
                    if (ReferenceEquals(spline, s))
                    {
                        path.UpdateSplineSDFs();
                        MicroVerse.instance?.Invalidate(path.GetBounds());
                        return;
                    }
                }
            }
        }

        
        void OnAfterSplineDataWasModified(SplineData<float> splineData)
        {
            var path = target as SplinePath;
       
            if (path == null) return;
            if (MicroVerse.instance == null) return;
            if (!MicroVerse.instance.enabled) return;
            if (!path.enabled) return;

            foreach (var sw in path.splineWidths)
            {
                if (splineData == sw.widthData)
                {
                    path.UpdateSplineSDFs();
                    EditorUtility.SetDirty(path);
                    MicroVerse.instance?.Invalidate(path.GetBounds());
                }
            }
        }


        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            EditorSplineUtility.UnregisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
            Spline.Changed -= OnSplineChanged;

        }

        private void OnUpdate()
        {
            foreach (var target in targets)
            {
                var path = (SplinePath)target;
                if (path != null && path.transform.hasChanged)
                {
                    path.UpdateSplineSDFs();
                    path.transform.hasChanged = false;
                    MicroVerse.instance?.Invalidate(path.GetBounds());
                }
            }
        }
        static GUIContent CWidthEasing = new GUIContent("Width Easing", "Controls the easing curve for the width of the spline when not consistent");
        public override void OnInspectorGUI()
        {
            GUIUtil.DrawHeaderLogo();
            using var changeScope = new EditorGUI.ChangeCheckScope();
            SplinePath sp = (SplinePath)target;
            if (sp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the heriarchy, will have no effect", MessageType.Warning);
            }
            serializedObject.Update();
            if (sp.multiSpline == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spline"));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("treatAsSplineArea"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sdfRes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchQuality"));
            
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                GUIUtil.DrawNoise(sp, sp.positionNoise, "Position Noise", FilterSet.NoiseOp.Add, false, false);
                GUIUtil.DrawNoise(sp, sp.widthNoise, "Width Noise", FilterSet.NoiseOp.Add, false, false);
            }

            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                var hprop = serializedObject.FindProperty("modifyHeightMap");
                EditorGUILayout.PropertyField(hprop);
                if (hprop.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("heightBlendMode"));

                    if (serializedObject.FindProperty("heightBlendMode").enumValueIndex == 3)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("blend"));
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothness"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trench"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useTrenchCurve"));
                    if (serializedObject.FindProperty("useTrenchCurve").boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.CurveField(serializedObject.FindProperty("trenchCurve"), Color.blue, new Rect(0, 1, -10, 10));
                        if (EditorGUI.EndChangeCheck())
                        {
                            sp.ClearCachedSplineTrenchCurve();
                        }
                    }
                    GUIUtil.DrawNoise(sp, sp.heightNoise, "Height Noise");
                    EditorGUILayout.Space();
                    using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("embankmentEasing").FindPropertyRelative("blend"), new GUIContent("Embankment Easing"));
                        GUIUtil.DrawNoise(sp, sp.embankmentNoise, "Embankment Noise");
                    }
                    EditorGUI.indentLevel--;
                }
            }

            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                var sprop = serializedObject.FindProperty("modifySplatMap");
                EditorGUILayout.PropertyField(sprop);
                if (sprop.boolValue)
                {
                    EditorGUI.indentLevel++;
                    GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("layer"), sp.GetBounds());
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splatWeight"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splatWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splatSmoothness"));
                    GUIUtil.DrawNoise(sp, sp.splatNoise);
                    GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("embankmentLayer"), sp.GetBounds());
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useTextureCurve"));
                    if (serializedObject.FindProperty("useTextureCurve").boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.CurveField(serializedObject.FindProperty("textureCurve"), Color.blue, new Rect(0, 0, 1, 1));
                        if (EditorGUI.EndChangeCheck())
                        {
                            sp.ClearCachedSplineTextureCurve();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                else if (serializedObject.FindProperty("layer").objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("Layer still has a value and will be applied to terrain", MessageType.Warning);
                    GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("layer"), sp.GetBounds());
                }
                else if (serializedObject.FindProperty("embankmentLayer").objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("Layer still has a value and will be applied to terrain", MessageType.Warning);
                    GUIUtil.DrawTextureLayerSelector(serializedObject.FindProperty("embankmentLayer"), sp.GetBounds());
                }

            }
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                var ocm = serializedObject.FindProperty("occludeHeightMod");
                EditorGUILayout.PropertyField(ocm);
                if (ocm.boolValue != false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeHeightWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeHeightSmoothness"));
                    EditorGUI.indentLevel--;
                }
                var oct = serializedObject.FindProperty("occludeTextureMod");
                EditorGUILayout.PropertyField(oct);
                if (oct.boolValue != false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeTextureWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("occludeTextureSmoothness"));
                    EditorGUI.indentLevel--;
                }
                var clearTrees = serializedObject.FindProperty("clearTrees");
                EditorGUILayout.PropertyField(clearTrees, new GUIContent("Occlude Trees", "Occludes any trees from spawning on the path"));
                if (clearTrees.boolValue != false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("treeWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("treeSmoothness"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useTreeCurve"));

                    if (serializedObject.FindProperty("useTreeCurve").boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.CurveField(serializedObject.FindProperty("treeCurve"), Color.blue, new Rect(0, 0, 1, 1));
                        if (EditorGUI.EndChangeCheck())
                        {
                            sp.ClearCachedSplineTreeCurve();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                var clearDetails = serializedObject.FindProperty("clearDetails");
                EditorGUILayout.PropertyField(clearDetails, new GUIContent("Occlude Details", "Occludes any details from appearing on the path"));
                if (clearDetails.boolValue != false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("detailWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("detailSmoothness"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useDetailCurve"));

                    if (serializedObject.FindProperty("useDetailCurve").boolValue)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.CurveField(serializedObject.FindProperty("detailCurve"), Color.blue, new Rect(0, 0, 1, 1));
                        if (EditorGUI.EndChangeCheck())
                        {
                            sp.ClearCachedSplineDetailCurve();
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                var clearObjects = serializedObject.FindProperty("clearObjects");
                EditorGUILayout.PropertyField(clearObjects, new GUIContent("Occlude Objects", "Occlude any objects from spawning on the path"));
                if (clearObjects.boolValue != false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("objectWidth"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("objectSmoothness"));
                    EditorGUI.indentLevel--;
                }
            }
            using (new GUILayout.VerticalScope(GUIUtil.boxStyle))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("splineWidthEasing").FindPropertyRelative("blend"), CWidthEasing);
            }
            serializedObject.ApplyModifiedProperties();
            if (changeScope.changed)
            {
                sp.UpdateSplineSDFs();
                MicroVerse.instance?.Invalidate(sp.GetBounds());
            }
            EditorGUILayout.BeginHorizontal();
            /*
            if (GUILayout.Button("Add Objects along Spline"))
            {
                foreach (var target in targets)
                {
                    SplinePath sps = (SplinePath)target;
                    sps.gameObject.AddComponent<SplineInstantiate>();
                }
            }

            
            GUI.enabled = false;
            if (GUILayout.Button("Add Spline Mesh"))
            {
                foreach (var target in targets)
                {
                    SplinePath sps = (SplinePath)target;
                    //sps.gameObject.AddComponent<SplineMesh>();
                }
            }
       
            GUI.enabled = true;
            */
            EditorGUILayout.EndHorizontal();
        }
    }

}
#endif

