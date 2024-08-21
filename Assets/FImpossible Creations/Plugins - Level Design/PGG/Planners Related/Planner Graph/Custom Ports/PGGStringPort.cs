using FIMSpace.Generating;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FIMSpace.Graph
{
    [System.Serializable]
    public class PGGStringPort : NodePortBase
    {
        public string StringVal = "";
        public override System.Type GetPortValueType { get { return typeof(string); } }
        public override object DefaultValue { get { return StringVal; } }

        /// <summary>
        /// If null then returning default value
        /// </summary>
        public string GetInputValue
        {
            get
            {
                object val = GetPortValueSafe;
                if (FGenerators.CheckIfIsNull(val)) return StringVal;
                if (val is string ) return (string)val;
                string str = val.ToString();
                if (string.IsNullOrEmpty(str) == false) return str;
                return StringVal;
            }
        }

        public override Color GetColor()
        {
            return new Color(0.5f, 0.325f, .675f, 1f);
        }

        public override bool AllowConnectionWithValueType(IFGraphPort other)
        {
            return true;
            //if (other.GetPortValueType == typeof(int)) return true;
            //if (other.GetPortValueType == typeof(bool)) return true;
            //if (other.GetPortValueType == typeof(float)) return true;
            //if (other.GetPortValueType == typeof(Vector3)) return true;
            //if (other.GetPortValueType == typeof(Vector2)) return true;
            //return base.AllowConnectionWithValueType(other);
        }

        public override void InitialValueRefresh(object initialValue)
        {
            if (initValueSet) return;
            base.InitialValueRefresh(initialValue);
            if (initialValue is string) StringVal = (string)initialValue;
        }

        public static void SplitTags(string str, List<string> alloc)
        {
            alloc.Clear();
            if (string.IsNullOrWhiteSpace(str)) return;

            if (str.Contains(","))
            {
                var split = str.Split(',').ToList();
                for (int s = 0; s < split.Count; s++) alloc.Add(split[s]);
            }
            else
                alloc.Add(str);
        }

        public static List<string> SplitTags(string str)
        {
            List<string> list = new List<string>();
            SplitTags(str, list);
            return list;
        }
    }
}