using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 집 안 가구와 우체통의 로우폴리 모델(실제 크기, 미터). PropModelMaker와 같은 방식(코드로 만든 각진 면, 같은 팔레트)이다.
/// 침대, 책상, 침대 옆 탁자, 선반, 작업대, 창턱, 책(도감), 우체통. 앞쪽은 +z, 바닥이 y=0, 가로세로 가운데가 원점.
/// 모서리를 깎은 상자(BBox)와 깎아 만든 다리(Frustum), 둥근 베개와 손잡이(Blob)로 디테일을 준다.
/// 메뉴: 유리병 정원 > 가구 모델 만들기 (그다음 장식 모델 가져오기 설정, 집 안 꾸미기, 섬의 우체통 바꾸기)
/// </summary>
public static partial class PropModelMaker
{
    [MenuItem("유리병 정원/가구 모델 만들기")]
    public static string BuildFurniture()
    {
        FurnBed(); FurnDesk(); FurnBedside(); FurnShelf(); FurnWorkbench(); FurnSill(); FurnBook(); FurnMailbox();
        AssetDatabase.Refresh();
        return "가구 모델 8종을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    /// <summary>유니티가 .obj를 불러올 때 x를 뒤집으므로(오른손 좌표계를 왼손으로), 미리 x를 뒤집어 만들어서 불러온 모습이 설계 그대로가 되게 한다.</summary>
    private static Builder NewMirrored()
    {
        Builder nb = new Builder();
        nb.xf = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
        return nb;
    }

    /// <summary>모서리를 깎은 상자: 세 개의 상자를 서로 다르게 줄여 겹쳐서 모서리가 한 단 깎여 보이게 한다.</summary>
    private static void BBox(Vector3 c, Vector3 s, float bv, int color)
    {
        Box(c, new Vector3(s.x, s.y - 2f * bv, s.z - 2f * bv), color);
        Box(c, new Vector3(s.x - 2f * bv, s.y, s.z - 2f * bv), color);
        Box(c, new Vector3(s.x - 2f * bv, s.y - 2f * bv, s.z), color);
    }

    // ───────── 침대 (1.1 x 2.1, 머리판이 +z) ─────────
    private static void FurnBed()
    {
        b = NewMirrored();
        foreach (float x in new[] { -0.5f, 0.5f })
        {
            Box(V(x, 0.46f, 1.0f), V(0.09f, 0.92f, 0.09f), Brown);        // 머리 쪽 기둥
            Box(V(x, 0.28f, -1.0f), V(0.09f, 0.56f, 0.09f), Brown);       // 발 쪽 기둥
            Box(V(x, 0.27f, 0f), V(0.05f, 0.14f, 1.9f), Brown);           // 옆 가로대
        }
        Box(V(0f, 0.70f, 1.0f), V(0.95f, 0.45f, 0.05f), Tan);             // 머리판
        Box(V(0f, 0.92f, 1.0f), V(0.95f, 0.08f, 0.07f), Brown);
        Box(V(0f, 0.40f, -1.0f), V(0.95f, 0.26f, 0.05f), Tan);            // 발판
        Box(V(0f, 0.32f, 0f), V(0.96f, 0.20f, 1.94f), Cream);             // 요
        Box(V(-0.235f, 0.46f, 0.74f), V(0.40f, 0.08f, 0.30f), White);     // 베개 둘
        Box(V(0.235f, 0.46f, 0.74f), V(0.40f, 0.08f, 0.30f), White);
        Box(V(0f, 0.455f, -0.30f), V(1.0f, 0.06f, 1.2f), Blue);           // 이불
        foreach (float x in new[] { -0.505f, 0.505f }) Box(V(x, 0.36f, -0.30f), V(0.03f, 0.20f, 1.2f), Blue);
        Box(V(0f, 0.36f, -0.905f), V(1.0f, 0.20f, 0.03f), Blue);
        b.Write("deco_furn_bed");
    }
    // ───────── 책상 (0.7 x 1.3, 높이 0.8, 서랍이 +x) ─────────
    private static void FurnDesk()
    {
        b = NewMirrored();
        Box(V(0f, 0.775f, 0f), V(0.74f, 0.05f, 1.34f), Tan);                                                     // 상판
        foreach (float x in new[] { -0.3f, 0.3f })
            foreach (float z in new[] { -0.6f, 0.6f }) Box(V(x, 0.375f, z), V(0.06f, 0.75f, 0.06f), Brown);       // 다리
        foreach (float x in new[] { -0.3f, 0.3f }) Box(V(x, 0.68f, 0f), V(0.03f, 0.1f, 1.14f), Brown);          // 옆 가로대
        foreach (float z in new[] { -0.6f, 0.6f }) Box(V(0f, 0.68f, z), V(0.6f, 0.1f, 0.03f), Brown);
        foreach (float z in new[] { -0.28f, 0.28f }) Box(V(0.31f, 0.68f, z), V(0.02f, 0.1f, 0.48f), DarkBrown);   // 서랍 둘
        b.Write("deco_furn_desk");
    }
    // ───────── 침대 옆 탁자 (0.5 x 0.45, 높이 0.5, 서랍이 +x) ─────────
    private static void FurnBedside()
    {
        b = NewMirrored();
        foreach (float x in new[] { -0.2f, 0.2f })
            foreach (float z in new[] { -0.17f, 0.17f }) Frustum(V(x, 0f, z), V(x, 0.43f, z), 0.025f, 0.032f, 6, Brown, Brown);
        BBox(V(0f, 0.46f, 0f), V(0.5f, 0.04f, 0.46f), 0.01f, Tan);
        Box(V(0f, 0.31f, 0f), V(0.42f, 0.30f, 0.38f), Brown);
        Box(V(0.213f, 0.37f, 0f), V(0.012f, 0.17f, 0.34f), DarkBrown);                                           // 서랍 틀
        Box(V(0.222f, 0.37f, 0f), V(0.012f, 0.145f, 0.30f), LightTan);
        Blob(V(0.238f, 0.37f, 0f), V(0.022f, 0.022f, 0.022f), 3, 8, Gold);
        Box(V(0.213f, 0.21f, 0f), V(0.012f, 0.07f, 0.34f), DarkBrown);                                           // 아래 칸 문
        Box(V(0.222f, 0.21f, 0f), V(0.012f, 0.055f, 0.30f), Tan);
        Blob(V(0.238f, 0.21f, 0f), V(0.02f, 0.02f, 0.02f), 3, 8, Gold);
        b.Write("deco_furn_bedside");
    }

    // ───────── 선반 (깊이 0.33, 길이 3.1, 높이 2.6, 앞이 +x) ─────────
    private static void FurnShelf()
    {
        b = NewMirrored();
        float[] boardY = { 0.25f, 0.80f, 1.35f, 1.90f, 2.45f };
        foreach (float y in boardY) Box(V(0f, y, 0f), V(0.30f, 0.04f, 3.06f), Tan);                              // 판
        foreach (float z in new[] { -1.52f, 0f, 1.52f }) Box(V(0f, 1.27f, z), V(0.33f, 2.5f, 0.06f), DarkBrown);  // 기둥
        Box(V(-0.13f, 1.27f, 0f), V(0.025f, 2.46f, 3.06f), Brown);                                               // 뒤판: 한 색 한 장
        b.Write("deco_furn_shelf");
    }
    // ───────── 작업대 (2.14 x 0.92, 높이 1.0, 앞이 +z) ─────────
    private static void FurnWorkbench()
    {
        b = NewMirrored();
        BBox(V(0f, 0.975f, 0f), V(2.14f, 0.05f, 0.92f), 0.015f, Tan);                                             // 상판
        Box(V(0f, 0.945f, 0f), V(2.10f, 0.012f, 0.88f), DarkBrown);
        Box(V(0f, 0.47f, 0f), V(1.96f, 0.90f, 0.76f), Brown);                                                     // 몸통
        Box(V(0f, 0.04f, 0f), V(2.0f, 0.08f, 0.80f), DarkBrown);                                                  // 밑 받침
        foreach (float x in new[] { -0.99f, 0.99f })
            foreach (float z in new[] { -0.39f, 0.39f }) BBox(V(x, 0.5f, z), V(0.08f, 0.9f, 0.08f), 0.012f, DarkBrown); // 모서리 기둥
        foreach (float x in new[] { -0.5f, 0.5f })                                                                // 문 둘
        {
            Box(V(x, 0.47f, 0.384f), V(0.90f, 0.74f, 0.012f), DarkBrown);
            Box(V(x, 0.47f, 0.393f), V(0.80f, 0.64f, 0.012f), LightTan);
            Box(V(x, 0.47f, 0.40f), V(0.64f, 0.48f, 0.01f), Tan);                                                 // 문 속 판
            Blob(V(x * 0.22f, 0.50f, 0.41f), V(0.025f, 0.025f, 0.02f), 3, 8, Gold);                               // 손잡이
        }
        Box(V(0f, 0.47f, 0.39f), V(0.06f, 0.78f, 0.02f), DarkBrown);                                              // 가운데 틀
        for (int i = 0; i < 4; i++) Box(V(-0.81f + i * 0.54f, 0.9f, 0.0f), V(0.012f, 0.012f, 0.9f), DarkBrown);   // 상판 판자 이음선
        b.Write("deco_furn_workbench");
    }

    // ───────── 창턱 (1.25 x 0.26, 길이가 x, 앞이 +z) ─────────
    private static void FurnSill()
    {
        b = NewMirrored();
        BBox(V(0f, 0.02f, 0f), V(1.25f, 0.04f, 0.26f), 0.008f, Tan);
        Box(V(0f, 0.0f, 0.12f), V(1.2f, 0.03f, 0.03f), DarkBrown);                                              // 앞 턱 아래 (판 앞면보다 5mm 나오게: 같은 면이 겹쳐 줄무늬 지지 않게)
        foreach (float x in new[] { -0.5f, 0f, 0.5f })                                                            // 받침 보
        {
            Box(V(x, -0.05f, 0.03f), V(0.06f, 0.1f, 0.2f), Brown);
            Box(V(x, -0.11f, 0.11f), V(0.06f, 0.05f, 0.07f), DarkBrown);
        }
        b.Write("deco_furn_sill");
    }

    // ───────── 펼친 책 (도감, 0.34 x 0.24, 앞이 +z) ─────────
    private static void FurnBook()
    {
        b = NewMirrored();
        Box(V(0f, 0.008f, 0f), V(0.34f, 0.016f, 0.25f), DarkBrown);                                               // 겉표지
        Box(V(-0.083f, 0.03f, 0f), V(0.158f, 0.028f, 0.225f), Cream);                                             // 왼쪽 쪽들
        Box(V(0.083f, 0.03f, 0f), V(0.158f, 0.028f, 0.225f), Cream);
        Box(V(0f, 0.034f, 0f), V(0.01f, 0.036f, 0.225f), Pale);                                                   // 가운데 접힘
        for (int i = 0; i < 4; i++)                                                                               // 글줄
        {
            Box(V(-0.083f, 0.0455f, -0.07f + i * 0.045f), V(0.11f, 0.002f, 0.008f), Brown);
            Box(V(0.083f, 0.0455f, -0.07f + i * 0.045f), V(0.11f, 0.002f, 0.008f), Brown);
        }
        Box(V(0.12f, 0.0455f, 0.06f), V(0.04f, 0.0025f, 0.04f), Coral);                                           // 그림 한 점
        Box(V(0f, 0.052f, 0.115f), V(0.012f, 0.002f, 0.07f), Red);                                                // 책갈피 끈
        b.Write("deco_furn_book");
    }

    // ───────── 우체통 (기둥, 둥근 지붕 통, 깃발. 문이 +z) ─────────
    private static void FurnMailbox()
    {
        b = NewMirrored();
        // 기둥과 받침
        Frustum(V(0f, 0f, 0f), V(0f, 1.1f, 0f), 0.085f, 0.065f, 8, Brown, Brown);
        Frustum(V(0f, 0f, 0f), V(0f, 0.08f, 0f), 0.13f, 0.1f, 8, DarkBrown, DarkBrown);
        Blob(V(0f, 0.02f, 0f), V(0.2f, 0.05f, 0.2f), 2, 8, Gray);                                                // 발치 돌
        Box(V(0f, 1.1f, 0f), V(0.16f, 0.05f, 0.56f), DarkBrown);                                                   // 통 받침 보
        foreach (float z in new[] { -0.22f, 0.22f })                                                              // 비스듬한 버팀
            Frustum(V(0f, 0.86f, z * 0.2f), V(0f, 1.1f, z), 0.024f, 0.02f, 6, Brown, Brown);
        // 통: 아래 상자 + 둥근 지붕(반원 단면을 9조각으로)
        float r = 0.23f, cy = 1.23f, halfLen = 0.35f; const int seg = 9;
        Box(V(0f, 1.17f, 0f), V(2f * r, 0.12f, 2f * halfLen), Red);
        Vector3 inside = V(0f, cy, 0f);
        for (int k = 0; k < seg; k++)
        {
            float a0 = Mathf.PI * k / seg, a1 = Mathf.PI * (k + 1) / seg;
            Vector3 p0 = V(r * Mathf.Cos(a0), cy + r * Mathf.Sin(a0), 0f), p1 = V(r * Mathf.Cos(a1), cy + r * Mathf.Sin(a1), 0f);
            b.Quad(p0 + V(0f, 0f, -halfLen), p1 + V(0f, 0f, -halfLen), p1 + V(0f, 0f, halfLen), p0 + V(0f, 0f, halfLen), k % 2 == 0 ? Red : DullRed, inside);
            b.Tri(V(0f, cy, halfLen), p0 + V(0f, 0f, halfLen), p1 + V(0f, 0f, halfLen), DullRed, inside);       // 문 쪽 둥근 면
            b.Tri(V(0f, cy, -halfLen), p0 + V(0f, 0f, -halfLen), p1 + V(0f, 0f, -halfLen), Red, inside);
        }
        // 문의 테두리, 손잡이, 편지 넣는 구멍, 이름표
        Box(V(0f, 1.20f, halfLen + 0.006f), V(0.40f, 0.06f, 0.012f), Dark);
        Blob(V(0f, 1.33f, halfLen + 0.018f), V(0.03f, 0.03f, 0.022f), 3, 8, Gold);
        Box(V(0f, 1.18f, halfLen + 0.008f), V(0.12f, 0.025f, 0.012f), Cream);
        // 깃발: 오른쪽(+x) 옆에 세운 막대와 노란 깃발
        Box(V(r + 0.015f, 1.30f, 0.12f), V(0.025f, 0.26f, 0.03f), Dark);
        Box(V(r + 0.07f, 1.40f, 0.12f), V(0.11f, 0.07f, 0.012f), Yellow);
        Blob(V(r + 0.015f, 1.44f, 0.12f), V(0.022f, 0.022f, 0.022f), 3, 6, Gold);
        // 왼쪽에는 작은 종 (좌우 균형)
        Box(V(-r - 0.04f, 1.36f, 0.12f), V(0.09f, 0.015f, 0.015f), Dark);
        Blob(V(-r - 0.075f, 1.32f, 0.12f), V(0.035f, 0.04f, 0.035f), 3, 8, Gold);
        b.Write("deco_furn_mailbox");
    }
}
