// 오로라: 밤하늘에 걸리는 빛의 커튼. 세로 줄기가 천천히 일렁이고, 아래는 초록, 가운데는 청록, 위는 보라로 번진다.
// 더하기(Additive)로 그려서 어두운 하늘에서 환하게 보인다. 모양(띠 메쉬)은 AuroraSky가 만들고, 세기는 AuroraSky가 밤에 맞춰 바꾼다.
Shader "유리병 정원/Aurora"
{
    Properties
    {
        _ColorA ("아래 색 (초록)", Color) = (0.25, 1.0, 0.55, 1)
        _ColorB ("가운데 색 (청록)", Color) = (0.2, 0.8, 1.0, 1)
        _ColorC ("위 색 (보라)", Color) = (0.7, 0.35, 1.0, 1)
        _Intensity ("세기", Range(0, 2)) = 1
        _Speed ("일렁이는 속도", Float) = 1
        _Seed ("무늬 씨앗", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorA, _ColorB, _ColorC;
            half _Intensity, _Speed, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i), b = hash(i + float2(1, 0)), c = hash(i + float2(0, 1)), d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed * 0.1;
                float u = i.uv.x, v = saturate(i.uv.y);

                // 커튼이 천천히 흔들린다
                float sway = vnoise(float2(u * 3.0 + t, _Seed)) * 0.15 + vnoise(float2(u * 9.0 - t * 1.5, _Seed + 3.0)) * 0.05;
                // 세로 줄기
                float rays = vnoise(float2(u * 38.0 + sway * 20.0 + t * 2.0, _Seed + 7.0));
                rays = pow(saturate(rays * 1.4), 2.0);
                // 커다란 밝고 어두움
                float body = (0.35 + 0.65 * rays) * (0.55 + 0.9 * vnoise(float2(u * 6.0 - t, _Seed + 11.0)));

                float bottom = smoothstep(0.0, 0.10, v);   // 아래 가장자리는 또렷하게
                float top = pow(1.0 - v, 1.8);              // 위로 갈수록 옅어진다
                float edge = smoothstep(0.0, 0.12, u) * smoothstep(1.0, 0.88, u); // 양 끝은 부드럽게 사라진다

                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0.10, 0.50, v));
                col = lerp(col, _ColorC.rgb, smoothstep(0.50, 1.00, v));
                return fixed4(col * (bottom * top * body * edge * _Intensity), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
