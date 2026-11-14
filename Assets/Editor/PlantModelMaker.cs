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
        // 몸은 앞(-z)을 향한다. 꽁무니(+z)는 밝은 노란빛.
        Blob(new Vector3(0f, 0.0075f, -0.001f), new Vector3(0.0036f, 0.0034f, 0.0075f), 3, 6, Dark);          // 가슴
        Blob(new Vector3(0f, 0.0072f, -0.0095f), new Vector3(0.0028f, 0.0027f, 0.0028f), 2, 6, Dark);        // 머리
        Blob(new Vector3(0f, 0.0070f, 0.0085f), new Vector3(0.0044f, 0.0040f, 0.0062f), 3, 6, Pale);          // 빛나는 꽁무니
        Blob(new Vector3(0f, 0.0070f, 0.0100f), new Vector3(0.0030f, 0.0028f, 0.0038f), 2, 5, LightGreen);
        // 날개 한 쌍
        Petal(new Vector3(0.0018f, 0.0100f, -0.0030f), new Vector3(1f, 0.35f, 1.1f), new Vector3(-1f, 0f, 1f), 0.0125f, 0.0058f, White, 0.0006f);
        Petal(new Vector3(-0.0018f, 0.0100f, -0.0030f), new Vector3(-1f, 0.35f, 1.1f), new Vector3(1f, 0f, 1f), 0.0125f, 0.0058f, White, 0.0006f);
        // 다리 세 쌍 (짧은 막대)
        for (int i = 0; i < 3; i++)
        {
            float z = -0.004f + i * 0.0045f;
            Frustum(b, new Vector3(0.0018f, 0.0058f, z), new Vector3(0.0050f, 0.0010f, z + 0.0015f), 0.0004f, 0.0003f, 3, Dark);
            Frustum(b, new Vector3(-0.0018f, 0.0058f, z), new Vector3(-0.0050f, 0.0010f, z + 0.0015f), 0.0004f, 0.0003f, 3, Dark);
        }
        // 더듬이
        Frustum(b, new Vector3(0.0010f, 0.0085f, -0.0115f), new Vector3(0.0030f, 0.0100f, -0.0175f), 0.0003f, 0.0002f, 3, Dark);
        Frustum(b, new Vector3(-0.0010f, 0.0085f, -0.0115f), new Vector3(-0.0030f, 0.0100f, -0.0175f), 0.0003f, 0.0002f, 3, Dark);
        b.Write("deco_firefly");
    }
}
