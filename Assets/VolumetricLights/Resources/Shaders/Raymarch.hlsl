
TEXTURE2D(_BlueNoise);
float4 _BlueNoise_TexelSize;

void SetJitter(float2 uv) {
    uv *= _ScreenParams.xy / (_Downscaling + 1);
#if VL_BLUENOISE
    float2 noiseUV = uv * _BlueNoise_TexelSize.xy + _WindDirection.ww;
    jitter = SAMPLE_TEXTURE2D_LOD(_BlueNoise, sampler_PointRepeat, noiseUV, 0).r;
#else
    const float3 magic = float3( 0.06711056, 0.00583715, 52.9829189 );
    jitter = frac( magic.z * frac( dot( uv, magic.xy ) ) );
#endif
}


inline float3 ProjectOnPlane(float3 v, float3 planeNormal) {
    float sqrMag = dot(planeNormal, planeNormal);
    float dt = dot(v, planeNormal);
	return v - planeNormal * dt / sqrMag;
}

inline float3 GetRayStart(float3 wpos) {
    float3 cameraPosition = GetCameraPositionWS();
    #if defined(ORTHO_SUPPORT)
	    float3 cameraForward = UNITY_MATRIX_V[2].xyz;
	    float3 rayStart = ProjectOnPlane(wpos - cameraPosition, cameraForward) + cameraPosition;
        return lerp(cameraPosition, rayStart, unity_OrthoParams.w);
    #else
        return cameraPosition;
    #endif
}


half SampleDensity(float3 wpos) {

#if VL_NOISE
    half density = tex3Dlod(_NoiseTex, float4(wpos * _NoiseScale - _WindDirection.xyz, 0)).r;
    density = saturate( (1.0 - density * _NoiseStrength) * _NoiseFinalMultiplier);
#else
    half density = 1.0;
#endif

    return density * DistanceAttenuation(wpos);
}

void AddFog(float3 wpos, float energyStep, half4 baseColor, inout half4 sum) {

   half density = SampleDensity(wpos);

   half4 fgCol = half4(baseColor.rgb, baseColor.a * density);
   fgCol.rgb *= fgCol.aaa;
   fgCol.a = min(1.0, fgCol.a);

   fgCol *= energyStep;
   sum += fgCol * (1.0 - sum.a);
}



half4 Raymarch(float3 rayStart, float3 rayDir, float t0, float t1) {

    // diffusion
    #if VL_POINT
        half spec = dot(rayDir, normalize(_ConeTipData.xyz - _WorldSpaceCameraPos));
    #else
        half spec = dot(rayDir, _ToLightDir.xyz);
    #endif
    half diffusion = 1.0 + spec * spec * _ToLightDir.w;
    half4 lightColor = half4(_LightColor.rgb * diffusion, _LightColor.a);

    // compute raymarch step
    float rs = MIN_STEPPING + max(0, log(t1-t0)) / FOG_STEPPING;
    half4 sum = half4(0,0,0,0);

    // raymarch
    float3 wpos = rayStart + rayDir * t0;
    rayDir *= rs;
    half energyStep = min(1.0, _Density * half(rs));
	float t = 0;
    t1 -= t0;
    for (int k=0;k<_RayMarchMaxSteps;k++) {
        if (t >= t1) break;
        #if VL_SHADOWS_CUBEMAP
       	    half atten = GetShadowCubemapAtten(wpos);
            half4 baseColor = lerp(_ShadowColor, lightColor, atten);
            AddFog(wpos, energyStep, baseColor, sum);
        #elif VL_SHADOWS || VL_SPOT_COOKIE || VL_SHADOWS_TRANSLUCENCY
       	    half3 atten = GetShadowAtten(t / t1);
            #if VL_SHADOWS
                half4 baseColor = lerp(_ShadowColor, lightColor, atten.r);
            #else
                half4 baseColor = half4(lightColor.rgb * atten, lightColor.a);
            #endif
            AddFog(wpos, energyStep, baseColor, sum);
       	#else 
            AddFog(wpos, energyStep, lightColor, sum);
        #endif
        if (sum.a > 0.99) break;
        t += rs;
       	wpos += rayDir;
    }

    return sum;
}
