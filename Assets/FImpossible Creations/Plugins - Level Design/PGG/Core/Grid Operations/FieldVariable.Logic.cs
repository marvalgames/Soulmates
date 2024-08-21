using UnityEngine;
using System;

namespace FIMSpace.Generating
{
    public partial class FieldVariable
    {
        public enum EAlgebraOperation
        {
            Add, Subtract, Divide, Multiply
        }

        public enum ELogicComparison
        {
            Equal, Greater, GreaterOrEqual, Lower, LowerOrEqual
        }



        /// <summary>
        /// Adjusting variable details to parent one
        /// </summary>
        public void UpdateVariableWith(FieldVariable fieldVariable, bool allowChangeType = false)
        {
            if (allowChangeType)
            {
                if (ValueType != fieldVariable.ValueType) ValueType = fieldVariable.ValueType;

                // Cleaning references to not take additional objects by mistake on the build
                if (gameObj != null) if (ValueType != EVarType.GameObject) gameObj = null;
                if (unityObj != null) if (ValueType != EVarType.ProjectObject) unityObj = null;
                if (mat != null) if (ValueType != EVarType.Material) mat = null;

            }

            if (ValueType == fieldVariable.ValueType)
            {
                if (ValueType == EVarType.Number)
                {
                    helper = fieldVariable.helper;
                }

                FloatSwitch = fieldVariable.FloatSwitch;
            }

            helpForFieldCommand = fieldVariable.helpForFieldCommand;
            displayOnScene = fieldVariable.displayOnScene;
            allowTransformFollow = fieldVariable.allowTransformFollow;

        }


        public static bool LogicComparison(Single a, Single b, ELogicComparison comparison)
        {
            if (comparison == ELogicComparison.Equal) return a == b;
            else if (comparison == ELogicComparison.Greater) return a > b;
            else if (comparison == ELogicComparison.GreaterOrEqual) return a >= b;
            else if (comparison == ELogicComparison.Lower) return a < b;
            else if (comparison == ELogicComparison.LowerOrEqual) return a <= b;
            return false;
        }

        public static bool LogicComparison(FieldVariable a, FieldVariable b, ELogicComparison comparison)
        {

            if (a.ValueType == EVarType.Bool)
            {
                if (b.ValueType == EVarType.Bool)
                {
                    return a.GetBoolValue() == b.GetBoolValue();
                }
                else
                {
                    if (b.ValueType == EVarType.GameObject) return FGenerators.NotNull(b.GetGameObjRef());
                    if (b.ValueType == EVarType.Material) return FGenerators.NotNull(b.GetMaterialRef());
                    if (b.ValueType == EVarType.ProjectObject) return FGenerators.NotNull(b.GetUnityObjRef());
                    if (b.returnTempRef) return FGenerators.NotNull(b.temporaryReference);
                }
            }

            if ( a.ValueType == EVarType.String)
            {
                string strA = a.str;
                string strB;
                if (b.ValueType == EVarType.String) strB = b.str; else strB = b.GetValue().ToString();

                bool negate = false;
                if (strA.Length > 0) if (strA[0] == '!') { negate = true; strA = strA.Substring(1, strA.Length - 1); }
                bool reslt = strA == strB;
                return negate ? !reslt : reslt;
            }

            // Comparing to null
            if (a.GetValue() == null || b.GetValue() == null)
            {
                if (comparison == ELogicComparison.Equal || comparison == ELogicComparison.LowerOrEqual || comparison == ELogicComparison.GreaterOrEqual)
                    return a.GetValue() == b.GetValue();
                else
                    return false;
            }

            if (a.ValueType == EVarType.Vector2)
            {
                if (comparison == ELogicComparison.Equal)
                {
                    return a.GetVector2Value() == b.GetVector2Value();
                }
            }
            else if (a.ValueType == EVarType.Vector3)
            {
                if (comparison == ELogicComparison.Equal)
                {
                    return a.GetVector3Value() == b.GetVector3Value();
                }
            }
            else
            {
                if (a.ValueType != EVarType.Number)
                {
                    if (comparison == ELogicComparison.Equal) return a.GetValue() == b.GetValue();
                }

                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                {
                    return LogicComparison(a.Float, b.Float, comparison);
                }
                else
                {
                    return LogicComparison(a.GetIntValue(), b.GetIntValue(), comparison);
                }
            }

            return false;
        }


        public void AlgebraOperation(FieldVariable a, FieldVariable b, EAlgebraOperation operation)
        {
            if (a.ValueType == EVarType.Number)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                {
                    float div0 = b.Float; if (div0 == 0f) div0 = 1f;
                    if (operation == EAlgebraOperation.Add) Float = a.Float + b.Float;
                    else if (operation == EAlgebraOperation.Subtract) Float = a.Float - b.Float;
                    else if (operation == EAlgebraOperation.Multiply) Float = a.Float * b.Float;
                    else if (operation == EAlgebraOperation.Divide) Float = a.Float / div0;
                }
                else
                {
                    SetValue(a.GetIntValue() + b.GetIntValue());

                    int div0 = b.GetIntValue(); if (div0 == 0) div0 = 1;
                    if (operation == EAlgebraOperation.Add) IntV = a.GetIntValue() + b.GetIntValue();
                    else if (operation == EAlgebraOperation.Subtract) IntV = a.GetIntValue() - b.GetIntValue();
                    else if (operation == EAlgebraOperation.Multiply) IntV = a.GetIntValue() * b.GetIntValue();
                    else if (operation == EAlgebraOperation.Divide) IntV = a.GetIntValue() / div0;
                }
            }
            else if (a.ValueType == EVarType.String)
            {
                ValueType = a.ValueType;
                if (b.ValueType == EVarType.String)
                {
                    str = a.GetStringValue() + b.GetStringValue();
                }
                else
                {
                    str = a.GetStringValue() + b.GetValue().ToString();
                }
            }
            else if (a.ValueType == EVarType.Vector2)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                {
                    if (b.ValueType == EVarType.Vector2)
                    {
                        Vector2 div0 = b.GetVector2Value(); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector2Value() + b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector2Value() - b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Multiply) SetValue(a.GetVector2Value() * b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Divide) SetValue(a.GetVector2Value() / div0);
                    }
                    else
                    {
                        float val = b.GetFloatValue();
                        Vector2 div0 = new Vector2(val, val); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector2Value() + b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector2Value() - b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Multiply) SetValue(a.GetVector2Value() * b.GetVector2Value());
                        else if (operation == EAlgebraOperation.Divide) SetValue(a.GetVector2Value() / div0);
                    }
                }
                else // Int
                {
                    if (b.ValueType == EVarType.Vector2)
                    {
                        Vector2Int div0 = b.GetVector2IntValue(); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector2IntValue() + b.GetVector2IntValue());
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector2IntValue() - b.GetVector2IntValue());
                        else if (operation == EAlgebraOperation.Multiply) SetValue(a.GetVector2IntValue() * b.GetVector2IntValue());
                        else if (operation == EAlgebraOperation.Divide) SetValue(new Vector2Int(a.GetVector2IntValue().x / div0.x, a.GetVector2IntValue().y / div0.y));
                    }
                    else
                    {
                        int val = b.GetIntValue();
                        Vector2Int div0 = new Vector2Int(val, val); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector2Value() + div0);
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector2Value() - div0);
                        else if (operation == EAlgebraOperation.Multiply) SetValue(a.GetVector2Value() * div0);
                        else if (operation == EAlgebraOperation.Divide) SetValue(a.GetVector2Value() / div0);
                    }
                }
            }
            else if (a.ValueType == EVarType.Vector3)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                {
                    if (b.ValueType == EVarType.Vector3)
                    {
                        Vector3 div0 = b.GetVector3Value(); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1; if (div0.z == 0) div0.z = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector3Value() + b.GetVector3Value());
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector3Value() - b.GetVector3Value());
                        else if (operation == EAlgebraOperation.Multiply) SetValue(Vector3.Scale(a.GetVector3Value(), b.GetVector3Value()));
                        else if (operation == EAlgebraOperation.Divide) SetValue(new Vector3(a.GetVector3Value().x / div0.x, a.GetVector3Value().y / div0.y, a.GetVector3Value().z / div0.z));
                    }
                    else
                    {
                        float val = b.GetFloatValue();
                        Vector3 div0 = new Vector3(val, val, val); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1; if (div0.z == 0) div0.z = 1;
                        Vector3 mul = new Vector3(val, val, val);

                        if (b.ValueType == EVarType.Vector2)
                        {
                            mul.y = b.GetVector2Value().y;
                            mul.z = 0f;
                        }

                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector3Value() + mul);
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector3Value() - mul);
                        else if (operation == EAlgebraOperation.Multiply) SetValue(Vector3.Scale(a.GetVector3Value(), mul));
                        else if (operation == EAlgebraOperation.Divide) SetValue(new Vector3(a.GetVector3Value().x / div0.x, a.GetVector3Value().y / div0.y, a.GetVector3Value().z / div0.z));
                    }
                }
                else // Int
                {
                    if (b.ValueType == EVarType.Vector3)
                    {
                        Vector3Int div0 = b.GetVector3IntValue(); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1; if (div0.z == 0) div0.z = 1;
                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector3IntValue() + b.GetVector3IntValue());
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector3IntValue() - b.GetVector3IntValue());
                        else if (operation == EAlgebraOperation.Multiply) SetValue(a.GetVector3IntValue() * b.GetVector3IntValue());
                        else if (operation == EAlgebraOperation.Divide) SetValue(new Vector3Int(a.GetVector3IntValue().x / div0.x, a.GetVector3IntValue().y / div0.y, a.GetVector3IntValue().z / div0.z));
                    }
                    else
                    {
                        int val = b.GetIntValue();
                        Vector3Int div0 = new Vector3Int(val, val, val); if (div0.x == 0) div0.x = 1; if (div0.y == 0) div0.y = 1; if (div0.z == 0) div0.z = 1;
                        Vector3Int mul = new Vector3Int(val, val, val);

                        if (b.ValueType == EVarType.Vector2)
                        {
                            mul.y = b.GetVector2IntValue().y;
                            mul.z = 0;
                        }

                        if (operation == EAlgebraOperation.Add) SetValue(a.GetVector3Value() + mul);
                        else if (operation == EAlgebraOperation.Subtract) SetValue(a.GetVector3Value() - mul);
                        else if (operation == EAlgebraOperation.Multiply) SetValue(Vector3.Scale(a.GetVector3Value(), mul));
                        else if (operation == EAlgebraOperation.Divide) SetValue(new Vector3(a.GetVector3Value().x / div0.x, a.GetVector3Value().y / div0.y, a.GetVector3Value().z / div0.z));
                    }
                }
            }
        }


        public void ChooseGreater(FieldVariable a, FieldVariable b)
        {
            if (a.ValueType == EVarType.Number)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    Float = Mathf.Max(a.Float, b.Float);
                else
                    IntV = Mathf.Max(a.GetIntValue(), b.GetIntValue());
            }
            else if (a.ValueType == EVarType.String)
            {
                ValueType = a.ValueType;
                str = a.str.Length > b.str.Length ? a.str : b.str;
            }
            else if (a.ValueType == EVarType.Vector2)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    SetValue( a.GetVector2Value().sqrMagnitude > b.GetVector2Value().sqrMagnitude ? a.GetVector2Value() : b.GetVector2Value() );
                else // Int
                    SetValue( a.GetVector2IntValue().sqrMagnitude > b.GetVector2IntValue().sqrMagnitude ? a.GetVector2IntValue() : b.GetVector2IntValue() );
            }
            else if (a.ValueType == EVarType.Vector3)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    SetValue(a.GetVector3Value().sqrMagnitude > b.GetVector3Value().sqrMagnitude ? a.GetVector3Value() : b.GetVector3Value());
                else // Int
                    SetValue(a.GetVector3IntValue().sqrMagnitude > b.GetVector3IntValue().sqrMagnitude ? a.GetVector3IntValue() : b.GetVector3IntValue());
            }
        }


        public void ChooseSmaller(FieldVariable a, FieldVariable b)
        {
            if (a.ValueType == EVarType.Number)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    Float = Mathf.Min(a.Float, b.Float);
                else
                    IntV = Mathf.Min(a.GetIntValue(), b.GetIntValue());
            }
            else if (a.ValueType == EVarType.String)
            {
                ValueType = a.ValueType;
                str = a.str.Length < b.str.Length ? a.str : b.str;
            }
            else if (a.ValueType == EVarType.Vector2)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    SetValue(a.GetVector2Value().sqrMagnitude < b.GetVector2Value().sqrMagnitude ? a.GetVector2Value() : b.GetVector2Value());
                else // Int
                    SetValue(a.GetVector2IntValue().sqrMagnitude < b.GetVector2IntValue().sqrMagnitude ? a.GetVector2IntValue() : b.GetVector2IntValue());
            }
            else if (a.ValueType == EVarType.Vector3)
            {
                if (a.FloatSwitch == EVarFloatingSwitch.Float)
                    SetValue(a.GetVector3Value().sqrMagnitude < b.GetVector3Value().sqrMagnitude ? a.GetVector3Value() : b.GetVector3Value());
                else // Int
                    SetValue(a.GetVector3IntValue().sqrMagnitude < b.GetVector3IntValue().sqrMagnitude ? a.GetVector3IntValue() : b.GetVector3IntValue());
            }
        }



        public void Clamp(FieldVariable value, FieldVariable min, FieldVariable max)
        {
            if (min.ValueType == EVarType.Number && max.ValueType == EVarType.Number && value.ValueType == EVarType.Number)
            {
                FloatSwitch = min.FloatSwitch;
                if (FloatSwitch == EVarFloatingSwitch.Float)
                {
                    Float = Mathf.Clamp(value.Float, min.Float, max.Float);
                }
                else
                {
                    IntV = Mathf.RoundToInt(Mathf.Clamp(value.Float, min.Float, max.Float));
                }
            }
        }

        public static bool SupportingType(object val)
        {
            if (val is Single) return true;
            if (val is int) return true;
            if (val is float) return true;
            if (val is bool) return true;
            if (val is string) return true;
            if (val is Vector2) return true;
            if (val is Vector3) return true;
            if (val is Vector2Int) return true;
            if (val is Vector3Int) return true;
            if (val is GameObject) return true;
            if (val is Material) return true;
            if (val is Color) return true;
            return false;
        }

    }
}
