﻿Shader "Arcade/Track"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Offset ("Offset", Float) = 1
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true"  }
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

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

			float _Offset;
			sampler2D _MainTex;
			float4 _Color;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
			    float2 p = i.uv;
				p.y = p.y + _Offset; 
				float4 c= tex2D(_MainTex,p);
				c.a = _Color.a;
				return c;
			}
			ENDCG
		}
	}
}
