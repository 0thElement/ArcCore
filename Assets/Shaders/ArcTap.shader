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
			#include "DistanceColorMath.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 worldpos : TEXCOORD1;
			};

			sampler2D _MainTex;
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
				float farCut = -124.25 + i.worldpos.y * 6;
				if(i.worldpos.z <= farCut) discard;
				float4 c = tex2D(_MainTex, i.uv);
				return alpha_from_pos(c, i.worldpos.z);
			}
			ENDCG
		}
	}
}
