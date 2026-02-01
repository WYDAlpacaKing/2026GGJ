Shader "Custom/WorldSpaceTriplanarEmission_Lit"
{
    Properties
    {
        _BaseMap("Base Color Map", 2D) = "white" {}
        _EmissionMap("Emission Map", 2D) = "black" {}

        _Tiling("World Tiling", Float) = 1
        _Blend("Blend Sharpness", Float) = 4

        [HDR]_EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength("Emission Strength", Float) = 1
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Opaque"
            }

            Pass
            {
                Name "ForwardLit"
                Tags { "LightMode" = "UniversalForward" }

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _SHADOWS_SOFT

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ===== 手动声明纹理（不再 include SurfaceInput）=====
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float _Tiling;
            float _Blend;
            float4 _EmissionColor;
            float _EmissionStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                return OUT;
            }

            float4 TriplanarSample(
                TEXTURE2D_PARAM(tex, samplerTex),
                float3 worldPos,
                float3 worldNormal,
                float tiling,
                float blend
            )
            {
                float3 n = abs(worldNormal);
                n = pow(n, blend);
                n /= (n.x + n.y + n.z);

                float2 uvX = worldPos.zy * tiling;
                float2 uvY = worldPos.xz * tiling;
                float2 uvZ = worldPos.xy * tiling;

                float4 x = SAMPLE_TEXTURE2D(tex, samplerTex, uvX);
                float4 y = SAMPLE_TEXTURE2D(tex, samplerTex, uvY);
                float4 z = SAMPLE_TEXTURE2D(tex, samplerTex, uvZ);

                return x * n.x + y * n.y + z * n.z;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);

                float3 baseColor = TriplanarSample(
                    TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap),
                    IN.positionWS,
                    normalWS,
                    _Tiling,
                    _Blend
                ).rgb;

                float3 emission = TriplanarSample(
                    TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap),
                    IN.positionWS,
                    normalWS,
                    _Tiling,
                    _Blend
                ).rgb * _EmissionColor.rgb * _EmissionStrength;

                // ===== URP 标准光照 =====
                InputData inputData;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = 0;
                inputData.vertexLighting = 0;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = float2(0,0);
                inputData.shadowMask = float4(1,1,1,1);

                SurfaceData surface;
                surface.albedo = baseColor;
                surface.metallic = 0;
                surface.smoothness = 0.3;
                surface.normalTS = float3(0,0,1);
                surface.occlusion = 1;
                surface.emission = emission;
                surface.alpha = 1;

                return UniversalFragmentPBR(inputData, surface);
            }
            ENDHLSL
        }
        }
}
