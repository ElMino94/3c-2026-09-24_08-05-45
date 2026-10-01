Shader "Custom/URP/PlanarMeshShadow"
{
    Properties
    {
        _ShadowColor("Shadow Color", Color) = (0,0,0,1)
        _Bias("Surface Bias", Float) = 0.02
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _ShadowColor;
            float _Bias;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Vertex world position
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // Object origin in world space
                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));

                // URP main directional light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);

                // Avoid divide by zero
                float denom = max(abs(lightDir.y), 0.001);

                // Project onto plane at object origin height
                float t = (worldPos.y - originWS.y) / denom;
                worldPos -= lightDir * t;

                // Small lift to avoid z-fighting
                worldPos.y += _Bias;

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                return _ShadowColor;
            }
            ENDHLSL
        }
    }
}
