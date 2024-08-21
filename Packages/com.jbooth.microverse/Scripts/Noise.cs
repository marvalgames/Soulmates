using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [System.Serializable]
    public class Noise : ICloneable
    {
        public enum NoiseType
        {
            None,
            Simple,
            FBM,
            Worley,
            Worm,
            WormFBM,
            Texture
        }

        public enum NoiseSpace
        {
            World,
            Stamp
        }

        public NoiseType noiseType = NoiseType.None;
        public NoiseSpace noiseSpace = NoiseSpace.World;
        public float frequency = 10;
        public float amplitude = 1;
        public float offset = 0;
        [Range(-0.5f, 0.5f)] public float balance = 0;
        public Texture2D texture;
        public Vector4 textureST = new Vector4(0, 0, 1, 1);
        public FalloffFilter.TextureChannel channel = FalloffFilter.TextureChannel.R;
        public float displayGamma = 0;

        public Vector4 GetParamVector()
        {
            return new Vector4(frequency, amplitude, offset, balance);
        }

        public Vector4 GetParam2Vector()
        {
            return new Vector4((int)noiseSpace, 0, 0, 0);
        }

        public Vector4 GetTextureParams()
        {
            return textureST;
        }

        public Vector2 GetTextureScale()
        {
            return new Vector2(textureST.x, textureST.y);
        }

        public Vector2 GetTextureOffset()
        {
            return new Vector2(textureST.z, textureST.w);
        }

        static string KeywordLookup(string key, NoiseType nt)
        {
            string str;

            switch (nt)
            {
                case NoiseType.Simple:
                    str = key + "NOISE";
                    break;
                case NoiseType.FBM:
                    str = key + "FBM";
                    break;
                case NoiseType.Worley:
                    str = key + "WORLEY";
                    break;
                case NoiseType.Worm:
                    str = key + "WORM";
                    break;
                case NoiseType.WormFBM:
                    str = key + "WORMFBM";
                    break;
                case NoiseType.Texture:
                    str = key + "NOISETEXTURE";
                    break;
                default:
                    str = "";
                    break;
            }
            return str;

        }

        public void PrepareMaterial(Material mat, string key, string prop, List<string> keywords)
        {
            EnableKeyword(mat, key, keywords);

            mat.SetVector(prop + "Noise", GetParamVector());
            mat.SetVector(prop + "Noise2", GetParam2Vector());
            var nm = prop + "NoiseTexture";
            mat.SetTexture(nm, texture);
            mat.SetTextureOffset(nm, GetTextureOffset());
            mat.SetTextureScale(nm, GetTextureScale());
            mat.SetFloat(prop + "NoiseChannel", (int)channel);
  

        }

        public void EnableKeyword(Material material, string prefix, List<string> keywords)
        {
            if (noiseType != NoiseType.None)
            {
                string key = KeywordLookup(prefix, noiseType);
                keywords.Add(key);
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
