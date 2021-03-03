Shader "Unlit/HeightIndicator"
{
    Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "CanUseSpriteAtlas"="true"  }

        Cull Off
        Lighting Off
		ZWrite Off 
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
			};
			 
			int _Highlight;
			float4 _Color;
            float4 _MainTex_ST;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				float4 c = tex2D(_MainTex,i.uv) ; 
				float4 inColor = i.color;
				c *= inColor;  
				return c;
			}
			ENDCG
		}
	}
}
