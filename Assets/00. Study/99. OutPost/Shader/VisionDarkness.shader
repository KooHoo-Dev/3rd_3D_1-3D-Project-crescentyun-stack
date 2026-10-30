Shader "OutPost/VisionDarkness"
{
    Properties
    {
        _DarkColor ("Dark Color", Color) = (0.02, 0.03, 0.06, 1)   // 시야 밖 색 (살짝 푸른 검정)
        _Darkness  ("Darkness", Range(0, 1)) = 0.75                // 1 = 완전히 DarkColor
        _Softness  ("Edge Softness", Range(0, 8)) = 3              // 경계 흐림 정도 (픽셀)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "VisionDarkness"

            HLSLPROGRAM
            #pragma vertex Vert      // Blit.hlsl에 있는 화면 전체 삼각형용 정점 함수
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_VisionMask);   // VisionMaskCamera가 SetGlobalTexture로 넘긴 마스크

            float4 _DarkColor;
            float  _Darkness;
            float  _Softness;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 원래 화면 색
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // 주변 3x3 평균으로 경계를 부드럽게 (1 = 시야 안, 0 = 밖)
                float2 offset = _Softness / _ScreenParams.xy;
                float mask = 0;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    mask += SAMPLE_TEXTURE2D(_VisionMask, sampler_LinearClamp, uv + float2(x, y) * offset).r;
                mask /= 9.0;

                // 시야 밖 색 → 마스크 값만큼 원래 색으로 되돌림
                half3 dark = lerp(color, _DarkColor.rgb, _Darkness);
                return half4(lerp(dark, color, mask), 1);
            }
            ENDHLSL
        }
    }
}