Shader "VolumetricLights/Invisible"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (0.9,0.9,0.9,0.3)
    }
    SubShader
		{
			Tags { "RenderType" = "Opaque" "Queue" = "Geometry-100" }
			ZTest Always
			ZWrite Off
			ColorMask 0
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				struct appdata
				{
					float4 vertex : POSITION;
				};

				struct v2f
				{
					float4 pos     : SV_POSITION;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.pos = -1000;
					return o;
				}

				half4 frag(v2f i) : SV_Target
				{ 
					return 0;
				}
				ENDCG
			}

		}
}