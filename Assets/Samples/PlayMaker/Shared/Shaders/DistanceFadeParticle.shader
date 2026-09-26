Shader "PlayMaker/DistanceFade/Particle"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)

        _FadeStart ("Fade Start Distance", Float) = 50
        _FadeEnd   ("Fade End Distance",   Float) = 100
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            float _FadeStart;
            float _FadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Tint;

                float dist = distance(i.worldPos, _WorldSpaceCameraPos);
                float t = saturate( (dist - _FadeStart) / max(0.0001, (_FadeEnd - _FadeStart)) );
                float fade = 1.0 - t;

                col.a *= fade;
                return col;
            }
            ENDCG
        }
    }

    Fallback "Particles/Alpha Blended"
}
