Shader "Unlit/HeightIndicator"
{
    Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_RedCol ("Red Color", Color) = (1,1,1,1)
		_RedMix ("Red Mix", Float) = 0
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
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
				float4 worldpos : TEXCOORD1;
			};
			 
			float _RedMix;
			float4 _Color, _RedCol;
            float4 _MainTex_ST;
			sampler2D _MainTex;

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
				if(i.worldpos.z <= -124.25) discard;
				float4 c = tex2D(_MainTex,i.uv) ; 
				float4 inColor = i.color;
				c *= inColor;  
				return c;
			}
			ENDCG
		}
	}
}
