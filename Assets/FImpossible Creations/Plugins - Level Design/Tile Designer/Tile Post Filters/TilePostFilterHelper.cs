using FIMSpace.FGenerating;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class TileDesign
    {
        [System.Serializable]
        public class PostFilterHelper
        {
            public bool Enabled = true;
            public bool Foldout = true;
            public TilePostFilterBase PostFilter = null;

            [SerializeField] private List<FUniversalVariable> variables = new List<FUniversalVariable>();

            public FUniversalVariable RequestVariable(string name, object defaultValue)
            {
                int hash = name.GetHashCode();
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i] == null) continue;
                    if (variables[i].GetNameHash == hash) return variables[i];
                }

                FUniversalVariable nVar = new FUniversalVariable(name, defaultValue);
                variables.Add(nVar);
                return nVar;
            }

            internal PostFilterHelper Copy()
            {
                return (PostFilterHelper)MemberwiseClone();
            }
        }

        public List<PostFilterHelper> PostFilters = new List<PostFilterHelper>();

    }
}