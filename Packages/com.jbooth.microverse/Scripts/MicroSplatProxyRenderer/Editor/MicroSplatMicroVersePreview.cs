//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;

namespace JBooth.MicroSplat
{
#if __MICROSPLAT__ && __MICROSPLAT_TESSELLATION__
    [InitializeOnLoad]
    public class MicroSplatMicroVersePreview : FeatureDescriptor
    {
        const string sDefine = "__MICROSPLAT_MICROVERSEPREVIEW__";
        static MicroSplatMicroVersePreview()
        {
            MicroSplatDefines.InitDefine(sDefine);
        }
        [PostProcessSceneAttribute(0)]
        public static void OnPostprocessScene()
        {
            MicroSplatDefines.InitDefine(sDefine);
        }
        public override string ModuleName()
        {
            return "MicroVersePreview";
        }

        public override string GetHelpPath()
        {
            return null;
        }

        public enum DefineFeature
        {
            _OUTPUTMICROVERSEPREVIEW, // tells the compiler to make another shader for digger
            kNumFeatures,
        };

        //TextAsset funcs;
        bool previewEnabled;

        static Dictionary<DefineFeature, string> sFeatureNames = new Dictionary<DefineFeature, string>();
        public static string GetFeatureName(DefineFeature feature)
        {
            string ret;
            if (sFeatureNames.TryGetValue(feature, out ret))
            {
                return ret;
            }
            string fn = System.Enum.GetName(typeof(DefineFeature), feature);
            sFeatureNames[feature] = fn;
            return fn;
        }

        public static bool HasFeature(string[] keywords, DefineFeature feature)
        {
            string f = GetFeatureName(feature);
            for (int i = 0; i < keywords.Length; ++i)
            {
                if (keywords[i] == f)
                    return true;
            }
            return false;
        }

        public static bool HasFeature(string[] keywords, string f)
        {
            for (int i = 0; i < keywords.Length; ++i)
            {
                if (keywords[i] == f)
                    return true;
            }
            return false;
        }

        public override string GetVersion()
        {
            return "3.9";
        }

        static GUIContent CEnableMicroVersePreview = new GUIContent("Export MicroVerse Preview", "Create a shader for MicroVerse to use to preview terrains faster than using unity's renderer?");


        public override void DrawFeatureGUI(MicroSplatKeywords keywords)
        {
            previewEnabled = EditorGUILayout.Toggle(CEnableMicroVersePreview, previewEnabled);

        }


        public override void DrawShaderGUI(MicroSplatShaderGUI shaderGUI, MicroSplatKeywords keywords, Material mat, MaterialEditor materialEditor, MaterialProperty[] props)
        {

        }

        public override MicroSplatShaderGUI.MicroSplatCompiler.AuxShader GetAuxShader()
        {
            return new MicroSplatShaderGUI.MicroSplatCompiler.AuxShader("_OUTPUTMICROVERSEPREVIEW", "_MVPreview");
        }

        public override void ModifyKeywordsForAuxShader(List<string> keywords)
        {
            if (keywords.Contains("_OUTPUTMICROVERSEPREVIEW"))
            {
                keywords.Remove("_OUTPUTMICROVERSEPREVIEW");
                keywords.Remove("_MICROTERRAIN");
                keywords.Add("_MICROMESHTERRAIN");
                keywords.Add("_MICROVERSEPREVIEW");
                if (!keywords.Contains("_TESSDISTANCE"))
                    keywords.Add("_TESSDISTANCE");
                keywords.Add("_TESSEDGE");
            }
        }

        public override void InitCompiler(string[] paths)
        {
            /*
         for (int i = 0; i < paths.Length; ++i)
         {
            string p = paths [i];
            if (p.EndsWith ("microsplat_microsplatpreview_func.txt"))
            {
               funcs = AssetDatabase.LoadAssetAtPath<TextAsset> (p);
            }
         }
            */
        }

        public override void WriteProperties(string[] features, System.Text.StringBuilder sb)
        {
            if (previewEnabled)
            {
                sb.AppendLine("  _TerrainHeight(\"Terrain Height\", Float) = 0");
            }
        }

        public override void ComputeSampleCounts(string[] features, ref int arraySampleCount, ref int textureSampleCount, ref int maxSamples, ref int tessellationSamples, ref int depTexReadLevel)
        {

        }

        public override string[] Pack()
        {
            List<string> features = new List<string>();
            if (previewEnabled)
            {
                features.Add(GetFeatureName(DefineFeature._OUTPUTMICROVERSEPREVIEW));
            }
            return features.ToArray();
        }

        public override void WritePerMaterialCBuffer(string[] features, System.Text.StringBuilder sb)
        {
            if (previewEnabled)
            {
                sb.AppendLine("      float _TerrainHeight;");
            }
        }

        public override void WriteFunctions(string[] features, System.Text.StringBuilder sb)
        {

        }

        public override void Unpack(string[] keywords)
        {
            previewEnabled = HasFeature(keywords, "_OUTPUTMICROVERSEPREVIEW") || HasFeature(keywords, "_MICROVERSEPREVIEW");
        }
    }
#endif

}
#endif
