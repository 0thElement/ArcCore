Shader "Unlit/Hold"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HighlightTex ("Highlight Texture", 2D) = "white" {}
        _GrayCol ("Gray", Color) = (1,1,1,1)

        _Direction("Direction", Float) = 1
        _Cutoff("Cutoff", Float) = 0
        _Highlight("Highlight", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Background+3"}
		Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
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

            sampler2D _MainTex,_HighlightTex;
            float4 _MainTex_ST;
            float4 _GrayCol;
            float _Cutoff,_Highlight,_Direction;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				if(_Cutoff == 1 && i.worldpos.z * _Direction > 0) return 0;
				if(i.worldpos.z < -100 || i.worldpos.z > 100) return 0;

                fixed4 col = (_Highlight > 0) ? tex2D(_HighlightTex, i.uv) : tex2D(_MainTex, i.uv);

                if (_Highlight < 0)
                {
                    col.a *= 0.5;
                }
                col.a *= 0.86;

                return col;
            }
            ENDCG
        }
    }
}
