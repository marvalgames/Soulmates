using FIMSpace.Graph;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Checker
{

    public class PR_CheckBoundsCollisionBetween : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "  Bounds Collision" : "Check Bounds Collision Between"; }
        public override string GetNodeTooltipDescription { get { return "Check if fields bounds collides with each other"; } }
        public override Color GetNodeColor() { return new Color(0.07f, 0.66f, 0.56f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(_EditorFoldout ? 286 : 240, _EditorFoldout ? 186 : 102); } }
        public override bool IsFoldable { get { return true; } }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort CollidingWith;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue, 1)] [Tooltip("If collision occured then true, if no then false")] public BoolPort IsColliding;
        [HideInInspector] [Port(EPortPinType.Input, 1)] [Tooltip("Using self if no input")] public PGGPlannerPort FirstColliderField;
        [HideInInspector] public Vector3 ScaleSelfBounds = Vector3.one;
        [HideInInspector] public Vector3 ScaleOtherBounds = Vector3.one;
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGVector3Port OffsetSelfBounds;

        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner aPlanner = GetPlannerFromPort(FirstColliderField);
            List<FieldPlanner> bPlanners = GetPlannersFromPort(CollidingWith);
            FieldPlanner collWith = null;

            IsColliding.Value = false;

            bool collided = false;

            Bounds myBounds = aPlanner.LatestChecker.GetFullBoundsWorldSpace();
            Bounds cBounds = new Bounds();

            myBounds.size = Vector3.Scale(myBounds.size, ScaleSelfBounds);
            myBounds.center += OffsetSelfBounds.GetInputValue;

            for (int i = 0; i < bPlanners.Count; i++)
            {
                if (bPlanners[i] == aPlanner) continue;
                if (!bPlanners[i].Available ) continue;

                cBounds = bPlanners[i].LatestChecker.GetFullBoundsWorldSpace();
                cBounds.size = Vector3.Scale(cBounds.size, ScaleOtherBounds);

                if (cBounds.Intersects(myBounds)) { collWith = bPlanners[i]; collided = true; break; }
            }

            IsColliding.Value = collided;

            if (Debugging)
            {
                if (collWith != null)
                {
                    DebuggingInfo = "Checking collision and detected with " + collWith.name + " " + collWith.ArrayNameString;

                    DebuggingGizmoEvent = new System.Action(() => 
                    {
                        FDebug.DrawBounds3D(myBounds, Color.red, 1f, 0.05f);
                        FDebug.DrawBounds3D(cBounds, Color.red, 1f, 0.05f);
                    });
                }
                else
                {
                    DebuggingGizmoEvent = new System.Action(() =>
                    {
                        FDebug.DrawBounds3D(myBounds, Color.green * 0.7f, 1f, 0.05f);
                        FDebug.DrawBounds3D(cBounds, Color.green* 0.7f, 1f, 0.05f);
                    });
                    DebuggingInfo = "Checking collision but no collision detected";
                }

                print._debugLatestExecuted = aPlanner.LatestResult.Checker;
            }
        }

#if UNITY_EDITOR

        SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            if (!_EditorFoldout) return;

            if (_EditorFoldout)
            {
                FirstColliderField.AllowDragWire = true;
                baseSerializedObject.Update();
                if (sp == null) sp = baseSerializedObject.FindProperty("FirstColliderField");
                SerializedProperty spc = sp.Copy();
                EditorGUILayout.PropertyField(spc);
                spc.Next(false); EditorGUILayout.PropertyField(spc);
                spc.Next(false); EditorGUILayout.PropertyField(spc);
                spc.Next(false); EditorGUILayout.PropertyField(spc);
                baseSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                FirstColliderField.AllowDragWire = false;
            }

        }
#endif

    }
}