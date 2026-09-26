Shader "PlayMaker/DistanceFade/StandardDither"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)

        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0

        _FadeStart ("Fade Start Distance", Float) = 50
        _FadeEnd   ("Fade End Distance",   Float) = 100

        _DitherScale ("Dither Scale (screen px)", Float) = 1.0
    }

    SubShader
    {
        // Still in Transparent queue (so it can fade visually),
        // but we WRITE depth to avoid concave parts drawing on top.
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" }
        LOD 200
        ZWrite On

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:clip addshadow
        #pragma target 3.0

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;

        half _Metallic;
        half _Glossiness;

        sampler2D _BumpMap;
        float _BumpScale;

        float _FadeStart;
        float _FadeEnd;
        float _DitherScale;

        // 4x4 Bayer-like dither pattern from screen pixel position
        float Dither4x4(float2 pixelPos)
        {
            // Scale down if you want bigger dither "dots"
            pixelPos /= max(_DitherScale, 0.0001);

            int2 p = int2(floor(pixelPos)) & 3; // wrap to 0..3

            int x = p.x;
            int y = p.y;

            // Generate a 0..15 index for a 4x4 Bayer-style matrix
            int index =
                (x & 1)        +  // bit 0
                ((y & 1) << 1) +  // bit 1
                ((x & 2) << 1) +  // bit 2
                ((y & 2) << 2);   // bit 3

            // Center thresholds between 0 and 1
            return (index + 0.5) / 16.0;
        }

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 worldPos;
            float4 screenPos; // needed for screen-space dither
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Standard PBR setup
            o.Albedo     = c.rgb;
            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness;

            // Normal map support
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap)) * _BumpScale;

            // Linear distance fade
            float dist = distance(IN.worldPos, _WorldSpaceCameraPos);
            float t = saturate((dist - _FadeStart) / max(0.0001, (_FadeEnd - _FadeStart)));
            float fade = 1.0 - t; // 1 near, 0 far

            // Screen-space dither: convert to pixel space
            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float2 pixelPos = screenUV * _ScreenParams.xy;

            float threshold = Dither4x4(pixelPos);

            // If fade is below threshold, discard this pixel.
            // This gives the illusion of smooth fade while keeping depth writes.
            if (fade <= threshold)
            {
                clip(-1); // discard
            }

            // Keep alpha as the texture alpha (used by lighting),
            // actual visibility is controlled by the dither clip above.
            o.Alpha = c.a;
        }
        ENDCG
    }

    Fallback "Transparent/VertexLit"
}
