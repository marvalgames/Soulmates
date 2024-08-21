using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Special
{

    public class PR_BoundsSeparatePushOut : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Bounds Separate Push" : "Bounds Separate Collision Push"; }
        public override string GetNodeTooltipDescription { get { return "Trying to push field out of not precise bounds collision"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }
        public override Vector2 NodeSize { get { return new Vector2(242, _EditorFoldout ? 204 : 112); } }
        public override bool IsFoldable { get { return true; } }
        public override Color GetNodeColor() { return new Color(0.1f, 0.7f, 1f, 0.95f); }

        [Port(EPortPinType.Input, 1)] public FloatPort BoundsSizeMultiplier;
        [Tooltip("If 'Collision With' left empty or -1 then colliding with every field in the current plan stage")]
        [Port(EPortPinType.Input, 1)] public PGGPlannerPort CollisionWith;
        [HideInInspector][Port(EPortPinType.Input, 1)] public PGGPlannerPort ToPush;
        [HideInInspector][Port(EPortPinType.Input, 1)] public FloatPort PushPowerMultiply;
        [HideInInspector] public bool RoundAccordingly = true;
        [Tooltip("Allow to call algorithm multiple times if resulting position is still colliding with other fields!")]
        [HideInInspector] public bool PushMultipleTimes = true;
        [HideInInspector] public float YBoundsSizeBoost = 1f;

        public override void OnCreated()
        {
            base.OnCreated();
            BoundsSizeMultiplier.Value = 1f;
            PushPowerMultiply.Value = 1f;
        }


        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(ToPush);
            CollisionWith.Editor_DefaultValueInfo = "(all)";
            if (planner == null) return;

            BoundsSizeMultiplier.TriggerReadPort(true);
            float boundsSizeMul = BoundsSizeMultiplier.GetInputValue;

            PushPowerMultiply.TriggerReadPort(true);
            float pushPowerMul = PushPowerMultiply.GetInputValue;

            planner.LatestChecker._IsCollidingWith_MyFirstCollisionCell = null;

            bool collideWithAll = false;
            if (CollisionWith.PortState() != EPortPinState.Connected)
            {
                if (CollisionWith.UniquePlannerID < 0)
                {
                    collideWithAll = true;
                }
            }

            bool pushed = false;

            Vector3 boundsMul = new Vector3(boundsSizeMul, boundsSizeMul, boundsSizeMul);
            boundsMul.y *= YBoundsSizeBoost;

            if (collideWithAll)
            {
                if (ParentPlanner)
                    if (ParentPlanner.ParentBuildPlanner)
                    {
                        var bp = ParentPlanner.ParentBuildPlanner;

                        var all = bp.CollectAllAvailablePlanners(true, true);
                        all.Remove(planner);

                        if (!PushMultipleTimes)
                        {
                            for (int i = 0; i < all.Count; i++)
                            {
                                FieldPlanner pl = all[i];
                                bool psh = planner.LatestChecker.PushOutOfBoundingBoxAway(pl.LatestChecker, RoundAccordingly, pushPowerMul, boundsMul);
                                if (psh) if (!pushed) pushed = true;
                            }
                        }
                        else
                        {
                            bool stillColliding = true;
                            int safety = -1;
                            while (stillColliding)
                            {
                                safety += 1;
                                if (safety > 200) break;

                                for (int i = 0; i < all.Count; i++)
                                {
                                    FieldPlanner pl = all[i];
                                    bool psh = planner.LatestChecker.PushOutOfBoundingBoxAway(pl.LatestChecker, RoundAccordingly, pushPowerMul + (safety * 0.1f), boundsMul);
                                    if (psh) if (!pushed) pushed = true;
                                }

                                stillColliding = false;
                                for (int i = 0; i < all.Count; i++)
                                {
                                    FieldPlanner pl = all[i];
                                    stillColliding = planner.LatestChecker.CheckSimpleBoundsCollision(pl.LatestChecker, boundsMul);
                                    if (stillColliding) break;
                                }
                            }
                        }
                    }
            }
            else
            {
                List<FieldPlanner> checkCollWith = GetPlannersFromPort(CollisionWith, false);

                if (!PushMultipleTimes)
                {
                    for (int i = 0; i < checkCollWith.Count; i++)
                    {
                        FieldPlanner pl = checkCollWith[i];
                        bool psh = planner.LatestChecker.PushOutOfBoundingBoxAway(pl.LatestChecker, RoundAccordingly, pushPowerMul, boundsMul);
                        if (psh) if (!pushed) pushed = true;
                    }
                }
                else
                {
                    bool stillColliding = true;
                    int safety = -1;
                    while (stillColliding)
                    {
                        safety += 1;
                        if (safety > 200) break;

                        for (int i = 0; i < checkCollWith.Count; i++)
                        {
                            FieldPlanner pl = checkCollWith[i];
                            bool psh = planner.LatestChecker.PushOutOfBoundingBoxAway(pl.LatestChecker, RoundAccordingly, pushPowerMul + (safety * 0.1f), boundsMul);
                            if (psh) if (!pushed) pushed = true;
                        }

                        stillColliding = false;

                        for (int i = 0; i < checkCollWith.Count; i++)
                        {
                            FieldPlanner pl = checkCollWith[i];
                            stillColliding = planner.LatestChecker.CheckSimpleBoundsCollision(pl.LatestChecker, boundsMul);
                            if (stillColliding) break;
                        }
                    }
                }
            }


            if ( RoundAccordingly) if (planner.RoundToScale) planner.LatestChecker.RoundRootPositionToScale();

            if (Debugging)
            {
                if (!pushed)
                {
                    DebuggingInfo = "Collision of " + planner.name + planner.ArrayNameString + " not detected";
                }
                else
                {
                    DebuggingInfo = "Collision of " + planner.name + planner.ArrayNameString + " DETECTED";
                }
            }
        }



#if UNITY_EDITOR

        private UnityEditor.SerializedProperty sp = null;
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            base.Editor_OnNodeBodyGUI(setup);

            PushPowerMultiply.AllowDragWire = false;

            if (_EditorFoldout)
            {
                GUILayout.Space(1);

                ToPush.AllowDragWire = true;
                baseSerializedObject.Update();
                if (sp == null) sp = baseSerializedObject.FindProperty("ToPush");
                UnityEditor.SerializedProperty scp = sp.Copy();
                UnityEditor.EditorGUILayout.PropertyField(scp);
                scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                //scp.Next(false); UnityEditor.EditorGUILayout.PropertyField(scp);
                baseSerializedObject.ApplyModifiedProperties();
                PushPowerMultiply.AllowDragWire = true;
            }
            else
            {
                ToPush.AllowDragWire = false;

                if (CollisionWith.PortState() != EPortPinState.Connected)
                    if (CollisionWith.UniquePlannerID < 0)
                    {
                        GUILayout.Space(-2);
                        UnityEditor.EditorGUILayout.HelpBox("Collide with all", UnityEditor.MessageType.None);
                    }
            }
        }

#endif

    }
}