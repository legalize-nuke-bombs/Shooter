Shader "FullScreen/VHS"
{
    Properties
    {
        _Scanline("Scanline", Range(0, 1)) = 0.4
        _Chroma("Chroma", Range(0, 3)) = 1.2
        _Wobble("Wobble", Range(0, 3)) = 1.0
        _Jitter("Jitter", Range(0, 3)) = 1.0
        _Noise("Noise", Range(0, 0.5)) = 0.08
        _Desat("Desaturate", Range(0, 1)) = 0.2
    }

    HLSLINCLUDE

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float _Scanline;
    float _Chroma;
    float _Wobble;
    float _Jitter;
    float _Noise;
    float _Desat;
    float _VhsStrength;

    float Hash(float2 p)
    {
        return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP, uint2(0, 0));

        float2 uv = posInput.positionNDC.xy;
        float t = _Time.y;
        float row = uv.y;
        float strength = saturate(_VhsStrength);

        // horizontal tracking wobble per row, with occasional jitter bursts
        float wob = sin(row * 720.0 + t * 4.0) * _Wobble * strength * 0.0025;
        float jseed = Hash(float2(floor(t * 11.0), floor(row * 36.0)));
        float jit = step(0.985, jseed) * _Jitter * strength * 0.03 * (Hash(float2(t, row)) - 0.5);
        float ux = frac(uv.x + wob + jit);

        // per-row chromatic split
        float cs = _Chroma * strength * 0.0035 * (0.6 + 0.4 * sin(row * 240.0 + t * 2.0));
        float3 col;
        col.r = CustomPassSampleCameraColor(float2(frac(ux + cs), uv.y), 0).r;
        col.g = CustomPassSampleCameraColor(float2(ux, uv.y), 0).g;
        col.b = CustomPassSampleCameraColor(float2(frac(ux - cs), uv.y), 0).b;

        // analog wash
        float luma = dot(col, float3(0.299, 0.587, 0.114));
        col = lerp(col, luma.xxx, _Desat * strength);

        // scanlines
        float scan = 0.5 + 0.5 * sin(uv.y * _ScreenSize.y * 1.2);
        col *= lerp(1.0, 0.55 + 0.45 * scan, saturate(_Scanline * strength));

        // vhs grain, scaled by luma so black stays black
        float lum = dot(col, float3(0.299, 0.587, 0.114));
        float n = Hash(uv * _ScreenSize.xy * 0.7 + t * 57.0);
        col += (n - 0.5) * _Noise * strength * (lum + 0.12);

        return float4(col, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "VHS"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
}
