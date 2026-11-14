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
}
