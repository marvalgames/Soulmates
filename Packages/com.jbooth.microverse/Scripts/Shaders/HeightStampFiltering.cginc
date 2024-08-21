#ifndef __HEIGHTSTAMPFILTERING__
#define __HEIGHTSTAMPFILTERING__

#if _COMPUTESHADER
    #define SAMPLE(tex, samp, uv) tex.SampleLevel(samp, uv, 0)
#else
    #if _REQUIRELODSAMPLER
        #define SAMPLE(tex, samp, uv) tex.SampleLevel(samp, uv, 0)
    #else
        #define SAMPLE(tex, samp, uv) tex.Sample(samp, uv)
    #endif
#endif

sampler2D _FalloffTexture;
float2 _Falloff;
int _FalloffTextureChannel;
float2 _FalloffTextureParams;
float4 _FalloffTextureRotScale;
float _FalloffAreaRange;
float _FalloffAreaBoost;

sampler2D _FalloffNoiseTexture;
float4 _FalloffNoiseTexture_ST;
float4 _FalloffNoise;
float4 _FalloffNoise2;
int _FalloffNoiseChannel;
int _CombineMode;

float _CombineBlend;

float4x4 _PaintAreaMatrix;
Texture2D _PaintAreaFalloffTexture;
float _PaintAreaClamp;

float3 _TerrainSize;

SamplerState shared_linear_clamp;


float2 RotateScaleUV(float2 uv, float2 amt)
{
    uv -= 0.5;
    uv *= amt.y;
    if (amt.x != 0)
    {
        float s = sin ( amt.x );
        float c = cos ( amt.x );
        float2x2 mtx = float2x2( c, -s, s, c);
        mtx *= 0.5;
        mtx += 0.5;
        mtx = mtx * 2-1;
        uv = mul ( uv, mtx );
    }
    uv += 0.5;
    return uv;
}

float RectFalloff(float2 uv, float falloff) 
{
    if (falloff == 1)
        return 1;
    uv = saturate(uv);
    uv -= 0.5;
    uv = abs(uv);
    uv = 0.5 - uv;
    falloff = 1 - falloff;
    uv = smoothstep(uv, 0, 0.03 * falloff);
    return min(uv.x, uv.y);
}

float CombineHeight(float oldHeight, float height, int combineMode)
{
    switch (combineMode)
    {
    case 0:
        return height;
    case 1:  
        return max(oldHeight, height);
    case 2:
        return min(oldHeight, height);
    case 3:
        return oldHeight + height;
    case 4:
        return oldHeight - height;
    case 5:
        return (oldHeight * height);
    case 6:
        return (oldHeight + height) / 2;
    case 7:
        return abs(height-oldHeight);
    case 8:
        return sqrt(oldHeight * height);
    case 9:
        return lerp(oldHeight, height, _CombineBlend);
    default:
        return oldHeight;
    }
}

float ComputeFalloff(float2 uv, float2 stampUV, float2 noiseUV, float noise)
{
    float falloff = 1;
    #if _USEFALLOFF
        falloff = RectFalloff(stampUV, saturate(_Falloff.y - noise));
    #elif _USEFALLOFFRANGE
    {
        float2 off = saturate(_Falloff * 0.5 - saturate(noise) * 0.5);
        float radius = length( stampUV-0.5 );
 	    falloff = 1.0 - saturate(( radius-off.x ) / max(0.001, ( off.y-off.x )));
    }
    #elif _USEFALLOFFTEXTURE
    {
        float falloffSample = tex2D(_FalloffTexture, RotateScaleUV(stampUV, _FalloffTextureRotScale.xy) + _FalloffTextureRotScale.zw)[_FalloffTextureChannel];
        falloff *= falloffSample;
        falloff *= _FalloffTextureParams.x;
        falloff += _FalloffTextureParams.y * falloffSample;
        falloff *= RectFalloff(stampUV, saturate(_Falloff.y - noise));
    }
    #elif _USEFALLOFFSPLINEAREA
    {
        float d = tex2D(_FalloffTexture, uv).r - _FalloffAreaBoost;
        d *= -1;
        d /= max(0.0001, _FalloffAreaRange - noise);
        falloff *= saturate(d);
    }
    #endif

    // not else, goes on top..
    #if _USEFALLOFFPAINTAREA
    {
        float2 worldPosition = noiseUV * _TerrainSize.xz * (1000 / _TerrainSize.xz);
        float3 localPos = mul(_PaintAreaMatrix, float4(worldPosition.x, 0, worldPosition.y, 1)).xyz;
        float2 luv = float2(localPos.x + 0.5, localPos.z + 0.5);
        float falloffSample = SAMPLE(_PaintAreaFalloffTexture, shared_linear_clamp, luv).r;
        falloff *= falloffSample;
        if (_PaintAreaClamp > 0.5)
            falloff *= RectFalloff(luv, 1);
    }
    #endif

    #if _FALLOFFSMOOTHSTEP
        falloff = smoothstep(0,1,falloff);
    #elif _FALLOFFEASEIN
        falloff *= falloff;
    #elif _FALLOFFEASEOUT
        falloff = 1 - (1 - falloff) * (1 - falloff);
    #elif _FALLOFFEASEINOUT
        falloff = falloff < 0.5 ? 2 * falloff * falloff : 1 - pow(-2 * falloff + 2, 2) / 2;
    #endif
    return falloff;
}

#endif

