#if UNITY_EDITOR
using UnityEditor;
using FIMSpace.FEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using FIMSpace.Generating.Checker;

namespace FIMSpace.Generating
{

    [System.Serializable]
    public class Checker3DPathFindSetup
    {
        /// <summary> Helper to define if to use path find setup or to use default setup </summary>
        public bool Activated = false;

        public bool WorldSpace = false;
        public bool UseLimits = false;

        public Vector3 LimitMinValues = new Vector3(-1000, -1000, -1000);
        public Vector3 LimitMaxValues = new Vector3(1000, 1000, 1000);

        public int SearchStepsLimit = 650;
        public int SeparateSelfYCells = 1;

        /// <summary> x is above, y is below </summary>
        public Vector2 SeparateCollisionY = Vector2.zero;

        //public int AllowChangeDirectionEvery = 1;
        public bool SphericalDistanceMeasuring = false;
        public bool OnAnyContact = false;
        public bool PrioritizePerpendicular = false;
        public bool PrioritizeTargetYLevel = true;

        public bool ForceStartPathOnSide = true;
        //public bool AllowAlignJoin = false;

        /// <summary> Realign Ending Cell</summary>
        public bool TryEndCentered = false;
        public bool SingleStepMode = false;
        public bool AdjustEndingCell = false;
        public bool ConnectEvenDiscarded = false;

        public bool IgnoreSelfCollision = true;
        public bool DiscardOnNoPathFound = true;
        //public bool TryStartCentered = true;

        public float SnapToInstructionsOnDistance = 0;

        public bool LogWarnings = true;

        public int DontAllowFinishId = -1;
        public float DontAllowFinishDist = 0;
        public bool DontAllowStartToo = false;

        public bool DiscardIfAligning = false;

        public float ExistingCellsCostMultiplier = 1f;
        public float SelfCellsExtraSearchRange = 1f;
        public CheckerField3D.EPathFindFocusLevel PathFindFocusLevel = CheckerField3D.EPathFindFocusLevel.Middle;

        public enum EPathFindCategory { MainSettings, Handling, ExtraConditions }
        public EPathFindCategory DrawCategory = EPathFindCategory.MainSettings;


        public List<PathStep> Directions = new List<PathStep>();

        /// <summary> start checker, current cell, target checker, target step cell, current cost </summary>
        public System.Func<CheckerField3D, FieldCell, CheckerField3D, FieldCell, float> StepCostAction;


        [System.Serializable]
        public struct PathStep
        {
            public Vector3Int StepDirection;
            public float StepCost;

            public int ForceChangeDirAfter;
            public bool DisallowFinishOn;
            public int AllowUseSinceStep;
            public int DirectionContinuityRequirement;
            public int DirectionOriginationRequirement;

            public float ChangeDirectionCost;
            public float KeepDirectionCost;

            public string Name
            {
                get
                {
                    string _tempName = "";
                    if (StepDirection.x > 0) _tempName += "Right";
                    if (StepDirection.y > 0) _tempName += "Up";
                    if (StepDirection.z > 0) _tempName += "Forw";
                    if (StepDirection.x < 0) _tempName += "Left";
                    if (StepDirection.y < 0) _tempName += "Down";
                    if (StepDirection.z < 0) _tempName += "Back";
                    return _tempName;
                }
            }


#if UNITY_EDITOR

            static GUIContent _gui = null;

            public PathStep(Vector3Int stepDir)
            {
                StepDirection = stepDir;
                StepCost = 1;
                ForceChangeDirAfter = 0;
                DisallowFinishOn = false;
                AllowUseSinceStep = 0;
                DirectionContinuityRequirement = 0;
                DirectionOriginationRequirement = 0;
                ChangeDirectionCost = 0f;
                KeepDirectionCost = 0f;
            }

            public PathStep Copy()
            {
                return (PathStep)MemberwiseClone();
            }

            public PathStep _Editor_GUI_Display(Checker3DPathFindSetup setup)
            {
                PathStep s = this;

                if (_gui == null) _gui = new GUIContent();

                s.StepDirection = EditorGUILayout.Vector3IntField("Step Direction:", StepDirection);
                // TODO Support for few cells jump (create cells betweem - line)
                if (s.StepDirection.x > 1) s.StepDirection.x = 1;
                if (s.StepDirection.x < -1) s.StepDirection.x = -1;
                if (s.StepDirection.y > 1) s.StepDirection.y = 1;
                if (s.StepDirection.y < -1) s.StepDirection.y = -1;
                if (s.StepDirection.z > 1) s.StepDirection.z = 1;
                if (s.StepDirection.z < -1) s.StepDirection.z = -1;

                float preCost = StepCost;
                EditorGUIUtility.labelWidth = 100;
                GUILayout.Space(6);
                _gui.text = "Step Cost:"; _gui.tooltip = "Step cost is defining how worth is using selected path direction step";
                s.StepCost = EditorGUILayout.FloatField(_gui, StepCost);
                if (StepCost <= 0f) s.StepCost = preCost;
                if (StepCost <= 0f) s.StepCost = 1f;

                FGUI_Inspector.DrawUILineCommon();

                EditorGUIUtility.labelWidth = 220;
                _gui.text = "Force Change This Direction After:"; _gui.tooltip = "(dedicated for Y axis steps) Set zero to not use it!\nIt's disabling this direction step if it was used few times in a row.\nUseful for start-like paths - to prevent generating two/more path steps straight down.";
                s.ForceChangeDirAfter = EditorGUILayout.IntField(_gui, ForceChangeDirAfter);
                if (s.ForceChangeDirAfter < 0) s.ForceChangeDirAfter = 0;

                GUILayout.Space(6);
                _gui.text = "Require Direction Continuity:"; _gui.tooltip = "(dedicated for Y axis steps) Allow to use this direction again only after few steps facing towards the same direction. (useful when preventing creating side-to-side stairways)";
                s.DirectionContinuityRequirement = EditorGUILayout.IntField(_gui, DirectionContinuityRequirement);
                if (s.DirectionContinuityRequirement < 0) s.DirectionContinuityRequirement = 0;
                GUILayout.Space(2);

                _gui.text = "Require Direction Origination:"; _gui.tooltip = "(only for Y axis steps) Require using previous forward direction after using this direction. It can help supporting stairs generating Field Setup logics.";

                s.DirectionOriginationRequirement = EditorGUILayout.Toggle(_gui, DirectionOriginationRequirement > 0) ? 1 : 0;

                //s.DirectionOriginationRequirement = EditorGUILayout.IntField(_gui, DirectionOriginationRequirement);
                //if (s.DirectionOriginationRequirement < 0) s.DirectionOriginationRequirement = 0;
                //if (s.DirectionOriginationRequirement > 3) s.DirectionOriginationRequirement = 3;

                FGUI_Inspector.DrawUILineCommon();

                _gui.text = "Disallow Finish Using This Direction:"; _gui.tooltip = "You can don't let path end using this direction.\nIt can be helpful to prevent ending path on room floor when using multi-floor-level setups.";
                s.DisallowFinishOn = EditorGUILayout.Toggle(_gui, DisallowFinishOn);

                _gui.text = "Allow Use Since Path Step:"; _gui.tooltip = "Unlocking this direction when path already generated some steps.\nIt can be useful if you don't want to start path immedietely with stairs if doing multi-floor-level setups.";
                s.AllowUseSinceStep = EditorGUILayout.IntField(_gui, AllowUseSinceStep);
                if (s.AllowUseSinceStep < 0) s.AllowUseSinceStep = 0;

                FGUI_Inspector.DrawUILineCommon();


                EditorGUILayout.BeginHorizontal();
                _gui.text = "Change Direction Cost:"; _gui.tooltip = "";
                s.ChangeDirectionCost = EditorGUILayout.FloatField(_gui, s.ChangeDirectionCost);

                if (GUILayout.Button(new GUIContent("A", "Apply this value to all steps"), GUILayout.Width(26)))
                {
                    for (int i = 0; i < setup.Directions.Count; i++)
                    {
                        var d = setup.Directions[i];
                        d.ChangeDirectionCost = s.ChangeDirectionCost;
                        setup.Directions[i] = d;
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                _gui.text = "Keep Direction Cost:"; _gui.tooltip = "";
                s.KeepDirectionCost = EditorGUILayout.FloatField(_gui, s.KeepDirectionCost);
                if (GUILayout.Button(new GUIContent("A", "Apply this value to all steps"), GUILayout.Width(26)))
                {
                    for (int i = 0; i < setup.Directions.Count; i++)
                    {
                        var d = setup.Directions[i];
                        d.KeepDirectionCost = s.KeepDirectionCost;
                        setup.Directions[i] = d;
                    }
                }

                EditorGUILayout.EndHorizontal();


                EditorGUIUtility.labelWidth = 0;

                return s;
            }
#endif

        }

        int selected = -1;


#if UNITY_EDITOR

        GUIContent _guiC = null;
        public void _Editor_GUI_DrawPathFindSetup(Object toDirty)
        {
            if (_guiC == null) _guiC = new GUIContent();

            EditorGUI.BeginChangeCheck();
            Activated = true;

            EditorGUIUtility.labelWidth = 210;

            _guiC.text = "Search Step Count Safety Limit:"; _guiC.tooltip = "Path find algorithm iteration limit to prevent searching path towards target endlessly";
            SearchStepsLimit = EditorGUILayout.IntField(_guiC, SearchStepsLimit);

            FGUI_Inspector.DrawUILine(0.25f, 0.6f, 1, 10);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = DrawCategory == EPathFindCategory.MainSettings ? Color.green : Color.white;
            if (GUILayout.Button("Main Settings", EditorStyles.miniButtonLeft)) { DrawCategory = EPathFindCategory.MainSettings; }
            GUI.backgroundColor = DrawCategory == EPathFindCategory.Handling ? Color.green : Color.white;
            if (GUILayout.Button("Handling", EditorStyles.miniButtonMid)) { DrawCategory = EPathFindCategory.Handling; }
            GUI.backgroundColor = DrawCategory == EPathFindCategory.ExtraConditions ? Color.green : Color.white;
            if (GUILayout.Button("Extra", EditorStyles.miniButtonRight)) { DrawCategory = EPathFindCategory.ExtraConditions; }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (DrawCategory == EPathFindCategory.Handling)
            {

                GUILayout.Space(5);
                EditorGUIUtility.labelWidth = 110;
                UseLimits = EditorGUILayout.Toggle("Use Limits:", UseLimits);

                if (UseLimits)
                {
                    EditorGUI.indentLevel += 1;

                    GUILayout.Space(2);
                    WorldSpace = EditorGUILayout.Toggle("World Space:", WorldSpace);
                    GUILayout.Space(4);

                    EditorGUIUtility.labelWidth = 60;

                    EditorGUILayout.BeginHorizontal();
                    LimitMinValues.x = EditorGUILayout.FloatField("Min X:", LimitMinValues.x);
                    LimitMaxValues.x = EditorGUILayout.FloatField("Max X:", LimitMaxValues.x);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    LimitMinValues.y = EditorGUILayout.FloatField("Min Y:", LimitMinValues.y);
                    LimitMaxValues.y = EditorGUILayout.FloatField("Max Y:", LimitMaxValues.y);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    LimitMinValues.z = EditorGUILayout.FloatField("Min Z:", LimitMinValues.z);
                    LimitMaxValues.z = EditorGUILayout.FloatField("Max Z:", LimitMaxValues.z);
                    EditorGUILayout.EndHorizontal();

                    EditorGUIUtility.labelWidth = 0;
                    EditorGUI.indentLevel -= 1;
                }


                FGUI_Inspector.DrawUILineCommon(14, 1, 0.9f);


                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 164;
                _guiC.text = "Discard On No Path Found:"; _guiC.tooltip = "If no path has been found, no path shape will be forwarded.";
                DiscardOnNoPathFound = EditorGUILayout.Toggle(_guiC, DiscardOnNoPathFound);

                GUILayout.Space(4);
                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 144;
                _guiC.text = "Connect If Discarded:"; _guiC.tooltip = "If there was found cell connected with target field but was discarded because of some light conditions (command max distance, side cells) still try generate path using discarded target position";
                ConnectEvenDiscarded = EditorGUILayout.Toggle(_guiC, ConnectEvenDiscarded);
                GUILayout.Space(4);
                EditorGUILayout.EndHorizontal();


                FGUI_Inspector.DrawUILineCommon(14, 1, 0.9f);


                EditorGUIUtility.labelWidth = 164;
                _guiC.text = "Separate Self Y Cells:"; _guiC.tooltip = "If you don't want to place cells too near each other in Y axis, increase this value. (it's self collision separation)";
                SeparateSelfYCells = EditorGUILayout.IntField(_guiC, SeparateSelfYCells);
                if (SeparateSelfYCells < 0) SeparateSelfYCells = 0;

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();

                _guiC.text = "Height Collision Margins:"; _guiC.tooltip = "if you want to prevent generating path behind some cells but make some more spacing";
                EditorGUILayout.LabelField(_guiC, GUILayout.Width(184));

                EditorGUIUtility.labelWidth = 50;
                SeparateCollisionY.x = EditorGUILayout.FloatField("Above:", SeparateCollisionY.x);
                if (SeparateCollisionY.x < 0) SeparateCollisionY.x = 0;
                GUILayout.Space(14);
                SeparateCollisionY.y = EditorGUILayout.FloatField("Below:", SeparateCollisionY.y);
                if (SeparateCollisionY.y < 0) SeparateCollisionY.y = 0;

                EditorGUILayout.EndHorizontal();






                GUILayout.Space(4);

            }
            else if (DrawCategory == EPathFindCategory.MainSettings)
            {

                EditorGUIUtility.labelWidth = 194;


                EditorGUILayout.BeginHorizontal();
                _guiC.text = "Start Path Inside Field Center:"; _guiC.tooltip = "Starting generating path inside the center on field or startinhon nearest side. (Enable if you want to generate path which starts in the ceiling instead of wall side)";
                ForceStartPathOnSide = !EditorGUILayout.Toggle(_guiC, ForceStartPathOnSide);

                GUILayout.Space(4);
                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 154;
                _guiC.text = "Realign End Cell Centered:"; _guiC.tooltip = "(Calculated after finding path) Ensuring left/right side of the ending path cell is having neightbour cells";
                TryEndCentered = EditorGUILayout.Toggle(_guiC, TryEndCentered);
                GUILayout.Space(4);
                EditorGUILayout.EndHorizontal();


                //if (ForceStartPathOnSide == false) GUI.enabled = false;
                //EditorGUIUtility.labelWidth = 284;
                //_guiC.text = "   ^ Allow Instant Path when Fields Are Aligning:"; _guiC.tooltip = "If fields are just next to each other, aligning with cells but not colliding, then generating path intantly out of two neightbour-aligning cells found.\nHelpful when generating connections between rectangle-packed rooms.";
                //AllowAlignJoin = EditorGUILayout.Toggle(_guiC, AllowAlignJoin);
                //GUILayout.Space(4);
                //GUI.enabled = true;


                EditorGUIUtility.labelWidth = 154;
                _guiC.text = "Adjust Ending Cell:"; _guiC.tooltip = "(Calculated before finding path) Ensuring left/right side of the ending path cell is having neightbour cells";
                AdjustEndingCell = EditorGUILayout.Toggle(_guiC, AdjustEndingCell);

                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 194;
                _guiC.text = "Ignore Self Collision:"; _guiC.tooltip = "Ignoring collision with start field and with already generated path cells.";
                IgnoreSelfCollision = EditorGUILayout.Toggle(_guiC, IgnoreSelfCollision);

                GUILayout.Space(4);
                GUILayout.FlexibleSpace();
                _guiC.text = "Single Step Mode:"; _guiC.tooltip = "Allow for just one step for pathfind. Dedicated for packed-aligned rectangles connection pathfind";
                EditorGUIUtility.labelWidth = 154;
                SingleStepMode = EditorGUILayout.Toggle(_guiC, SingleStepMode);
                GUILayout.Space(4);
                EditorGUILayout.EndHorizontal();

                if (SingleStepMode)
                {
                    EditorGUILayout.HelpBox("Using Single-Step-Mode which is dedicated for packed-aligned rectangles connection pathfind.", MessageType.None);
                }

                //_guiC.text = "Try Start Centered:"; _guiC.tooltip = "Finding most centered cell of the path origin grid's edge and starting path search from it";
                //TryStartCentered = EditorGUILayout.Toggle(_guiC, TryStartCentered);

                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                EditorGUIUtility.labelWidth = 194;
                _guiC.text = "Spherical Distance Measure:"; _guiC.tooltip = "Measuring distance to drive path finding using Rectangular distance (manhattan) or spherical (sqrt).";
                SphericalDistanceMeasuring = EditorGUILayout.Toggle(_guiC, SphericalDistanceMeasuring);

                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 154;
                _guiC.text = "Connect On Any Contact:"; _guiC.tooltip = "Skipping all basic rules and connecting to target field on first contact.";
                OnAnyContact = EditorGUILayout.Toggle(_guiC, OnAnyContact);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                //EditorGUIUtility.labelWidth = 160;
                _guiC.text = "Prioritize Target Y Level:"; _guiC.tooltip = "Trying to reach target point's y axis value as soon as possible.\nUseful for multi-floor-level path generation.\nUse path steps 'Change Direction Cost' and 'Keep Direction Cost' values to customize path shape logic.";
                PrioritizeTargetYLevel = EditorGUILayout.Toggle(_guiC, PrioritizeTargetYLevel);

                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 154;
                _guiC.text = "Prioritize Perpendicular:"; _guiC.tooltip = "Trying to redirect path to start straightened and end with defined line towards target.";
                PrioritizePerpendicular = EditorGUILayout.Toggle(_guiC, PrioritizePerpendicular);
                EditorGUILayout.EndHorizontal();

                EditorGUIUtility.labelWidth = 194;
                GUILayout.Space(5);
                _guiC.text = "Snap to instructions range:"; _guiC.tooltip = "Snapping start/end path point towards already generated cell instruction positions.";
                SnapToInstructionsOnDistance = EditorGUILayout.FloatField(_guiC, SnapToInstructionsOnDistance);

                GUILayout.Space(5);

                LogWarnings = EditorGUILayout.Toggle("Log Warnings:", LogWarnings);

                GUILayout.Space(5);

                PathFindFocusLevel = (CheckerField3D.EPathFindFocusLevel)EditorGUILayout.EnumPopup(new GUIContent("Focus Level", "Cells level on which 'path start/path end' should find cells for connections"), PathFindFocusLevel);

                GUILayout.Space(5);

                //EditorGUIUtility.labelWidth = 180;
                //_guiC.text = "Allow Change Direction Every:"; _guiC.tooltip = "You can set it higher if you want to prevent single-cell path direction changes (unless there is obstacle in a way)";
                //AllowChangeDirectionEvery = EditorGUILayout.IntField(_guiC, AllowChangeDirectionEvery);
                //if (AllowChangeDirectionEvery < 1) AllowChangeDirectionEvery = 1;

                EditorGUIUtility.labelWidth = 0;

                GUI.enabled = false;
                EditorGUILayout.IntField("TODO: Spacing on sides", 0);
                EditorGUILayout.IntField("TODO: Path Thickness", 0);
                GUI.enabled = true;
            }
            else if (DrawCategory == EPathFindCategory.ExtraConditions)
            {
                GUILayout.Space(5);




                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 230;
                DontAllowFinishId = EditorGUILayout.IntField("Dont allow complete near to command:", DontAllowFinishId, GUILayout.Width(290));
                GUILayout.Space(8);
                EditorGUIUtility.labelWidth = 59;
                DontAllowFinishDist = EditorGUILayout.FloatField("Distance", DontAllowFinishDist);

                GUILayout.Space(4);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);
                EditorGUIUtility.labelWidth = 230;
                DontAllowStartToo = EditorGUILayout.Toggle("Try Same for start path position:", DontAllowStartToo);
                GUILayout.Space(4);

                if (DontAllowFinishId <= -1) DontAllowFinishId = -1;

                if (DontAllowFinishDist <= 0)
                {
                    DontAllowFinishDist = 0;
                    EditorGUILayout.HelpBox("Disallowing finishing path near to commands OFF", MessageType.None);
                }
                else
                {
                    if (DontAllowFinishId <= -1)
                    {
                        EditorGUILayout.HelpBox("Disallowing finishing path near to ANY command in distance of " + DontAllowFinishDist + " cells", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Disallowing finishing path near to command of ID=" + DontAllowFinishId + " in distance of " + DontAllowFinishDist + " cells", MessageType.None);
                    }
                }


                FGUI_Inspector.DrawUILineCommon(14, 1, 0.9f);

                GUILayout.Space(4);

                _guiC.text = "Discard If Targets Aligning:"; _guiC.tooltip = "If fields are just next to each other it will discard path creations in favor custom handling connection";
                DiscardIfAligning = EditorGUILayout.Toggle(_guiC, DiscardIfAligning);

                GUILayout.Space(4);

                ExistingCellsCostMultiplier = EditorGUILayout.FloatField(new GUIContent("Existing Cells Step Cost Multiplier:", "Its cost multiplier. When path search encounters cell which already belongs to the path (during extra search) then it can prioritize choosing this cell for path shape."), ExistingCellsCostMultiplier);
                if (ExistingCellsCostMultiplier < 0f) ExistingCellsCostMultiplier = 0f;
                //if ( ExistingCellsCostMultiplier != 1f)
                //{
                //    EditorGUI.indentLevel++;
                //    SelfCellsExtraSearchRange = EditorGUILayout.FloatField("Self Cells Search Range:", SelfCellsExtraSearchRange);
                //    if (SelfCellsExtraSearchRange < 1f) SelfCellsExtraSearchRange = 1f;
                //    EditorGUI.indentLevel--;
                //}

                GUILayout.Space(4);
            }

            GUILayout.Space(8);
            FGUI_Inspector.DrawUILine(0.25f, 0.6f, 1, 10);
            GUILayout.Space(-6);

            EditorGUILayout.BeginHorizontal(FGUI_Resources.ViewBoxStyle, GUILayout.Height(28));
            EditorGUILayout.LabelField("Find Step Directions (" + Directions.Count + "):");
            if (GUILayout.Button("+", GUILayout.Width(22))) { Directions.Add(new PathStep()); }
            EditorGUILayout.EndHorizontal();

            float lastViewWidth = EditorGUIUtility.currentViewWidth;

            EditorGUILayout.BeginHorizontal();
            float currentWdth = 6f;
            GUIContent guiC = new GUIContent();

            for (int i = 0; i < Directions.Count; i++)
            {
                guiC.text = "[" + i + "] " + Directions[i].Name;
                Vector2 disp = EditorStyles.miniButton.CalcSize(guiC);
                float nextWdth = currentWdth + disp.x;

                if (nextWdth > lastViewWidth - 16f)
                {
                    currentWdth = 6f;
                    nextWdth = currentWdth + disp.x;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                currentWdth = nextWdth;

                if (selected == i) GUI.backgroundColor = Color.green;
                if (GUILayout.Button(guiC, GUILayout.Width(disp.x))) { if (selected == i) selected = -1; else selected = i; }
                if (selected == i) GUI.backgroundColor = Color.white;
            }

            if (selected >= Directions.Count) selected = Directions.Count - 1;

            EditorGUILayout.EndHorizontal();
            FGUI_Inspector.DrawUILine(0.15f, 0.6f, 1, 10);

            if (Directions.Count == 0)
            {
                EditorGUILayout.HelpBox("No directions setted - using default flat 2D search then", MessageType.None);
            }
            else
            {
                if (selected == -1)
                {
                    EditorGUILayout.HelpBox("No Path-find step selected", MessageType.None);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Settings for [" + selected + "] direction");

                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f, 1f);
                    if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(23))) { Directions.RemoveAt(selected); selected -= 1; }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(4);
                    if (Directions.Count > 0) Directions[selected] = Directions[selected]._Editor_GUI_Display(this);
                }
            }

            EditorGUIUtility.labelWidth = 0;
            GUILayout.Space(4);


            GUILayout.FlexibleSpace();

            if (Directions.Count < 4) EditorGUILayout.HelpBox("You should provide at least 4 directions for path find!\n(using default 3D search now)", MessageType.Warning);

            FGUI_Inspector.DrawUILine(0.25f, 0.6f, 1, 4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Presets:", GUILayout.Width(50));
            if (GUILayout.Button("2D Flat")) { Preset_Apply2DFlat(); }
            //if (GUILayout.Button("2D +Diag")) { Preset_Apply2DDiag(); }
            if (GUILayout.Button("3D Dirs")) { Preset_Apply3D(false); }
            if (GUILayout.Button("3D +Setup")) { Preset_Apply3D(true); }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (EditorGUI.EndChangeCheck())
            {
                if (toDirty) EditorUtility.SetDirty(toDirty);
            }
        }

        void Preset_Apply2DFlat()
        {
            Directions.Clear();

            Directions.Add(new PathStep(new Vector3Int(1, 0, 0)));
            Directions.Add(new PathStep(new Vector3Int(-1, 0, 0)));
            Directions.Add(new PathStep(new Vector3Int(0, 0, 1)));
            Directions.Add(new PathStep(new Vector3Int(0, 0, -1)));
        }

        void Preset_Apply3D(bool settings)
        {
            Directions.Clear();

            Directions.Add(new PathStep(new Vector3Int(1, 0, 0)));
            Directions.Add(new PathStep(new Vector3Int(-1, 0, 0)));
            Directions.Add(new PathStep(new Vector3Int(0, 0, 1)));
            Directions.Add(new PathStep(new Vector3Int(0, 0, -1)));

            PathStep step = new PathStep(new Vector3Int(0, 1, 0));
            step.ForceChangeDirAfter = 1;
            step.DirectionContinuityRequirement = 1;
            step.DirectionOriginationRequirement = 1;
            step.DisallowFinishOn = true;
            step.AllowUseSinceStep = 2;
            Directions.Add(step);

            step = step.Copy();
            step.StepDirection = new Vector3Int(0, -1, 0);
            Directions.Add(step);

            if (settings)
            {
                ForceStartPathOnSide = true;
                //TryStartCentered = true;
                PrioritizeTargetYLevel = true;
                SeparateSelfYCells = 1;
                SeparateCollisionY = new Vector2(1, 0);
                UseLimits = false;
            }

        }


#endif


        public CheckerField3D.PathFindParams ToCheckerFieldPathFindParams()
        {
            CheckerField3D.PathFindParams pathParams;

            if (!Activated || Directions.Count < 2)
            {
                pathParams = new CheckerField3D.PathFindParams();
                pathParams.directions = CheckerField3D.GetDefaultDirections3D;
                pathParams.NoLimits = true;
            }
            else
            {
                pathParams = new CheckerField3D.PathFindParams();
                pathParams.directions = new List<CheckerField3D.LineFindHelper>();

                for (int i = 0; i < Directions.Count; i++)
                {
                    PathStep mDir = Directions[i];
                    CheckerField3D.LineFindHelper dir = new CheckerField3D.LineFindHelper(mDir.StepDirection, mDir.StepCost, mDir.ForceChangeDirAfter, mDir.DisallowFinishOn, mDir.AllowUseSinceStep, mDir.DirectionContinuityRequirement, mDir.DirectionOriginationRequirement);
                    dir.ChangeDirectionCost = mDir.ChangeDirectionCost;
                    dir.KeepDirectionCost = mDir.KeepDirectionCost;
                    pathParams.directions.Add(dir);
                }

                pathParams.NoLimits = !UseLimits;
                if (UseLimits)
                {
                    pathParams.LimitMinX = LimitMinValues.x;
                    pathParams.LimitMaxX = LimitMaxValues.x;
                    pathParams.LimitLowestY = LimitMinValues.y;
                    pathParams.LimitHighestY = LimitMaxValues.y;
                    pathParams.LimitMinZ = LimitMinValues.z;
                    pathParams.LimitMaxZ = LimitMaxValues.z;
                }
            }

            pathParams.YLevelSeparation = SeparateSelfYCells;
            pathParams.CollisionYMargins = SeparateCollisionY;
            pathParams.WorldSpace = WorldSpace;
            pathParams.IgnoreSelfCollision = IgnoreSelfCollision;
            pathParams.DiscardOnNoPathFound = DiscardOnNoPathFound;
            pathParams.SearchStepIterationLimit = SearchStepsLimit;
            //pathParams.KeepDirectionFor = AllowChangeDirectionEvery;
            pathParams.SphericalDistanceMeasure = SphericalDistanceMeasuring;
            pathParams.FindFocusLevel = PathFindFocusLevel;
            pathParams.ConnectOnAnyContact = OnAnyContact;
            pathParams.PrioritizeTargetedYLevel = PrioritizeTargetYLevel;
            pathParams.PrioritizePerpendicular = PrioritizePerpendicular;

            pathParams.DontAllowFinishDist = DontAllowFinishDist;
            pathParams.DontAllowFinishId = DontAllowFinishId;
            pathParams.DontAllowStartToo = DontAllowStartToo;
            pathParams.LogWarnings = LogWarnings;
            pathParams.ConnectEvenDiscarded = ConnectEvenDiscarded;
            pathParams.DiscardIfAligning = DiscardIfAligning;
            pathParams.SnapToInstructionsOnDistance = SnapToInstructionsOnDistance;

            pathParams.ExistingCellsCostMul = ExistingCellsCostMultiplier;
            pathParams.ExistingCellsCheckRange = SelfCellsExtraSearchRange;

            if (TryEndCentered)
            {
                pathParams.End_RequireCellsOnLeftSide = 1;
                pathParams.End_RequireCellsOnRightSide = 1;
            }

            pathParams.TryEndCentered = AdjustEndingCell;
            pathParams.SingleStepMode = SingleStepMode;

            //pathParams.StartCentered = TryStartCentered;
            pathParams.StartOnSide = ForceStartPathOnSide;
            //pathParams.AllowNeightbourAlign = AllowAlignJoin;

            return pathParams;
        }

    }

#if UNITY_EDITOR
    public class PathSettingsWindow : EditorWindow
    {
        public static PathSettingsWindow Get;
        public static bool OnChange = false;
        public static bool GetChanged() { if (!OnChange) return false; OnChange = false; return true; }

        static Checker3DPathFindSetup lastSetup = null;
        static UnityEngine.Object ToDirty = null;

        public static void Init(UnityEngine.Object toDirty, Checker3DPathFindSetup setup)
        {
            if (FGenerators.CheckIfIsNull(setup))
            {
                UnityEngine.Debug.Log("Null Setup Reference! ");
                return;
            }

            ToDirty = toDirty;

            lastSetup = setup;
            PathSettingsWindow window = (PathSettingsWindow)GetWindow(typeof(PathSettingsWindow), true);
            window.titleContent = new GUIContent(" Path-find Setup", PGGUtils.TEX_CellInstr);
            window.Show();
            window.minSize = new Vector2(300, 320);
            Get = window;
        }

        private void OnEnable()
        {
            Get = this;
        }

        private void OnGUI()
        {
            if (lastSetup == null) { Close(); return; }

            GUILayout.Space(6);
            string spName = "";
            EditorGUILayout.LabelField("Prepare Path-Find Setup" + spName, FGUI_Resources.HeaderStyle);
            FGUI_Inspector.DrawUILine(0.25f, 0.6f, 2, 8);


            if (lastSetup == null)
            {
                EditorGUILayout.HelpBox("No Data To Display", MessageType.Info);
                return;
            }

            lastSetup._Editor_GUI_DrawPathFindSetup(ToDirty);
        }

    }


#endif

}
