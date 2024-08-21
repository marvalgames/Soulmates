using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace FIMSpace.Generating.Planning.PlannerNodes.Generating
{

    public class PR_GetFattenShape : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return "Get Fattened Shape"; }
        public override string GetNodeTooltipDescription { get { return "Generating grid extra cells to make shape more fat"; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.CellsManipulation; } }
        public override Color GetNodeColor() { return new Color(0.45f, 0.9f, 0.15f, 0.9f); }
        public override Vector2 NodeSize { get { return new Vector2(214, _EditorFoldout ? 145 : 125); } }
        public override bool DrawInputConnector { get { return false; } }
        public override bool DrawOutputConnector { get { return false; } }
        public override bool IsFoldable { get { return false; } }

        [Port(EPortPinType.Input, EPortNameDisplay.Default, EPortValueDisplay.HideOnConnected, 1)] public PGGPlannerPort Source;
        [Port(EPortPinType.Output, EPortValueDisplay.HideValue)] public PGGPlannerPort FattenExtraCells;
        //[Port(EPortPinType.Input, 1, 1)] public IntPort Thickness;
        [Space(4)]
        [Tooltip("[UsingHelperVectors] If cells are containing helper direction data, it can be used to expand shape in defined direction")]
        public EMode Mode = EMode.OmniDirectional;
        public enum EMode { OmniDirectional, ByOrder, UsingHelperVectors }

        public override void OnStartReadingNode()
        {
            Source.TriggerReadPort(true);

            var checker = Source.GetInputCheckerSafe;

            if (checker == null) checker = GetCheckerFromPort(Source);

            if (checker == null)
            {
                var plan = GetPlannerFromPort(Source, false);
                if (plan != null) checker = plan.LatestChecker;
            }

            if (checker == null) return;
            if (checker.ChildPositionsCount == 0) return;

            //Thickness.TriggerReadPort(true);
            //int thickn = Mathf.Max(Thickness.GetInputValue, 1);

            CheckerField3D fattening = new CheckerField3D();
            fattening.CopyParamsFrom(checker);

            if (Mode == EMode.OmniDirectional)
            {
                for (int i = 0; i < checker.ChildPositionsCount; i++)
                {
                    var cell = checker.GetCell(i);
                    fattening.AddLocal(cell.Pos + new Vector3Int(1, 0, 0));
                    fattening.AddLocal(cell.Pos + new Vector3Int(-1, 0, 0));
                    fattening.AddLocal(cell.Pos + new Vector3Int(0, 0, 1));
                    fattening.AddLocal(cell.Pos + new Vector3Int(0, 0, -1));
                }

                fattening.RemoveCellsCollidingWith(checker);
            }
            else if (Mode == EMode.ByOrder)
            {
                if (checker.ChildPositionsCount > 1)
                {
                    Vector3 prePos = checker.AllCells[0].Pos + (checker.AllCells[0].Pos - checker.AllCells[1].Pos);
                    Vector3 addDir = Vector3.right;
                    Vector3 preDir = Vector3.zero;

                    for (int i = 0; i < checker.ChildPositionsCount; i++)
                    {
                        var cell = checker.GetCell(i);
                        Quaternion dir = Quaternion.LookRotation(checker.AllCells[i].Pos - prePos);

                        Vector3 targetDir = dir * addDir;
                        if (targetDir == Vector3.right && preDir == Vector3.back) addDir = Vector3.left;
                        else if (targetDir == Vector3.left && preDir == Vector3.forward) addDir = Vector3.left;
                        else if (targetDir != preDir) addDir = Vector3.right;

                        preDir = dir * addDir;
                        prePos = checker.AllCells[i].Pos;
                        var added = fattening.AddLocal(checker.AllCells[i].Pos + preDir);
                        added.HelperVector = cell.HelperVector;
                    }
                }
            }
            else if (Mode == EMode.UsingHelperVectors)
            {
                if (checker.ChildPositionsCount > 1)
                {
                    Vector3 addDir = Vector3.right;
                    Vector3 preDir = Vector3.zero;

                    for (int i = 0; i < checker.ChildPositionsCount; i++)
                    {
                        var cell = checker.GetCell(i);
                        if (cell.HelperVector == Vector3.zero) continue;
                        
                        Quaternion dir = Quaternion.LookRotation(cell.HelperVector);

                        Vector3 targetDir = dir * addDir;
                        if (targetDir == Vector3.right && preDir == Vector3.back) addDir = Vector3.left;
                        else if (targetDir == Vector3.left && preDir == Vector3.forward) addDir = Vector3.left;
                        else if (targetDir != preDir) addDir = Vector3.right;

                        preDir = dir * addDir;
                        var added = fattening.AddLocal(checker.AllCells[i].Pos + preDir);
                        
                        added.HelperVector = cell.HelperVector;

                        //if (i == checker.ChildPositionsCount - 1)
                        //{
                        //    checker.DebugLogDrawCellInWorldSpace(cell, Color.green);
                        //    UnityEngine.Debug.DrawRay(checker.GetWorldPos(cell) + Vector3.up, preDir, Color.blue, 1.01f);
                        //}
                    }
                }
            }

            FattenExtraCells.Output_Provide_Checker(fattening);
        }


#if UNITY_EDITOR
        //UnityEditor.SerializedProperty sp = null;
        //public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        //{
        //    base.Editor_OnNodeBodyGUI(setup);

        //    baseSerializedObject.Update();
        //    if (_EditorFoldout)
        //    {
        //        GUILayout.Space(1);
        //        if (sp == null) sp = baseSerializedObject.FindProperty("CopyCellReferences");
        //        UnityEditor.EditorGUILayout.PropertyField(sp, true);
        //    }
        //    baseSerializedObject.ApplyModifiedProperties();
        //}
#endif

    }
}