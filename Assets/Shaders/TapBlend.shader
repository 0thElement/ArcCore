Shader "Unlit/Blend/Tap"
{
    Properties
    {
        _LightTex ("Light Texture", 2D) = "white" {}
        _ConflictTex ("Conflict Texture", 2D) = "white" {}
        _BlendStyle ("Blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Background+4"}
		Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
		ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "DistanceColorMath.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldpos : TEXCOORD1;
            };

            sampler2D _LightTex;
            sampler2D _ConflictTex;
            float _BlendStyle;
            float4 _LightTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _LightTex);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_LightTex, i.uv) * (1 - _BlendStyle) + tex2D(_ConflictTex, i.uv) * _BlendStyle;
                col.a *= alpha_from_pos(i.worldpos.z);
                return col;
            }
            ENDCG
        }
    }
}
