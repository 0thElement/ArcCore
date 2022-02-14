Shader "Unlit/Blend/Arctap"
{
	Properties
	{
		_LightTex ("Light Texture", 2D) = "white" {}
		_ConflictTex ("Conflict Texture", 2D) = "white" {}
		_BlendStyle ("Blend", Float) = 0
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

			sampler2D _LightTex;
			sampler2D _ConflictTex;
            float4 _LightTex_ST;
			float _BlendStyle;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _LightTex);
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				float farCut = -124.25 + i.worldpos.y * 6;
				if(i.worldpos.z <= farCut) return 0;

				float4 light = tex2D(_LightTex, i.uv);
				float4 conflict = tex2D(_ConflictTex, i.uv);

				float4 c = conflict * _BlendStyle + light * (1 - _BlendStyle);
				c.a *= alpha_from_pos(i.worldpos.z);

				return c;
			}
			ENDCG
		}
	}
}
