using FIMSpace.Generating;
using FIMSpace.Generating.Checker;
using FIMSpace.Generating.Planning;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Graph
{
    [System.Serializable]
    public class PGGUniversalPort : NodePortBase
    {
        public FieldVariable Variable = new FieldVariable();

        public override System.Type GetPortValueType { get { return typeof(System.Single); } }
        public override object DefaultValue { get { return Variable.GetValue(); } }
        protected override bool setAsUniversal { get { return true; } }

#if UNITY_EDITOR
        public override bool AllowConnectionWithValueType(IFGraphPort other)
        {
            if (FieldVariable.SupportingType(other.GetPortValue))
            {
                return true;
            }
            else
            {
                return base.AllowConnectionWithValueType(other);
            }
        }
#endif

        public override void TriggerReadPort(bool callRead = false)
        {
            Variable.SetTemporaryReference(false);

            // Support for cell and planenr variables
            var conn = FirstNoSender();

            if (FGenerators.CheckIfExist_NOTNULL(conn))
            {
                if (FGenerators.CheckIfExist_NOTNULL(conn.PortReference))
                {
                    if (conn.PortReference is PGGCellPort || conn.PortReference is PGGPlannerPort)
                    {
                        Variable.SetTemporaryReference(true, conn.PortReference);
                    }
                }
            }

            base.TriggerReadPort(callRead);
        }

        public IFGraphPort GetConnectedPortOfType(Type type)
        {
            if (Connections == null) return null;

            for (int c = 0; c < Connections.Count; c++)
            {
                var conn = Connections[c];
                if (conn == null) continue;
                if (conn.PortReference == null) continue;
                if (conn.PortReference.GetType() == type) return conn.PortReference;
            }

            return null;
        }

        public override Color GetColor()
        {
            switch (Variable.ValueType)
            {
                case FieldVariable.EVarType.Number:
                    if (Variable.FloatSwitch == FieldVariable.EVarFloatingSwitch.Float)
                        return new Color(0.4f, 0.4f, .9f, 1f);
                    else
                        return new Color(0.2f, 0.6f, .9f, 1f);

                case FieldVariable.EVarType.Bool:
                    return new Color(0.9f, 0.4f, .4f, 1f);

                case FieldVariable.EVarType.Vector3:
                case FieldVariable.EVarType.Vector2:
                    return new Color(0.2f, 0.8f, .5f, 1f);
            }

            return new Color(0.4f, 0.4f, .5f, 1f);
        }

        public static Bounds TryReadAsBounds(object val)
        {
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            if (val == null) return b;

            if (val is Bounds)
            {
                b = (Bounds)val;
            }
            else if (val is ICheckerReference)
            {
                b = (val as ICheckerReference).CheckerReference.GetFullBoundsWorldSpace();
            }
            else if (val is List<ICheckerReference>)
            {
                List<ICheckerReference> list = (val as List<ICheckerReference>);
                for (int l = 0; l < list.Count; l++)
                {
                    if (list[l] == null) continue;
                    if (b.size == Vector3.zero) b = list[l].CheckerReference.GetFullBoundsWorldSpace();
                    else b.Encapsulate(list[l].CheckerReference.GetFullBoundsWorldSpace());
                }
            }
            else if (val is List<CheckerField3D>)
            {
                List<CheckerField3D> list = (val as List<CheckerField3D>);
                for (int l = 0; l < list.Count; l++)
                {
                    if (list[l] == null) continue;
                    if (b.size == Vector3.zero) b = list[l].GetFullBoundsWorldSpace();
                    else b.Encapsulate(list[l].GetFullBoundsWorldSpace());
                }
            }
            else if (val is List<FieldPlanner>)
            {
                List<FieldPlanner> list = (val as List<FieldPlanner>);
                for (int l = 0; l < list.Count; l++)
                {
                    if (list[l] == null) continue;
                    if (b.size == Vector3.zero) b = list[l].CheckerReference.GetFullBoundsWorldSpace();
                    else b.Encapsulate(list[l].CheckerReference.GetFullBoundsWorldSpace());
                }
            }

            return b;
        }

    }
}