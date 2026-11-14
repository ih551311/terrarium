using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 눈 섬 전용 병 두 종류의 유리 모델과 마개를 코드로 만들어 Assets/Models/Deco 에 .obj 로 저장한다.
///  - 원기둥 병(jar_cylinder): 곧은 원기둥 몸통의 조금 넓은 병, 나무색 마개
///  - 눈꽃 병(jar_lantern): 결정처럼 각진 열 면의 랜턴 모양 유리, 빨간 뾰족 마개
/// 바깥면과 안쪽면, 바닥, 입구 둘레를 가진 속이 빈 유리 껍데기다. 안쪽 반지름 표는 JarSetup이 이 모델에서 읽는다
/// (안쪽 바닥 가운데에 꼭짓점이 있어야 한다). 병 유리 재질은 '장식 모델 가져오기 설정'이 입힌다. 크기는 원래 병과 비슷하다(약 0.2m, 게임에서 2배).
/// 메뉴: 유리병 정원 > 눈 섬 병 모델 만들기 (그다음 장식 모델 가져오기 설정, 병 만들기, 병 그림 만들기, UI 만들기)
/// </summary>
public static class BottleModelMaker
{
    private const int Ice = 33, White = 2, Brown = 4, Red = 14, Gold = 21;

    private static Builder b;

    [MenuItem("유리병 정원/눈 섬 병 모델 만들기")]
    public static string Build()
    {
        Cylinder();
        Lantern();
        AssetDatabase.Refresh();
        return "병 모델 2종(원기둥 병, 눈꽃 병)을 만들었어요 (이어서 '장식 모델 가져오기 설정'을 누르세요)";
    }

    // ───────── 유리 껍데기 ─────────

    /// <summary>
    /// 회전체 유리: outer와 inner는 아래에서 위로 가는 (반지름, 높이) 점들. 첫 점은 축(반지름 0)에서 시작한다.
    /// 바깥 바닥, 바깥 옆면, 입구 둘레, 안쪽 옆면, 안쪽 바닥을 이어서 만든다.
    /// </summary>
    private static void Shell(List<Vector2> outer, List<Vector2> inner, int segments)
    {
        for (int i = 0; i < outer.Count - 1; i++) Ring(outer[i], outer[i + 1], segments, false);
        // 입구 둘레: 바깥 위 끝에서 안쪽 위 끝으로 (위를 본다)
        Ring(outer[outer.Count - 1], inner[inner.Count - 1], segments, false, true);
        for (int i = inner.Count - 1; i > 0; i--) Ring(inner[i], inner[i - 1], segments, true);
    }

    private static void Ring(Vector2 p0, Vector2 p1, int segments, bool inner, bool rim = false)
    {
        for (int j = 0; j < segments; j++)
        {
            float a0 = Mathf.PI * 2f * j / segments, a1 = Mathf.PI * 2f * (j + 1) / segments;
            Vector3 A0 = At(p0, a0), A1 = At(p0, a1), B0 = At(p1, a0), B1 = At(p1, a1);
            Vector3 mid = (A0 + A1 + B0 + B1) * 0.25f;
            Vector3 inside;
            if (rim) inside = new Vector3(mid.x, mid.y - 0.5f, mid.z);                         // 입구 둘레는 위를 본다
            else if (Mathf.Abs(p0.y - p1.y) < 1e-5f) inside = new Vector3(mid.x, mid.y + (inner ? -0.5f : 0.5f), mid.z); // 바닥: 바깥은 아래를, 안쪽은 위를 본다
            else inside = inner ? new Vector3(mid.x * 4f, mid.y, mid.z * 4f) : new Vector3(0f, mid.y, 0f);               // 옆면: 바깥은 밖을, 안쪽은 축 쪽을 본다
            b.Quad(A0, A1, B1, B0, Ice, inside);
        }
    }

    private static Vector3 At(Vector2 p, float angle)
    {
        return new Vector3(p.x * Mathf.Cos(angle), p.y, p.x * Mathf.Sin(angle));
    }

    // ───────── 원기둥 병 ─────────

    private static void Cylinder()
    {
        // 곧은 원기둥 몸통(반지름 0.075)에 입구가 살짝 좁아지는 넓은 병. 바닥 모서리만 아주 조금 깎는다.
        var outer = new List<Vector2>
        {
            new Vector2(0f, 0f), new Vector2(0.070f, 0f), new Vector2(0.075f, 0.006f), new Vector2(0.075f, 0.180f),
            new Vector2(0.064f, 0.192f), new Vector2(0.064f, 0.206f)
        };
        var inner = new List<Vector2>
        {
            new Vector2(0f, 0.006f), new Vector2(0.069f, 0.006f), new Vector2(0.072f, 0.010f), new Vector2(0.072f, 0.178f),
            new Vector2(0.061f, 0.189f), new Vector2(0.061f, 0.206f)
        };
        b = new Builder();
        Shell(outer, inner, 28);
        b.Write("jar_cylinder");

        // 마개: 코르크 몸통과 나무색 덮개
        b = new Builder();
        Cylinder(0.0605f, 0.190f, 0.210f, 14, Brown);
        Cylinder(0.0680f, 0.210f, 0.222f, 14, 6);
        Dome(0.0600f, 0.222f, 0.008f, 6);
        b.Write("jar_cylinder_cork");
    }
    // ───────── 눈꽃 병 (각진 랜턴) ─────────

    private static void Lantern()
    {
        var outer = new List<Vector2>
        {
            new Vector2(0f, 0f), new Vector2(0.046f, 0f), new Vector2(0.060f, 0.012f), new Vector2(0.062f, 0.030f),
            new Vector2(0.062f, 0.130f), new Vector2(0.052f, 0.165f), new Vector2(0.034f, 0.190f), new Vector2(0.034f, 0.222f)
        };
        var inner = new List<Vector2>
        {
            new Vector2(0f, 0.006f), new Vector2(0.054f, 0.006f), new Vector2(0.058f, 0.014f), new Vector2(0.058f, 0.032f), // 안쪽 아래 모서리는 거의 곧게 (흙 옆면이 층져 보이지 않게)
            new Vector2(0.058f, 0.128f), new Vector2(0.048f, 0.162f), new Vector2(0.031f, 0.186f), new Vector2(0.031f, 0.222f)
        };
        b = new Builder();
        Shell(outer, inner, 10); // 열 면: 결정처럼 각이 진다
        b.Write("jar_lantern");

        // 마개: 코르크 몸통, 빨간 뾰족 지붕, 금빛 꼭지
        b = new Builder();
        Cylinder(0.0290f, 0.205f, 0.226f, 10, Brown);
        Cone(0.0420f, 0.0030f, 0.226f, 0.262f, 10, Red);
        Dome(0.0030f, 0.262f, 0.0045f, Gold);
        b.Write("jar_lantern_cork");
    }

    // ───────── 마개 도우미 ─────────

    private static void Cylinder(float radius, float y0, float y1, int sides, int color)
    {
        Cone(radius, radius, y0, y1, sides, color);
    }

    private static void Cone(float r0, float r1, float y0, float y1, int sides, int color)
    {
        Vector3 mid = new Vector3(0f, (y0 + y1) * 0.5f, 0f);
        for (int j = 0; j < sides; j++)
        {
            float a0 = Mathf.PI * 2f * j / sides, a1 = Mathf.PI * 2f * (j + 1) / sides;
            Vector3 A0 = At(new Vector2(r0, y0), a0), A1 = At(new Vector2(r0, y0), a1), B0 = At(new Vector2(r1, y1), a0), B1 = At(new Vector2(r1, y1), a1);
            b.Quad(A0, A1, B1, B0, color, mid);
            b.Tri(new Vector3(0f, y0, 0f), A1, A0, color, mid);
            b.Tri(new Vector3(0f, y1, 0f), B0, B1, color, mid);
        }
    }

    private static void Dome(float radius, float baseY, float height, int color)
    {
        const int rings = 3, sec = 12;
        Vector3 c = new Vector3(0f, baseY, 0f);
        Vector3 P(int i, int j)
        {
            float th = Mathf.PI * 0.5f * i / rings, ph = Mathf.PI * 2f * j / sec;
            return new Vector3(radius * Mathf.Cos(th) * Mathf.Cos(ph), baseY + height * Mathf.Sin(th), radius * Mathf.Cos(th) * Mathf.Sin(ph));
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < sec; j++)
                b.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), color, c);
    }
}
