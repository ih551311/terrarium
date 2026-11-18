using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 풀과 꽃 모델 8종을 코드로 만들어 Assets/Models/Deco 에 .obj 로 저장한다.
/// 튤립, 해바라기, 라벤더, 밀 이삭, 코스모스, 네잎클로버, 풀 다발, 갈대.
/// 다른 장식 모델과 같은 팔레트 그림(34색)을 쓰는 각진 로우폴리다. 얇은 잎과 꽃잎은 앞뒤가 모두 보인다.
/// 모양이나 색을 바꾸고 싶으면 아래 숫자와 팔레트 번호만 고치고 메뉴를 다시 누른다.
/// 메뉴: 유리병 정원 > 풀 모델 만들기 (그 뒤에 '장식 모델 가져오기 설정'을 눌러야 재질이 붙는다)
/// </summary>
public static class PlantModelMaker
{
    // 팔레트 번호: 3 짙은 회색, 4 갈색, 5 짙은 갈색, 7 연한 갈색, 9~13 초록, 14 빨강, 16 노랑, 17 분홍, 18 보라, 21 진노랑, 22 연보라, 23 살구, 25 산호, 27 모래, 30 크림, 31 연노랑
    private const int Dark = 3, Brown = 5, Tan = 7, Green = 9, LightGreen = 10, DeepGreen = 11, Leaf = 12, Moss = 13,
                       Red = 14, Yellow = 16, Pink = 17, Purple = 18, Gold = 21, Lavender = 22, Peach = 23, Coral = 25, Sand = 27, Cream = 30, Pale = 31;

    [MenuItem("유리병 정원/풀 모델 만들기")]
    public static string Build()
    {
        Tulip(); Sunflower(); Lavender_(); Wheat(); Cosmos(); FourLeafClover(); GrassTuft(); Reed();
        AssetDatabase.Refresh();
        return "풀 모델 8종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    // ───────── 기본 모양 ─────────

    /// <summary>두 점 사이의 굵기가 변하는 기둥 (줄기, 꽃심)</summary>
    private static void Frustum(Builder b_, Vector3 p0, Vector3 p1, float r0, float r1, int sides, int color, int cap = -1)
    {
        Vector3 axis = (p1 - p0).normalized;
        Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 v = Vector3.Cross(axis, u);
        Vector3 mid = (p0 + p1) * 0.5f;
        if (cap < 0) cap = color;
        for (int j = 0; j < sides; j++)
        {
            float a0 = Mathf.PI * 2f * j / sides, a1 = Mathf.PI * 2f * (j + 1) / sides;
            Vector3 A0 = p0 + r0 * (Mathf.Cos(a0) * u + Mathf.Sin(a0) * v), A1 = p0 + r0 * (Mathf.Cos(a1) * u + Mathf.Sin(a1) * v);
            Vector3 B0 = p1 + r1 * (Mathf.Cos(a0) * u + Mathf.Sin(a0) * v), B1 = p1 + r1 * (Mathf.Cos(a1) * u + Mathf.Sin(a1) * v);
            b.Quad(A0, A1, B1, B0, color, mid);
            b.Tri(p0, A1, A0, cap, mid);
            b.Tri(p1, B0, B1, cap, mid);
        }
    }

    // 위 함수들이 쓰는 빌더 (모델 하나를 만드는 동안만 가리킨다)
    private static Builder b;

    /// <summary>납작하게도 길쭉하게도 만들 수 있는 타원체 (꽃봉오리, 낟알, 솜털)</summary>
    private static void Blob(Vector3 c, Vector3 r, int rings, int sec, int color)
    {
        Vector3 P(int i, int j)
        {
            float th = Mathf.PI * i / rings, ph = Mathf.PI * 2f * j / sec;
            return c + new Vector3(r.x * Mathf.Sin(th) * Mathf.Cos(ph), r.y * Mathf.Cos(th), r.z * Mathf.Sin(th) * Mathf.Sin(ph));
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < sec; j++)
                b.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), color, c);
    }

    /// <summary>꽃잎이나 잎 한 장. 밑동 base에서 dir 쪽으로 length만큼 뻗고, 가운데가 가장 넓은 마름모꼴이다. lift는 가운데를 살짝 들어 올린다(둥글게 휜 느낌).</summary>
    private static void Petal(Vector3 baseP, Vector3 dir, Vector3 side, float length, float width, int color, float lift = 0.0008f)
    {
        dir = dir.normalized; side = side.normalized;
        Vector3 up = Vector3.Cross(dir, side).normalized;
        if (up.y < 0f) up = -up;
        Vector3 mid = baseP + dir * length * 0.5f + up * lift;
        Vector3 l = mid + side * width * 0.5f, r = mid - side * width * 0.5f;
        Vector3 tip = baseP + dir * length;
        b.DoubleTri(baseP, l, r, color);
        b.DoubleTri(l, tip, r, color);
    }

    /// <summary>풀잎 한 장. 밑동에서 위로 자라다가 lean 쪽으로 휜다. 끝으로 갈수록 좁아진다.</summary>
    private static void Blade(Vector3 baseP, Vector3 lean, float height, float width, int color)
    {
        Vector3 side = Vector3.Cross(Vector3.up, lean.sqrMagnitude > 1e-6f ? lean : Vector3.forward).normalized;
        Vector3 Pt(float t) => baseP + Vector3.up * height * t + lean * height * 0.45f * t * t;
        float W(float t) => width * (1f - t * 0.8f) * 0.5f;
        const int segs = 3;
        for (int i = 0; i < segs; i++)
        {
            float t0 = i / (float)segs, t1 = (i + 1) / (float)segs;
            Vector3 a0 = Pt(t0) + side * W(t0), a1 = Pt(t0) - side * W(t0);
            Vector3 c0 = Pt(t1) + side * W(t1), c1 = Pt(t1) - side * W(t1);
            if (i == segs - 1) { b.DoubleTri(a0, Pt(1f), a1, color); }
            else { b.DoubleTri(a0, c0, c1, color); b.DoubleTri(a0, c1, a1, color); }
        }
    }

    private static Vector3 Radial(float angle) => new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

    // ───────── 모델 ─────────

    private static void Tulip()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.040f, 0f), 0.0014f, 0.0012f, 5, Green);
        Blade(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0.3f), 0.030f, 0.008f, LightGreen);
        Blade(new Vector3(0f, 0f, 0f), new Vector3(-0.9f, 0f, -0.4f), 0.026f, 0.007f, Green);
        // 컵 모양 꽃: 꽃잎 여섯 장이 위로 오므려 있다
        for (int k = 0; k < 6; k++)
        {
            float a = Mathf.PI * 2f * k / 6f;
            Vector3 dir = Radial(a) * 0.30f + Vector3.up;
            Vector3 side = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            Petal(new Vector3(0f, 0.038f, 0f) + Radial(a) * 0.0012f, dir, side, 0.021f, 0.0125f, k % 2 == 0 ? Red : Coral, 0.0016f);
        }
        Blob(new Vector3(0f, 0.038f, 0f), new Vector3(0.0025f, 0.002f, 0.0025f), 2, 6, Green);
        b.Write("deco_tulip");
    }

    private static void Sunflower()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.050f, 0f), 0.0020f, 0.0016f, 5, DeepGreen);
        Blade(new Vector3(0f, 0.004f, 0f), new Vector3(1f, 0f, 0f), 0.022f, 0.011f, Green);
        Blade(new Vector3(0f, 0.012f, 0f), new Vector3(-1f, 0f, 0.2f), 0.020f, 0.010f, LightGreen);
        // 꽃송이: 정면(-z)과 위를 향해 기울어져 있다
        b.xf = Matrix4x4.TRS(new Vector3(0f, 0.052f, 0f), Quaternion.Euler(-42f, 0f, 0f), Vector3.one);
        Frustum(b, new Vector3(0f, -0.002f, 0f), new Vector3(0f, 0.002f, 0f), 0.0105f, 0.0105f, 10, Brown, Dark);
        for (int k = 0; k < 14; k++)
        {
            float a = Mathf.PI * 2f * k / 14f;
            Vector3 r = Radial(a);
            Petal(r * 0.0095f + new Vector3(0f, 0.0012f, 0f), r, new Vector3(-r.z, 0f, r.x), 0.0125f, 0.0068f, k % 2 == 0 ? Gold : Yellow, 0.0003f);
        }
        b.xf = Matrix4x4.identity;
        b.Write("deco_sunflower");
    }

    private static void Lavender_()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.052f, 0f), 0.0013f, 0.0010f, 5, Leaf);
        for (int i = 0; i < 10; i++)
        {
            float y = 0.030f + i * 0.0023f, a = i * 1.9f;
            float spread = 0.0026f * (1f - i / 14f);
            Blob(new Vector3(0f, y, 0f) + Radial(a) * spread, new Vector3(0.0030f, 0.0032f, 0.0030f), 2, 5, i % 2 == 0 ? Purple : Lavender);
        }
        Blob(new Vector3(0f, 0.056f, 0f), new Vector3(0.0022f, 0.0035f, 0.0022f), 2, 5, Lavender);
        for (int k = 0; k < 3; k++)
            Blade(new Vector3(0f, 0.002f, 0f), Radial(k * 2.1f + 0.4f) * 0.8f, 0.028f, 0.0035f, Leaf);
        b.Write("deco_lavender");
    }

    private static void Wheat()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.050f, 0f), 0.0013f, 0.0011f, 5, Tan);
        for (int i = 0; i < 6; i++)
        {
            float y = 0.034f + i * 0.0026f, a = i * 1.05f;
            Vector3 r = Radial(a);
            Blob(new Vector3(0f, y, 0f) + r * 0.0021f, new Vector3(0.0016f, 0.0034f, 0.0016f), 2, 5, i % 2 == 0 ? Gold : Pale);
            Blob(new Vector3(0f, y, 0f) - r * 0.0021f, new Vector3(0.0016f, 0.0034f, 0.0016f), 2, 5, i % 2 == 0 ? Pale : Gold);
        }
        Blob(new Vector3(0f, 0.052f, 0f), new Vector3(0.0016f, 0.0040f, 0.0016f), 2, 5, Gold);
        Blade(new Vector3(0f, 0.004f, 0f), new Vector3(1f, 0f, 0.2f), 0.030f, 0.0045f, Leaf);
        Blade(new Vector3(0f, 0.010f, 0f), new Vector3(-1f, 0f, -0.1f), 0.026f, 0.0045f, Green);
        b.Write("deco_wheat");
    }

    private static void Cosmos()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.046f, 0f), 0.0012f, 0.0010f, 5, Green);
        Blade(new Vector3(0f, 0.004f, 0f), new Vector3(1f, 0f, 0.3f), 0.024f, 0.0028f, LightGreen);
        Blade(new Vector3(0f, 0.008f, 0f), new Vector3(-1f, 0f, 0f), 0.020f, 0.0028f, Green);
        b.xf = Matrix4x4.TRS(new Vector3(0f, 0.048f, 0f), Quaternion.Euler(-38f, 0f, 0f), Vector3.one);
        Frustum(b, new Vector3(0f, -0.0012f, 0f), new Vector3(0f, 0.0016f, 0f), 0.0035f, 0.0035f, 6, Yellow, Gold);
        for (int k = 0; k < 8; k++)
        {
            float a = Mathf.PI * 2f * k / 8f;
            Vector3 r = Radial(a);
            Petal(r * 0.0028f + new Vector3(0f, 0.0006f, 0f), r, new Vector3(-r.z, 0f, r.x), 0.0135f, 0.0092f, k % 2 == 0 ? Pink : Peach, 0.0004f);
        }
        b.xf = Matrix4x4.identity;
        b.Write("deco_cosmos");
    }

    private static void FourLeafClover()
    {
        b = new Builder();
        // 줄기째 비스듬히 기울여서, 위에서 본 네 잎이 잘 보이게 한다
        b.xf = Matrix4x4.Rotate(Quaternion.Euler(-48f, 0f, 0f));
        Frustum(b, Vector3.zero, new Vector3(0f, 0.014f, 0f), 0.0009f, 0.0008f, 5, Green);
        for (int k = 0; k < 4; k++)
        {
            float a = Mathf.PI * 0.5f * k + 0.785f;
            Vector3 r = Radial(a);
            // 잎 한 장은 납작한 하트 모양: 둥근 덩어리 둘을 나란히 놓는다. 가운데에서 바깥으로 갈수록 살짝 들린다.
            Vector3 side = new Vector3(-r.z, 0f, r.x);
            Vector3 c = new Vector3(0f, 0.014f, 0f) + r * 0.0064f + Vector3.up * 0.0010f;
            Blob(c + side * 0.0031f, new Vector3(0.0060f, 0.0011f, 0.0060f), 2, 6, k % 2 == 0 ? LightGreen : Green);
            Blob(c - side * 0.0031f, new Vector3(0.0060f, 0.0011f, 0.0060f), 2, 6, k % 2 == 0 ? Green : LightGreen);
        }
        b.xf = Matrix4x4.identity;
        b.Write("deco_four_leaf_clover");
    }

    private static void GrassTuft()
    {
        b = new Builder();
        int[] colors = { Green, LightGreen, Leaf, Moss };
        for (int i = 0; i < 12; i++)
        {
            float a = i * 2.4f;
            float ring = 0.002f + 0.0016f * (i % 3);
            float h = 0.032f + 0.028f * Mathf.Abs(Mathf.Sin(i * 1.7f));
            Blade(Radial(a) * ring, Radial(a) * (0.35f + 0.5f * ((i * 7) % 5) / 4f), h, 0.0050f, colors[i % 4]);
        }
        b.Write("deco_grass_tuft");
    }

    private static void Reed()
    {
        b = new Builder();
        float[] heights = { 0.054f, 0.062f, 0.048f, 0.058f };
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.6f + 0.3f;
            Vector3 bp = Radial(a) * 0.0025f;
            Vector3 top = bp + Radial(a) * 0.0045f + Vector3.up * heights[i];
            Frustum(b, bp, top, 0.0011f, 0.0007f, 4, Tan);
            Vector3 dir = (top - bp).normalized;
            // 끝의 솜털 이삭
            Blob(top - dir * 0.004f, new Vector3(0.0034f, 0.0075f, 0.0034f), 3, 6, i % 2 == 0 ? Cream : Sand);
        }
        for (int k = 0; k < 3; k++)
            Blade(new Vector3(0f, 0.001f, 0f), Radial(k * 2.2f + 1f) * 0.9f, 0.034f, 0.0038f, Leaf);
        b.Write("deco_reed");
    }

    // ───────── 날씨 재료와 반딧불이 ─────────

    private const int Ice = 33, White = 2, Cyan = 20, Teal = 28, SkyBlue = 19, Gray = 32;

    /// <summary>날씨에 따라 나타나는 재료 5종: 눈꽃, 안개꽃, 이슬방울, 바다 유리, 반딧불이.</summary>
    [MenuItem("유리병 정원/날씨 재료 모델 만들기")]
    public static string BuildWeatherModels()
    {
        SnowFlower(); MistFlower(); DewDrop(); SeaGlass(); Firefly();
        AssetDatabase.Refresh();
        return "날씨 재료 모델 5종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    private static void SnowFlower()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.040f, 0f), 0.0013f, 0.0011f, 5, DeepGreen);
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(1f, 0f, 0.2f), 0.022f, 0.007f, Green);
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(-0.9f, 0f, -0.3f), 0.018f, 0.006f, LightGreen);
        // 눈송이 모양 꽃: 길쭉한 흰 꽃잎 여섯 장 + 사이사이 작은 얼음빛 꽃잎 여섯 장
        b.xf = Matrix4x4.TRS(new Vector3(0f, 0.041f, 0f), Quaternion.Euler(-30f, 0f, 0f), Vector3.one);
        for (int k = 0; k < 6; k++)
        {
            float a = Mathf.PI * 2f * k / 6f;
            Vector3 r = Radial(a);
            Petal(r * 0.002f, r + Vector3.up * 0.12f, new Vector3(-r.z, 0f, r.x), 0.0185f, 0.0058f, k % 2 == 0 ? Ice : White, 0.0006f);
            float a2 = a + Mathf.PI / 6f;
            Vector3 r2 = Radial(a2);
            Petal(r2 * 0.002f, r2 + Vector3.up * 0.08f, new Vector3(-r2.z, 0f, r2.x), 0.0105f, 0.0048f, Cyan, 0.0004f);
        }
        Blob(new Vector3(0f, 0.0008f, 0f), new Vector3(0.0028f, 0.0022f, 0.0028f), 2, 6, Yellow);
        b.xf = Matrix4x4.identity;
        b.Write("deco_snow_flower");
    }

    private static void MistFlower()
    {
        b = new Builder();
        // 가는 줄기 여섯 개가 부채처럼 퍼지고, 끝마다 작고 하얀 꽃송이들이 뭉쳐 있다 (안개꽃)
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.05f + 0.2f;
            Vector3 top = Radial(a) * (0.007f + 0.004f * (i % 3)) + Vector3.up * (0.036f + 0.008f * (i % 3));
            Frustum(b, new Vector3(0f, 0f, 0f), top, 0.0009f, 0.0005f, 4, Moss);
            for (int k = 0; k < 4; k++)
            {
                Vector3 off = Radial(a + k * 1.6f) * 0.0045f + Vector3.up * (0.003f * (k % 2));
                Blob(top + off, new Vector3(0.0032f, 0.0032f, 0.0032f), 2, 5, k % 2 == 0 ? White : Ice);
            }
            Blob(top + Vector3.up * 0.004f, new Vector3(0.0030f, 0.0030f, 0.0030f), 2, 5, White);
        }
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(1f, 0f, 0.4f), 0.020f, 0.004f, Leaf);
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(-1f, 0f, -0.2f), 0.017f, 0.004f, Green);
        b.Write("deco_mist_flower");
    }

    private static void DewDrop()
    {
        b = new Builder();
        // 초록 잎 한 장 위에 맺힌 이슬방울
        Petal(new Vector3(-0.014f, 0.002f, 0f), new Vector3(1f, 0.12f, 0f), new Vector3(0f, 0f, 1f), 0.030f, 0.021f, Leaf, 0.0025f);
        Petal(new Vector3(-0.014f, 0.0022f, 0f), new Vector3(1f, 0.12f, 0.35f), new Vector3(-0.35f, 0f, 1f), 0.022f, 0.012f, Green, 0.0020f);
        Blob(new Vector3(0.001f, 0.0125f, 0f), new Vector3(0.0078f, 0.0095f, 0.0078f), 3, 7, Cyan);
        Blob(new Vector3(-0.0015f, 0.0158f, -0.0020f), new Vector3(0.0026f, 0.0026f, 0.0026f), 2, 5, Pale); // 반짝이는 하이라이트
        Blob(new Vector3(0.011f, 0.0085f, 0.007f), new Vector3(0.0035f, 0.0038f, 0.0035f), 2, 6, SkyBlue);   // 작은 물방울
        b.Write("deco_dew_drop");
    }

    private static void SeaGlass()
    {
        b = new Builder();
        // 파도에 닳아 둥글어진 유리 조각 세 개 (청록, 하늘, 연한 얼음빛)
        Blob(new Vector3(0f, 0.0052f, 0f), new Vector3(0.0135f, 0.0052f, 0.0100f), 3, 6, Teal);
        Blob(new Vector3(0.0165f, 0.0040f, 0.0070f), new Vector3(0.0082f, 0.0040f, 0.0072f), 3, 6, Cyan);
        Blob(new Vector3(-0.0150f, 0.0032f, 0.0085f), new Vector3(0.0062f, 0.0032f, 0.0052f), 3, 5, Ice);
        Blob(new Vector3(0.0020f, 0.0030f, -0.0120f), new Vector3(0.0070f, 0.0030f, 0.0050f), 3, 5, SkyBlue);
        b.Write("deco_sea_glass");
    }

    // ───────── 날씨 재료 더 8종 ─────────

    /// <summary>날씨 재료 8종 더: 빗물 이끼, 푸른 버섯, 청개구리(비 온 뒤 숲), 이슬 거미줄(안개 낀 날 숲), 바람 깃털, 민들레 홀씨(바람 부는 날 꽃 언덕), 번개 유리(폭풍우 치는 날 바닷가), 무지개 조약돌(비 갠 날 바닷가).</summary>
    [MenuItem("유리병 정원/날씨 재료 모델 더 만들기")]
    public static string BuildMoreWeatherModels()
    {
        RainMoss(); BlueMushroom(); TreeFrog(); SpiderWeb(); WindFeather(); DandelionPuff(); LightningGlass(); RainbowPebble();
        AssetDatabase.Refresh();
        return "날씨 재료 모델 8종을 더 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    /// <summary>얇은 끈 하나 (거미줄). side는 끈이 놓인 면 안에서 끈과 직각인 방향이라 앞에서 보면 선으로 보인다.</summary>
    private static void Strand(Vector3 p0, Vector3 p1, float width, int color)
    {
        Vector3 side = Vector3.Cross(p1 - p0, Vector3.forward).normalized * width * 0.5f;
        b.DoubleTri(p0 + side, p1 + side, p1 - side, color);
        b.DoubleTri(p0 + side, p1 - side, p0 - side, color);
    }

    /// <summary>띠마다 색이 다른 타원체 (무지개 조약돌). 위에서부터 한 줄(ring)씩 cols의 색을 쓴다.</summary>
    private static void BandBlob(Vector3 c, Vector3 r, int rings, int sec, int[] cols)
    {
        Vector3 P(int i, int j)
        {
            float th = Mathf.PI * i / rings, ph = Mathf.PI * 2f * j / sec;
            return c + new Vector3(r.x * Mathf.Sin(th) * Mathf.Cos(ph), r.y * Mathf.Cos(th), r.z * Mathf.Sin(th) * Mathf.Sin(ph));
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < sec; j++)
                b.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), cols[i % cols.Length], c);
    }

    private static void RainMoss()
    {
        b = new Builder();
        // 비를 머금은 이끼 방석: 초록 덩어리 여러 개가 겹치고, 꼭대기마다 물방울이 맺혀 있다
        Blob(new Vector3(0f, 0.0060f, 0f), new Vector3(0.0200f, 0.0075f, 0.0160f), 3, 8, Moss);
        Blob(new Vector3(0.0120f, 0.0050f, 0.0060f), new Vector3(0.0110f, 0.0060f, 0.0090f), 3, 7, Green);
        Blob(new Vector3(-0.0110f, 0.0045f, 0.0070f), new Vector3(0.0100f, 0.0055f, 0.0080f), 3, 7, LightGreen);
        Blob(new Vector3(-0.0020f, 0.0125f, -0.0040f), new Vector3(0.0110f, 0.0055f, 0.0090f), 3, 7, Leaf);
        Blob(new Vector3(0.0070f, 0.0115f, 0.0070f), new Vector3(0.0070f, 0.0045f, 0.0060f), 3, 6, LightGreen);
        Blob(new Vector3(-0.0040f, 0.0172f, -0.0040f), new Vector3(0.0024f, 0.0026f, 0.0024f), 2, 5, Cyan);
        Blob(new Vector3(0.0090f, 0.0150f, 0.0080f), new Vector3(0.0020f, 0.0022f, 0.0020f), 2, 5, Pale);
        Blob(new Vector3(-0.0100f, 0.0100f, 0.0060f), new Vector3(0.0020f, 0.0022f, 0.0020f), 2, 5, SkyBlue);
        Blob(new Vector3(0.0150f, 0.0105f, 0.0020f), new Vector3(0.0019f, 0.0020f, 0.0019f), 2, 5, Cyan);
        // 가는 포자 줄기 몇 가닥
        for (int i = 0; i < 4; i++)
        {
            Vector3 foot = new Vector3(-0.007f + i * 0.0055f, 0.011f, -0.007f + (i % 2) * 0.004f);
            Frustum(b, foot, foot + new Vector3(0.001f * (i - 1.5f), 0.008f, 0f), 0.0005f, 0.0004f, 4, DeepGreen);
            Blob(foot + new Vector3(0.0015f * (i - 1.5f), 0.0095f, 0f), new Vector3(0.0018f, 0.0020f, 0.0018f), 2, 5, i % 2 == 0 ? Pink : Pale);
        }
        b.Write("deco_rain_moss");
    }

    private static void BlueMushroomOne(Vector3 baseP, float stemH, float stemR, float capR, int cap, int spot)
    {
        Frustum(b, baseP, baseP + Vector3.up * stemH, stemR, stemR * 0.75f, 6, Cream);
        Vector3 c = baseP + Vector3.up * (stemH + capR * 0.12f);
        Blob(c, new Vector3(capR, capR * 0.6f, capR), 4, 8, cap);
        for (int k = 0; k < 3; k++)
        {
            Vector3 d = Radial(k * 2.1f + baseP.x * 80f);
            Blob(c + d * capR * 0.52f + Vector3.up * capR * 0.42f, new Vector3(capR * 0.2f, capR * 0.12f, capR * 0.2f), 2, 5, spot);
        }
    }

    private static void BlueMushroom()
    {
        b = new Builder();
        BlueMushroomOne(new Vector3(0f, 0f, 0f), 0.021f, 0.0030f, 0.0150f, Teal, Pale);
        BlueMushroomOne(new Vector3(0.0170f, 0f, 0.0060f), 0.012f, 0.0022f, 0.0092f, SkyBlue, Ice);
        BlueMushroomOne(new Vector3(-0.0130f, 0f, 0.0100f), 0.009f, 0.0018f, 0.0072f, Teal, Ice);
        b.Write("deco_blue_mushroom");
    }

    private static void TreeFrog()
    {
        b = new Builder();
        // 앞(+x)을 보고 앉은 청개구리
        Blob(new Vector3(0f, 0.0085f, 0f), new Vector3(0.0115f, 0.0075f, 0.0105f), 4, 8, Green);
        Blob(new Vector3(0.0020f, 0.0045f, 0f), new Vector3(0.0090f, 0.0035f, 0.0085f), 3, 8, Cream);                 // 배
        Blob(new Vector3(0.0095f, 0.0105f, 0f), new Vector3(0.0062f, 0.0055f, 0.0072f), 3, 8, Green);                  // 머리
        for (int s = -1; s <= 1; s += 2)
        {
            Blob(new Vector3(0.0100f, 0.0158f, 0.0046f * s), new Vector3(0.0024f, 0.0024f, 0.0024f), 3, 6, White);     // 눈
            Blob(new Vector3(0.0120f, 0.0160f, 0.0046f * s), new Vector3(0.0013f, 0.0016f, 0.0013f), 2, 5, Dark);      // 눈동자
            Blob(new Vector3(0.0075f, 0.0025f, 0.0078f * s), new Vector3(0.0045f, 0.0025f, 0.0022f), 2, 6, LightGreen); // 앞다리
            Blob(new Vector3(0.0105f, 0.0016f, 0.0092f * s), new Vector3(0.0030f, 0.0014f, 0.0030f), 2, 6, LightGreen); // 앞발
            Blob(new Vector3(-0.0070f, 0.0040f, 0.0100f * s), new Vector3(0.0085f, 0.0040f, 0.0034f), 3, 6, Green);    // 뒷다리
            Blob(new Vector3(0.0000f, 0.0016f, 0.0115f * s), new Vector3(0.0050f, 0.0016f, 0.0034f), 2, 6, LightGreen); // 뒷발
            Blob(new Vector3(0.0125f, 0.0085f, 0.0050f * s), new Vector3(0.0018f, 0.0018f, 0.0018f), 2, 5, Coral);      // 볼
        }
        Blob(new Vector3(0.0010f, 0.0145f, 0.0030f), new Vector3(0.0020f, 0.0010f, 0.0020f), 2, 5, LightGreen);        // 등 얼룩
        Blob(new Vector3(-0.0030f, 0.0140f, -0.0030f), new Vector3(0.0022f, 0.0010f, 0.0022f), 2, 5, DeepGreen);
        b.Write("deco_tree_frog");
    }

    private static void SpiderWeb()
    {
        b = new Builder();
        // 나뭇가지 두 개 사이에 걸린 거미줄. 줄마다 이슬이 맺혀 있다 (앞에서 볼 때 가장 예쁘다)
        Vector3 c = new Vector3(0f, 0.027f, 0f);
        Frustum(b, new Vector3(-0.0215f, 0f, 0f), new Vector3(-0.0185f, 0.044f, 0f), 0.0017f, 0.0011f, 5, Brown);
        Frustum(b, new Vector3(0.0215f, 0f, 0f), new Vector3(0.0185f, 0.044f, 0f), 0.0017f, 0.0011f, 5, Brown);
        const float R = 0.0165f;
        const int spokes = 8;
        for (int i = 0; i < spokes; i++)
        {
            float a = Mathf.PI * 2f * i / spokes;
            Strand(c, c + new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0f), 0.0011f, White);
        }
        float[] rings = { 0.30f, 0.55f, 0.80f, 1.0f };
        for (int rg = 0; rg < rings.Length; rg++)
            for (int i = 0; i < spokes; i++)
            {
                float a0 = Mathf.PI * 2f * i / spokes, a1 = Mathf.PI * 2f * (i + 1) / spokes;
                float rr = R * rings[rg];
                Strand(c + new Vector3(Mathf.Cos(a0) * rr, Mathf.Sin(a0) * rr, 0f), c + new Vector3(Mathf.Cos(a1) * rr, Mathf.Sin(a1) * rr, 0f), 0.0009f, Pale);
            }
        // 가지에 거는 줄
        Strand(c + new Vector3(R, 0f, 0f), new Vector3(0.0198f, 0.030f, 0f), 0.0011f, White);
        Strand(c + new Vector3(-R, 0f, 0f), new Vector3(-0.0198f, 0.030f, 0f), 0.0011f, White);
        // 이슬
        for (int i = 0; i < spokes; i += 2)
        {
            float a = Mathf.PI * 2f * (i + 0.5f) / spokes + 0.2f;
            float rr = R * (i % 4 == 0 ? 0.80f : 0.55f);
            Blob(c + new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0f), new Vector3(0.0019f, 0.0022f, 0.0019f), 2, 5, i % 4 == 0 ? Cyan : Ice);
        }
        Blob(c + new Vector3(0.0035f, -0.0050f, 0f), new Vector3(0.0022f, 0.0026f, 0.0022f), 2, 5, Cyan);
        b.Write("deco_spider_web");
    }

    private static void WindFeather()
    {
        b = new Builder();
        // 바람에 날려 온 깃털 한 장이 흙에 꽂힌 모습: 살짝 휜 깃대와 양쪽의 깃
        Vector3 Pt(float t) => new Vector3(0.012f * t * t, 0.048f * t, 0f);
        const int segs = 8;
        for (int i = 0; i < segs; i++)
            Frustum(b, Pt(i / (float)segs), Pt((i + 1) / (float)segs), 0.0011f * (1f - i / (float)segs * 0.7f), 0.0011f * (1f - (i + 1) / (float)segs * 0.7f), 4, i < 2 ? Cream : Tan);
        int[] cols = { Cream, White, Sand, White };
        for (int s = -1; s <= 1; s += 2)
        {
            for (int i = 2; i < segs; i++)
            {
                float t0 = i / (float)segs, t1 = (i + 1) / (float)segs;
                float w0 = Mathf.Sin(Mathf.PI * (t0 - 0.2f) / 0.82f) * 0.0085f, w1 = Mathf.Sin(Mathf.PI * Mathf.Min(t1 - 0.2f, 0.82f) / 0.82f) * 0.0085f;
                Vector3 a0 = Pt(t0), a1 = Pt(t1);
                Vector3 e0 = a0 + new Vector3(s * w0, 0.0040f, 0f), e1 = a1 + new Vector3(s * w1, 0.0040f, 0f); // 깃이 앞(-z)에서 넓게 보이도록 좌우(x)로 편다
                int col = cols[i % cols.Length];
                b.DoubleTri(a0, a1, e1, col);
                b.DoubleTri(a0, e1, e0, col);
            }
            // 깃털 끝으로 모이는 뾰족한 끝
            b.DoubleTri(Pt(1f) + new Vector3(0f, 0.002f, 0f), Pt(0.9f) + new Vector3(s * 0.0010f, 0.0040f, 0f), Pt(0.9f), Cream);
        }
        // 깃털에 묻은 작은 하늘빛 줄무늬
        Blob(Pt(0.55f) + new Vector3(0.0050f, 0.0020f, -0.0006f), new Vector3(0.0016f, 0.0016f, 0.0010f), 2, 5, SkyBlue);
        Blob(Pt(0.55f) + new Vector3(-0.0050f, 0.0020f, -0.0006f), new Vector3(0.0016f, 0.0016f, 0.0010f), 2, 5, SkyBlue);
        b.Write("deco_wind_feather");
    }

    private static void DandelionPuff()
    {
        b = new Builder();
        // 솜털 공이 된 민들레 홀씨: 줄기 끝에 씨앗 십여 개가 공처럼 퍼져 있다
        Frustum(b, Vector3.zero, new Vector3(0f, 0.032f, 0f), 0.0013f, 0.0011f, 5, Green);
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(1f, 0f, 0.3f), 0.016f, 0.0065f, Leaf);
        Blade(new Vector3(0f, 0.001f, 0f), new Vector3(-0.9f, 0f, -0.4f), 0.013f, 0.0055f, Green);
        Vector3 c = new Vector3(0f, 0.040f, 0f);
        Blob(c, new Vector3(0.0036f, 0.0036f, 0.0036f), 2, 6, Tan);
        const int n = 18;
        for (int i = 0; i < n; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / n;
            float rad = Mathf.Sqrt(1f - y * y), ph = i * 2.39996f;
            Vector3 d = new Vector3(Mathf.Cos(ph) * rad, y, Mathf.Sin(ph) * rad);
            Frustum(b, c + d * 0.0030f, c + d * 0.0140f, 0.00040f, 0.00025f, 3, i % 3 == 0 ? Cream : Pale);
            Blob(c + d * 0.0148f, new Vector3(0.0022f, 0.0016f, 0.0022f), 2, 5, White);
        }
        b.Write("deco_dandelion_puff");
    }

    private static void LightningGlass()
    {
        b = new Builder();
        // 번개가 모래에 내리꽂혀 녹아 굳은 유리관(섬전암): 지그재그 줄기와 곁가지
        Vector3[] p =
        {
            new Vector3(0.0000f, 0.000f, 0.000f), new Vector3(0.0040f, 0.011f, 0.0020f), new Vector3(-0.0030f, 0.022f, 0.0010f),
            new Vector3(0.0035f, 0.033f, -0.0020f), new Vector3(-0.0005f, 0.045f, 0.0020f)
        };
        for (int i = 0; i < p.Length - 1; i++)
        {
            float r0 = 0.0050f * (1f - i * 0.18f), r1 = 0.0050f * (1f - (i + 1) * 0.18f);
            Frustum(b, p[i], p[i + 1], r0, r1, 5, i % 2 == 0 ? Ice : Pale, Sand);
            Blob(p[i + 1], new Vector3(r1 * 1.35f, r1 * 0.9f, r1 * 1.35f), 2, 5, Sand);                    // 모래가 달라붙은 마디
        }
        Frustum(b, p[1], new Vector3(0.0160f, 0.020f, 0.0010f), 0.0026f, 0.0013f, 4, Ice, Sand);
        Blob(new Vector3(0.0165f, 0.0205f, 0.0010f), new Vector3(0.0020f, 0.0016f, 0.0020f), 2, 5, Pale);
        Frustum(b, p[2], new Vector3(-0.0150f, 0.030f, 0.0040f), 0.0024f, 0.0012f, 4, Pale, Sand);
        Blob(new Vector3(-0.0155f, 0.0305f, 0.0040f), new Vector3(0.0018f, 0.0015f, 0.0018f), 2, 5, Ice);
        Blob(new Vector3(0f, 0.0015f, 0f), new Vector3(0.0105f, 0.0030f, 0.0095f), 3, 7, Sand);            // 밑동의 모래 덩어리
        Blob(p[4] + new Vector3(0f, 0.0020f, 0f), new Vector3(0.0022f, 0.0030f, 0.0022f), 2, 5, Yellow);   // 끝에 남은 번갯불 색
        b.Write("deco_lightning_glass");
    }

    private static void RainbowPebble()
    {
        b = new Builder();
        // 무지개 빛깔 띠가 둘러진 매끈한 조약돌 (띠가 비스듬히 지나가게 기울여 만든다)
        b.xf = Matrix4x4.TRS(new Vector3(0f, 0.0115f, 0f), Quaternion.Euler(0f, 0f, 28f), Vector3.one);
        BandBlob(Vector3.zero, new Vector3(0.0170f, 0.0105f, 0.0130f), 6, 10, new[] { Red, Carrot, Yellow, Green, SkyBlue, Purple });
        b.xf = Matrix4x4.identity;
        b.Write("deco_rainbow_pebble");
    }

    // ───────── 사막 섬 재료 14종 ─────────

    private const int Mid = 4, CamelTan = 6, DarkTan = 8, Orange = 24, Terra = 29;

    /// <summary>사막 섬 재료 14종: 선인장, 대추야자, 사암 조각, 사막 장미, 화석, 선인장 꽃, 알로에 잎, 마른 가지, 옛 도자기 조각, 풍뎅이, 낙타, 야자나무, 사막 천막, 작은 피라미드. 야자나무, 천막, 피라미드, 낙타는 사막 섬 풍경에도 크게 키워서 쓴다.</summary>
    [MenuItem("유리병 정원/사막 재료 모델 만들기")]
    public static string BuildDesertModels()
    {
        CactusPiece(); DateFruit(); Sandstone(); DesertRose(); Fossil(); CactusFlower(); AloeLeaf(); DryBranch(); PotteryShard(); Scarab();
        Camel(); PalmTree(); DesertTent(); MiniPyramid(); StepPyramid();
        AssetDatabase.Refresh();
        return "사막 재료 모델 14종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    private static void CactusPiece()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.034f, 0f), 0.0065f, 0.0058f, 8, Green);
        Blob(new Vector3(0f, 0.034f, 0f), new Vector3(0.0058f, 0.0050f, 0.0058f), 3, 8, LightGreen);
        Frustum(b, new Vector3(0.005f, 0.016f, 0f), new Vector3(0.0155f, 0.020f, 0f), 0.0034f, 0.0034f, 6, Green);
        Frustum(b, new Vector3(0.0155f, 0.019f, 0f), new Vector3(0.0165f, 0.031f, 0f), 0.0033f, 0.0030f, 6, Green);
        Blob(new Vector3(0.0165f, 0.031f, 0f), new Vector3(0.0031f, 0.0030f, 0.0031f), 3, 6, LightGreen);
        Frustum(b, new Vector3(-0.005f, 0.022f, 0f), new Vector3(-0.0135f, 0.025f, 0f), 0.0030f, 0.0030f, 6, Green);
        Frustum(b, new Vector3(-0.0135f, 0.024f, 0f), new Vector3(-0.0142f, 0.032f, 0f), 0.0029f, 0.0027f, 6, Green);
        Blob(new Vector3(-0.0142f, 0.032f, 0f), new Vector3(0.0028f, 0.0027f, 0.0028f), 3, 6, LightGreen);
        for (int i = 0; i < 10; i++) // 가시
            Blob(Radial(i * 0.9f) * 0.0068f + Vector3.up * (0.004f + i * 0.0029f), new Vector3(0.0007f, 0.0009f, 0.0007f), 2, 4, Cream);
        Blob(new Vector3(0f, 0.0385f, 0f), new Vector3(0.0022f, 0.0018f, 0.0022f), 2, 5, Pink);
        b.Write("deco_cactus_piece");
    }

    private static void DateFruit()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.030f, 0f), 0.0012f, 0.0010f, 5, Brown);
        for (int k = 0; k < 3; k++)
        {
            float a = k * 2.1f + 0.4f;
            Petal(new Vector3(0f, 0.030f, 0f), Radial(a) + Vector3.up * 0.7f, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), 0.020f, 0.0065f, k % 2 == 0 ? Green : Leaf, 0.0010f);
        }
        for (int k = 0; k < 8; k++)
        {
            float a = k * 0.8f;
            Vector3 pos = new Vector3(0f, 0.025f, 0f) + Radial(a) * 0.0070f + Vector3.up * (-0.004f * (k % 3));
            Blob(pos, new Vector3(0.0032f, 0.0055f, 0.0032f), 3, 6, k % 3 == 0 ? Orange : (k % 2 == 0 ? Mid : Brown));
        }
        b.Write("deco_date_fruit");
    }

    private static void Sandstone()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0040f, 0f), new Vector3(0.0170f, 0.0045f, 0.0130f), 3, 8, Sand);
        Blob(new Vector3(0.0010f, 0.0105f, 0.0010f), new Vector3(0.0140f, 0.0040f, 0.0110f), 3, 8, CamelTan);
        Blob(new Vector3(0.0020f, 0.0160f, -0.0010f), new Vector3(0.0105f, 0.0038f, 0.0085f), 3, 7, DarkTan);
        Blob(new Vector3(-0.0005f, 0.0205f, 0f), new Vector3(0.0065f, 0.0030f, 0.0055f), 3, 6, Sand);
        b.Write("deco_sandstone");
    }

    private static void DesertRose()
    {
        b = new Builder();
        for (int k = 0; k < 8; k++)
        {
            float a = k * 0.785f + 0.3f;
            Vector3 pos = Radial(a) * 0.0070f + Vector3.up * (0.0060f + (k % 3) * 0.0030f);
            b.xf = Matrix4x4.TRS(pos, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 55f - (k % 3) * 10f), Vector3.one);
            Blob(Vector3.zero, new Vector3(0.0085f, 0.0013f, 0.0060f), 2, 6, k % 2 == 0 ? Peach : Cream);
            b.xf = Matrix4x4.identity;
        }
        Blob(new Vector3(0f, 0.0080f, 0f), new Vector3(0.0060f, 0.0040f, 0.0060f), 3, 6, Sand);
        b.Write("deco_desert_rose");
    }

    private static void Fossil()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0035f, 0f), new Vector3(0.0165f, 0.0035f, 0.0150f), 3, 8, DarkTan);
        for (int i = 0; i < 26; i++)
        {
            float t = i / 25f;
            float ang = t * Mathf.PI * 5f, rad = 0.0017f + t * 0.0100f, s = 0.0017f + 0.0012f * t;
            Blob(Radial(ang) * rad + Vector3.up * 0.0062f, new Vector3(s, 0.0014f, s), 2, 5, i % 2 == 0 ? Cream : Sand);
        }
        b.Write("deco_fossil");
    }

    private static void CactusFlower()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0085f, 0f), new Vector3(0.0095f, 0.0085f, 0.0095f), 4, 8, Green);
        for (int i = 0; i < 8; i++)
            Blob(Radial(i * 0.8f) * 0.0094f + Vector3.up * (0.004f + (i % 3) * 0.0035f), new Vector3(0.0007f, 0.0009f, 0.0007f), 2, 4, Cream);
        b.xf = Matrix4x4.TRS(new Vector3(0f, 0.0165f, 0f), Quaternion.identity, Vector3.one);
        for (int k = 0; k < 7; k++)
        {
            float a = Mathf.PI * 2f * k / 7f;
            Vector3 r = Radial(a);
            Petal(r * 0.0015f, r + Vector3.up * 0.35f, new Vector3(-r.z, 0f, r.x), 0.0085f, 0.0045f, k % 2 == 0 ? Pink : Coral, 0.0004f);
        }
        Blob(new Vector3(0f, 0.0006f, 0f), new Vector3(0.0022f, 0.0016f, 0.0022f), 2, 5, Yellow);
        b.xf = Matrix4x4.identity;
        b.Write("deco_cactus_flower");
    }

    private static void AloeLeaf()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0020f, 0f), new Vector3(0.0050f, 0.0030f, 0.0050f), 2, 6, Moss);
        for (int k = 0; k < 9; k++)
        {
            bool outer = k < 6;
            float a = k * 2.399f;
            Vector3 r = Radial(a);
            Petal(r * 0.002f + Vector3.up * 0.002f, r + Vector3.up * (outer ? 0.9f : 2.2f), new Vector3(-r.z, 0f, r.x), outer ? 0.030f : 0.026f, 0.0085f,
                  k % 3 == 0 ? LightGreen : (k % 3 == 1 ? Green : Leaf), 0.0010f);
        }
        b.Write("deco_aloe_leaf");
    }

    private static void DryBranch()
    {
        b = new Builder();
        Vector3[] p =
        {
            new Vector3(0f, 0f, 0f), new Vector3(0.003f, 0.010f, 0.002f), new Vector3(-0.002f, 0.020f, 0.001f),
            new Vector3(0.004f, 0.031f, -0.001f), new Vector3(0.001f, 0.042f, 0.002f)
        };
        for (int i = 0; i < p.Length - 1; i++)
            Frustum(b, p[i], p[i + 1], 0.0030f * (1f - i * 0.15f), 0.0030f * (1f - (i + 1) * 0.15f), 5, i % 2 == 0 ? Tan : DarkTan);
        Frustum(b, p[2], new Vector3(-0.0130f, 0.030f, 0.0020f), 0.0016f, 0.0009f, 4, DarkTan);
        Frustum(b, p[3], new Vector3(0.0140f, 0.040f, -0.0020f), 0.0014f, 0.0008f, 4, Tan);
        Frustum(b, new Vector3(-0.0130f, 0.030f, 0.0020f), new Vector3(-0.0190f, 0.036f, 0.0030f), 0.0009f, 0.0005f, 3, Tan);
        Blob(new Vector3(0f, 0.0015f, 0f), new Vector3(0.0050f, 0.0025f, 0.0050f), 2, 6, DarkTan);
        b.Write("deco_dry_branch");
    }

    private static void PotteryShard()
    {
        b = new Builder();
        // 항아리에서 떨어진 둥글게 휜 조각: 테라코타 색에 크림색 띠와 검은 지그재그 무늬
        const float R = 0.016f;
        const int segs = 8;
        float[] ys = { 0f, 0.007f, 0.011f, 0.016f, 0.018f, 0.024f };
        int[] cols = { Terra, Cream, Terra, Dark, Terra };
        b.xf = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-14f, 0f, 0f), Vector3.one);
        for (int i = 0; i < segs; i++)
        {
            float a0 = Mathf.Deg2Rad * (-55f + 110f * i / segs), a1 = Mathf.Deg2Rad * (-55f + 110f * (i + 1) / segs);
            for (int band = 0; band < cols.Length; band++)
            {
                int col = cols[band];
                if (band == 3 && i % 2 == 1) col = Terra; // 지그재그
                Vector3 p00 = Radial(a0) * R + Vector3.up * ys[band], p10 = Radial(a1) * R + Vector3.up * ys[band];
                Vector3 p01 = Radial(a0) * R + Vector3.up * ys[band + 1], p11 = Radial(a1) * R + Vector3.up * ys[band + 1];
                b.DoubleTri(p00, p10, p11, col);
                b.DoubleTri(p00, p11, p01, col);
            }
        }
        b.xf = Matrix4x4.identity;
        b.Write("deco_pottery_shard");
    }

    private static void Scarab()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0048f, 0.0030f), new Vector3(0.0105f, 0.0048f, 0.0042f), 3, 8, Teal);
        Blob(new Vector3(0f, 0.0048f, -0.0030f), new Vector3(0.0105f, 0.0048f, 0.0042f), 3, 8, SkyBlue);
        Blob(new Vector3(0f, 0.0052f, 0f), new Vector3(0.0105f, 0.0042f, 0.0007f), 2, 6, Dark);
        Blob(new Vector3(0.0108f, 0.0030f, 0f), new Vector3(0.0042f, 0.0030f, 0.0050f), 2, 6, Dark);
        for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 3; k++)
            {
                float x = -0.004f + k * 0.0045f;
                Frustum(b, new Vector3(x, 0.0025f, s * 0.0050f), new Vector3(x + (k - 1) * 0.0015f, 0.0003f, s * 0.0110f), 0.0004f, 0.0003f, 3, Dark);
            }
        Blob(new Vector3(-0.0020f, 0.0092f, 0f), new Vector3(0.0018f, 0.0012f, 0.0018f), 2, 5, Gold);
        b.Write("deco_scarab");
    }

    private static void Camel()
    {
        b = new Builder();
        foreach (float x in new[] { 0.0140f, -0.0140f })
            foreach (float z in new[] { 0.0055f, -0.0055f })
            {
                Frustum(b, new Vector3(x, 0f, z), new Vector3(x * 0.95f, 0.026f, z * 0.8f), 0.0022f, 0.0030f, 5, Mid);
                Blob(new Vector3(x, 0.0008f, z), new Vector3(0.0028f, 0.0010f, 0.0028f), 2, 5, Dark);
            }
        Blob(new Vector3(0f, 0.0320f, 0f), new Vector3(0.0215f, 0.0105f, 0.0095f), 4, 8, CamelTan);
        Blob(new Vector3(-0.0020f, 0.0435f, 0f), new Vector3(0.0075f, 0.0075f, 0.0075f), 3, 7, CamelTan);
        Blob(new Vector3(0.0105f, 0.0425f, 0f), new Vector3(0.0065f, 0.0070f, 0.0065f), 3, 7, CamelTan);
        Blob(new Vector3(0.0040f, 0.0390f, 0f), new Vector3(0.0058f, 0.0035f, 0.0108f), 2, 6, Red);
        Frustum(b, new Vector3(0.0190f, 0.0360f, 0f), new Vector3(0.0300f, 0.0560f, 0f), 0.0045f, 0.0032f, 6, CamelTan);
        Blob(new Vector3(0.0345f, 0.0585f, 0f), new Vector3(0.0072f, 0.0042f, 0.0036f), 3, 6, CamelTan);
        Blob(new Vector3(0.0405f, 0.0565f, 0f), new Vector3(0.0035f, 0.0030f, 0.0032f), 2, 5, Sand);
        Blob(new Vector3(0.0348f, 0.0610f, 0.0030f), new Vector3(0.0010f, 0.0018f, 0.0010f), 2, 4, Mid);
        Blob(new Vector3(0.0348f, 0.0610f, -0.0030f), new Vector3(0.0010f, 0.0018f, 0.0010f), 2, 4, Mid);
        Blob(new Vector3(0.0378f, 0.0594f, 0.0031f), new Vector3(0.0007f, 0.0007f, 0.0007f), 2, 4, Dark);
        Blob(new Vector3(0.0378f, 0.0594f, -0.0031f), new Vector3(0.0007f, 0.0007f, 0.0007f), 2, 4, Dark);
        Frustum(b, new Vector3(-0.0205f, 0.0360f, 0f), new Vector3(-0.0245f, 0.0260f, 0f), 0.0012f, 0.0008f, 4, Mid);
        b.Write("deco_camel");
    }

    private static void PalmTree()
    {
        b = new Builder();
        Vector3[] p =
        {
            new Vector3(0f, 0f, 0f), new Vector3(0.003f, 0.015f, 0f), new Vector3(0.0065f, 0.030f, 0.001f),
            new Vector3(0.007f, 0.046f, 0.002f), new Vector3(0.0055f, 0.060f, 0.002f)
        };
        for (int i = 0; i < p.Length - 1; i++)
            Frustum(b, p[i], p[i + 1], 0.0032f - i * 0.0004f, 0.0032f - (i + 1) * 0.0004f, 6, i % 2 == 0 ? Mid : DarkTan);
        Vector3 top = p[p.Length - 1];
        for (int k = 0; k < 8; k++)
        {
            float a = Mathf.PI * 2f * k / 8f + 0.2f;
            Vector3 r = Radial(a);
            Vector3 side = new Vector3(-r.z, 0f, r.x);
            Vector3 d1 = (r + Vector3.up * 0.35f).normalized;
            int col = k % 2 == 0 ? Green : Leaf;
            Petal(top, d1, side, 0.016f, 0.0075f, col, 0.0008f);
            Vector3 tip = top + d1 * 0.016f;
            Petal(tip, (r - Vector3.up * 0.55f).normalized, side, 0.015f, 0.0055f, k % 2 == 0 ? LightGreen : Green, 0.0005f);
        }
        for (int k = 0; k < 3; k++)
            Blob(top + Radial(k * 2.1f) * 0.0030f + Vector3.down * 0.0030f, new Vector3(0.0028f, 0.0030f, 0.0028f), 2, 6, Brown);
        b.Write("deco_palm_tree");
    }

    private static void DesertTent()
    {
        b = new Builder();
        // 유목민의 천막: 붉은 줄무늬 지붕, 어두운 입구, 양쪽 기둥
        Vector3 A = new Vector3(-0.017f, 0f, -0.015f), B = new Vector3(0.017f, 0f, -0.015f), C = new Vector3(0.017f, 0f, 0.015f), D = new Vector3(-0.017f, 0f, 0.015f);
        Vector3 R0 = new Vector3(-0.019f, 0.026f, 0f), R1 = new Vector3(0.019f, 0.026f, 0f), inside = new Vector3(0f, 0.01f, 0f);
        for (int i = 0; i < 5; i++)
        {
            float t0 = i / 5f, t1 = (i + 1) / 5f;
            int col = i % 2 == 0 ? Cream : Red;
            b.Quad(Vector3.Lerp(A, B, t0), Vector3.Lerp(A, B, t1), Vector3.Lerp(R0, R1, t1), Vector3.Lerp(R0, R1, t0), col, inside);
            b.Quad(Vector3.Lerp(D, C, t0), Vector3.Lerp(D, C, t1), Vector3.Lerp(R0, R1, t1), Vector3.Lerp(R0, R1, t0), col, inside);
        }
        b.Tri(A, D, R0, Cream, inside);
        b.Tri(B, C, R1, Cream, inside);
        b.Tri(new Vector3(0.0172f, 0f, -0.0060f), new Vector3(0.0172f, 0f, 0.0060f), new Vector3(0.0172f, 0.0165f, 0f), Dark, inside);
        foreach (float x in new[] { -0.019f, 0.019f })
            Frustum(b, new Vector3(x, 0.024f, 0f), new Vector3(x, 0.034f, 0f), 0.0009f, 0.0007f, 4, Brown);
        b.Tri(new Vector3(-0.019f, 0.034f, 0f), new Vector3(-0.019f, 0.029f, 0f), new Vector3(-0.0115f, 0.0315f, 0f), Red, inside);
        b.Write("deco_desert_tent");
    }

    private static void Pyr(Vector3 center, float s, float h, int c0, int c1, int c2, int c3)
    {
        Vector3[] k = { center + new Vector3(-s, 0f, -s), center + new Vector3(s, 0f, -s), center + new Vector3(s, 0f, s), center + new Vector3(-s, 0f, s) };
        Vector3 apex = center + new Vector3(0f, h, 0f), inside = center + new Vector3(0f, h * 0.35f, 0f);
        b.Tri(k[0], k[1], apex, c0, inside);
        b.Tri(k[1], k[2], apex, c1, inside);
        b.Tri(k[2], k[3], apex, c2, inside);
        b.Tri(k[3], k[0], apex, c3, inside);
    }

    /// <summary>사막 섬 풍경용 계단식 피라미드: 열두 단이 쌓여 있고 앞쪽 아래에 어두운 입구가 있다. (섬에는 키 16m로 키워서 놓는다)</summary>
    private static void StepPyramid()
    {
        b = new Builder();
        const int tiers = 12;
        const float s0 = 0.020f, th = 0.0023f;
        for (int i = 0; i < tiers; i++)
        {
            float s = s0 * (1f - i / (float)tiers * 0.92f);
            float y0 = i * th, y1 = y0 + th;
            Vector3 inside = new Vector3(0f, y0 + th * 0.5f, 0f);
            Vector3 a0 = new Vector3(-s, y0, -s), b0 = new Vector3(s, y0, -s), c0 = new Vector3(s, y0, s), d0 = new Vector3(-s, y0, s);
            Vector3 a1 = new Vector3(-s, y1, -s), b1 = new Vector3(s, y1, -s), c1 = new Vector3(s, y1, s), d1 = new Vector3(-s, y1, s);
            bool alt = i % 2 == 0;
            b.Quad(a0, b0, b1, a1, alt ? Sand : Cream, inside);   // 앞(-z)
            b.Quad(b0, c0, c1, b1, alt ? Tan : Sand, inside);     // 오른쪽
            b.Quad(c0, d0, d1, c1, alt ? DarkTan : Tan, inside);  // 뒤
            b.Quad(d0, a0, a1, d1, alt ? Cream : Sand, inside);   // 왼쪽
            b.Quad(a1, b1, c1, d1, Pale, inside);                 // 윗면 (다음 단에 가려지고 둘레만 보인다)
        }
        // 입구: 맨 아래 앞면에 어둡게 파인 문
        Vector3 ins = new Vector3(0f, 0.004f, 0f);
        b.Quad(new Vector3(-0.0032f, 0f, -s0 - 0.0003f), new Vector3(0.0032f, 0f, -s0 - 0.0003f), new Vector3(0.0032f, 0.0021f, -s0 - 0.0003f), new Vector3(-0.0032f, 0.0021f, -s0 - 0.0003f), Dark, ins);
        b.Write("deco_pyramid_steps");
    }

    private static void MiniPyramid()
    {
        b = new Builder();
        Pyr(Vector3.zero, 0.020f, 0.026f, Sand, Tan, DarkTan, Cream);
        Pyr(new Vector3(0.0300f, 0f, -0.0110f), 0.0110f, 0.0145f, Sand, Tan, DarkTan, Cream);
        Pyr(new Vector3(0.0200f, 0f, 0.0200f), 0.0075f, 0.0100f, Tan, Sand, Cream, DarkTan);
        b.Tri(new Vector3(-0.0045f, 0f, -0.0202f), new Vector3(0.0045f, 0f, -0.0202f), new Vector3(0f, 0.0070f, -0.0185f), Dark, new Vector3(0f, 0.01f, 0f)); // 입구
        b.Write("deco_pyramid");
    }

    // ───────── 눈 섬 재료 11종 ─────────

    private const int Carrot = 15;

    /// <summary>눈 섬에서 얻는 재료 11종: 솔잎 가지, 눈 덮인 솔방울, 장작, 얼음 조각, 고드름, 호랑가시 열매, 서리 낀 잎, 눈덩이, 미니 눈사람, 겨울 방울, 벙어리장갑.</summary>
    [MenuItem("유리병 정원/눈 섬 재료 모델 만들기")]
    public static string BuildWinterModels()
    {
        PineSprig(); SnowyPinecone(); Firewood(); IceShard(); Icicle(); HollyBerry(); FrostLeaf(); Snowball(); MiniSnowman(); WinterBell(); Mitten();
        AssetDatabase.Refresh();
        return "눈 섬 재료 모델 11종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    private static void PineSprig()
    {
        b = new Builder();
        Frustum(b, new Vector3(-0.026f, 0.0035f, 0f), new Vector3(0.026f, 0.0075f, 0f), 0.0012f, 0.0006f, 4, Brown);
        for (int i = 0; i < 8; i++)
        {
            float x = -0.020f + i * 0.0058f, y = 0.0035f + (x + 0.026f) / 0.052f * 0.004f;
            float len = 0.016f - i * 0.0009f;
            Blade(new Vector3(x, y, 0.0005f), new Vector3(0f, 0.15f, 1f), len, 0.0042f, i % 2 == 0 ? DeepGreen : Green);
            Blade(new Vector3(x + 0.002f, y, -0.0005f), new Vector3(0f, 0.15f, -1f), len, 0.0042f, i % 2 == 0 ? Green : DeepGreen);
        }
        Blob(new Vector3(0.027f, 0.0085f, 0f), new Vector3(0.0045f, 0.003f, 0.0045f), 2, 6, White); // 끝에 앉은 눈
        Blob(new Vector3(-0.004f, 0.0155f, 0.0065f), new Vector3(0.0045f, 0.0025f, 0.0035f), 2, 5, White);
        b.Write("deco_pine_sprig");
    }

    private static void SnowyPinecone()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0115f, 0f), new Vector3(0.0070f, 0.0115f, 0.0070f), 4, 7, Brown);
        for (int k = 0; k < 7; k++) // 비늘 사이사이 연한 갈색 줄
        {
            float a = Mathf.PI * 2f * k / 7f;
            Blob(Radial(a) * 0.0052f + new Vector3(0f, 0.0075f + 0.0017f * (k % 3), 0f), new Vector3(0.0032f, 0.0028f, 0.0032f), 2, 5, k % 2 == 0 ? Tan : Brown);
        }
        Blob(new Vector3(0f, 0.0230f, 0f), new Vector3(0.0062f, 0.0030f, 0.0062f), 3, 7, White); // 꼭대기에 쌓인 눈
        Blob(new Vector3(0.0034f, 0.0175f, 0.0032f), new Vector3(0.0030f, 0.0021f, 0.0030f), 2, 5, White);
        Blob(new Vector3(-0.0036f, 0.0138f, -0.0030f), new Vector3(0.0026f, 0.0018f, 0.0026f), 2, 5, Ice);
        b.Write("deco_snowy_pinecone");
    }

    private static void Firewood()
    {
        b = new Builder();
        // 통나무 세 개 (아래 둘, 위 하나). 끝면은 연한 나이테색
        void Log(Vector3 c, float rot, float r)
        {
            Vector3 d = new Vector3(Mathf.Cos(rot), 0f, Mathf.Sin(rot)) * 0.020f;
            Frustum(b, c - d, c + d, r, r * 0.95f, 7, Brown, Tan);
        }
        Log(new Vector3(0f, 0.0065f, -0.0075f), 0.05f, 0.0068f);
        Log(new Vector3(0.0005f, 0.0065f, 0.0075f), -0.04f, 0.0066f);
        Log(new Vector3(0f, 0.0185f, 0f), 0.12f, 0.0064f);
        Blob(new Vector3(0.004f, 0.0262f, 0.0010f), new Vector3(0.0100f, 0.0022f, 0.0040f), 2, 6, White); // 위에 앉은 눈
        b.Write("deco_firewood");
    }

    private static void IceShard()
    {
        b = new Builder();
        // 육각 수정처럼 끝이 뾰족한 얼음 조각 세 개
        void Shard(Vector3 basePos, Vector3 tilt, float h, float r, int color)
        {
            b.xf = Matrix4x4.TRS(basePos, Quaternion.Euler(tilt), Vector3.one);
            Frustum(b, Vector3.zero, new Vector3(0f, h * 0.72f, 0f), r, r * 0.85f, 6, color, color);
            Frustum(b, new Vector3(0f, h * 0.72f, 0f), new Vector3(0f, h, 0f), r * 0.85f, 0.0004f, 6, Ice, Ice);
            b.xf = Matrix4x4.identity;
        }
        Shard(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), 0.034f, 0.0085f, Cyan);
        Shard(new Vector3(0.0105f, 0f, 0.0020f), new Vector3(0f, 0f, -16f), 0.024f, 0.0060f, SkyBlue);
        Shard(new Vector3(-0.0095f, 0f, -0.0030f), new Vector3(8f, 0f, 20f), 0.020f, 0.0055f, Ice);
        b.Write("deco_ice_shard");
    }

    private static void Icicle()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0400f, 0f), new Vector3(0.0170f, 0.0040f, 0.0055f), 2, 7, White); // 처마에 쌓인 눈
        float[] xs = { -0.011f, -0.003f, 0.0045f, 0.011f };
        float[] hs = { 0.022f, 0.036f, 0.028f, 0.018f };
        for (int i = 0; i < 4; i++)
            Frustum(b, new Vector3(xs[i], 0.0405f - 0.0004f, 0f), new Vector3(xs[i] * 0.95f, 0.0405f - hs[i], 0f), 0.0042f, 0.0005f, 6, i % 2 == 0 ? Cyan : Ice, Ice);
        b.Write("deco_icicle");
    }

    private static void HollyBerry()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0.002f, 0.015f, 0f), 0.0008f, 0.0006f, 4, Brown);
        for (int k = 0; k < 4; k++) // 뾰족뾰족한 짙은 초록 잎 네 장
        {
            float a = k * 1.7f + 0.3f;
            Vector3 r = Radial(a);
            Petal(new Vector3(0.002f, 0.013f, 0f) + r * 0.001f, r + Vector3.up * 0.15f, new Vector3(-r.z, 0f, r.x), 0.016f, 0.0085f, k % 2 == 0 ? DeepGreen : Green, 0.0012f);
        }
        Blob(new Vector3(0.0020f, 0.0178f, 0.0015f), new Vector3(0.0038f, 0.0038f, 0.0038f), 3, 6, Red);
        Blob(new Vector3(-0.0020f, 0.0172f, -0.0012f), new Vector3(0.0036f, 0.0036f, 0.0036f), 3, 6, Red);
        Blob(new Vector3(0.0035f, 0.0172f, -0.0025f), new Vector3(0.0033f, 0.0033f, 0.0033f), 3, 6, Coral);
        b.Write("deco_holly_berry");
    }

    private static void FrostLeaf()
    {
        b = new Builder();
        Frustum(b, Vector3.zero, new Vector3(0f, 0.014f, 0.0015f), 0.0008f, 0.0006f, 4, Brown);
        Petal(new Vector3(0f, 0.014f, 0.0015f), new Vector3(0f, 0.55f, 1f), new Vector3(1f, 0f, 0f), 0.034f, 0.020f, Leaf, 0.003f);
        // 서리: 잎 가장자리와 가운데에 얼음빛 얼룩
        Petal(new Vector3(0f, 0.0165f, 0.0050f), new Vector3(0f, 0.55f, 1f), new Vector3(1f, 0f, 0f), 0.024f, 0.011f, Ice, 0.0034f);
        Petal(new Vector3(0.0045f, 0.0190f, 0.0200f), new Vector3(0.2f, 0.4f, 1f), new Vector3(1f, 0f, -0.2f), 0.009f, 0.005f, White, 0.0020f);
        b.Write("deco_frost_leaf");
    }

    private static void Snowball()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0120f, 0f), new Vector3(0.0120f, 0.0120f, 0.0120f), 5, 9, White);
        Blob(new Vector3(0.0045f, 0.0040f, 0.0025f), new Vector3(0.0062f, 0.0060f, 0.0062f), 3, 7, Ice);  // 아래에 눌려 붙은 얼음빛 부분
        Blob(new Vector3(0.0140f, 0.0050f, 0.0060f), new Vector3(0.0050f, 0.0050f, 0.0050f), 3, 6, White); // 곁의 작은 눈덩이
        b.Write("deco_snowball");
    }

    private static void MiniSnowman()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0120f, 0f), new Vector3(0.0125f, 0.0120f, 0.0125f), 5, 9, White);
        Blob(new Vector3(0f, 0.0300f, 0f), new Vector3(0.0085f, 0.0085f, 0.0085f), 4, 8, White);
        Blob(new Vector3(0f, 0.0222f, 0f), new Vector3(0.0098f, 0.0026f, 0.0098f), 2, 8, Red);          // 목도리
        Blob(new Vector3(0.0050f, 0.0185f, -0.0050f), new Vector3(0.0030f, 0.0064f, 0.0024f), 2, 5, Red); // 목도리 꼬리
        Frustum(b, new Vector3(0f, 0.0305f, -0.0070f), new Vector3(0f, 0.0295f, -0.0155f), 0.0016f, 0.0003f, 5, Carrot, Carrot); // 당근 코
        Blob(new Vector3(-0.0030f, 0.0335f, -0.0066f), new Vector3(0.0012f, 0.0012f, 0.0012f), 2, 4, Dark); // 눈
        Blob(new Vector3(0.0030f, 0.0335f, -0.0066f), new Vector3(0.0012f, 0.0012f, 0.0012f), 2, 4, Dark);
        Frustum(b, new Vector3(0.0105f, 0.0140f, 0f), new Vector3(0.0215f, 0.0200f, 0f), 0.0010f, 0.0006f, 4, Brown); // 나뭇가지 팔
        Frustum(b, new Vector3(-0.0105f, 0.0140f, 0f), new Vector3(-0.0215f, 0.0200f, 0f), 0.0010f, 0.0006f, 4, Brown);
        for (int i = 0; i < 2; i++) Blob(new Vector3(0f, 0.0150f - i * 0.0075f, -0.0118f), new Vector3(0.0013f, 0.0013f, 0.0013f), 2, 4, Dark); // 단추
        b.Write("deco_mini_snowman");
    }

    private static void WinterBell()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0100f, 0f), new Vector3(0.0085f, 0.0090f, 0.0085f), 4, 8, Gold);
        Frustum(b, new Vector3(0f, 0.0010f, 0f), new Vector3(0f, 0.0040f, 0f), 0.0095f, 0.0085f, 8, Yellow, Yellow); // 벌어진 입구
        Blob(new Vector3(0f, 0.0010f, 0f), new Vector3(0.0028f, 0.0026f, 0.0028f), 2, 5, Brown);                        // 추
        Frustum(b, new Vector3(0f, 0.0185f, 0f), new Vector3(0f, 0.0225f, 0f), 0.0010f, 0.0010f, 4, Gold);
        Petal(new Vector3(0f, 0.0200f, 0f), new Vector3(1f, 0.3f, 0f), new Vector3(0f, 0f, 1f), 0.0110f, 0.0060f, Red, 0.0010f); // 리본 매듭
        Petal(new Vector3(0f, 0.0200f, 0f), new Vector3(-1f, 0.3f, 0f), new Vector3(0f, 0f, 1f), 0.0110f, 0.0060f, Red, 0.0010f);
        Blob(new Vector3(0f, 0.0200f, 0f), new Vector3(0.0020f, 0.0020f, 0.0020f), 2, 5, Coral);
        for (int k = 0; k < 2; k++) // 호랑가시 잎 두 장
        {
            float s = k == 0 ? 1f : -1f;
            Petal(new Vector3(0f, 0.0195f, 0f), new Vector3(0.5f * s, 0.1f, -1f), new Vector3(1f, 0f, 0.5f * s), 0.0105f, 0.0050f, DeepGreen, 0.0008f);
        }
        b.Write("deco_winter_bell");
    }

    private static void Mitten()
    {
        b = new Builder();
        Blob(new Vector3(0f, 0.0160f, 0f), new Vector3(0.0100f, 0.0140f, 0.0058f), 4, 8, Red);                         // 손
        Blob(new Vector3(-0.0098f, 0.0140f, 0f), new Vector3(0.0050f, 0.0072f, 0.0048f), 3, 6, Red);                    // 엄지
        Blob(new Vector3(0f, 0.0034f, 0f), new Vector3(0.0105f, 0.0036f, 0.0064f), 3, 8, White);                       // 흰 소매 끝
        Blob(new Vector3(0f, 0.0200f, -0.0040f), new Vector3(0.0034f, 0.0034f, 0.0010f), 2, 5, White);                 // 무늬 점
        Blob(new Vector3(0f, 0.0125f, -0.0040f), new Vector3(0.0034f, 0.0034f, 0.0010f), 2, 5, White);
        b.Write("deco_mitten");
    }

    private static void Firefly()
    {
        b = new Builder();
        // 벌레 모양은 징그러워서, 작고 말랑한 빛 구슬로 만든다. 밤에는 NightGlow가 빛무리를 더해 반딧불이처럼 반짝인다.
        Blob(new Vector3(0f, 0.0085f, 0f), new Vector3(0.0075f, 0.0075f, 0.0075f), 5, 10, Pale);
        b.Write("deco_firefly");
    }
}
