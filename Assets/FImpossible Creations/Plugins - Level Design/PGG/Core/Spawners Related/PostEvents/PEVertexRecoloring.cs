using FIMSpace;
using FIMSpace.Generating;
using FIMSpace.Generating.Planning.PlannerNodes.Math.Values;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "PE Color Vertices", menuName = "Vertices Instance", order = 1)]
    public class PEVertexRecoloring : FieldSpawnerPostEvent_Base
    {
        FieldVariable ColVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Color", Color.red); }
        Color PaintColor(FieldSetup.CustomPostEventHelper helper) { return ColVar(helper).GetColor(); }
        FieldVariable NoiseVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Noise Scale", 0.1f); }
        float GetNoiseScale(FieldSetup.CustomPostEventHelper helper) { return NoiseVar(helper).GetFloatValue(); }

        FieldVariable FalloffVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Falloff", 0.2f); }
        float GetFalloff(FieldSetup.CustomPostEventHelper helper) { return FalloffVar(helper).GetFloatValue(); }

        FieldVariable FalloffCurveVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Intensity Curve", AnimationCurve.Linear(0f, 1f, 1f, 0f)); }


        FieldVariable ModeVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Mode", 0); }
        FieldVariable ApplyVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Apply", 1); }
        FieldVariable AxisVar(FieldSetup.CustomPostEventHelper helper) { return helper.RequestVariable("Axis", 0); }

        public enum EPaintOn
        { 
            WholeMesh, UpperEdges, LowerEdges, ExcludeUpDownEdges//, SideEdges, ExcludeAllEdges
        }

        public enum EApply { Override, Blend }
        public enum EAxis { Wall, Flat }


        private EAxis executionAxis = EAxis.Wall;

        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            Color col = PaintColor(helper);
            float scale = GetNoiseScale(helper);

            Vector2 randomOffset = new Vector2(FGenerators.GetRandom(-10000, 10000), FGenerators.GetRandom(-10000, 10000));
            EPaintOn paintMode = (EPaintOn)ModeVar(helper).IntV;

            float fallff = GetFalloff(helper);

            AnimationCurve curve = FalloffCurveVar(helper).GetCurve();
            if (curve == null) curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");
            string useTag = tagg.GetStringValue();
            if (useTag == "Untagged") useTag = "";

            EApply apply = (EApply)ApplyVar(helper).IntV;

            executionAxis = (EAxis)AxisVar(helper).IntV;

            ProceedPainting(generatedRef.CombinedNonStaticContainer, useTag, col, scale, fallff, randomOffset, paintMode, apply, curve);
            ProceedPainting(generatedRef.CombinedStaticContainer, useTag, col, scale, fallff, randomOffset, paintMode, apply, curve);
        }

        void ProceedPainting(GameObject parent, string useTag, Color col, float scale, float falloff, Vector2 randomOffset, EPaintOn paintMode, EApply apply, AnimationCurve curve)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                MeshFilter filter = parent.transform.GetChild(i).GetComponent<MeshFilter>();
                if (filter == null) continue;
                if (useTag.Length > 0) if (filter.gameObject.CompareTag(useTag) == false) continue;
                ProceedMeshPainting(filter.transform, filter.sharedMesh, col, scale, falloff, randomOffset, paintMode, apply, curve);
            }
        }



        // Main procedure here

        void ProceedMeshPainting(Transform parent, Mesh mesh, Color col, float scale, float falloff, Vector2 randomOffset, EPaintOn paintMode, EApply apply, AnimationCurve curve)
        {
            if (parent == null) return; if (mesh == null) return;

            MeshPaintHelper paintHelper = new MeshPaintHelper(parent, mesh);
            if (!paintHelper.IsValid) return;

            if (paintMode != EPaintOn.WholeMesh) paintHelper.GeneratePaintingGrid();

            for (int i = 0; i < paintHelper.verts.Count; i++)
            {
                Vector3 vertWPos = parent.TransformPoint(paintHelper.verts[i]);
                float blendMul = 1f;


                #region Paint Mode Switch


                if (paintMode == EPaintOn.WholeMesh)
                {
                    // No restrictions
                }
                else if (paintMode == EPaintOn.UpperEdges)
                {
                    blendMul = paintHelper.GetEdgeFactor(i, vertWPos, true, falloff);
                }
                else if (paintMode == EPaintOn.LowerEdges)
                {
                    blendMul = paintHelper.GetEdgeFactor(i, vertWPos, false, falloff);
                }
                else if (paintMode == EPaintOn.ExcludeUpDownEdges)
                {
                    float upMul = paintHelper.GetEdgeFactor(i, vertWPos, true, falloff);
                    float lowMul = paintHelper.GetEdgeFactor(i, vertWPos, false, falloff);

                    blendMul = 1f - Mathf.Max(upMul, lowMul);
                }

                #endregion

                if (blendMul < 0.0001f) continue;

                float x = vertWPos.x * scale + randomOffset.x;
                float y = 0f;
                float z = 0f;

                if (executionAxis == EAxis.Wall)
                {
                    y = vertWPos.y * scale + randomOffset.y;
                    z = vertWPos.z * scale + randomOffset.x;
                }
                else
                {
                    y = vertWPos.z * scale + randomOffset.y;
                }



                float blend = Mathf.PerlinNoise(x + z, y) * blendMul;
                blend = curve.Evaluate(1f - blend);

                #region Color Apply

                Color nCol = paintHelper.colors[i];

                if (apply == EApply.Blend)
                    nCol = Color.LerpUnclamped(nCol, col, blend);
                else
                    nCol = Color.LerpUnclamped(Color.clear, col, blend);

                #endregion


                paintHelper.colors[i] = nCol;
            }

            mesh.SetColors(paintHelper.colors);
        }



        class MeshPaintHelper
        {
            public Transform parent;
            public Mesh mesh;
            public List<Color> colors;
            public List<Vector3> verts;
            public Vector3[] worldPosVerts;

            Dictionary<Vector3Int, VertexCluster> clusterMap;
            float padding = 1f;

            public MeshPaintHelper(Transform parent, Mesh mesh)
            {
                this.parent = parent;
                this.mesh = mesh;

                colors = new List<Color>();
                mesh.GetColors(colors);

                verts = new List<Vector3>();
                mesh.GetVertices(verts);

                worldPosVerts = new Vector3[(int)verts.Count];
                for (int v = 0; v < verts.Count; v++) worldPosVerts[v] = parent.TransformPoint(verts[v]);
            }


            public bool IsValid
            {
                get
                {
                    if (mesh == null) return false;
                    if (colors == null) return false;
                    if (verts == null) return false;
                    if (verts.Count == 0) return false;
                    if (verts.Count != colors.Count) return false;
                    return true;
                }
            }


            //public struct VertexHelper
            //{
            //    public Vector3 worldPos;
            //    public Vector3 vertPos;
            //    public Vector3 freeDir;
            //    public float collisionDistance;

            //    public int nearest0;
            //    public int nearest1;
            //    public int nearest2;
            //    public int nearest3;
            //    public int nearest4;

            //    public VertexHelper(Vector3 vPos, Vector3 wPos)
            //    {
            //        vertPos = vPos;
            //        worldPos = wPos;

            //        freeDir = Vector3.zero;
            //        collisionDistance = 0f;
            //        nearest0 = -1;
            //        nearest1 = -1;
            //        nearest2 = -1;
            //        nearest3 = -1;
            //        nearest4 = -1;
            //    }
            //}

            public class VertexCluster
            {
                public Vector3Int clusterID;
                public Bounds clusterBounds;
                public List<int> vertsIn;

                public VertexCluster(Vector3Int id)
                {
                    clusterID = id;
                    clusterBounds = new Bounds(Vector3.zero, Vector3.zero);
                    vertsIn = new List<int>();
                }

                public void AddVertex(int i, Vector3 wPos)
                {
                    vertsIn.Add(i);
                    if (clusterBounds.center == Vector3.zero) clusterBounds = new Bounds(wPos, Vector3.zero);
                    clusterBounds.Encapsulate(wPos);
                }
            }

            VertexCluster GetCluster(Vector3Int key)
            {
                VertexCluster get;
                if (clusterMap.TryGetValue(key, out get)) return get;
                get = new VertexCluster(key);
                clusterMap.Add(key, get);
                return get;
            }

            // Precomputing vertex structure to help out edges painting for each vertex
            public void GeneratePaintingGrid()
            {
                Vector3 wBounds = Vector3.Scale(parent.lossyScale, mesh.bounds.size);
                padding = ((wBounds.x + wBounds.y + wBounds.z) / 3f) * 0.4f;
                //UnityEngine.Debug.Log("padding = " + padding);

                clusterMap = new Dictionary<Vector3Int, VertexCluster>();

                for (int v = 0; v < verts.Count; v++)
                {
                    Vector3 wPos = worldPosVerts[v];
                    Vector3Int key = RoundValueTo(wPos, padding).V3toV3Int();

                    VertexCluster cluster = GetCluster(key);
                    cluster.AddVertex(v, wPos);
                }

                // Debug Draw
                //float h = 0f;
                //foreach (var item in clusterMap)
                //{
                //    h += 0.135f;
                //    if (h > 1f) h -= 1f;
                //    Color debCol = Color.HSVToRGB(h, 0.6f, 0.6f);

                //    FDebug.DrawBounds3D(item.Value.clusterBounds, Color.red * 1.1f, 1f);
                //    for (int i = 0; i < item.Value.vertsIn.Count - 1; i++) UnityEngine.Debug.DrawLine(worldPosVerts[item.Value.vertsIn[i]], worldPosVerts[item.Value.vertsIn[i + 1]], debCol, 1.01f);
                //}

            }




            internal float GetEdgeFactor(int i, Vector3 vertWPos, bool upper, float falloff)
            {
                Vector3Int key = RoundValueTo(vertWPos, padding).V3toV3Int();

                VertexCluster helperCluster;
                clusterMap.TryGetValue(key + (upper ? new Vector3Int(0, 1, 0) : new Vector3Int(0, -1, 0)), out helperCluster);

                var cluster = GetCluster(key);
                //if ( helperCluster == null) if (vertWPos.y > cluster.clusterBounds.max.y - cluster.clusterBounds.extents.y * 0.01f) return 1f;

                int highest = i;

                for (int c = 0; c < cluster.vertsIn.Count; c++)
                {
                    int othId = cluster.vertsIn[c];
                    Vector3 othWpos = worldPosVerts[othId];

                    float dist = Vector2.Distance(new Vector2(othWpos.x, othWpos.z), new Vector2(vertWPos.x, vertWPos.z));
                    if (dist > padding * 0.2f) continue;

                    if (upper)
                    {
                        if (othWpos.y > worldPosVerts[highest].y) highest = othId;
                    }
                    else
                        if (othWpos.y < worldPosVerts[highest].y) highest = othId;
                }


                if (helperCluster != null)
                {
                    for (int c = 0; c < helperCluster.vertsIn.Count; c++)
                    {
                        int othId = helperCluster.vertsIn[c];
                        Vector3 othWpos = worldPosVerts[othId];

                        float dist = Vector2.Distance(new Vector2(othWpos.x, othWpos.z), new Vector2(vertWPos.x, vertWPos.z));
                        if (dist > padding * 0.2f) continue;

                        if (upper)
                        {
                            if (othWpos.y > worldPosVerts[highest].y) highest = othId;
                        }
                        else
                            if (othWpos.y < worldPosVerts[highest].y) highest = othId;
                    }
                }


                // Debug backup
                //if (highest != i) UnityEngine.Debug.DrawLine(vertWPos, worldPosVerts[highest], Color.green, 1.01f);

                float edgeDistance = 0f;
                if (highest != i) edgeDistance = Mathf.Abs(vertWPos.y - worldPosVerts[highest].y);
                //if (highest != i) edgeDistance = Vector3.Distance(vertWPos, worldPosVerts[highest]);

                if (falloff < 0.0001f)
                {
                    if (edgeDistance < padding * 0.001f) return 1f;
                }

                return Mathf.InverseLerp(falloff, 0f, edgeDistance); // Far from edge = 0 blend, near edge = 1
            }



            private Vector3 RoundValueTo(Vector3 toRound, float to)
            {
                return new Vector3(RoundValueTo(toRound.x, to), RoundValueTo(toRound.y, to), RoundValueTo(toRound.z, to));
            }

            private float RoundValueTo(float toRound, float to)
            {
                return Mathf.Round(toRound / to);
            }

        }



        // Editor inspector display code below

#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Painting vertices of the combined meshes. Really useful if you use vertex color in your shaders.";

            FieldVariable.Editor_DrawTweakableVariable(ColVar(helper));
            FieldVariable.Editor_DrawTweakableVariable(NoiseVar(helper));

            GUILayout.Space(4);
            var falloff = FalloffVar(helper);
            FieldVariable.Editor_DrawTweakableVariable(falloff);
            if (falloff.Float < 0f) falloff.Float = 0f;

            GUILayout.Space(2);
            var curveVar = FalloffCurveVar(helper);
            curveVar.helper = new Vector3(0f, 1f, 0f);
            FieldVariable.Editor_DrawTweakableVariable(curveVar);

            GUILayout.Space(4);

            int mode = ModeVar(helper).IntV;
            EPaintOn eMode = (EPaintOn)mode;
            eMode = (EPaintOn)EditorGUILayout.EnumPopup("Paint Mode:", eMode);
            ModeVar(helper).IntV = (int)eMode;

            GUILayout.Space(2);

            int app = ApplyVar(helper).IntV;
            EApply eAppl = (EApply)app;
            eAppl = (EApply)EditorGUILayout.EnumPopup(new GUIContent("Apply Mode:", "If combined mesh already contains vertex color data, you might want to blend it instead of overriding."), eAppl);
            ApplyVar(helper).IntV = (int)eAppl;

            int ax = AxisVar(helper).IntV;
            EAxis eAx = (EAxis)ax;
            eAx = (EAxis)EditorGUILayout.EnumPopup(new GUIContent("Noise Axis:"), eAx);
            AxisVar(helper).IntV = (int)eAx;


            GUILayout.Space(4);
            FieldVariable tagg = helper.RequestVariable("Tag", "Untagged");

            string info = "";
            if (tagg.GetStringValue() == "Untagged") info = "Untagged = Apply To All";
            else info = "Apply To Tagged:";
            tagg.SetValue(UnityEditor.EditorGUILayout.TagField(info, tagg.GetStringValue()));

            GUILayout.Space(2);

            base.Editor_DisplayGUI(helper);
        }
#endif

    }
}
