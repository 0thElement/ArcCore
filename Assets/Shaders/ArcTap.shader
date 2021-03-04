Shader "Arcade/ArcTap"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Overlay+1"  "RenderType"="Transparent" "CanUseSpriteAtlas"="true"  }
		Cull Front
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "ColorSpace.cginc"

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

			sampler2D _MainTex;
            float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				return half4(tex2D(_MainTex,i.uv).rgb, 1);
			}
			ENDCG
		}
	}
}
