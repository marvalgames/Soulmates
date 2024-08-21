using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static FIMSpace.Generating.TileMeshSetup;

namespace FIMSpace.Generating
{
    [System.Serializable]
    public partial class TileDesign
    {
        public string DesignName = "New Tile";
        public List<TileMeshSetup> TileMeshes = new List<TileMeshSetup>();

        public static TileDesign _CopyFrom = null;

        public static string CommandsInfo
        {
            get
            {
                return "Example:\n[0] set height 2;[0] set collapse 1\n[0] curve2 points 0,1 0,0 1,1\n[0] set U 2  (U is UV.x)\n[0] set V 2  (V is UV.y)\n[0] inst[0] enable;[0] inst[1] disable" +
                 "\nOther commands\nLoft Only: [0] cornerIN {subdivs} {offset}, and cornerOUT. For example '[0] cornerOUT 2'\nor '[0] cornerIN 2 0.2' \n[0] fillIN and [0] fillOUT\npush {x y curve points push}\n" +
                 "\nMain Setup Commands (without [NR]) : 'collider 0' 0 = box  5 = combined mesh collider\n" +   
                 "\nUse semicolons ';' for multiple commands.";
            }
        }

        public void ApplyCustomCommand(string designModifyCommand)
        {
            string[] commands = designModifyCommand.Split(';');
            bool curve1Cleared = false;
            bool curve2Cleared = false;

            for (int i = 0; i < commands.Length; i++)
            {
                string[] arguments = commands[i].Split(' ');

                if (arguments.Length < 2) continue;

                // Arg 0 = [NR]
                if (arguments[0].Length < 3) continue;
                //if (arguments[0][0] != '[') continue;

                TileMeshSetup tile = null;

                #region Choose Tile

                int id = -1;
                if (arguments[0][2] == ']')
                {
                    if (!int.TryParse(arguments[0][1].ToString(), out id)) continue;
                }
                else if (arguments[0][3] == ']')
                {
                    if (!int.TryParse((arguments[0][1] + arguments[0][2]).ToString(), out id)) continue;
                }
                else if (arguments[0] == "collider")
                {
                    if (arguments.Length > 1)
                    {
                        int arg1Value;
                        if (int.TryParse(arguments[1], out arg1Value))
                        {
                            if (arg1Value >= 0 && arg1Value <= 5) ColliderMode = (EColliderMode)arg1Value;
                        }
                    }

                    continue;
                }
                else
                {

                    continue;
                }


                if (!TileMeshes.ContainsIndex(id)) continue;

                tile = TileMeshes[id];

                #endregion

                if (tile == null)
                {
                    UnityEngine.Debug.Log("no tile!");
                    continue;
                }

                int a = 1;
                string arg = arguments[a];

                if (arg == "set") // arg = arguments[a]
                {
                    if (a + 1 >= arguments.Length) continue; // No args
                    if (a + 2 >= arguments.Length) break; // No args

                    string setWhat = arguments[a + 1];
                    string valStr = "";

                    for (int v = a + 2; v < arguments.Length; v++) valStr += arguments[v];

                    float val = -1f;
                    if (!float.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) continue;

                    if (setWhat == "height")
                    {
                        tile.height = val;
                    }
                    else if (setWhat == "width")
                    {
                        tile.width = val;
                    }
                    else if (setWhat == "depth")
                    {
                        tile.depth = val;
                    }
                    else if (setWhat == "collapse")
                    {
                        tile._loft_Collapse = val;
                    }
                    else if (setWhat == "U")
                    {
                        tile.UVMul = new Vector2(val, tile.UVMul.y);
                    }
                    else if (setWhat == "V")
                    {
                        tile.UVMul = new Vector2(tile.UVMul.x, val);
                    }
                }
                else if (arg.StartsWith("inst")) // arg = arguments[a]
                {
                    if (a + 1 >= arguments.Length) continue; // No args

                    int len = "inst".Length;
                    if (arg.Length < len + 3) continue;
                    if (arg[len] != '[') continue;
                    if (arg[len + 2] != ']') continue;

                    int instID;
                    if (int.TryParse(arg[len + 1].ToString(), out instID) == false) continue;

                    if (tile.Instances.ContainsIndex(instID) == false) continue;
                    var inst = tile.Instances[instID];

                    string action = arguments[a + 1];
                    if (action == "enable") inst.Enabled = true;
                    else if (action == "disable") inst.Enabled = false;
                }
                else if (arg.StartsWith("curve")) // arg = arguments[a]
                {
                    if (a + 1 >= arguments.Length) continue; // No args

                    List<CurvePoint> curvePoints;

                    if (arg == "curve2")
                    {
                        curvePoints = tile.GetCurve2();

                        if (curve2Cleared == false)
                        {
                            curvePoints.Clear();
                            curve2Cleared = true;
                        }
                    }
                    else
                    {
                        curvePoints = tile.GetCurve1();

                        if (curve1Cleared == false)
                        {
                            curvePoints.Clear();
                            curve1Cleared = true;
                        }
                    }

                    string nextA = arguments[a + 1];

                    if (nextA == "points")
                    {
                        for (int n = a + 2; n < arguments.Length; n += 1)
                        {
                            string numArg = arguments[n];
                            if (numArg.Contains(",") == false) continue;

                            int indexOfComma = numArg.IndexOf(',');
                            string x = numArg.Substring(0, indexOfComma);
                            string y = numArg.Substring(indexOfComma + 1, numArg.Length - (indexOfComma + 1));

                            float xVal, yVal;
                            if (!float.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out xVal)) continue;
                            if (!float.TryParse(y, NumberStyles.Any, CultureInfo.InvariantCulture, out yVal)) continue;

                            curvePoints.Add(new CurvePoint(xVal, yVal, true));
                        }
                    }
                }
                else if (arg == "push") // arg = arguments[a]
                {
                    if (arguments.ContainsIndex(a + 1, true))
                    {
                        var curvePoints = tile.GetCurve1();
                        float x = 0f;
                        float.TryParse(arguments[a + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out x);

                        if (arguments.ContainsIndex(a + 2, true)) // Just X push
                        {
                            for (int c = 0; c < curvePoints.Count; c++) curvePoints[c].localPos += new Vector2(x, 0);
                        }
                        else // XY push
                        {
                            float y = 0f;
                            float.TryParse(arguments[a + 2], NumberStyles.Any, CultureInfo.InvariantCulture, out y);
                            for (int c = 0; c < curvePoints.Count; c++) curvePoints[c].localPos += new Vector2(x, y);
                        }
                    }
                }
                else if (arg == "cornerOUT") // arg = arguments[a]
                {
                    float sp = 1f; // Seapration / spacing
                    float subdivs = 1f;

                    if (arguments.ContainsIndex(a + 1, true))
                    {
                        float.TryParse(arguments[a + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out subdivs);

                        if (arguments.ContainsIndex(a + 2, true))
                        {
                            float mul = 1f;
                            float.TryParse(arguments[a + 2], NumberStyles.Any, CultureInfo.InvariantCulture, out mul);
                            sp *= mul;
                            if (sp == 0f) sp = 1f;
                        }
                    }

                    Command_Curve_CornerOUTShape(sp, subdivs, tile.GetCurve2());
                }
                else if (arg == "cornerIN") // arg = arguments[a]
                {
                    float sp = 1f; // Seapration / spacing
                    float subdivs = 1f;

                    if (arguments.ContainsIndex(a + 1, true))
                    {
                        float.TryParse(arguments[a + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out subdivs);

                        if (arguments.ContainsIndex(a + 2, true))
                        {
                            float mul = 1f;
                            float.TryParse(arguments[a + 2], NumberStyles.Any, CultureInfo.InvariantCulture, out mul);
                            sp *= mul;
                            if (sp == 0f) sp = 1f;
                        }
                    }

                    Command_Curve_CornerOUTShape(sp, subdivs, tile.GetCurve2());
                    tile.GetCurve2().Reverse();

                    //var curvePoints = tile.GetCurve2();
                    //curvePoints.Clear();

                    //curvePoints.Add(new CurvePoint(new Vector2(sp, 0f), true));
                    //curvePoints.Add(new CurvePoint(new Vector2(0f, 0f), true));
                    //curvePoints.Add(new CurvePoint(new Vector2(0f, sp), true));

                    //if (subdivs >= 2f)
                    //{
                    //    if (subdivs >= 3f)
                    //    {
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, 0.2f * axisMul.y), true));

                    //        curvePoints.Add(new CurvePoint(new Vector2(0.05f * axisMul.y, 0.1f * axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0.1f * axisMul.y, 0.05f * axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0.1f * axisMul.y, 0.05f * axisMul.y), true));

                    //        curvePoints.Add(new CurvePoint(new Vector2(0.2f * axisMul.y, 0f), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(axisMul.x, 0f), true));
                    //    }
                    //    else
                    //    {
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, 0.125f * axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0.05f * axisMul.y, 0.05f * axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0.125f * axisMul.y, 0f), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(axisMul.x, 0f), true));
                    //    }
                    //}
                    //else
                    //{
                    //    if (subdivs < 0f)
                    //    {
                    //        float mul = Mathf.Abs(subdivs);
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(0f, mul * axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(mul * axisMul.y, 0f), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(axisMul.x, 0f), true));
                    //    }
                    //    else
                    //    {
                    //        curvePoints.Add(new CurvePoint(new Vector2(1f, axisMul.y), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(1f, 1f), true));
                    //        curvePoints.Add(new CurvePoint(new Vector2(axisMul.x, 1f), true));
                    //    }
                    //}
                }
                else if (arg == "fillIN")
                {
                    //[0] cornerIN 2 0.2
                    tile._loft_Collapse = 1f;
                    var curvef = tile.GetCurve2();
                    curvef.Clear();
                    curvef.Add(new CurvePoint(0, 0, true));
                    curvef.Add(new CurvePoint(0, 1, true));
                    curvef.Add(new CurvePoint(1, 1, true));
                }
                else if (arg == "fillOUT")
                {
                    tile._loft_Collapse = 1f;
                    var curvef = tile.GetCurve2();
                    curvef.Clear();
                    curvef.Add(new CurvePoint(1, 1, true));
                    curvef.Add(new CurvePoint(0, 1, true));
                    curvef.Add(new CurvePoint(0, 0, true));
                    //[0] set collapse 1; [0] curve2 points 1,1 0,1 0,0
                }

            }
        }


        void Command_Curve_CornerOUTShape(float spacing, float subdivs, List<CurvePoint> curvePoints)
        {
            float sp = spacing; // Seapration / spacing

            curvePoints.Clear();

            if (subdivs >= 2f)
            {
                if (subdivs >= 3f)
                {
                    curvePoints.Add(new CurvePoint(new Vector2(0f, sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0f, 0.2f * sp), true));

                    curvePoints.Add(new CurvePoint(new Vector2(0.05f * sp, 0.1f * sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0.1f * sp, 0.05f * sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0.1f * sp, 0.05f * sp), true));

                    curvePoints.Add(new CurvePoint(new Vector2(0.2f * sp, 0f), true));
                    curvePoints.Add(new CurvePoint(new Vector2(sp, 0f), true));
                }
                else
                {
                    curvePoints.Add(new CurvePoint(new Vector2(0f, sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0f, 0.125f * sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0.05f * sp, 0.05f * sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0.125f * sp, 0f), true));
                    curvePoints.Add(new CurvePoint(new Vector2(sp, 0f), true));
                }
            }
            else
            {
                if (subdivs < 0f)
                {
                    float mul = Mathf.Abs(subdivs);
                    curvePoints.Add(new CurvePoint(new Vector2(0f, sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0f, mul * sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(mul * sp, 0f), true));
                    curvePoints.Add(new CurvePoint(new Vector2(sp, 0f), true));
                }
                else
                {
                    curvePoints.Add(new CurvePoint(new Vector2(0f, sp), true));
                    curvePoints.Add(new CurvePoint(new Vector2(0f, 0f), true));
                    curvePoints.Add(new CurvePoint(new Vector2(sp, 0f), true));
                }
            }
        }



        public void PasteEverythingFrom(TileDesign from)
        {
            DesignName = from.DesignName;
            DefaultMaterial = from.DefaultMaterial;

            TileMeshes.Clear();

            for (int t = 0; t < from.TileMeshes.Count; t++)
            {
                TileMeshSetup meshSet = new TileMeshSetup(from.TileMeshes[t].Name);
                from.TileMeshes[t].PasteAllSetupTo(meshSet, true);
                TileMeshes.Add(meshSet);
            }

            PostFilters = new List<PostFilterHelper>();
            for (int p = 0; p < from.PostFilters.Count; p++)
            {
                PostFilters.Add(from.PostFilters[p].Copy());
            }

            PasteColliderParameters(from, this);
            PasteGameObjectParameters(from, this);
        }

    }
}