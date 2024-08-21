using FIMSpace.FEditor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating
{
    public static class StackedTileGenerator
    {
        [System.Serializable]
        public class StackSetup
        {
            public List<Mesh> toStack = new List<Mesh>();
            public bool RandomFlipX = false;
            public bool RandomFlipY = false;
            public bool RandomFlipZ = false;
            public Vector3 SourceRotationCorrection = Vector3.zero;
            public Vector3 SourceScaleCorrection = Vector3.one;
            public Vector3 ExtraSpacing = Vector3.zero;
            public int StackCount = 5;
            public int StackCountX = 1;
            public Vector3 RandomOffsets = Vector3.zero;
            public Vector3 RandomRotations = Vector3.zero;
            public Vector3 RandomScale = Vector3.zero;
            public Vector3 Rotate = Vector3.zero;

            public List<RemovalBox> RemovalBoxes = new List<RemovalBox>();

            public StackSetup Copy()
            {
                return (StackSetup)MemberwiseClone();
            }
        }

        [System.Serializable]
        public struct RemovalBox
        {
            public Vector3 Position;
            public Vector3 Size;
            public Bounds ToBounds()
            {
                return new Bounds(Position, Size);
            }
        }

        [System.Serializable]
        public struct IndividualModifier
        {
            public int InstanceIndex;
            public Vector3 PositionOffset;
            public Vector3 RotationOffset;
            public Vector3 SizeMul;

            public IndividualModifier(int index)
            {
                InstanceIndex = index;
                PositionOffset = Vector3.zero;
                RotationOffset = Vector3.zero;
                SizeMul = Vector3.one;
            }
        }


        static bool _Foldout = true;
        public static Mesh GenerateStack(StackSetup setup, System.Random random = null)
        {
            if (setup == null) return null;
            if (setup.toStack == null) return null;
            if (setup.toStack.Count == 0) return null;


            if (random == null) random = new System.Random(UnityEngine.Random.Range(-100000, 100000));
            Mesh finalMesh = new Mesh();

            for (int i = 0; i < setup.toStack.Count; i++)
            {
                if (setup.toStack[i] == null) return null;
            }

            List<Mesh> shufflingList = new List<Mesh>();

            // Prepare list of corrected meshes and use it for random shuffling
            for (int i = 0; i < setup.toStack.Count; i++)
            {
                shufflingList.Add(FMeshUtils.GetSourceMeshCopy(setup.toStack[i], setup.SourceRotationCorrection, setup.SourceScaleCorrection, EOrigin.Center));
            }

            shufflingList.Shuffle();
            int getI = 0;

            CombineInstance[] meshInstances = new CombineInstance[setup.StackCount * setup.StackCountX];
            CombineInstance preInstY = new CombineInstance();
            CombineInstance preInstYt = new CombineInstance();
            CombineInstance preInstX = new CombineInstance();

            for (int y = 0; y < setup.StackCount; y += 1)
            {
                for (int x = 0; x < setup.StackCountX; x += 1)
                {
                    CombineInstance inst = new CombineInstance();
                    inst.mesh = shufflingList[getI];
                    getI += 1;

                    if (getI == shufflingList.Count)
                    {
                        getI = 0;
                        shufflingList.Shuffle();
                        if ( shufflingList.Count > 1) if (shufflingList[0] == preInstX.mesh) shufflingList.RemoveAt(0);
                    }

                    Vector3 pos;

                    if (y == 0)
                    {
                        pos = inst.mesh.bounds.center;
                        pos.y += inst.mesh.bounds.extents.y;
                    }
                    else
                    {
                        pos = preInstY.transform.PosFromMatrix();
                        pos.y += preInstY.mesh.bounds.extents.y;
                        pos.y += inst.mesh.bounds.extents.y * (1f + setup.ExtraSpacing.y);
                    }

                    if (x > 0)
                    {
                        pos.x = preInstX.transform.PosFromMatrix().x;
                        pos.x += preInstX.mesh.bounds.extents.x;
                        pos.x += inst.mesh.bounds.extents.x * (1f + setup.ExtraSpacing.x);
                    }

                    if (setup.RandomOffsets.x != 0) pos.x += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomOffsets.x;
                    if (setup.RandomOffsets.y != 0) pos.y += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomOffsets.y;
                    if (setup.RandomOffsets.z != 0) pos.z += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomOffsets.z;

                    Vector3 rotation = Vector3.zero;
                    if (setup.RandomRotations.x != 0) rotation.x += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomRotations.x;
                    if (setup.RandomRotations.y != 0) rotation.y += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomRotations.y;
                    if (setup.RandomRotations.z != 0) rotation.z += FGenerators.GetRandom(-1f, 1f, random) * setup.RandomRotations.z;

                    rotation += setup.Rotate;

                    Vector3 size = Vector3.one;
                    if (setup.RandomFlipX) size.x = random.Next(0, 2) == 1 ? 1f : -1f;
                    if (setup.RandomFlipY) size.y = random.Next(0, 2) == 1 ? 1f : -1f;
                    if (setup.RandomFlipZ) size.z = random.Next(0, 2) == 1 ? 1f : -1f;

                    float from = setup.RandomScale.x < 0f ? 0f : -1f;
                    if (setup.RandomScale.x != 0) size.x += FGenerators.GetRandom(from, 1f, random) * Mathf.Abs(setup.RandomScale.x);
                    from = setup.RandomScale.y < 0f ? 0f : -1f;
                    if (setup.RandomScale.y != 0) size.y += FGenerators.GetRandom(from, 1f, random) * Mathf.Abs(setup.RandomScale.y);
                    from = setup.RandomScale.z < 0f ? 0f : -1f;
                    if (setup.RandomScale.z != 0) size.z += FGenerators.GetRandom(from, 1f, random) * Mathf.Abs(setup.RandomScale.z);

                    inst.transform = Matrix4x4.TRS(pos, Quaternion.Euler(rotation), size);
                    meshInstances[y * setup.StackCountX + x] = inst;

                    preInstX = inst;
                    if (x == 0) preInstYt = inst;
                    if (x == setup.StackCountX - 1) preInstY = preInstYt;
                }
            }


            #region Check removals

            if (setup.RemovalBoxes.Count > 0)
            {
                for (int i = 0; i < meshInstances.Length; i++)
                {
                    Bounds meshBox = FEngineering.TransformBounding(meshInstances[i].mesh.bounds, meshInstances[i].transform);
                    bool collided = false;

                    for (int r = 0; r < setup.RemovalBoxes.Count; r++)
                    {
                        if (setup.RemovalBoxes[r].ToBounds().Intersects(meshBox)) collided = true;
                        if (collided) break;
                    }

                    if (collided)
                    {
                        var inst = meshInstances[i];
                        inst.mesh = null;
                        meshInstances[i] = inst;
                    }
                }
            }

            #endregion


            finalMesh.CombineMeshes(meshInstances);
            FMeshUtils.RenameMesh(finalMesh);
            return finalMesh;
        }

        #region Editor Code
#if UNITY_EDITOR

        /// <summary> Returns true when GUI change occured </summary>
        public static bool _Editor_DisplayStackerSettings(StackSetup setup)
        {
            EditorGUI.BeginChangeCheck();

            setup.SourceRotationCorrection = EditorGUILayout.Vector3Field("Correct Rotation:", setup.SourceRotationCorrection);
            setup.SourceScaleCorrection = EditorGUILayout.Vector3Field("Correct Scale:", setup.SourceScaleCorrection);

            GUILayout.Space(5);

            setup.StackCountX = EditorGUILayout.IntSlider("X Stack Count:", setup.StackCountX, 1, 20);
            setup.StackCount = EditorGUILayout.IntSlider("Y Stack Count:", setup.StackCount, 1, 20);
            setup.ExtraSpacing = EditorGUILayout.Vector3Field("Spacing:", setup.ExtraSpacing);

            GUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Random Flip:", GUILayout.Width(100));
            EditorGUIUtility.labelWidth = 20;
            setup.RandomFlipX = EditorGUILayout.Toggle("X:", setup.RandomFlipX, GUILayout.Width(50));
            setup.RandomFlipY = EditorGUILayout.Toggle("Y:", setup.RandomFlipY, GUILayout.Width(50));
            setup.RandomFlipZ = EditorGUILayout.Toggle("Z:", setup.RandomFlipZ, GUILayout.Width(50));
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();

            setup.RandomOffsets = EditorGUILayout.Vector3Field("Random Offsets:", setup.RandomOffsets);
            setup.RandomRotations = EditorGUILayout.Vector3Field("Random Rotations:", setup.RandomRotations);
            setup.RandomScale = EditorGUILayout.Vector3Field("Random Scale:", setup.RandomScale);

            GUILayout.Space(3);
            setup.Rotate = EditorGUILayout.Vector3Field("Rotate:", setup.Rotate);

            GUILayout.Space(3);

            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_Foldout ? FGUI_Resources.Tex_DownFold : FGUI_Resources.Tex_RightFold, EditorStyles.label, GUILayout.Width(22), GUILayout.Height(18)))
            {
                _Foldout = !_Foldout;
            }

            if (GUILayout.Button("Meshes To Generate Stack Of:", EditorStyles.label))
            {
                _Foldout = !_Foldout;
            }

            #region Drag and drop

            var rect = GUILayoutUtility.GetLastRect();
            var dropEvent = Event.current;

            if (dropEvent != null)
            {
                if (dropEvent.type == EventType.DragPerform || dropEvent.type == EventType.DragUpdated)
                {
                    if (rect.Contains(dropEvent.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (dropEvent.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (var dragged in DragAndDrop.objectReferences)
                            {
                                if (dragged is Mesh)
                                {
                                    setup.toStack.Add(dragged as Mesh);
                                }
                                else if (dragged is GameObject)
                                {
                                    GameObject o = dragged as GameObject;
                                    foreach (var filt in o.GetComponentsInChildren<MeshFilter>())
                                    {
                                        if (filt.sharedMesh) setup.toStack.Add(filt.sharedMesh);
                                    }
                                }
                            }
                        }

                        Event.current.Use();
                    }
                }
            }

            #endregion

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", FGUI_Resources.ButtonStyle, GUILayout.Width(22)))
            {
                setup.toStack.Add(setup.toStack.Count > 0 ? setup.toStack[setup.toStack.Count - 1] : null);
            }
            EditorGUILayout.EndHorizontal();

            int toRemove = -1;
            if (_Foldout)
            {
                EditorGUIUtility.labelWidth = 50;

                for (int i = 0; i < setup.toStack.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    setup.toStack[i] = (Mesh)EditorGUILayout.ObjectField("[" + i + "]", setup.toStack[i], typeof(Mesh), false);

                    if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(19), GUILayout.Width(22)))
                    {
                        toRemove = i;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUILayout.EndVertical();

            if (toRemove != -1) setup.toStack.RemoveAt(toRemove);


            #region Removal Boxes

            GUILayout.Space(3);

            EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Removal Box Points:");

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", FGUI_Resources.ButtonStyle, GUILayout.Width(22)))
            {
                setup.RemovalBoxes.Add(new RemovalBox());
            }
            EditorGUILayout.EndHorizontal();

            toRemove = -1;
            for (int i = 0; i < setup.RemovalBoxes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                Editor_DrawBoundsSettings(setup, i);
                if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(19), GUILayout.Width(22))) toRemove = i;

                EditorGUILayout.EndHorizontal();
            }
            if (toRemove != -1) setup.RemovalBoxes.RemoveAt(toRemove);

            EditorGUILayout.EndVertical();


            #endregion


            GUILayout.Space(3);

            return EditorGUI.EndChangeCheck();
        }


        static void Editor_DrawBoundsSettings(StackSetup setup, int i)
        {
            if (setup.RemovalBoxes.ContainsIndex(i) == false) return;
            RemovalBox rBox = setup.RemovalBoxes[i];
            //EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("[" + i + "]", EditorStyles.boldLabel, GUILayout.Width(28));
            rBox.Position = EditorGUILayout.Vector3Field("Removal Box Center:", rBox.Position);
            EditorGUIUtility.labelWidth = 40;
            GUILayout.Space(8);
            rBox.Size = EditorGUILayout.Vector3Field("Size:", rBox.Size);
            EditorGUIUtility.labelWidth = 0;
            //EditorGUILayout.EndHorizontal();
            setup.RemovalBoxes[i] = rBox;
        }

#endif
        #endregion


    }
}