Shader "PlayMaker/DistanceFade/URP_StandardDither"
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
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.0"
        }

        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        LOD 300

        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        HLSLINCLUDE
        #pragma target 3.0

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
        TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;

            float _Metallic;
            float _Glossiness;

            float _BumpScale;

            float _FadeStart;
            float _FadeEnd;
            float _DitherScale;
        CBUFFER_END

        float2 TransformUV(float2 uv, float4 st) { return uv * st.xy + st.zw; }

        float Dither4x4(float2 pixelPos)
        {
            pixelPos /= max(_DitherScale, 0.0001);
            int2 p = ((int2)floor(pixelPos)) & int2(3, 3);

            int x = p.x;
            int y = p.y;

            int index =
                (x & 1)        +
                ((y & 1) << 1) +
                ((x & 2) << 1) +
                ((y & 2) << 2);

            return (index + 0.5) / 16.0;
        }

        float ComputeFade01(float3 worldPos)
        {
            float dist = distance(worldPos, GetCameraPositionWS());
            float denom = max(0.0001, (_FadeEnd - _FadeStart));
            float t = saturate((dist - _FadeStart) / denom);
            return 1.0 - t;
        }

        void ApplyDitherClip(float fade01, float4 positionCS)
        {
            float2 screenUV = (positionCS.xy / max(positionCS.w, 1e-6)) * 0.5 + 0.5;
            float2 pixelPos = screenUV * _ScreenParams.xy;

            float threshold = Dither4x4(pixelPos);
            clip(fade01 - threshold - 1e-5);
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;

            float3 positionWS : TEXCOORD1;
            float3 normalWS   : TEXCOORD2;
            float4 tangentWS  : TEXCOORD3; // xyz=tangent, w=sign
        };

        Varyings Vert(Attributes v)
        {
            Varyings o;

            VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS = posInputs.positionCS;
            o.positionWS = posInputs.positionWS;

            // Compute normal/tangent ourselves for URP-version compatibility.
            o.normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));

            float3 tWS = TransformObjectToWorldDir(v.tangentOS.xyz);
            tWS = normalize(tWS);

            // Tangent sign: tangent.w * oddNegativeScale (handles mirrored transforms)
            float sign = v.tangentOS.w * GetOddNegativeScale();

            o.tangentWS = float4(tWS, sign);

            o.uv = TransformUV(v.uv, _MainTex_ST);
            return o;
        }

        void DoFadeDitherClip(Varyings i)
        {
            float fade01 = ComputeFade01(i.positionWS);
            ApplyDitherClip(fade01, i.positionCS);
        }

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            half4 Frag(Varyings i) : SV_Target
            {
                DoFadeDitherClip(i);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 c   = tex * _Color;

                SurfaceData surfaceData;
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.albedo     = c.rgb;
                surfaceData.metallic   = saturate(_Metallic);
                surfaceData.smoothness = saturate(_Glossiness);
                surfaceData.alpha      = c.a;

                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv),
                    _BumpScale
                );

                InputData inputData;
                ZERO_INITIALIZE(InputData, inputData);

                inputData.positionWS = i.positionWS;

                float3 nWS = normalize(i.normalWS);
                float3 tWS = normalize(i.tangentWS.xyz);
                float3 bWS = normalize(cross(nWS, tWS) * i.tangentWS.w);
                float3x3 TBN = float3x3(tWS, bWS, nWS);

                inputData.normalWS = normalize(mul(normalTS, TBN));
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);

                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = ComputeFogFactor(i.positionCS.z);

                // Explicit vector assignments (fixes “numeric-type constructor” errors)
                inputData.bakedGI = half3(0, 0, 0);
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepth

            half4 FragDepth(Varyings i) : SV_Target
            {
                DoFadeDitherClip(i);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            ShadowVaryings ShadowVert(Attributes v)
            {
                ShadowVaryings o;

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionWS = posInputs.positionWS;
                o.uv = TransformUV(v.uv, _MainTex_ST);

                float3 normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));

                o.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(o.positionWS, normalWS, _MainLightPosition.xyz)
                );

                return o;
            }

            half4 ShadowFrag(ShadowVaryings i) : SV_Target
            {
                float fade01 = ComputeFade01(i.positionWS);
                ApplyDitherClip(fade01, i.positionCS);

                float alpha = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color).a;
                clip(alpha - 0.001);

                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
