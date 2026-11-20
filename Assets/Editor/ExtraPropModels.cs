using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 새 꾸밈 재료 17종의 모델 (PropModelMaker와 같은 방식: 코드로 만든 각진 로우폴리, 같은 팔레트, 3~6cm).
/// 우체통 구매 12종: 작은 오두막, 풍차, 새집, 허수아비, 작은 연못, 그네, 미니 우체통, 가로등, 해변 파라솔, 오아시스, 장작더미, 정원 요정.
/// 줍는 것 5종: 그루터기, 통나무(숲, 도끼), 불가사리, 산호(바닷가), 터키석(사막, 곡괭이).
/// 메뉴: 유리병 정원 > 새 꾸밈 소품 모델 만들기 (그다음 장식 모델 가져오기 설정, 재료 데이터 만들기 ...)
/// </summary>
public static partial class PropModelMaker
{
    [MenuItem("유리병 정원/새 꾸밈 소품 모델 만들기")]
    public static string BuildMore()
    {
        // 이미 같은 이름의 모델 파일이 있으면(제공받은 모델) 덮어쓰지 않고, 없는 것만 코드로 만든다
        int made = 0;
        void Make(string file, System.Action gen)
        {
            if (System.IO.File.Exists("Assets/Models/Deco/" + file + ".obj")) return;
            gen();
            made++;
        }
        Make("deco_windmill", Windmill); Make("deco_birdhouse", Birdhouse); Make("deco_scarecrow", Scarecrow); Make("deco_pond", Pond);
        Make("deco_swing", Swing); Make("deco_mini_mailbox", MiniMailbox); Make("deco_street_lamp", StreetLamp); Make("deco_parasol", Parasol);
        Make("deco_oasis", Oasis); Make("deco_woodpile", Woodpile); Make("deco_gnome", Gnome);
        Make("deco_stump", Stump); Make("deco_log", Log); Make("deco_starfish", Starfish); Make("deco_turquoise", Turquoise);
        AssetDatabase.Refresh();
        return "새 꾸밈 모델 " + made + "종을 만들었어요 (이미 있던 모델은 그대로 두었어요). 이어서 '장식 모델 가져오기 설정'을 누르세요";
    }

    /// <summary>박공지붕: 밑변 w x d, 처마 높이 y0, 마루 높이 y1 (마루는 x 방향)</summary>
    private static void Gable(float w, float d, float y0, float y1, int color, Vector3 inside)
    {
        Vector3 A = V(-w / 2, y0, -d / 2), B = V(w / 2, y0, -d / 2), C = V(w / 2, y0, d / 2), D = V(-w / 2, y0, d / 2);
        Vector3 R1 = V(-w / 2, y1, 0), R2 = V(w / 2, y1, 0);
        b.Quad(A, B, R2, R1, color, inside);
        b.Quad(D, C, R2, R1, color, inside);
        b.Tri(A, D, R1, color, inside);
        b.Tri(B, C, R2, color, inside);
        b.Quad(A, B, C, D, color, inside); // 아래
    }

private static void Windmill()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.034f, 0), 0.011f, 0.0075f, 8, Cream, Cream);
        Frustum(V(0, 0.034f, 0), V(0, 0.045f, 0), 0.0092f, 0.0008f, 8, Red, Red);
        Box(V(0, 0.005f, -0.0105f), V(0.006f, 0.010f, 0.0015f), DarkBrown);       // 문
        Blob(V(0, 0.031f, -0.0085f), V(0.0028f, 0.0028f, 0.0028f), 3, 6, Dark);   // 중심
        Box(V(0, 0.031f, -0.0105f), V(0.034f, 0.0028f, 0.0015f), Tan);            // 날개 가로
        Box(V(0, 0.031f, -0.0105f), V(0.0028f, 0.034f, 0.0015f), Tan);            // 날개 세로
        foreach (float s in new[] { -1f, 1f })
        {
            Box(V(s * 0.0125f, 0.031f, -0.0112f), V(0.009f, 0.0045f, 0.0008f), White);
            Box(V(0, 0.031f + s * 0.0125f, -0.0112f), V(0.0045f, 0.009f, 0.0008f), White);
        }
        b.Write("deco_windmill");
    }

    private static void Birdhouse()
    {
        b = new Builder();
        Box(V(0, 0.014f, 0), V(0.003f, 0.028f, 0.003f), DarkBrown);
        Box(V(0, 0.0355f, 0), V(0.018f, 0.015f, 0.014f), Tan);
        Gable(0.022f, 0.018f, 0.043f, 0.052f, Red, V(0, 0.046f, 0));
        Box(V(0, 0.036f, -0.0072f), V(0.005f, 0.005f, 0.0008f), Dark);            // 구멍
        Box(V(0, 0.0295f, -0.0085f), V(0.006f, 0.0012f, 0.004f), LightTan);       // 앉는 막대
        b.Write("deco_birdhouse");
    }

    private static void Scarecrow()
    {
        b = new Builder();
        Box(V(0, 0.022f, 0), V(0.003f, 0.044f, 0.003f), Brown);
        Box(V(0, 0.032f, 0), V(0.032f, 0.003f, 0.003f), Brown);
        Box(V(0, 0.028f, 0), V(0.013f, 0.014f, 0.007f), Red);
        Box(V(0, 0.0215f, 0), V(0.0135f, 0.004f, 0.0075f), Blue);                 // 바지 끝
        Blob(V(0, 0.0415f, 0), V(0.0058f, 0.0058f, 0.0058f), 4, 8, Yellow);
        Frustum(V(0, 0.0455f, 0), V(0, 0.0465f, 0), 0.0105f, 0.0105f, 8, Tan, Tan); // 모자 챙
        Frustum(V(0, 0.0465f, 0), V(0, 0.0545f, 0), 0.0068f, 0.0042f, 8, Tan, Tan);
        foreach (float x in new[] { -0.0155f, 0.0155f }) Box(V(x, 0.0295f, 0), V(0.003f, 0.006f, 0.0025f), Yellow); // 짚 손
        b.Write("deco_scarecrow");
    }

    private static void Pond()
    {
        b = new Builder();
        Blob(V(0, 0.0015f, 0), V(0.026f, 0.0034f, 0.019f), 2, 10, Cyan);
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI * 2f * i / 10f;
            Box(V(Mathf.Cos(a) * 0.026f, 0.0035f, Mathf.Sin(a) * 0.019f), V(0.006f, 0.005f, 0.005f), i % 2 == 0 ? Gray : LightGray);
        }
        Blob(V(-0.006f, 0.0036f, 0.003f), V(0.0055f, 0.0008f, 0.0055f), 2, 8, Leaf);   // 연잎
        Blob(V(0.008f, 0.0036f, -0.004f), V(0.0042f, 0.0008f, 0.0042f), 2, 8, Green);
        Blob(V(0.008f, 0.0046f, -0.004f), V(0.0018f, 0.0018f, 0.0018f), 3, 6, Pink);   // 연꽃
        b.Write("deco_pond");
    }

    private static void Swing()
    {
        b = new Builder();
        foreach (float x in new[] { -0.019f, 0.019f })
        {
            Box(V(x, 0.0215f, 0), V(0.003f, 0.043f, 0.003f), Brown);
            Box(V(x, 0.0012f, -0.006f), V(0.003f, 0.0024f, 0.012f), DarkBrown);
            Box(V(x, 0.0012f, 0.006f), V(0.003f, 0.0024f, 0.012f), DarkBrown);
        }
        Box(V(0, 0.0435f, 0), V(0.044f, 0.003f, 0.003f), Brown);
        foreach (float x in new[] { -0.0065f, 0.0065f }) Box(V(x, 0.0285f, 0), V(0.0008f, 0.029f, 0.0008f), Tan);
        Box(V(0, 0.0135f, 0), V(0.0165f, 0.0022f, 0.0058f), LightTan);
        b.Write("deco_swing");
    }

    private static void MiniMailbox()
    {
        b = new Builder();
        Box(V(0, 0.012f, 0), V(0.004f, 0.024f, 0.004f), DarkBrown);
        Box(V(0, 0.0285f, 0), V(0.020f, 0.011f, 0.011f), Red);
        Box(V(0, 0.0345f, 0), V(0.018f, 0.002f, 0.009f), DullRed);
        Box(V(0, 0.0285f, -0.0057f), V(0.008f, 0.005f, 0.0008f), Dark);             // 입구
        Box(V(0.0112f, 0.0325f, 0), V(0.0015f, 0.008f, 0.0015f), Yellow);           // 깃발
        Box(V(0.0125f, 0.0358f, 0), V(0.004f, 0.0025f, 0.0015f), Yellow);
        b.Write("deco_mini_mailbox");
    }

    private static void StreetLamp()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.004f, 0), 0.0065f, 0.0045f, 6, Dark, Dark);
        Frustum(V(0, 0.004f, 0), V(0, 0.050f, 0), 0.0022f, 0.0016f, 6, Dark, Dark);
        Box(V(0, 0.0555f, 0), V(0.011f, 0.009f, 0.011f), Pale);
        foreach (float x in new[] { -0.0058f, 0.0058f })
            foreach (float z in new[] { -0.0058f, 0.0058f })
                Box(V(x, 0.0555f, z), V(0.0014f, 0.0095f, 0.0014f), Dark);
        Frustum(V(0, 0.060f, 0), V(0, 0.0655f, 0), 0.0090f, 0.0010f, 4, Dark, Dark);
        Blob(V(0, 0.0555f, 0), V(0.0035f, 0.0045f, 0.0035f), 3, 6, Yellow);
        b.Write("deco_street_lamp");
    }

    private static void Parasol()
    {
        b = new Builder();
        Box(V(0, 0.021f, 0), V(0.002f, 0.042f, 0.002f), White);
        Frustum(V(0, 0.034f, 0), V(0, 0.041f, 0), 0.024f, 0.013f, 8, Red, Red);
        Frustum(V(0, 0.041f, 0), V(0, 0.0475f, 0), 0.013f, 0.0008f, 8, White, White);
        Box(V(0, 0.0008f, 0.012f), V(0.032f, 0.0016f, 0.013f), Cyan);                // 수건
        Box(V(0, 0.0016f, 0.012f), V(0.032f, 0.0004f, 0.0035f), White);
        b.Write("deco_parasol");
    }

    private static void Oasis()
    {
        b = new Builder();
        Blob(V(0, 0.0008f, 0), V(0.028f, 0.0026f, 0.022f), 2, 10, Tan);
        Blob(V(0, 0.0016f, 0), V(0.021f, 0.0028f, 0.015f), 2, 10, Cyan);
        Frustum(V(0.013f, 0.002f, 0.002f), V(0.016f, 0.034f, 0.002f), 0.0026f, 0.0018f, 6, Brown, Brown);
        Blob(V(0.016f, 0.036f, 0.002f), V(0.012f, 0.0034f, 0.012f), 3, 8, Green);
        Blob(V(0.016f, 0.0345f, 0.002f), V(0.0055f, 0.0034f, 0.0055f), 3, 6, Leaf);
        Blob(V(-0.014f, 0.004f, 0.006f), V(0.007f, 0.0045f, 0.007f), 3, 6, Leaf);    // 덤불
        b.Write("deco_oasis");
    }

    private static void Woodpile()
    {
        b = new Builder();
        float r = 0.0038f, h = 0.0125f;
        foreach (float x in new[] { -0.008f, 0f, 0.008f })
            Frustum(V(x, r, -h), V(x, r, h), r, r, 8, Brown, LightTan);
        foreach (float x in new[] { -0.004f, 0.004f })
            Frustum(V(x, r * 2.7f, -h), V(x, r * 2.7f, h), r, r, 8, DarkBrown, LightTan);
        Frustum(V(0f, r * 4.4f, -h), V(0f, r * 4.4f, h), r, r, 8, Brown, LightTan);
        b.Write("deco_woodpile");
    }

    private static void Gnome()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.018f, 0), 0.0085f, 0.0062f, 8, Blue, Blue);
        Blob(V(0, 0.0215f, 0), V(0.0052f, 0.0052f, 0.0052f), 4, 8, Salmon);
        Blob(V(0, 0.0175f, -0.0028f), V(0.0055f, 0.0075f, 0.0035f), 3, 8, White);    // 수염
        Frustum(V(0, 0.0245f, 0), V(0, 0.044f, 0), 0.0064f, 0.0004f, 8, Red, Red);   // 뾰족 모자
        Blob(V(0, 0.0212f, -0.0050f), V(0.0010f, 0.0010f, 0.0010f), 2, 4, Pink);    // 코
        Box(V(0, 0.003f, -0.0075f), V(0.012f, 0.003f, 0.004f), Dark);                 // 신발
        b.Write("deco_gnome");
    }

    private static void Stump()
    {
        b = new Builder();
        Frustum(V(0, 0, 0), V(0, 0.016f, 0), 0.0125f, 0.0105f, 10, Brown, LightTan);
        Frustum(V(0, 0.0158f, 0), V(0, 0.0166f, 0), 0.0075f, 0.0075f, 10, Tan, Tan);   // 나이테 안쪽
        foreach (float a in new[] { 0.3f, 2.2f, 4.1f })
            Box(V(Mathf.Cos(a) * 0.0125f, 0.0025f, Mathf.Sin(a) * 0.0125f), V(0.005f, 0.005f, 0.004f), DarkBrown);   // 뿌리
        Blob(V(0.006f, 0.0172f, 0.003f), V(0.004f, 0.0014f, 0.004f), 2, 6, Leaf);      // 이끼
        b.Write("deco_stump");
    }

    private static void Log()
    {
        b = new Builder();
        Frustum(V(-0.021f, 0.006f, 0), V(0.021f, 0.006f, 0), 0.0058f, 0.0058f, 10, Brown, LightTan);
        Box(V(0.004f, 0.0125f, 0), V(0.004f, 0.005f, 0.004f), Brown);                    // 가지 그루터기
        Blob(V(-0.006f, 0.0115f, 0), V(0.006f, 0.0018f, 0.0035f), 2, 6, Leaf);           // 이끼
        b.Write("deco_log");
    }

    private static void Starfish()
    {
        b = new Builder();
        const int n = 10;
        Vector3 ct = V(0, 0.004f, 0), cb = V(0, 0f, 0), inside = V(0, 0.002f, 0);
        Vector3[] top = new Vector3[n], bot = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float r = i % 2 == 0 ? 0.0185f : 0.0075f;
            float a = Mathf.PI * 2f * i / n + Mathf.PI * 0.5f;
            top[i] = V(Mathf.Cos(a) * r, i % 2 == 0 ? 0.0028f : 0.004f, Mathf.Sin(a) * r);
            bot[i] = V(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            b.Tri(ct, top[i], top[j], Coral, inside);
            b.Tri(cb, bot[j], bot[i], Coral, inside);
            b.Quad(bot[i], bot[j], top[j], top[i], Orange, inside);
        }
        for (int i = 0; i < n; i += 2) Blob(top[i] * 0.55f + V(0, 0.0012f, 0), V(0.0012f, 0.0010f, 0.0012f), 2, 4, Pale); // 돌기
        b.Write("deco_starfish");
    }

private static void Turquoise()
    {
        b = new Builder();
        Blob(V(0, 0.0025f, 0), V(0.014f, 0.0035f, 0.012f), 2, 7, Gray);                 // 바탕 돌
        Blob(V(0, 0.0085f, 0), V(0.011f, 0.0085f, 0.0085f), 2, 6, Teal);
        Blob(V(0.004f, 0.0095f, -0.002f), V(0.0060f, 0.0060f, 0.0050f), 2, 5, Cyan);
        Blob(V(-0.005f, 0.0070f, 0.003f), V(0.0045f, 0.0048f, 0.0045f), 2, 5, Teal);
        b.Write("deco_turquoise");
    }
}
