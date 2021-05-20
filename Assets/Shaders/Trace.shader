﻿Shader "Unlit/Trace"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		
		_Direction ("Direction", Float) = 1
		_Cutoff ("Cutoff", Float) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent+1"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true"  }
        Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
  
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "ColorSpace.cginc"
			#include "DistanceColorMath.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color    : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION; 
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float3 worldpos : TEXCOORD1;
			};
			 
			float _Direction,_Cutoff;
			float4 _Color;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				if(_Cutoff == 1 && i.worldpos.z * _Direction > 0) return 0;
				float4 c = i.color * _Color;
				c.a *= alpha_from_pos(i.worldpos.z) * 0.86;

				return c;
			}
			ENDCG
		}
	}
}