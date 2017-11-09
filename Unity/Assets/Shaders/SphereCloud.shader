// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Sphere/Cloud" {
	Properties {
		_Color ("Color Tint", Color) = (1,1,1,1)
		_MainTex ("Main (RGB)", 2D) = "white" {}
		_DetailTex ("Detail (RGB)", 2D) = "white" {}
		_FalloffPow ("Falloff Power", Range(0,3)) = 2
		_FalloffScale ("Falloff Scale", Range(0,20)) = 3
		_DetailScale ("Detail Scale", Range(0,1000)) = 100
		_DetailOffset ("Detail Offset", Color) = (0,0,0,0)
		_DetailDist ("Detail Distance", Range(0,1)) = 0.00875
		_MinLight ("Minimum Light", Range(0,1)) = .5
		_FadeDist ("Fade Distance", Range(0,100)) = 10
		_FadeScale ("Fade Scale", Range(0,1)) = .002
		_RimDist ("Rim Distance", Range(0,1)) = 1
	}
	
SubShader {
			Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off ZWrite Off
			Offset -1,-1

	Pass { 
			Tags{ "LightMode" = "ForwardBase" }

		CGPROGRAM
		
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#define MAG_ONE 1.4142135623730950488016887242097
		#pragma fragmentoption ARB_precision_hint_nicest
		#define PI 3.1415926535897932384626
		#define INV_PI (1.0/PI)
		#define TWOPI (2.0*PI) 
		#define INV_2PI (1.0/TWOPI)

		    // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"
	 
		sampler2D _MainTex;
		sampler2D _DetailTex;
		fixed4 _Color;
		fixed4 _DetailOffset;
		float _FalloffPow;
		float _FalloffScale;
		float _DetailScale;
		float _DetailDist;
		float _MinLight;
		float _FadeDist;
		float _FadeScale;
		float _RimDist;
		
		struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float3 normal : NORMAL;
			};

		struct v2f {
			float4 pos : SV_POSITION;
			float3 worldVert : TEXCOORD0;
			float3 worldOrigin : TEXCOORD1;
			float  viewDist : TEXCOORD2;
			float3 worldNormal : TEXCOORD3;
			float3 objNormal : TEXCOORD4;
			float3 viewDir : TEXCOORD5;
			SHADOW_COORDS(6)
		};	
		

		v2f vert (appdata_t v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
		   float3 vertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		   float3 origin = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
	   	   o.worldVert = vertexPos;
	   	   o.worldOrigin = origin;
	   	   o.viewDist = distance(vertexPos,_WorldSpaceCameraPos);
	   	   o.worldNormal = normalize(vertexPos-origin);
	   	   o.objNormal = normalize( v.vertex);
	   	   o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
		   TRANSFER_SHADOW(o)

	   	   return o;
	 	}
	 	
		float4 Derivatives( float3 pos )  
		{  
		    float lat = INV_2PI*atan2( pos.y, pos.x );  
		    float lon = INV_PI*acos( pos.z );  
		    float2 latLong = float2( lat, lon );  
		    float latDdx = INV_2PI*length( ddx( pos.xy ) );  
		    float latDdy = INV_2PI*length( ddy( pos.xy ) );  
		    float longDdx = ddx( lon );  
		    float longDdy = ddy( lon );  
		 	
		    return float4( latDdx , longDdx , latDdy, longDdy );  
		} 
	 		
		fixed4 frag (v2f IN) : COLOR
			{
			half4 color;
			float3 objNrm = IN.objNormal;
		 	float2 uv;
		 	uv.x = .5 + (INV_2PI*atan2(objNrm.z, objNrm.x));
		 	uv.y = INV_PI*acos(-objNrm.y);
		 	float4 uvdd = Derivatives(objNrm);
		    half4 main = tex2D(_MainTex, uv, uvdd.xy, uvdd.zw)*_Color;
			half4 detailX = tex2D (_DetailTex, objNrm.zy*_DetailScale + _DetailOffset.xy);
			half4 detailY = tex2D (_DetailTex, objNrm.zx*_DetailScale + _DetailOffset.xy);
			half4 detailZ = tex2D (_DetailTex, objNrm.xy*_DetailScale + _DetailOffset.xy);
			objNrm = abs(objNrm);
			half4 detail = lerp(detailZ, detailX, objNrm.x);
			detail = lerp(detail, detailY, objNrm.y);
			half detailLevel = saturate(2*_DetailDist*IN.viewDist);
			color = main.rgba * lerp(detail.rgba, 1, detailLevel);

			float rim = saturate(dot(IN.viewDir, IN.worldNormal));
			rim = saturate(pow(_FalloffScale*rim,_FalloffPow));
			float dist = distance(IN.worldVert,_WorldSpaceCameraPos);
			float distLerp = saturate(_RimDist*(distance(IN.worldOrigin,_WorldSpaceCameraPos)-1.01*distance(IN.worldVert,IN.worldOrigin)));
			float distFade = saturate((_FadeScale*dist)-_FadeDist);
	   	   	float distAlpha = lerp(distFade, rim, distLerp);

			color.a = lerp(0, color.a,  distAlpha);

          	//lighting
			half3 ambientLighting = half3(0.05, 0.05, 0.05);
			half3 lightDirection = normalize(_WorldSpaceLightPos0);
			half NdotL = saturate(dot (IN.worldNormal, lightDirection));
	        half diff = (NdotL - 0.01) / 0.99;
			half lightIntensity = saturate(_LightColor0.a * diff * 4);
			color.rgb *= saturate(ambientLighting + ((_MinLight + _LightColor0.rgb) * lightIntensity));

			fixed shadow = SHADOW_ATTENUATION(IN);
			color.rgb *= shadow;

          	return color;
		}
		ENDCG
	
		}  
		
	} 
	
}