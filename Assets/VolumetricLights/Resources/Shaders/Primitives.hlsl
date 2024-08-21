#ifndef VOLUMETRIC_LIGHTS_PRIMITIVES
#define VOLUMETRIC_LIGHTS_PRIMITIVES

#define LIGHT_POS _ConeTipData.xyz
#define CONE_TIP_RADIUS _ConeTipData.w
#define CONE_BASE_RADIUS _ExtraGeoData.x
#define RANGE_SQR _ConeAxis.w
#define RANGE _ExtraGeoData.w

float BoxIntersection(float3 origin, float3 viewDir) {
    origin = mul(unity_WorldToObject, float4(origin, 1.0)).xyz;
    viewDir = mul((float3x3)unity_WorldToObject, viewDir);
    float3 ro     = origin - _MeshBoundsCenter;
    float3 invR   = 1.0.xxx / viewDir;
    float3 tbot   = invR * (-_MeshBoundsExtents - ro);
    float3 ttop   = invR * (_MeshBoundsExtents - ro);
    float3 tmin   = min (ttop, tbot);
    float2 tt0    = max (tmin.xx, tmin.yz);
    float t = max(tt0.x, tt0.y);
    return t;
}

float SphereIntersection(float3 origin, float3 viewDir) {
    float3  oc = origin - LIGHT_POS;
    float   b = dot(viewDir, oc);
    float   c = dot(oc,oc) - RANGE_SQR;
    float   t = b*b - c;
    t = sqrt(abs(t));
    return -b-t;
}

#define dot2(v) dot(v,v)

// from: https://www.iquilezles.org/www/articles/intersectors/intersectors.htm
float ConeIntersection(float3 origin, float3 viewDir) {
    float3 pb = LIGHT_POS + _ConeAxis.xyz;

    float3 ba = pb - LIGHT_POS;
    float3  oa = origin - LIGHT_POS;
    float3  ob = origin - pb;
    float m0 = dot(ba,ba);
    float m1 = dot(oa,ba);
    float m2 = dot(viewDir,ba);
    float m3 = dot(viewDir,oa);
    float m5 = dot(oa,oa);
    float m9 = dot(ob,ba); 
    
    //caps
    if( m1<0.0 ) {
        if( dot2(oa*m2-viewDir*m1) < CONE_TIP_RADIUS*CONE_TIP_RADIUS*m2*m2 )
            return -m1/m2;
    }
    else if( m9>0.0 ) {
    	float t = -m9/m2;
        if( dot2(ob+viewDir*t)< CONE_BASE_RADIUS*CONE_BASE_RADIUS )
            return t;
    }
    
    // body
    float rr = CONE_TIP_RADIUS - CONE_BASE_RADIUS;
    float hy = m0 + rr*rr;
    float k2 = m0*m0    - m2*m2*hy;
    float k1 = m0*m0*m3 - m1*m2*hy + m0*CONE_TIP_RADIUS*(rr*m2*1.0        );
    float k0 = m0*m0*m5 - m1*m1*hy + m0*CONE_TIP_RADIUS*(rr*m1*2.0 - m0*CONE_TIP_RADIUS);
    float h = k1*k1 - k2*k0;
    if( h<0.0 ) return -1; //no intersection
    float t = (-k1-sqrt(abs(h)))/k2;
    float y = m1 + t*m2;
    if( y<0.0 || y>m0 ) return -1; //no intersection
    return t;
}


float ComputeIntersection(float3 origin, float3 viewDir) {
    #if VL_POINT
        float t = SphereIntersection(origin, viewDir);
    #elif VL_SPOT || VL_SPOT_COOKIE
        float t = ConeIntersection(origin, viewDir);
    #else
        float t = BoxIntersection(origin, viewDir);
    #endif
    t = max(t, 0);
    return t;
}


void BoundsIntersection(float3 origin, float3 viewDir, inout float t0, inout float t1) {
    float3 ro     = origin - _BoundsCenter;
    float3 invR   = 1.0.xxx / viewDir;
    float3 tbot   = invR * (-_BoundsExtents - ro);
    float3 ttop   = invR * (_BoundsExtents - ro);
    float3 tmin   = min (ttop, tbot);
    float2 tt0    = max (tmin.xx, tmin.yz);
    t0 = max(tt0.x, tt0.y);
    t0 = max(t0, 0);
    float3 tmax = max (ttop, tbot);
    tt0 = min (tmax.xx, tmax.yz);
    t1 = min(tt0.x, tt0.y);  
    t1 = max(t1, t0);
}

bool TestBounds(float3 wpos) {
    return all ( abs(wpos - _BoundsCenter) <= _BoundsExtents );
}


float ConeAttenuation(float3 wpos) {

    float3 p = wpos - LIGHT_POS;
    float t = dot(p, _ConeAxis.xyz) / RANGE_SQR;
    t = saturate(t);

    float3 projection = t * _ConeAxis.xyz;
    float distSqr = dot2(p - projection);

    float maxDist = lerp(CONE_TIP_RADIUS, CONE_BASE_RADIUS, t);
    float maxDistSqr = maxDist * maxDist;
    float cone = (maxDistSqr - distSqr ) / (maxDistSqr * _Border);
    cone = saturate(cone);

    float t0 = dot2(p) / RANGE_SQR; // ensure light doesn't extend beyound spherical range

#if VL_PHYSICAL_ATTEN
    float t1 = t0 * _DistanceFallOff;
    float atten = (1.0 - t0) / dot(_FallOff, float3(1.0, t1, t1*t1));
#else
    float atten = 1.0 - (t0 * _DistanceFallOff);
#endif

    atten *= step(_NearClipDistance, t);

    cone *= saturate(atten);
    return cone;
}


float SphereAttenuation(float3 wpos) {
    float3 v = wpos - LIGHT_POS;
    float distSqr = dot2(v);

#if VL_PHYSICAL_ATTEN
    float t = distSqr / RANGE_SQR;
    float t1 = t * _DistanceFallOff;
    float atten = (1.0 - t) / dot(_FallOff, float3(1.0, t1, t1*t1));
#else
    float atten = distSqr * _ExtraGeoData.y + _ExtraGeoData.z;
#endif
    return atten;
}


float AreaRectAttenuation(float3 wpos) {
    float3 v = mul(unity_WorldToObject, float4(wpos, 1)).xyz;
    v = abs(v);
    float3 extents = _AreaExtents.xyz;
    float baseMultiplier = 1.0 + _AreaExtents.w * v.z;
    extents.xy *= baseMultiplier;
    float3 dd = extents - v;
    dd.xy = saturate(dd.xy / (extents.xy * _Border));
    float rect = min(dd.x, dd.y);

#if VL_PHYSICAL_ATTEN
    float t = v.z / _AreaExtents.z;
    float t1 = t * _DistanceFallOff;
    float atten = (1.0 - t) / dot(_FallOff, float3(1.0, t1, t1*t1));
#else
    float atten = 1.0 - (v.z / _AreaExtents.z) * _DistanceFallOff;
#endif
    rect *= saturate(atten);
    return rect;
}


float AreaDiscAttenuation(float3 wpos) {
    float3 v = mul(unity_WorldToObject, float4(wpos, 1)).xyz;
    //v.z = max(v.z, 0); // abs(v);
    float distSqr = dot2(v.xy);

    float maxDistSqr = _AreaExtents.x;
    float baseMultiplier = 1.0 + _AreaExtents.w * v.z;
    maxDistSqr *= baseMultiplier * baseMultiplier;

    float disc = saturate ((maxDistSqr - distSqr) / (maxDistSqr * _Border));

#if VL_PHYSICAL_ATTEN
    float t = v.z / _AreaExtents.z;
    float t1 = t * _DistanceFallOff;
    float atten = (1.0 - t) / dot(_FallOff, float3(1.0, t1, t1*t1));
#else
    float atten = 1.0 - (v.z / _AreaExtents.z) * _DistanceFallOff;
#endif
    disc *= saturate(atten);
    return disc;
}


float DistanceAttenuation(float3 wpos) {

    #if VL_SPOT || VL_SPOT_COOKIE
        return ConeAttenuation(wpos);
    #elif VL_POINT
        return SphereAttenuation(wpos);
    #elif VL_AREA_RECT
        return AreaRectAttenuation(wpos);
    #elif VL_AREA_DISC
        return AreaDiscAttenuation(wpos);
    #else
        return 1;
    #endif
}

#endif //  VOLUMETRIC_LIGHTS_PRIMITIVES