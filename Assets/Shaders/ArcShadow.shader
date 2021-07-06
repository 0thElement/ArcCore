Shader "Unlit/ArcShadow "
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		//Highlight = 0 -> normal, 1 -> highlight, -1 -> gray	
		_Highlight ("Highlight", Float) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true"  }
        Cull Off
		ZTest Off
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
				float2 uv : TEXCOORD0;
				float3 worldpos : TEXCOORD1;
			};
			 
			float _Highlight;
			float4 _Color;
            float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				float4 c = _Color;

				c.a *= alpha_from_pos(i.worldpos.z) * 0.5;

				if (_Highlight < 0) c.a *= 0.85;
				return c;
			}
			ENDCG
		}
	}
}
