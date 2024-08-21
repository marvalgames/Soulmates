using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class FieldSetup : ScriptableObject
    {
        [Serializable]
        public class CustomPostEventHelper
        {
            public bool Enabled = true;
            public bool Foldout = true;
            public FieldSpawnerPostEvent_Base PostEvent = null;

            [SerializeField] private List<FieldVariable> variables = new List<FieldVariable>();

            /// <summary> Can be used for containing any parasable value or just strings </summary>
            [SerializeField, HideInInspector] public List<string> customStringList = null;
            /// <summary> Support for list of unity objects </summary>
            [SerializeField, HideInInspector] public List<UnityEngine.Object> customObjectList = null;

            public FieldVariable RequestVariable(string name, object defaultValue)
            {
                int hash = name.GetHashCode();
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i].GetNameHash == hash) return variables[i];
                }

                FieldVariable nVar = new FieldVariable(name, defaultValue);
                variables.Add(nVar);
                return nVar;
            }
        }

        public List<CustomPostEventHelper> CustomPostEvents = new List<CustomPostEventHelper>();
    }

}