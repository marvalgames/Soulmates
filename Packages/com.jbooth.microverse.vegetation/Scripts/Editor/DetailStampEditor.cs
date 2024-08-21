using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(DetailStamp))]
    public class DetailStampEditor : Editor
    {

        GUIContent detailIcon;
        void LoadDetailIcon(DetailPrototypeSerializable dp)
        {
            detailIcon = new GUIContent();
            if (dp.prototype == null)
            {
                detailIcon.text = "Missing";
            }
            else if (dp.usePrototypeMesh)
            {
                Texture tex = AssetPreview.GetAssetPreview(dp.prototype);
                detailIcon.image = tex != null ? tex : null;
                detailIcon.text = detailIcon.tooltip = dp.prototype != null ? dp.prototype.name : "Missing";
            }
            else
            {
                detailIcon.image = dp.prototypeTexture;
                if (dp.prototypeTexture == null)
                {
                    detailIcon.text = "Missing";
                }
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (DetailStamp)target;
            if (stamp.filterSet.falloffFilter.filterType == FalloffFilter.FilterType.PaintMask)
            {
                GUIUtil.DoPaintSceneView(stamp, SceneView.currentDrawingSceneView, stamp.filterSet.falloffFilter.paintMask, stamp.GetBounds(), stamp.transform);
            }
        }

        Vector2 scrollPosition;
        static Texture2D barTex;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUIUtil.DrawHeaderLogo();
            DetailStamp dp = (DetailStamp)target;
            if (dp.GetComponentInParent<MicroVerse>() == null)
            {
                EditorGUILayout.HelpBox("Stamp is not under MicroVerse in the heriarchy, will have no effect", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("settings"));
            serializedObject.ApplyModifiedProperties();
            if (dp.prototype == null)
            {
                dp.prototype = new DetailPrototypeSerializable();
            }
            var proto = dp.prototype;
            bool allowProtoEdit = true;
            if (dp.settings != null && dp.settings.prototype != null)
            {
                proto = dp.settings.prototype;
                allowProtoEdit = false;
            }
            
            LoadDetailIcon(proto);
            using var changeScope = new EditorGUI.ChangeCheckScope();

            //EditorGUILayout.LabelField("Detail Object to Place");

            if (barTex == null)
            {
                barTex = new Texture2D(1, 1);
                barTex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                barTex.Apply();
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();

            
            if (proto.usePrototypeMesh)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.Width(128), GUILayout.Height(128));
                GUI.DrawTexture(r, detailIcon.image == null ? Texture2D.blackTexture : detailIcon.image);
                r.height /= 5;
                r.y += r.height * 4;
                GUI.DrawTexture(r, barTex);
                EditorGUI.LabelField(r, detailIcon.text);
                //EditorGUILayout.LabelField(detailIcon, GUILayout.Width(128), GUILayout.Height(128));
            }


            EditorGUILayout.EndVertical();

            if (allowProtoEdit)
            {
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button("Create Settings Object From Prototype"))
                {
                    DetailPrototypeSettings settings = ScriptableObject.CreateInstance<DetailPrototypeSettings>();
                    settings.prototype = new DetailPrototypeSerializable(dp.prototype.GetPrototype());
                    System.IO.Directory.CreateDirectory("Assets/MicroVerse/DetailSettings/");
                    string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath("Assets/MicroVerse/DetailSettings/" + dp.name + ".asset");

                    AssetDatabase.CreateAsset(settings, assetPathAndName);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    dp.settings = settings;
                    proto = settings.prototype;
                }
                else
                {
                    TerrainDetailMeshWizard.DrawInspector(dp, dp.prototype);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("occludedByOthers"));
            GUIUtil.DoSDFFilter(serializedObject);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weightRange"));


            var otherTextureWeight = serializedObject.FindProperty("filterSet").FindPropertyRelative("otherTextureWeight");
            var filters = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilters");
            var filtersEnabled = serializedObject.FindProperty("filterSet").FindPropertyRelative("textureFilterEnabled");
            GUIUtil.DrawFilterSet(dp, dp.filterSet, otherTextureWeight, filters, filtersEnabled, dp.transform, true);


            serializedObject.ApplyModifiedProperties();

            if (changeScope.changed)
            {
                EditorUtility.SetDirty(dp);

                MicroVerse.instance?.Invalidate(dp.GetBounds(), MicroVerse.InvalidateType.Tree);
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
                    overlayTex = Resources.Load<Texture2D>("microverse_stamp_detail");
                }
                var terrains = MicroVerse.instance.terrains;
                var hs = (target as DetailStamp);
                if (hs == null)
                    return;
                Color color = Color.yellow;
                if (MicroVerse.instance != null)
                {
                    color = MicroVerse.instance.options.colors.detailStampColor;
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
                var detail = (DetailStamp)target;
                if (detail != null && detail.transform.hasChanged)
                {
                    detail.transform.hasChanged = false;
                    var r = detail.transform.localRotation.eulerAngles;
                    r.z = 0;
                    detail.transform.localRotation = Quaternion.Euler(r);
                    MicroVerse.instance?.Invalidate(detail.GetBounds(), MicroVerse.InvalidateType.Tree);
                }
            }
        }
    }
}
