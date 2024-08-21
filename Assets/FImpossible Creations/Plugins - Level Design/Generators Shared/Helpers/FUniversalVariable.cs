using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace FIMSpace.FGenerating
{
    [System.Serializable]
    public class FUniversalVariable
    {
        public string VariableName = "Variable";
        [SerializeField] protected string Tooltip = "";

        #region Tooltip refresh clean code helper

        bool _tooltipWasSet = false;
        public bool TooltipAssigned { get { return _tooltipWasSet; } }
        public void AssignTooltip( string tooltip )
        {
            if( _tooltipWasSet ) return;
            Tooltip = tooltip;
            _tooltipWasSet = true;
        }

        #endregion

        /// <summary> For Number type, .w == 1 -> Int   0 -> Float </summary>
        [SerializeField] protected Vector4 _value = Vector4.zero;
        [SerializeField] protected string _string = "";
        [SerializeField] protected AnimationCurve _curve = null;
        [SerializeField] protected UnityEngine.Object _uObject = null;
        [SerializeField] protected object _object = null;

        public FUniversalVariable( string name, object value )
        {
            VariableName = name;
            SetValue( value );
        }

        [NonSerialized] private int nameHash = 0;
        public int GetNameHash
        {
            get
            {
                if( nameHash == 0 ) nameHash = VariableName.GetHashCode();
                return nameHash;
            }
        }

        public enum EVariableType
        {
            Number, Bool, Vector2, Vector3, Color, String, Curve, UnityObject, CustomObject
        }

        public EVariableType VariableType = EVariableType.Number;
        protected virtual int GetVariableType() { return (int)VariableType; }
        protected virtual void SetVariableType( int id ) { VariableType = (EVariableType)id; }
        [HideInInspector]
        public bool IsFloat
        {
            get { return _value.w != 1; }
            set { _value.w = value ? 0 : 1; }
        }

        public virtual void SetValue( object o )
        {
            if( o is int ) { _value = new Vector4( (int)o, 0, 0, 1 ); VariableType = EVariableType.Number; IsFloat = false; }
            else if( o is float ) { _value = new Vector4( (float)o, 0, 0, 0 ); VariableType = EVariableType.Number; IsFloat = true; }
            else if( o is bool ) { bool v = (bool)o; if( v ) _value.x = 1; else _value.x = 0; VariableType = EVariableType.Bool; }
            else if( o is Vector2Int ) { Vector2Int v = (Vector2Int)o; _value = new Vector4( v.x, v.y ); VariableType = EVariableType.Vector2; IsFloat = false; }
            else if( o is Vector3Int ) { Vector3Int v = (Vector3Int)o; _value = new Vector4( v.x, v.y, v.z ); VariableType = EVariableType.Vector3; IsFloat = false; }
            else if( o is Vector2 ) { Vector2 v = (Vector2)o; _value = v; VariableType = EVariableType.Vector2; IsFloat = true; }
            else if( o is Vector3 ) { Vector3 v = (Vector3)o; _value = v; VariableType = EVariableType.Vector3; IsFloat = true; }
            else if( o is string ) { _string = o as string; VariableType = EVariableType.String; }
            else if( o is Color ) { Color c = (Color)o; _value = new Vector4( c.r, c.g, c.b, c.a ); VariableType = EVariableType.Color; }
            else if( o is AnimationCurve ) { _curve = o as AnimationCurve; VariableType = EVariableType.Curve; }
            else if( o is UnityEngine.Object ) { _uObject = o as UnityEngine.Object; VariableType = EVariableType.UnityObject; }
            else if( o != null ) { _object = o; VariableType = EVariableType.CustomObject; }
            else { _object = o; _uObject = null; VariableType = EVariableType.CustomObject; }
        }

        public int GetInt() { return (int)_value.x; }
        public float GetFloat() { return _value.x; }
        public bool GetBool() { return _value.x == 1; }
        public Color GetColor() { return new Color( _value.x, _value.y, _value.z, _value.w ); }
        public Vector2 GetVector2() { return new Vector2( _value.x, _value.y ); }
        public Vector2Int GetVector2Int() { return new Vector2Int( Mathf.RoundToInt( _value.x ), Mathf.RoundToInt( _value.y ) ); }
        public Vector3 GetVector3() { return new Vector3( _value.x, _value.y, _value.z ); }
        public Vector3Int GetVector3Int() { return new Vector3Int( Mathf.RoundToInt( _value.x ), Mathf.RoundToInt( _value.y ), Mathf.RoundToInt( _value.z ) ); }
        public string GetString() { return _string; }
        public AnimationCurve GetCurve() { return _curve; }
        public UnityEngine.Object GetUnityObject() { return _uObject; }
        public object GetObject() { return _object; }

        public GameObject GetGameObject() { return _uObject as GameObject; }
        public Material GetMaterial() { return _uObject as Material; }

        public void SetMinMaxSlider( float min, float max )
        { _rangeHelper = new Vector4( min, max, 0, 0 ); }

        public void SetCurveFixedRange( float startTime, float startValue, float endTime, float endValue )
        { _rangeHelper = new Vector4( startTime, startValue, endTime, endValue ); }

        /// <summary>
        /// Returns value according to lastest assigned value type
        /// </summary>
        public virtual object GetValue()
        {
            switch( VariableType )
            {
                case EVariableType.Number: return GetFloat();
                case EVariableType.Bool: return GetBool();
                case EVariableType.Vector2: return GetVector2();
                case EVariableType.Vector3: return GetVector3();
                case EVariableType.Color: return GetColor();
                case EVariableType.String: return GetString();
                case EVariableType.Curve: return GetCurve();
                case EVariableType.UnityObject: return GetUnityObject();
                case EVariableType.CustomObject: return _object;
            }

            return null;
        }

        [SerializeField] private Vector4 _rangeHelper = Vector4.zero;
        public Vector4 RangesHelperValue { get { return _rangeHelper; } }

        [NonSerialized] public Texture Icon = null;

        public virtual FUniversalVariable Copy()
        {
            return (FUniversalVariable)MemberwiseClone();
        }

        [NonSerialized] public bool _GUI_HideVariable = false;
        [NonSerialized] public string _GUI_DisplayNameReplace = "";
        [NonSerialized] public Color _GUI_CurveColor = Color.cyan;
        [NonSerialized] GUILayoutOption[] _GUI_Layout = null;

        /// <summary> Returns true if gui changed </summary>
        public virtual bool Editor_DisplayVariableGUI( GUILayoutOption[] guiLayoutOptions = null )
        {
#if UNITY_EDITOR
            if( _GUI_HideVariable ) return false;

            GUILayoutOption[] options = _GUI_Layout;

            if( guiLayoutOptions == null )
            {
                if( _GUI_Layout == null || _GUI_Layout.Length == 0 ) _GUI_Layout = new GUILayoutOption[1];
                _GUI_Layout[0] = GUILayout.Height( 18 );
            }
            else options = guiLayoutOptions;

            EditorGUI.BeginChangeCheck();

            GUIContent nameG = new GUIContent( string.IsNullOrWhiteSpace( _GUI_DisplayNameReplace ) ? VariableName : _GUI_DisplayNameReplace, Tooltip );

            nameG.image = Icon;

            if( _GUI_CurveColor == Color.clear ) _GUI_CurveColor = Color.cyan;

            if( GetVariableType() == (int)EVariableType.Number )
            {
                if( !IsFloat ) // Int
                {
                    if( _rangeHelper.x != _rangeHelper.y && _rangeHelper.y != 0 )
                        _value.x = EditorGUILayout.IntSlider( nameG, (int)_value.x, (int)_rangeHelper.x, (int)_rangeHelper.y, options );
                    else
                        _value.x = EditorGUILayout.IntField( nameG, (int)_value.x, options );
                }
                else // Float
                {
                    if( _rangeHelper.x != _rangeHelper.y && _rangeHelper.y != 0 )
                        _value.x = EditorGUILayout.Slider( nameG, _value.x, _rangeHelper.x, _rangeHelper.y, options );
                    else
                        _value.x = EditorGUILayout.FloatField( nameG, _value.x, options );
                }
            }
            else if( GetVariableType() == (int)EVariableType.Bool )
            {
                bool v = _value.x == 1;
                v = EditorGUILayout.Toggle( nameG, v, options );
                SetValue( v );
            }
            else if( GetVariableType() == (int)EVariableType.Vector2 )
            {
                if( IsFloat ) _value = EditorGUILayout.Vector2Field( nameG, _value, options ); else SetValue( EditorGUILayout.Vector2IntField( nameG, GetVector2Int(), options ) );
            }
            else if( GetVariableType() == (int)EVariableType.Vector3 )
            {
                if( IsFloat ) _value = EditorGUILayout.Vector3Field( nameG, _value, options ); else SetValue( EditorGUILayout.Vector3IntField( nameG, GetVector3Int(), options ) );
            }
            else if( GetVariableType() == (int)EVariableType.String )
            {
                _string = EditorGUILayout.TextField( nameG, _string, options );
            }
            else if( GetVariableType() == (int)EVariableType.Curve )
            {
                if( _rangeHelper == Vector4.zero )
                { _curve = EditorGUILayout.CurveField( nameG, _curve, options ); }
                else
                {
                    var fixedRect = new Rect( _rangeHelper.x, _rangeHelper.y, _rangeHelper.z - _rangeHelper.x, _rangeHelper.w - _rangeHelper.y );
                    _curve = EditorGUILayout.CurveField( nameG, GetCurve(), _GUI_CurveColor, fixedRect, options );
                }
            }
            else if( GetVariableType() == (int)EVariableType.Color )
            {
                SetValue( EditorGUILayout.ColorField( "Color:", GetColor(), options ) );
            }
            else if( GetVariableType() == (int)EVariableType.UnityObject )
            {
                _uObject = EditorGUILayout.ObjectField( nameG, _uObject, typeof( UnityEngine.Object ), true, options );
            }
            else if( GetVariableType() == (int)EVariableType.CustomObject )
            {
                if( _object == null )
                    EditorGUILayout.LabelField( "Containing Null" );
                else
                    EditorGUILayout.LabelField( "Containing custom, not serialized object" );
            }

            return EditorGUI.EndChangeCheck();
#else
            return false;
#endif
        }

    }
}