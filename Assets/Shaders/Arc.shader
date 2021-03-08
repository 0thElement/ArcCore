Shader "Arcade/Arc"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_From ("From", Float) = 0
		_To ("To", Float) = 1 
		_Cutoff ("Cutoff", Float) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true"  }
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
			 
			float _From,_To,_Cutoff;
			float4 _Color;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;
				o.worldpos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				if(i.worldpos.z <= -124.25 || (i.worldpos.z >= 0 && _Cutoff <= 0)) discard;
			    if(i.uv.y < _From || i.uv.y > _To) return 0;
				float4 c = tex2D(_MainTex,i.uv) ; 
				float4 inColor = i.color;
				c *= inColor;
				return c;
			}
			ENDCG
		}
	}
}
