Shader "Unlit/ArcIndicator"
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
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float2 uv : TEXCOORD0;
				fixed4 color    : COLOR;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST, _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color * _Color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                return c;
            }
            ENDCG
        }
    }
}
