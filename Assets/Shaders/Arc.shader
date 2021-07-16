Shader "Unlit/Arc"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_HighlightTex ("Highlight Texture", 2D) = "white" {}
		_RedCol ("Red Color", Color) = (1,1,1,1)
		_GrayCol ("Gray Color", Color) = (1,1,1,1)
		_Color ("Color", Color) = (1,1,1,1)

		//Highlight = 0 -> normal, 1 -> highlight, -1 -> gray	
		_Highlight ("Highlight", Float) = 0
		_RedMix ("Red Mix", Float) = 0
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
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
				float3 worldpos : TEXCOORD1;
			};
			 
			float _RedMix,_Highlight;
			float4 _Color,_RedCol,_GrayCol;
            float4 _MainTex_ST;
			sampler2D _MainTex, _HighlightTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * lerp(_Color, _RedCol, _RedMix);
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				float4 c = (_Highlight > 0) ? tex2D(_HighlightTex, i.uv) : tex2D(_MainTex,i.uv);
				float4 inColor = i.color;

				c *=  inColor;
				if (_Highlight < 0)
				{
					c = lerp(c, _GrayCol, 0.2);
					c.a *= 0.85;
				}

				c.a *= alpha_from_pos(i.worldpos.z) * 0.95;

				return c;
			}
			ENDCG
		}
	}
}
