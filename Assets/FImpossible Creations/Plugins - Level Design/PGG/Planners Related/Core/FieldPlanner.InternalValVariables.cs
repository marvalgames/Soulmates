using System.Collections.Generic;

namespace FIMSpace.Generating.Planning
{
    public partial class FieldPlanner
    {
        private List<FieldVariable> internalValueVariables = null;

        public void PrepareInternalValueVariables()
        {
            if (internalValueVariables != null) internalValueVariables.Clear();
        }

        public void SetInternalValueVariable(string name, object targetValue)
        {
            if (internalValueVariables == null)
            {
                GenerateInternalValueVariable(name, targetValue);
                return;
            }

            for (int i = 0; i < internalValueVariables.Count; i++)
            {
                var intVars = internalValueVariables[i];

                if (intVars == null) continue;

                if (intVars.Name == name)
                {
                    intVars.SetValue(targetValue);
                    return;
                }
            }

            GenerateInternalValueVariable(name, targetValue);
        }

        public bool ContainsInternalValueOfName(string name)
        {
            if (internalValueVariables == null) return false;

            for (int i = 0; i < internalValueVariables.Count; i++)
            {
                var intVars = internalValueVariables[i];
                if (intVars == null) continue;
                if (intVars.Name == name) return true;
            }

            return false;
        }

        public FieldVariable GetInternalValueVariable(string name, object initialValue, bool generateIfull = true)
        {
            if (internalValueVariables == null)
            {
                if(!generateIfull) if (initialValue == null) return null;
                return GenerateInternalValueVariable(name, initialValue);
            }

            for (int i = 0; i < internalValueVariables.Count; i++)
            {
                var intVars = internalValueVariables[i];
                if (intVars == null) continue;
                if (intVars.Name == name) return intVars;
            }

            if (!generateIfull) if (initialValue == null) return null;
            return GenerateInternalValueVariable(name, initialValue);
        }

        private FieldVariable GenerateInternalValueVariable(string name, object initialValue)
        {
            FieldVariable nVar = new FieldVariable(name, initialValue);
            nVar = AddInternalValueVariable(nVar);
            return nVar;
        }

        public FieldVariable AddInternalValueVariable(FieldVariable variable)
        {
            if (variable == null) return null;
            if (internalValueVariables == null) internalValueVariables = new List<FieldVariable>();

            for (int i = 0; i < internalValueVariables.Count; i++)
            {
                var cvar = internalValueVariables[i];
                if (variable == cvar) return cvar;
                if (variable.Name == cvar.Name) return cvar;
            }

            internalValueVariables.Add(variable);
            return variable;
        }

    }
}