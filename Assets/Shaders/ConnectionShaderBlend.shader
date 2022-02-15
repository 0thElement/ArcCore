Shader "Unlit/Blend/ConnectionLine"
{
    Properties
    {
		_LightColor ("Light Color", Color) = (1, 1, 1, 1)
		_ConflictColor ("Conflict Color", Color) = (1, 1, 1, 1)
		_BlendStyle ("Blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay"}
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            float4 _LightColor;
            float4 _ConflictColor;
            float4 _MainTex_ST;
            float _BlendStyle;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if(i.worldpos.z <= -100) discard;
                return _LightColor * (1 - _BlendStyle) + _ConflictColor * _BlendStyle;
            }
            ENDCG
        }
    }
}
