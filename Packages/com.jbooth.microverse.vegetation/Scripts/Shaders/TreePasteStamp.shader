Shader "Hidden/MicroVerse/TreePasteStamp"
{
    Properties
    {

    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _TreePos;
            sampler2D _TreeRand;
            sampler2D _Heightmap;
            float4x4 _StampTransform;
            float4x4 _TerrainTransform;
            float3 _RealSize;
            float _Indexes[32];
            sampler2D _ClearMask;
            sampler2D _PlacementMask;
            float _ClearLayer;

            sampler2D _RandomTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };



            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            struct FragmentOutput
            {
                half4 posWeight : SV_Target0;
                half4 randoms : SV_Target1;
            };


            FragmentOutput frag(v2f i)
            {
                FragmentOutput o = (FragmentOutput)0;

                half4 posData = tex2D(_TreePos, i.uv);
                half4 randData = tex2D(_TreeRand, i.uv);
                posData.xz -= 0.5;
                // to worldspace
                posData.xz = mul(_StampTransform, float4(posData.xyz, 1)).xz;
                // to terrain space
                posData.xz = mul(_TerrainTransform, float4(posData.xyz, 1)).xz;
                // recenter
                posData.xz += 0.5;
                // to 0-1 space
                posData.xz /= _RealSize.xz;
                // clip terrain bounds
                bool cp = (posData.x <= 0 || posData.x >= 1 || posData.z <= 0 || posData.z >= 1);
                if (cp)
                {
                    posData.w = 0;
                    o.posWeight = posData;
                    o.randoms = randData;
                    return o;
                }
                posData.w *= 1.0 - tex2D(_PlacementMask, posData.xz).b;
                float2 clearMask = tex2D(_ClearMask, posData.xz).xy;
                if (round(clearMask.r * 256) > _ClearLayer+0.5)
                    posData.w *= 1.0 - clearMask.g;
    
                // remap height data
                half height = UnpackHeightmap(tex2D(_Heightmap, posData.xz));
                posData.y = height;
                o.posWeight = posData;
                o.randoms = randData;
                return o;
            }
            ENDCG
        }
    }
}
