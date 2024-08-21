using FIMSpace.FEditor;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating
{
    //[CreateAssetMenu(fileName = "FS Post Event - Set Field Variable", menuName = "PE Field Var Instance", order = 1)]
    public class FSPostEvent_SetVariable : FieldSpawnerPostEvent_Base
    {
        object toRestore;
        FieldVariable fieldVar;

        enum EVarType
        {
            Number, Material, GameObject, Color
        }

        FieldVariable VariableName(FieldSetup.CustomPostEventHelper helper)
        {
            return helper.RequestVariable("Field Variable", "Field Variable");
        }

        FieldVariable VariableType(FieldSetup.CustomPostEventHelper helper)
        {
            return helper.RequestVariable("Type", 2);
        }

        FGenerators.DefinedRandom rand;

        public override void OnBeforeRunningCall(FieldSetup.CustomPostEventHelper helper, FieldSetup preset)
        {
            fieldVar = preset.GetVariable(VariableName(helper).GetStringValue());
            if (fieldVar != null)
            {
                toRestore = fieldVar.GetValue();

                rand = new FGenerators.DefinedRandom(FGenerators.LatestSeed + 1000);
                GameObject toSet = helper.customObjectList[rand.GetRandom(0, helper.customObjectList.Count)] as GameObject;
                fieldVar.SetValue(toSet);
            }
        }

        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            if (fieldVar != null) fieldVar.SetValue(toRestore);
        }


#if UNITY_EDITOR
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            PostEventInfo = "Changing Field Setup variable to random provided value before generating.";

            var hVar = VariableName(helper);
            FieldVariable.Editor_DrawTweakableVariable(hVar);

            hVar = VariableType(helper);
            int mode = hVar.IntV;
            EVarType eMode = (EVarType)mode;
            eMode = (EVarType)EditorGUILayout.EnumPopup("Variable Type:", eMode);
            hVar.IntV = (int)eMode;

            GUILayout.Space(4);

            int listCount;
            if (eMode == EVarType.GameObject || eMode == EVarType.Material) listCount = helper.customObjectList.Count;
            else listCount = helper.customStringList.Count;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Choose From (" + listCount + ")");
            if (GUILayout.Button("+"))
            {
                if (eMode == EVarType.GameObject || eMode == EVarType.Material)
                    helper.customObjectList.Add(null);
                else
                    helper.customStringList.Add("");
            }

            EditorGUILayout.EndHorizontal();
            int toRemove = -1;


            if (eMode == EVarType.GameObject || eMode == EVarType.Material)
            {
                for (int i = 0; i < helper.customObjectList.Count; i++)
                {
                    var obj = helper.customObjectList[i];
                    EditorGUILayout.BeginHorizontal();

                    if (eMode == EVarType.GameObject)// I know it can be optimal with multiple fors, but it's lightweight editor script so we do if in every for iteration for CLEANER CODE
                        helper.customObjectList[i] = (UnityEngine.Object)EditorGUILayout.ObjectField("[" + i + "]", obj, typeof(GameObject), false);
                    else
                        helper.customObjectList[i] = (UnityEngine.Object)EditorGUILayout.ObjectField("[" + i + "]", obj, typeof(Material), false);

                    if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(22))) { toRemove = i; }
                    EditorGUILayout.EndHorizontal();
                }

                if (toRemove != -1) helper.customObjectList.RemoveAt(toRemove);
            }
            else
            {

                for (int i = 0; i < helper.customStringList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    if (eMode == EVarType.Number) // I know it can be optimal with multiple fors, but it's lightweight editor script so we do if in every for iteration for CLEANER CODE
                    {
                        float val = 0f;
                        float.TryParse(helper.customStringList[i], NumberStyles.Any, CultureInfo.InvariantCulture, out val);
                        val = EditorGUILayout.FloatField("[" + i + "]", val);
                        helper.customStringList[i] = val.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (eMode == EVarType.Color)
                    {
                        Color col = HexToColor(helper.customStringList[i]);
                        helper.customStringList[i] = ColorToHex(EditorGUILayout.ColorField("[" + i + "]", col));
                    }

                    if (GUILayout.Button(FGUI_Resources.GUIC_Remove, FGUI_Resources.ButtonStyle, GUILayout.Height(18), GUILayout.Width(22))) { toRemove = i; }
                    EditorGUILayout.EndHorizontal();
                }

                if (toRemove != -1) helper.customStringList.RemoveAt(toRemove);
            }

        }

        void SetDirty(FieldSetup preset)
        {
            EditorUtility.SetDirty(preset);
        }

        public static string ColorToHex(Color color)
        {
            Color32 col32 = new Color32((byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
            string hex = "";
            hex += System.String.Format("{0}{1}{2}{3}"
               , col32.r.ToString("X").Length == 1 ? System.String.Format("0{0}", col32.r.ToString("X")) : col32.r.ToString("X")
               , col32.g.ToString("X").Length == 1 ? System.String.Format("0{0}", col32.g.ToString("X")) : col32.g.ToString("X")
               , col32.b.ToString("X").Length == 1 ? System.String.Format("0{0}", col32.b.ToString("X")) : col32.b.ToString("X")
               , col32.a.ToString("X").Length == 1 ? System.String.Format("0{0}", col32.a.ToString("X")) : col32.a.ToString("X"));

            return hex;
        }

        public static Color32 HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            uint rgba = 0x000000FF;
            hex = hex.Replace("#", "");
            hex = hex.Replace("0x", "");
            if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out rgba)) return Color.white;
            return new Color32((byte)((rgba & -16777216) >> 0x18), (byte)((rgba & 0xff0000) >> 0x10), (byte)((rgba & 0xff00) >> 8), (byte)(rgba & 0xff));
        }

#endif

    }
}