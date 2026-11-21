using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 꾸밀 소품 15종과 병 속 효과 재료(작은 유리병 모양) 5종의 모델을 코드로 만들어 Assets/Models/Deco 에 .obj 로 저장한다.
/// 소품: 랜턴, 벤치, 나무 울타리, 돌다리, 작은 등대, 분수, 망원경, 깃발, 연, 돛단배, 버섯 집.
/// 동물: 작은 새, 토끼, 고양이, 여우.
/// 효과 병: 눈 내리기, 반짝이 가루, 꽃잎 날리기, 비눗방울, 보슬비 (병에 놓으면 병 속에 입자가 흩날린다 - BottleEffect).
/// 다른 장식 모델과 같은 팔레트 그림(34색)을 쓰는 각진 로우폴리다. 크기는 4~7cm.
/// 메뉴: 유리병 정원 > 꾸밈 소품 모델 만들기 (그다음 장식 모델 가져오기 설정, 재료 데이터 만들기 ...)
/// </summary>
public static partial class PropModelMaker
{
    // 팔레트 번호 (palette.png 참고)
    private const int Gray = 0, LightGray = 1, White = 2, Dark = 3, Brown = 4, DarkBrown = 5, Tan = 6, LightTan = 7, Green = 9, Leaf = 12,
                      Red = 14, Orange = 15, Yellow = 16, Pink = 17, Blue = 19, Cyan = 20, Gold = 21, Salmon = 24, Coral = 25, DullRed = 26,
                      Teal = 28, Cream = 30, Pale = 31, Ice = 33;

    private static Builder b;

    [MenuItem("유리병 정원/꾸밈 소품 모델 만들기")]
    public static string Build()
    {
        Lantern(); Bench(); Fence(); StoneBridge(); Lighthouse(); Fountain(); Telescope(); Flag(); Kite(); Sailboat(); MushroomHouse();
        Bird(); Rabbit(); Cat(); Fox();
        Fish("deco_smelt", LightGray, White, Ice, Blue, 0.016f);
        Fish("deco_ice_trout", Teal, Ice, Blue, Coral, 0.019f);
        Fish("deco_goldfish", Orange, Pale, Salmon, Red, 0.013f);
        Fish("deco_aurora_fish", 18, 22, Cyan, Green, 0.018f);
        Vial("deco_fx_snow", Ice, White);
        Vial("deco_fx_glitter", Gold, Pale);
        Vial("deco_fx_petals", Pink, White);
        Vial("deco_fx_bubbles", Cyan, White);
        Vial("deco_fx_rain", Blue, Ice);
        AssetDatabase.Refresh();
        return "꾸밈 소품 모델 15종과 효과 병 5종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    // ───────── 기본 모양 ─────────

    /// <summary>켜면 상자마다 크기를 0~4mm씩 다르게 키운다: 이웃한 부품의 면이 같은 평면에 놓여 줄무늬(z-fighting)가 지는 것을 막는다 (가구 모델용)</summary>
    private static bool boxJitter;
    private static int boxCount;

    private static void Box(Vector3 c, Vector3 s, int color)
    {
        if (boxJitter) s += Vector3.one * ((boxCount++ % 5) * 0.002f);
        Vector3 h = s * 0.5f;
        Vector3 p000 = c + new Vector3(-h.x, -h.y, -h.z), p100 = c + new Vector3(h.x, -h.y, -h.z), p110 = c + new Vector3(h.x, h.y, -h.z), p010 = c + new Vector3(-h.x, h.y, -h.z);
        Vector3 p001 = c + new Vector3(-h.x, -h.y, h.z), p101 = c + new Vector3(h.x, -h.y, h.z), p111 = c + new Vector3(h.x, h.y, h.z), p011 = c + new Vector3(-h.x, h.y, h.z);
        b.Quad(p000, p100, p101, p001, color, c); // 아래
        b.Quad(p010, p011, p111, p110, color, c); // 위
        b.Quad(p000, p010, p110, p100, color, c); // 앞(-z)
        b.Quad(p001, p101, p111, p011, color, c); // 뒤(+z)
        b.Quad(p000, p001, p011, p010, color, c); // 왼쪽
        b.Quad(p100, p110, p111, p101, color, c); // 오른쪽
    }

    private static void Frustum(Vector3 p0, Vector3 p1, float r0, float r1, int sides, int color, int cap = -1)
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

    private static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    // ───────── 소품 ─────────

    private static void Lantern()
    {
        b = new Builder();
        Box(V(0, 0.002f, 0), V(0.014f, 0.004f, 0.014f), Dark);
        Box(V(0, 0.0135f, 0), V(0.0105f, 0.019f, 0.0105f), Pale);
        foreach (float x in new[] { -0.0058f, 0.0058f })
            foreach (float z in new[] { -0.0058f, 0.0058f })
                Box(V(x, 0.0135f, z), V(0.0022f, 0.020f, 0.0022f), Dark);
        Frustum(V(0, 0.0235f, 0), V(0, 0.0315f, 0), 0.0108f, 0.0016f, 4, Dark);
        Frustum(V(0, 0.0315f, 0), V(0, 0.0345f, 0), 0.0022f, 0.0022f, 6, Dark);
        Blob(V(0, 0.0125f, 0), V(0.0036f, 0.0052f, 0.0036f), 3, 6, Orange); // 불꽃
        b.Write("deco_lantern");
    }

    private static void Bench()
    {
        b = new Builder();
        Box(V(0, 0.012f, 0), V(0.044f, 0.004f, 0.015f), Tan);
        foreach (float x in new[] { -0.018f, 0.018f })
        {
            foreach (float z in new[] { -0.005f, 0.005f }) Box(V(x, 0.0055f, z), V(0.004f, 0.011f, 0.004f), DarkBrown);
            Box(V(x, 0.0215f, 0.0075f), V(0.004f, 0.015f, 0.003f), DarkBrown);
        }
        Box(V(0, 0.0245f, 0.0075f), V(0.040f, 0.0045f, 0.002f), LightTan);
        Box(V(0, 0.0185f, 0.0075f), V(0.040f, 0.0045f, 0.002f), LightTan);
        b.Write("deco_bench");
    }

    private static void Fence()
    {
        b = new Builder();
        foreach (float x in new[] { -0.024f, -0.008f, 0.008f, 0.024f })
        {
            Box(V(x, 0.014f, 0), V(0.004f, 0.028f, 0.004f), Tan);
            Frustum(V(x, 0.028f, 0), V(x, 0.031f, 0), 0.0028f, 0.0003f, 4, Tan);
        }
        Box(V(0, 0.011f, -0.0035f), V(0.056f, 0.004f, 0.003f), LightTan);
        Box(V(0, 0.020f, -0.0035f), V(0.056f, 0.004f, 0.003f), LightTan);
        b.Write("deco_fence");
    }

    private static void StoneBridge()
    {
        b = new Builder();
        foreach (float x in new[] { -0.021f, 0.021f }) Box(V(x, 0.010f, 0), V(0.014f, 0.020f, 0.020f), Gray);
        Box(V(0, 0.0185f, 0), V(0.030f, 0.007f, 0.020f), Gray);        // 아치 윗부분
        Box(V(0, 0.0235f, 0), V(0.056f, 0.004f, 0.020f), LightGray);   // 다리 바닥
        foreach (float z in new[] { -0.0085f, 0.0085f }) Box(V(0, 0.0275f, z), V(0.056f, 0.005f, 0.003f), LightGray);
        Blob(V(0, 0.0015f, 0), V(0.012f, 0.0015f, 0.011f), 2, 6, Cyan); // 다리 아래 물
        b.Write("deco_stone_bridge");
    }

    private static void Lighthouse()
    {
        b = new Builder();
        float R(float y) { return 0.0085f - 0.0030f * y / 0.048f; }
        Frustum(V(0, 0, 0), V(0, 0.048f, 0), R(0f), R(0.048f), 8, White, White);
        foreach (float y0 in new[] { 0.010f, 0.026f })
            Frustum(V(0, y0, 0), V(0, y0 + 0.008f, 0), R(y0) + 0.0003f, R(y0 + 0.008f) + 0.0003f, 8, Red, Red);
        Frustum(V(0, 0.048f, 0), V(0, 0.0505f, 0), 0.0072f, 0.0072f, 8, Dark);
        Blob(V(0, 0.0550f, 0), V(0.0046f, 0.0046f, 0.0046f), 3, 8, Pale);
        Frustum(V(0, 0.0585f, 0), V(0, 0.0645f, 0), 0.0056f, 0.0006f, 8, Red);
        Box(V(0, 0.0015f, 0), V(0.020f, 0.003f, 0.020f), Gray);
        b.Write("deco_lighthouse");
    }

    private static void Fountain()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.009f, 0), 0.021f, 0.019f, 8, LightGray, LightGray);
        Blob(V(0, 0.0092f, 0), V(0.0165f, 0.0014f, 0.0165f), 2, 8, Cyan);
        Frustum(V(0, 0.009f, 0), V(0, 0.030f, 0), 0.0042f, 0.0030f, 6, LightGray);
        Frustum(V(0, 0.030f, 0), V(0, 0.034f, 0), 0.0030f, 0.0100f, 8, White, Cyan);
        Blob(V(0, 0.0405f, 0), V(0.0028f, 0.0085f, 0.0028f), 3, 6, Cyan);
        foreach (float a in new[] { 0f, 1.57f, 3.14f, 4.71f })
            Blob(V(Mathf.Cos(a) * 0.0075f, 0.0345f, Mathf.Sin(a) * 0.0075f), V(0.0018f, 0.0030f, 0.0018f), 2, 5, Ice);
        b.Write("deco_fountain");
    }

    private static void Telescope()
    {
        b = new Builder();
        for (int i = 0; i < 3; i++)
        {
            float a = Mathf.PI * 2f * i / 3f + 1.57f;
            Frustum(V(0, 0.021f, 0), V(Mathf.Cos(a) * 0.013f, 0f, Mathf.Sin(a) * 0.013f), 0.0009f, 0.0006f, 4, DarkBrown);
        }
        b.xf = Matrix4x4.TRS(V(0, 0.026f, 0), Quaternion.Euler(-32f, 0f, 0f), Vector3.one);
        Frustum(V(0, 0, -0.016f), V(0, 0, 0.018f), 0.0042f, 0.0034f, 8, Teal);
        Frustum(V(0, 0, -0.0045f), V(0, 0, -0.0025f), 0.0046f, 0.0046f, 8, Gold);
        Frustum(V(0, 0, 0.0095f), V(0, 0, 0.0115f), 0.0040f, 0.0040f, 8, Gold);
        Frustum(V(0, 0, -0.0200f), V(0, 0, -0.0160f), 0.0050f, 0.0042f, 8, Dark, Ice);
        b.xf = Matrix4x4.identity;
        b.Write("deco_telescope");
    }

    private static void Flag()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.050f, 0), 0.0013f, 0.0008f, 5, DarkBrown);
        Blob(V(0, 0.0015f, 0), V(0.0055f, 0.0015f, 0.0055f), 2, 6, Gray);
        b.DoubleTri(V(0, 0.049f, 0), V(0, 0.039f, 0), V(0.022f, 0.044f, 0), Red);
        b.DoubleTri(V(0, 0.044f, 0), V(0, 0.039f, 0), V(0.011f, 0.0415f, 0), Gold);
        Blob(V(0, 0.0510f, 0), V(0.0016f, 0.0016f, 0.0016f), 2, 5, Gold);
        b.Write("deco_flag");
    }

    private static void Kite()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.020f, 0), 0.0009f, 0.0007f, 4, DarkBrown);
        b.xf = Matrix4x4.TRS(V(0, 0.020f, 0), Quaternion.Euler(-18f, 0f, 0f), Vector3.one);
        Vector3 top = V(0, 0.030f, 0), left = V(-0.015f, 0.010f, 0), right = V(0.015f, 0.010f, 0), bottom = V(0, -0.012f, 0), ctr = V(0, 0.010f, 0);
        b.DoubleTri(top, left, ctr, Pink); b.DoubleTri(top, ctr, right, Yellow);
        b.DoubleTri(left, bottom, ctr, Yellow); b.DoubleTri(ctr, bottom, right, Pink);
        for (int i = 0; i < 3; i++) // 꼬리 리본
        {
            float y = -0.014f - i * 0.0075f;
            b.DoubleTri(V(0, y + 0.002f, 0), V(-0.0035f, y - 0.0035f, 0), V(0.0035f, y - 0.0035f, 0), i % 2 == 0 ? Red : Cyan);
        }
        b.xf = Matrix4x4.identity;
        b.Write("deco_kite");
    }

    private static void Sailboat()
    {
        b = new Builder();
        Blob(V(0, 0.0015f, 0), V(0.027f, 0.0015f, 0.016f), 2, 8, Cyan);
        Box(V(0, 0.0055f, 0), V(0.034f, 0.006f, 0.012f), Brown);
        Box(V(0, 0.0090f, 0), V(0.0345f, 0.0015f, 0.0125f), Red);
        Frustum(V(0.017f, 0.0055f, 0), V(0.0215f, 0.0075f, 0), 0.0055f, 0.0005f, 4, Brown); // 뾰족한 뱃머리
        Frustum(V(0, 0.009f, 0), V(0, 0.048f, 0), 0.0011f, 0.0008f, 5, DarkBrown);
        b.DoubleTri(V(0.0015f, 0.046f, 0), V(0.0015f, 0.013f, 0), V(0.020f, 0.013f, 0), White);
        b.DoubleTri(V(-0.0015f, 0.046f, 0), V(-0.0015f, 0.013f, 0), V(-0.014f, 0.013f, 0), Cream);
        b.DoubleTri(V(0, 0.048f, 0), V(0, 0.0435f, 0), V(0.007f, 0.0457f, 0), Red);
        b.Write("deco_sailboat");
    }

    private static void MushroomHouse()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.028f, 0), 0.0125f, 0.0105f, 7, Cream, Cream);
        Blob(V(0, 0.0345f, 0), V(0.0245f, 0.0165f, 0.0245f), 4, 8, Red);
        foreach ((float x, float y, float z, float r) in new[] { (0.011f, 0.0425f, -0.0120f, 0.0036f), (-0.013f, 0.0405f, -0.0090f, 0.0032f), (0.0f, 0.0505f, 0.0f, 0.0040f), (0.0145f, 0.0405f, 0.0100f, 0.0030f), (-0.0080f, 0.0455f, 0.0130f, 0.0034f) })
            Blob(V(x, y, z), V(r, r * 0.5f, r), 2, 6, White);
        Box(V(0, 0.0075f, -0.0105f), V(0.0075f, 0.0125f, 0.0022f), DarkBrown);
        Box(V(0.0075f, 0.0185f, -0.0098f), V(0.0050f, 0.0050f, 0.0022f), Yellow);
        Box(V(-0.0075f, 0.0185f, -0.0098f), V(0.0050f, 0.0050f, 0.0022f), Yellow);
        b.Write("deco_mushroom_house");
    }

    // ───────── 동물 ─────────

    private static void Bird()
    {
        b = new Builder();
        Blob(V(0, 0.0160f, 0.001f), V(0.0105f, 0.0095f, 0.0150f), 4, 7, Blue);
        Blob(V(0, 0.0125f, -0.003f), V(0.0075f, 0.0055f, 0.0095f), 3, 6, White);
        Blob(V(0, 0.0245f, -0.0105f), V(0.0075f, 0.0075f, 0.0075f), 3, 7, Blue);
        Frustum(V(0, 0.0240f, -0.0170f), V(0, 0.0232f, -0.0235f), 0.0028f, 0.0003f, 4, Orange);
        foreach (float x in new[] { -0.0035f, 0.0035f }) Blob(V(x, 0.0262f, -0.0150f), V(0.0011f, 0.0011f, 0.0011f), 2, 4, Dark);
        b.DoubleTri(V(-0.0095f, 0.0200f, -0.0040f), V(-0.0095f, 0.0200f, 0.0080f), V(-0.0200f, 0.0120f, 0.0100f), Cyan);
        b.DoubleTri(V(0.0095f, 0.0200f, -0.0040f), V(0.0095f, 0.0200f, 0.0080f), V(0.0200f, 0.0120f, 0.0100f), Cyan);
        b.DoubleTri(V(0, 0.0185f, 0.0100f), V(-0.0045f, 0.0160f, 0.0215f), V(0.0045f, 0.0160f, 0.0215f), Teal);
        foreach (float x in new[] { -0.0030f, 0.0030f }) Frustum(V(x, 0.0085f, 0f), V(x, 0f, 0.0008f), 0.0008f, 0.0006f, 4, Orange);
        b.Write("deco_bird");
    }

    private static void Rabbit()
    {
        b = new Builder();
        Blob(V(0, 0.0115f, 0.002f), V(0.0130f, 0.0115f, 0.0165f), 4, 7, White);
        Blob(V(0, 0.0245f, -0.0105f), V(0.0095f, 0.0085f, 0.0085f), 3, 7, White);
        foreach (float s in new[] { -1f, 1f })
        {
            b.DoubleTri(V(s * 0.0035f, 0.0290f, -0.0115f), V(s * 0.0090f, 0.0300f, -0.0100f), V(s * 0.0065f, 0.0480f, -0.0090f), White);
            b.DoubleTri(V(s * 0.0043f, 0.0310f, -0.0125f), V(s * 0.0078f, 0.0316f, -0.0118f), V(s * 0.0063f, 0.0440f, -0.0105f), Pink);
            Blob(V(s * 0.0038f, 0.0265f, -0.0178f), V(0.0011f, 0.0011f, 0.0011f), 2, 4, Dark);
            Blob(V(s * 0.0048f, 0.0030f, -0.0105f), V(0.0042f, 0.0028f, 0.0058f), 2, 5, White);
        }
        Blob(V(0, 0.0235f, -0.0188f), V(0.0013f, 0.0010f, 0.0010f), 2, 4, Pink);
        Blob(V(0, 0.0130f, 0.0195f), V(0.0055f, 0.0055f, 0.0055f), 3, 6, White);
        b.Write("deco_rabbit");
    }

    private static void Cat()
    {
        b = new Builder();
        Blob(V(0, 0.0105f, 0.002f), V(0.0105f, 0.0100f, 0.0170f), 4, 7, Salmon);
        Blob(V(0, 0.0090f, -0.0085f), V(0.0065f, 0.0060f, 0.0060f), 3, 6, White);
        Blob(V(0, 0.0235f, -0.0115f), V(0.0090f, 0.0085f, 0.0080f), 3, 7, Salmon);
        foreach (float s in new[] { -1f, 1f })
        {
            Frustum(V(s * 0.0055f, 0.0295f, -0.0120f), V(s * 0.0050f, 0.0370f, -0.0120f), 0.0035f, 0.0003f, 3, Salmon);
            Blob(V(s * 0.0036f, 0.0252f, -0.0185f), V(0.0013f, 0.0016f, 0.0010f), 2, 4, Green);
            Blob(V(s * 0.0035f, 0.0035f, -0.0125f), V(0.0036f, 0.0030f, 0.0050f), 2, 5, White);
        }
        Blob(V(0, 0.0235f, -0.0192f), V(0.0011f, 0.0009f, 0.0009f), 2, 4, Pink);
        Frustum(V(0, 0.0120f, 0.0170f), V(0, 0.0160f, 0.0260f), 0.0024f, 0.0020f, 5, Salmon);
        Frustum(V(0, 0.0160f, 0.0260f), V(0, 0.0270f, 0.0262f), 0.0020f, 0.0016f, 5, DullRed);
        for (int i = 0; i < 3; i++) Blob(V(0, 0.0200f, 0.0f + i * 0.0075f), V(0.0060f, 0.0014f, 0.0016f), 2, 5, DullRed); // 등의 줄무늬
        b.Write("deco_cat");
    }

    private static void Fox()
    {
        b = new Builder();
        Blob(V(0, 0.0105f, 0.003f), V(0.0105f, 0.0095f, 0.0180f), 4, 7, Orange);
        Blob(V(0, 0.0095f, -0.0090f), V(0.0065f, 0.0065f, 0.0060f), 3, 6, White);
        Blob(V(0, 0.0235f, -0.0115f), V(0.0085f, 0.0078f, 0.0075f), 3, 7, Orange);
        Frustum(V(0, 0.0225f, -0.0165f), V(0, 0.0222f, -0.0235f), 0.0042f, 0.0012f, 5, Orange);
        Blob(V(0, 0.0222f, -0.0240f), V(0.0014f, 0.0012f, 0.0012f), 2, 4, Dark);
        foreach (float s in new[] { -1f, 1f })
        {
            Frustum(V(s * 0.0050f, 0.0290f, -0.0115f), V(s * 0.0058f, 0.0380f, -0.0115f), 0.0033f, 0.0003f, 3, Orange);
            Frustum(V(s * 0.0056f, 0.0350f, -0.0115f), V(s * 0.0058f, 0.0382f, -0.0115f), 0.0018f, 0.0003f, 3, Dark);
            Blob(V(s * 0.0034f, 0.0250f, -0.0172f), V(0.0010f, 0.0010f, 0.0009f), 2, 4, Dark);
            Frustum(V(s * 0.0042f, 0.0070f, -0.0090f), V(s * 0.0042f, 0.0000f, -0.0095f), 0.0013f, 0.0010f, 4, DarkBrown);
        }
        Blob(V(0, 0.0125f, 0.0300f), V(0.0058f, 0.0058f, 0.0140f), 3, 7, Orange);
        Blob(V(0, 0.0125f, 0.0420f), V(0.0046f, 0.0046f, 0.0050f), 3, 6, White);
        b.Write("deco_fox");
    }

    // ───────── 물고기 (낚시) ─────────

    /// <summary>작은 얼음 조각 위에 놓인 물고기. 몸은 x 방향(머리가 +x)이고 length는 몸의 반길이.</summary>
    private static void Fish(string name, int body, int belly, int fin, int mark, float length)
    {
        b = new Builder();
        Blob(V(0, 0.0085f, 0), V(length, 0.0065f, 0.0042f), 4, 8, body);
        Blob(V(0.002f, 0.0062f, 0), V(length * 0.8f, 0.0035f, 0.0036f), 3, 7, belly);
        Blob(V(-0.001f, 0.0092f, 0), V(length * 0.78f, 0.0012f, 0.0044f), 2, 8, mark); // 등의 줄무늬
        b.DoubleTri(V(-length * 0.9f, 0.0085f, 0), V(-length * 1.55f, 0.0150f, 0), V(-length * 1.55f, 0.0020f, 0), fin); // 꼬리
        b.DoubleTri(V(-0.003f, 0.0140f, 0), V(length * 0.4f, 0.0140f, 0), V(0.001f, 0.0195f, 0), fin);                  // 등지느러미
        foreach (float s in new[] { -1f, 1f })
        {
            b.DoubleTri(V(0.004f, 0.0075f, s * 0.0038f), V(-0.004f, 0.0075f, s * 0.0038f), V(-0.002f, 0.0035f, s * 0.0078f), fin); // 가슴지느러미
            Blob(V(length * 0.62f, 0.0098f, s * 0.0037f), V(0.0011f, 0.0011f, 0.0011f), 2, 4, Dark);                                 // 눈
        }
        Blob(V(0, 0.0010f, 0), V(length * 0.95f, 0.0012f, 0.0072f), 2, 8, Ice); // 받침 얼음
        b.Write(name);
    }

    // ───────── 효과 병 ─────────

    private static void Vial(string name, int bodyColor, int dotColor)
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.022f, 0), 0.0088f, 0.0088f, 8, bodyColor, bodyColor);
        Frustum(V(0, 0.022f, 0), V(0, 0.028f, 0), 0.0088f, 0.0045f, 8, bodyColor, bodyColor);
        Frustum(V(0, 0.028f, 0), V(0, 0.0315f, 0), 0.0045f, 0.0045f, 8, bodyColor, bodyColor);
        Frustum(V(0, 0.0315f, 0), V(0, 0.0375f, 0), 0.0050f, 0.0050f, 8, Brown, Tan); // 코르크 마개
        Frustum(V(0, 0.0075f, 0), V(0, 0.0095f, 0), 0.0092f, 0.0092f, 8, dotColor, dotColor); // 띠
        for (int i = 0; i < 6; i++) // 겉에 반짝이는 점
        {
            float a = Mathf.PI * 2f * i / 6f;
            Blob(V(Mathf.Cos(a) * 0.0086f, 0.0125f + 0.0035f * (i % 2), Mathf.Sin(a) * 0.0086f), V(0.0017f, 0.0017f, 0.0017f), 2, 4, dotColor);
        }
        b.Write(name);
    }
}
