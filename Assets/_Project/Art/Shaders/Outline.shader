Shader "TehranNoir/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.85, 0.15, 1)
        _Thickness ("Thickness (screen)", Range(0.001, 0.02)) = 0.004
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Thickness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 clip = TransformWorldToHClip(positionWS);

                // مرکز آبجکت در فضای کلیپ
                float3 centerWS = float3(
                    unity_ObjectToWorld._m03,
                    unity_ObjectToWorld._m13,
                    unity_ObjectToWorld._m23);
                float4 centerClip = TransformWorldToHClip(centerWS);

                // جهت از مرکز به رأس، روی صفحه
                float2 vertexNDC = clip.xy / clip.w;
                float2 centerNDC = centerClip.xy / centerClip.w;

                float2 direction = vertexNDC - centerNDC;
                float len = max(length(direction), 1e-5);
                direction /= len;

                clip.xy += direction * _Thickness * clip.w;

                output.positionCS = clip;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}