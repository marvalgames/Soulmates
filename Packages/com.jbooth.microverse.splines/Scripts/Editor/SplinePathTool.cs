using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

namespace JBooth.MicroVerseCore
{
    
    [EditorTool("Width Tool", typeof(SplinePath))]
    public class SplinePathTool : EditorTool, IDrawSelectedHandles
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        bool m_DisableHandles = false;

        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = Resources.Load<Texture2D>("Icons/WidthTool"),
                text = "Width Tool",
                tooltip = "Adjust the width of the created path."
            };
        }

        protected const float k_HandleSize = 0.15f;

        protected bool DrawDataPoints(ISpline spline, SplineData<float> splineData)
        {
            var inUse = false;
            for (int dataFrameIndex = 0; dataFrameIndex < splineData.Count; dataFrameIndex++)
            {
                var dataPoint = splineData[dataFrameIndex];

                var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(normalizedT, out var position, out var tangent, out var up);
                tangent.y = 0;
                if (DrawDataPoint(position, tangent, Vector3.up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData[dataFrameIndex] = dataPoint;
                    inUse = true;
                }
            }
            return inUse;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var splineDataTarget = target as SplinePath;
            if (splineDataTarget == null || splineDataTarget.spline == null)
                return;

            Undo.RecordObject(splineDataTarget, "Modifying Width SplineData");

            for (int i = 0; i < splineDataTarget.splineWidths.Count; ++i)
            {
                if (i < splineDataTarget.spline.Splines.Count)
                {
                    var spline = splineDataTarget.spline.Splines[i];

                    var nativeSpline = new NativeSpline(spline, splineDataTarget.spline.transform.localToWorldMatrix);

                    Handles.color = Color.blue;
                    m_DisableHandles = false;

                    //User defined handles to manipulate width
                    DrawDataPoints(nativeSpline, splineDataTarget.splineWidths[i].widthData);

                    //Using the out-of the box behaviour to manipulate indexes
                    nativeSpline.DataPointHandles(splineDataTarget.splineWidths[i].widthData);
                }
            }
            
        }

        public void OnDrawHandles()
        {
            
            var splineDataTarget = target as SplinePath;
            if (ToolManager.IsActiveTool(this) || splineDataTarget == null || splineDataTarget.spline == null)
                return;

            while (splineDataTarget.splineWidths.Count > splineDataTarget.spline.Splines.Count)
            {
                splineDataTarget.splineWidths.RemoveAt(splineDataTarget.splineWidths.Count - 1);
            }
            for (int i = 0; i < splineDataTarget.spline.Splines.Count; ++i)
            {
                if (i >= splineDataTarget.splineWidths.Count)
                {
                    var sw = new SplinePath.SplineWidthData();
                    sw.widthData = new SplineData<float>();
                    sw.widthData.PathIndexUnit = PathIndexUnit.Normalized;
                    splineDataTarget.splineWidths.Add(sw);
                }
                
                var nativeSpline = new NativeSpline(splineDataTarget.spline.Splines[i], splineDataTarget.spline.transform.localToWorldMatrix);

                Color color = Color.blue;
                color.a = 0.5f;
                Handles.color = color;
                m_DisableHandles = true;
                
                DrawDataPoints(nativeSpline, splineDataTarget.splineWidths[i].widthData);
                
            }
            
            
            
        }

        protected bool DrawDataPoint(
            Vector3 position,
            Vector3 tangent,
            Vector3 up,
            float inValue,
            out float outValue)
        {
            int id1 = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);
            int id2 = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);

            outValue = 0f;
            if (tangent == Vector3.zero)
                return false;

            if (Event.current.type == EventType.MouseUp
                && Event.current.button != 0
                && (GUIUtility.hotControl == id1 || GUIUtility.hotControl == id2))
            {
                Event.current.Use();
                return false;
            }

            var handleColor = Handles.color;
            if (GUIUtility.hotControl == id1 || GUIUtility.hotControl == id2)
                handleColor = Handles.selectedColor;
            else if (GUIUtility.hotControl == 0 && (HandleUtility.nearestControl == id1 || HandleUtility.nearestControl == id2))
                handleColor = Handles.preselectionColor;

            var normalDirection = math.normalize(math.cross(tangent, up));
            inValue++;
            var extremity1 = position - inValue * (Vector3)normalDirection;
            var extremity2 = position + inValue * (Vector3)normalDirection;
            Vector3 val1, val2;
            using (new Handles.DrawingScope(handleColor))
            {
                Handles.DrawLine(extremity1, extremity2);
                val1 = Handles.Slider(id1, extremity1, normalDirection,
                    k_HandleSize * .5f * HandleUtility.GetHandleSize(position), CustomHandleCap, 0);
                val2 = Handles.Slider(id2, extremity2, normalDirection,
                    k_HandleSize * .5f * HandleUtility.GetHandleSize(position), CustomHandleCap, 0);
            }

            if (GUIUtility.hotControl == id1 && math.abs((val1 - extremity1).magnitude) > 0)
            {
                outValue = math.max(0, math.abs((val1 - position).magnitude) - 1);
                return true;
            }

            if (GUIUtility.hotControl == id2 && math.abs((val2 - extremity2).magnitude) > 0)
            {
                outValue = math.max(0, math.abs((val2 - position).magnitude) - 1);
                return true;
            }

            return false;
        }

        public void CustomHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.CubeHandleCap(controlID, position, rotation, size, m_DisableHandles ? EventType.Repaint : eventType);
        }
    }
    
}
