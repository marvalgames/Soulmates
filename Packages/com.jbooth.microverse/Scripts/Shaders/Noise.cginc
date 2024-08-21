#ifndef __NOISE__
#define __NOISE__

    #define PI 3.14159265358979

    #define DECLARENOISEKEYWORDS(_PRE) #pragma shader_feature_local _ _PRE##NOISE _PRE##FBM _PRE##WORLEY _PRE##WORM _PRE##WORMFBM _PRE##NOISETEXTURE
    #define DECLARENOISEVARS(_PRE) Texture2D _PRE##NoiseTexture; float4 _PRE##NoiseTexture_ST; float4 _PRE##Noise; int _PRE##NoiseChannel;

    float Hash12(float2 p)
    {
	    float3 p3  = frac(float3(p.xyx) * .1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }

    float2 Hash2D( float2 x )
    {
        float2 k = float2( 0.3183099, 0.3678794 );
        x = x*k + k.yx;
        return -1.0 + 2.0*frac( 16.0 * k*frac( x.x*x.y*(x.x+x.y)) );
    }

    float3 Erode(float2 p, float2 dir)
    {    
        float2 ip = floor(p);
        float2 fp = frac(p);
        float f = 2.*PI;
        float3 va = 0;
   	    float wt = 0.0;
        for (int i=-2; i<=1; i++)
        {
		    for (int j=-2; j<=1; j++)
            {		
        	    float2 o = float2(i, j);
        	    float2 h = Hash2D(ip - o)*0.5;
                float2 pp = fp +o - h;
                float d = dot(pp, pp);
                float w = exp(-d*2.0);
                wt +=w;
                float mag = dot(pp,dir);
                va += float3(cos(mag*f), -sin(mag*f)*(pp+dir))*w;
            }
        }
        return va/wt;
    }

    float ErosionNoise(float2 uv, float3 n)
    {
        float2 dir = n.zx * float2(1.0, -1.0);
    
        float3 h = 0;
        float a = 0.7;
        float f = 1.0;
        for (int xx=0;xx<5;xx++)
        {
            float3 eros = Erode(uv*f, dir+h.zy*float2(1.0, -1.0));
            h += float3(1.0, f, f) * eros * a;
            a*=0.4;
            f*=2.0;
        }

        return abs(h.x);
    }

    float Noise2D(float2 p )
    {
        float2 i = floor( p );
        float2 f = frac( p );
         
        float2 u = f*f*(3.0-2.0*f);

        return lerp( lerp( dot( Hash2D( i + float2(0.0,0.0) ), f - float2(0.0,0.0) ), 
                        dot( Hash2D( i + float2(1.0,0.0) ), f - float2(1.0,0.0) ), u.x),
                    lerp( dot( Hash2D( i + float2(0.0,1.0) ), f - float2(0.0,1.0) ), 
                        dot( Hash2D( i + float2(1.0,1.0) ), f - float2(1.0,1.0) ), u.x), u.y);
    }
      

    float2 WorleyHash2D(float2 p)
    {
        return frac(cos(mul(p, float2x2(-64.2,71.3,81.4,-29.8)))*8321.3); 
    }

    float WorleyNoise2D(float2 p)
    {
        float dist = 1;
        float2 i = floor(p);
        float2 f = frac(p);
    
        for(int x = -1;x<=1;x++)
        {
            for(int y = -1;y<=1;y++)
            {
                float d = length(WorleyHash2D(i+float2(x,y))+float2(x,y) - f);
                dist = min(dist,d);
            }
        }
        return sqrt(dist);
    
    }

    float FBM2D(float2 uv)
    {
        float f = 0.5000*Noise2D( uv ); uv *= 2.01;
        f += 0.3300*Noise2D( uv ); uv *= 1.96;
        f += 0.170*Noise2D( uv );
        return f;
    }


    float2 Hash22( float2 n ) { return sin(n.x*n.y+float2(0,1.571)); }

    float WormNoise(float2 p)
    {
        const float kF = 6.0; 
    
        float2 i = floor(p) + 77;
	    float2 f = frac(p);
        f = f*f*(3.0-2.0*f);
        return lerp(lerp(sin(kF*dot(p,Hash22(i+float2(0,0)))),
               	       sin(kF*dot(p,Hash22(i+float2(1,0)))),f.x),
                   lerp(sin(kF*dot(p,Hash22(i+float2(0,1)))),
               	       sin(kF*dot(p,Hash22(i+float2(1,1)))),f.x),f.y);
    }

    float WormNoiseFBM(float2 uv)
    {
        const float2x2 m = float2x2( 1.6,  1.2, -1.2,  1.6 );
		float f  = 0.5000*WormNoise( uv ); uv = mul(uv, m);
		f += 0.3300*WormNoise( uv ); uv = mul(uv, m);
		f += 0.1700*WormNoise( uv );
        return f;
    }


    float Noise(float2 uv, float4 param)
    {
        return ((Noise2D(uv * param.x + param.z) - param.w) * param.y);
    }

    float NoiseFBM(float2 uv, float4 param)
    {
        return ((FBM2D(uv * param.x + param.z) - param.w) * param.y);
    }

    float NoiseWorley(float2 uv, float4 param)
    {
        return ((WorleyNoise2D(uv * param.x + param.z) - param.w) * param.y);
    }

    float NoiseWorm(float2 uv, float4 param)
    {
        return ((WormNoise(uv * param.x + param.z) - param.w) * param.y);
    }

    float NoiseWormFBM(float2 uv, float4 param)
    {
        return ((WormNoiseFBM(uv * param.x + param.z) - param.w) * param.y);
    }

    float NoiseErosion(float2 uv, float4 param, float3 normal)
    {
        return ((ErosionNoise(uv * param.x + param.z, normal) - param.w) * param.y);
    }

#endif

