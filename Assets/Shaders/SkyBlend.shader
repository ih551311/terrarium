// 하늘 섞기: 낮 하늘과 밤 하늘(둘 다 큐브맵)을 Blend 값으로 섞는다. 0이면 낮, 1이면 밤.
// 하늘마다 색조(Tint)와 밝기(Exposure)를 따로 줄 수 있어서, 아침과 노을에는 낮 하늘에 따뜻한 색과 어두운 밝기를 입혀 쓴다.
// Unity 기본 Skybox/Cubemap 셰이더와 같은 방식이다. (SkyController가 시간에 맞춰 값을 바꾼다)
Shader "유리병 정원/Sky Blend"
{
    Properties
    {
        [NoScaleOffset] _TexA ("낮 하늘", Cube) = "grey" {}
        [NoScaleOffset] _TexB ("밤 하늘", Cube) = "grey" {}
        _TintA ("낮 색조", Color) = (0.5, 0.5, 0.5, 0.5)
        _TintB ("밤 색조", Color) = (0.5, 0.5, 0.5, 0.5)
        _ExposureA ("낮 밝기", Range(0, 8)) = 1
        _ExposureB ("밤 밝기", Range(0, 8)) = 1
        _Blend ("섞기 (0 낮 ~ 1 밤)", Range(0, 1)) = 0
        // 흐린 날: 하늘을 구름 낀 회색으로 덮는다. (색을 어둡게만 하면 짙은 남색이 되어 어색해서, 하늘 위쪽과 지평선 색을 따로 준다)
        _Overcast ("흐림 (0 맑음 ~ 1 온통 구름)", Range(0, 1)) = 0
        _CloudZenith ("구름 색 (머리 위)", Color) = (0.58, 0.62, 0.68, 1)
        _CloudHorizon ("구름 색 (지평선)", Color) = (0.76, 0.78, 0.8, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            samplerCUBE _TexA;
            samplerCUBE _TexB;
            half4 _TexA_HDR;
            half4 _TexB_HDR;
            half4 _TintA;
            half4 _TintB;
            half _ExposureA;
            half _ExposureB;
            half _Blend;
            half _Overcast;
            half4 _CloudZenith;
            half4 _CloudHorizon;

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 a = DecodeHDR(texCUBE(_TexA, i.texcoord), _TexA_HDR) * _TintA.rgb * unity_ColorSpaceDouble.rgb * _ExposureA;
                half3 b = DecodeHDR(texCUBE(_TexB, i.texcoord), _TexB_HDR) * _TintB.rgb * unity_ColorSpaceDouble.rgb * _ExposureB;
                half3 sky = lerp(a, b, _Blend);

                // 구름 낀 하늘: 지평선은 밝은 회색, 머리 위는 조금 짙은 회색. 원래 하늘의 밝고 어두움을 살짝 남겨서 밋밋하지 않게 한다.
                half up = saturate(normalize(i.texcoord).y * 1.6);
                half3 cloud = lerp(_CloudHorizon.rgb, _CloudZenith.rgb, up);
                half luma = dot(a, half3(0.333, 0.333, 0.333));
                cloud *= 0.88 + 0.24 * saturate(luma);
                return half4(lerp(sky, cloud, _Overcast), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
