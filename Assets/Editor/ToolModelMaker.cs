using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Builder = ShellModelMaker.Builder;

/// <summary>
/// 1인칭으로 보이는 도구(도끼, 곡괭이, 괭이)와 그것을 쥔 오른손을 다른 장식 모델과 같은 팔레트 로우폴리로 만든다.
/// 메쉬는 Assets/Models/Tools/*.asset (같은 파일 안에서 내용만 바꾼다), 묶음 프리팹은 Assets/Resources/ToolRig.prefab.
/// 좌표: 자루가 +Y로 서 있고, 손이 쥔 자리(자루 아래쪽)가 원점이다. 카메라는 +Z를 보므로 날은 +Z(앞)를 향한다.
/// 손: 손등은 오른쪽(+X), 손가락 네 개가 자루 앞쪽을 감아 왼쪽에서 끝나고, 엄지는 카메라 쪽(-Z)을 가로지른다.
/// 팔뚝과 소매는 오른쪽 아래 뒤(화면 밖)로 빠진다. 도구 머리(Head)는 흰색이라 강화 단계 색을 곱하면 그 색이 된다.
/// ToolView가 Resources에서 불러 쓴다. 메뉴: 유리병 정원 > 도구와 손 모델 만들기
/// </summary>
public static class ToolModelMaker
{
    private const string MeshFolder = "Assets/Models/Tools";
    private const string PrefabPath = "Assets/Resources/ToolRig.prefab";
    private const string PaletteMaterialPath = "Assets/Models/Deco/LowpolyPalette.mat";
    private const int PaletteCount = 34;

    // 팔레트 번호
    private const int LightGray = 1, White = 2, Brown = 4, DarkBrown = 5, Wood = 8, Green = 9, SleeveRib = 12, Skin = 23, Cream = 30, SkinShade = 17; // 손가락 마디는 살짝 붉은 기

    private static Builder b;

    [MenuItem("유리병 정원/도구와 손 모델 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory(MeshFolder);
        Directory.CreateDirectory("Assets/Resources");
        Material palette = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
        if (palette == null) return "팔레트 재질을 찾지 못했어요: " + PaletteMaterialPath;

        var meshes = new Dictionary<string, Mesh>();
        b = new Builder(); Handle(); meshes["Handle"] = Save("tool_handle");
        b = new Builder(); AxeHead(); meshes["AxeHead"] = Save("tool_axe_head");
        b = new Builder(); PickHead(); meshes["PickHead"] = Save("tool_pickaxe_head");
        b = new Builder(); HoeHead(); meshes["HoeHead"] = Save("tool_hoe_head");
        b = new Builder(); Hand(); meshes["Hand"] = Save("tool_hand", true);
        AssetDatabase.SaveAssets();

        // 묶음: Hand + Axe(Handle, Head) + Pickaxe(Handle, Head) + Hoe(Handle, Head)
        GameObject root = new GameObject("ToolRig");
        Part(root.transform, "Hand", meshes["Hand"], palette);
        foreach (var t in new[] { ("Axe", "AxeHead"), ("Pickaxe", "PickHead"), ("Hoe", "HoeHead") })
        {
            GameObject tool = new GameObject(t.Item1);
            tool.transform.SetParent(root.transform, false);
            Part(tool.transform, "Handle", meshes["Handle"], palette);
            Part(tool.transform, "Head", meshes[t.Item2], palette);
        }
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return "도구 3종과 손 모델을 만들었어요: " + PrefabPath;
    }

    private static void Part(Transform parent, string name, Mesh mesh, Material mat)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    /// <summary>빌더의 삼각형을 메쉬 에셋으로 저장한다 (있으면 같은 파일 안에서 내용만 바꾼다). 면마다 따로 점을 둬서 각진 그림자가 생긴다.</summary>
    private static Mesh Save(string name, bool smooth = false)
    {
        var m = new Mesh { name = name };
        var uv = new List<Vector2>(b.pos.Count);
        var idx = new int[b.pos.Count];
        for (int t = 0; t < b.colors.Count; t++)
            for (int k = 0; k < 3; k++) uv.Add(new Vector2((b.colors[t] + 0.5f) / PaletteCount, 0.5f));
        for (int i = 0; i < idx.Length; i++) idx[i] = i;
        if (smooth)
        {
            // 같은 자리, 같은 색의 점을 하나로 합쳐서 면 사이가 매끄럽게 이어지게 한다 (각진 그림자 없이 부드러운 음영)
            var map = new Dictionary<(int, int, int, int), int>();
            var verts = new List<Vector3>(); var uvs = new List<Vector2>();
            for (int i = 0; i < b.pos.Count; i++)
            {
                Vector3 p = b.pos[i];
                var key = (Mathf.RoundToInt(p.x * 50000f), Mathf.RoundToInt(p.y * 50000f), Mathf.RoundToInt(p.z * 50000f), b.colors[i / 3]);
                if (!map.TryGetValue(key, out int at)) { at = verts.Count; map[key] = at; verts.Add(p); uvs.Add(uv[i]); }
                idx[i] = at;
            }
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
        }
        else
        {
            m.SetVertices(b.pos);
            m.SetUVs(0, uv);
        }
        m.SetTriangles(idx, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();

        string path = MeshFolder + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(m, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(m);
            return existing;
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // ───────── 도형 도우미 ─────────

    /// <summary>두 점 사이의 굵기가 변하는 기둥 (양 끝은 막는다)</summary>
    private static void Tube(Vector3 p0, Vector3 p1, float r0, float r1, int sides, int color, int cap = -1)
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

    /// <summary>타원체 (손가락 끝, 마디)</summary>
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

    /// <summary>세 축(ax, ay, az)으로 돌린 상자. half는 각 축의 반 길이</summary>
    private static void Box(Vector3 c, Vector3 ax, Vector3 ay, Vector3 az, Vector3 half, int color)
    {
        Vector3 C(int sx, int sy, int sz) => c + ax * half.x * sx + ay * half.y * sy + az * half.z * sz;
        b.Quad(C(1, -1, -1), C(1, 1, -1), C(1, 1, 1), C(1, -1, 1), color, c);
        b.Quad(C(-1, -1, -1), C(-1, -1, 1), C(-1, 1, 1), C(-1, 1, -1), color, c);
        b.Quad(C(-1, 1, -1), C(-1, 1, 1), C(1, 1, 1), C(1, 1, -1), color, c);
        b.Quad(C(-1, -1, -1), C(1, -1, -1), C(1, -1, 1), C(-1, -1, 1), color, c);
        b.Quad(C(-1, -1, 1), C(1, -1, 1), C(1, 1, 1), C(-1, 1, 1), color, c);
        b.Quad(C(-1, -1, -1), C(-1, 1, -1), C(1, 1, -1), C(1, -1, -1), color, c);
    }

    private static void Box(Vector3 c, Vector3 half, int color) => Box(c, Vector3.right, Vector3.up, Vector3.forward, half, color);

    /// <summary>점들을 잇는 네모난 관. 단면은 x 방향 반폭 hw, 그와 직각인 방향 반높이 hh. 끝은 뾰족하게 모인다.</summary>
    private static void SquareTube(Vector3[] pts, float[] hw, float[] hh, int color)
    {
        Vector3[][] rings = new Vector3[pts.Length][];
        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 dir = (i < pts.Length - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
            Vector3 n = Vector3.Cross(dir, Vector3.right).normalized;
            rings[i] = new[]
            {
                pts[i] + Vector3.right * hw[i] + n * hh[i], pts[i] - Vector3.right * hw[i] + n * hh[i],
                pts[i] - Vector3.right * hw[i] - n * hh[i], pts[i] + Vector3.right * hw[i] - n * hh[i]
            };
        }
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 mid = (pts[i] + pts[i + 1]) * 0.5f;
            for (int k = 0; k < 4; k++)
                b.Quad(rings[i][k], rings[i][(k + 1) % 4], rings[i + 1][(k + 1) % 4], rings[i + 1][k], color, mid);
        }
        Vector3 last = pts[pts.Length - 1], end = last + (last - pts[pts.Length - 2]).normalized * 0.004f;
        for (int k = 0; k < 4; k++) b.Tri(rings[pts.Length - 1][k], rings[pts.Length - 1][(k + 1) % 4], end, color, pts[pts.Length - 2]);
    }

    /// <summary>자루를 감싼 원통 위의 점: 각도(도, +X에서 +Z 쪽으로), 반지름, 높이</summary>
    private static Vector3 Arc(float deg, float r, float y)
    {
        float a = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
    }

    // ───────── 자루 (세 도구 공통) ─────────

    private static void Handle()
    {
        Tube(new Vector3(0f, -0.135f, 0f), new Vector3(0f, -0.115f, 0f), 0.017f, 0.025f, 8, DarkBrown);  // 끝 손잡이 혹
        Tube(new Vector3(0f, -0.115f, 0f), new Vector3(0f, 0.075f, 0f), 0.021f, 0.020f, 8, Brown);      // 손이 쥐는 곳 (가죽 감은 색)
        Tube(new Vector3(0f, 0.075f, 0f), new Vector3(0f, 0.46f, 0f), 0.019f, 0.016f, 8, Wood);         // 나무 자루
        for (int i = 0; i < 3; i++) // 감은 끈 자국
            Tube(new Vector3(0f, -0.10f + i * 0.06f, 0f), new Vector3(0f, -0.092f + i * 0.06f, 0f), 0.0225f, 0.0225f, 8, DarkBrown);
    }

    // ───────── 도끼 머리: 앞(+Z)으로 넓게 퍼지는 날 ─────────

    private static void AxeHead()
    {
        Box(new Vector3(0f, 0.452f, 0.002f), new Vector3(0.019f, 0.036f, 0.026f), LightGray);   // 자루가 끼는 눈
        Box(new Vector3(0f, 0.452f, -0.034f), new Vector3(0.015f, 0.022f, 0.012f), LightGray);  // 뒤통수
        // 날: 뒤는 두껍고(±0.013) 앞 끝은 얇다(±0.0025). 앞 끝은 위아래로 퍼진다
        Vector3 bt = new Vector3(0f, 0.478f, 0.024f), bb = new Vector3(0f, 0.426f, 0.024f);
        Vector3 ft = new Vector3(0f, 0.515f, 0.140f), fb = new Vector3(0f, 0.383f, 0.146f);
        Vector3 xb = new Vector3(0.013f, 0f, 0f), xf = new Vector3(0.0025f, 0f, 0f);
        Vector3 inside = new Vector3(0f, 0.45f, 0.08f);
        b.Quad(bt + xb, bb + xb, fb + xf, ft + xf, White, inside);     // 오른쪽 면
        b.Quad(bt - xb, ft - xf, fb - xf, bb - xb, White, inside);     // 왼쪽 면
        b.Quad(bt + xb, ft + xf, ft - xf, bt - xb, LightGray, inside); // 윗면
        b.Quad(bb + xb, bb - xb, fb - xf, fb + xf, LightGray, inside); // 아랫면
        b.Quad(ft + xf, fb + xf, fb - xf, ft - xf, White, inside);     // 날끝
    }

    // ───────── 곡괭이 머리: 앞은 뾰족한 곡괭이, 뒤는 납작한 정 ─────────

    private static void PickHead()
    {
        Box(new Vector3(0f, 0.456f, 0f), new Vector3(0.020f, 0.032f, 0.026f), LightGray);
        SquareTube(new[] { new Vector3(0f, 0.462f, 0.02f), new Vector3(0f, 0.458f, 0.10f), new Vector3(0f, 0.43f, 0.19f) },
                   new[] { 0.015f, 0.010f, 0.002f }, new[] { 0.014f, 0.009f, 0.002f }, White);
        SquareTube(new[] { new Vector3(0f, 0.462f, -0.02f), new Vector3(0f, 0.458f, -0.09f), new Vector3(0f, 0.44f, -0.155f) },
                   new[] { 0.015f, 0.016f, 0.017f }, new[] { 0.013f, 0.008f, 0.0025f }, White);
    }

    // ───────── 괭이 머리: 앞으로 꺾인 목과 아래로 내려온 넓은 날 ─────────

    private static void HoeHead()
    {
        Box(new Vector3(0f, 0.452f, 0f), new Vector3(0.018f, 0.026f, 0.020f), LightGray);  // 자루 끝 쇠테
        SquareTube(new[] { new Vector3(0f, 0.465f, 0.012f), new Vector3(0f, 0.495f, 0.045f), new Vector3(0f, 0.488f, 0.072f) },
                   new[] { 0.007f, 0.007f, 0.007f }, new[] { 0.007f, 0.007f, 0.007f }, LightGray);
        // 날: 너비 0.15, 위(0.495)에서 아래(0.405)로, 아래로 갈수록 조금 앞으로 기운다
        Vector3 up = new Vector3(0f, 0.495f, 0.074f), down = new Vector3(0f, 0.405f, 0.092f);
        Vector3 dir = (down - up).normalized;
        Vector3 normal = Vector3.Cross(Vector3.right, dir).normalized;
        Box((up + down) * 0.5f, Vector3.right, dir, normal, new Vector3(0.075f, Vector3.Distance(up, down) * 0.5f, 0.004f), White);
        Box(down + dir * 0.002f, Vector3.right, dir, normal, new Vector3(0.078f, 0.004f, 0.003f), LightGray); // 날 끝
    }

    // ───────── 자루를 쥔 오른손과 팔뚝 ─────────

    /// <summary>회전시킨 타원체 (손등, 손바닥 두툼한 곳). yawDeg만큼 Y축으로 돌린다</summary>
    private static void TurnedBlob(Vector3 c, float yawDeg, Vector3 r, int rings, int sec, int color)
    {
        b.xf = Matrix4x4.TRS(c, Quaternion.Euler(0f, -yawDeg, 0f), Vector3.one);
        Blob(Vector3.zero, r, rings, sec, color);
        b.xf = Matrix4x4.identity;
    }

    // ───────── 부드러운 손: 이어진 관(Sweep)으로 만든다 ─────────

    /// <summary>점들을 부드러운 곡선으로 늘린다 (Catmull-Rom). 굵기도 같이 이어 준다.</summary>
    private static void Smooth(Vector3[] ctrl, float[] rad, int sub, out Vector3[] pts, out float[] radii)
    {
        var p = new List<Vector3>(); var r = new List<float>();
        for (int i = 0; i < ctrl.Length - 1; i++)
        {
            Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Length - 1)];
            for (int s = 0; s < sub; s++)
            {
                float t = s / (float)sub, t2 = t * t, t3 = t2 * t;
                p.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                r.Add(Mathf.Lerp(rad[i], rad[i + 1], t));
            }
        }
        p.Add(ctrl[ctrl.Length - 1]); r.Add(rad[rad.Length - 1]);
        pts = p.ToArray(); radii = r.ToArray();
    }

    /// <summary>점들을 따라 굵기가 변하는 매끈한 관. 고리를 서로 나눠 써서 이음매가 없다. roundEnd면 끝을 반구로 둥글게 닫는다.</summary>
    private static void Sweep(Vector3[] pts, float[] radii, int sides, int color, bool roundEnd)
    {
        var P = new List<Vector3>(pts); var Rr = new List<float>(radii);
        if (roundEnd)
        {
            Vector3 tl = (pts[pts.Length - 1] - pts[pts.Length - 2]).normalized;
            float rl = radii[radii.Length - 1];
            foreach (float a in new[] { 35f, 62f, 82f })
            {
                P.Add(pts[pts.Length - 1] + tl * rl * Mathf.Sin(a * Mathf.Deg2Rad));
                Rr.Add(rl * Mathf.Cos(a * Mathf.Deg2Rad));
            }
        }
        int n = P.Count;
        var rings = new Vector3[n][];
        Vector3 t0 = (P[1] - P[0]).normalized;
        Vector3 u = Vector3.Cross(t0, Mathf.Abs(t0.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        for (int i = 0; i < n; i++)
        {
            Vector3 t = (P[Mathf.Min(i + 1, n - 1)] - P[Mathf.Max(i - 1, 0)]).normalized;
            u = (u - Vector3.Dot(u, t) * t).normalized;
            Vector3 v = Vector3.Cross(t, u);
            rings[i] = new Vector3[sides];
            for (int k = 0; k < sides; k++)
            {
                float a = Mathf.PI * 2f * k / sides;
                rings[i][k] = P[i] + Rr[i] * (Mathf.Cos(a) * u + Mathf.Sin(a) * v);
            }
        }
        for (int i = 0; i < n - 1; i++)
        {
            Vector3 mid = (P[i] + P[i + 1]) * 0.5f;
            for (int k = 0; k < sides; k++)
                b.Quad(rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k], color, mid);
        }
        for (int k = 0; k < sides; k++) // 시작과 끝을 막는다
        {
            b.Tri(P[0], rings[0][(k + 1) % sides], rings[0][k], color, P[1]);
            b.Tri(P[n - 1], rings[n - 1][k], rings[n - 1][(k + 1) % sides], color, P[n - 2]);
        }
    }

    private static void Finger(Vector3[] ctrl, float[] rad, int color)
    {
        Smooth(ctrl, rad, 4, out Vector3[] pts, out float[] radii);
        Sweep(pts, radii, 10, color, true);
    }

    private static void Hand()
    {
        const float R = 0.021f; // 자루 반지름 (쥐는 곳)
        // 손가락 네 개 (위에서부터 검지, 중지, 약지, 새끼). 서로 맞닿게 붙여서 깔끔한 한 덩이로 보이고,
        // 손등 쪽 뿌리(14도)에서 자루 앞(90도)과 왼쪽(180도)을 감아 카메라 쪽(220도 안팎)에서 끝난다.
        float[] ys = { 0.050f, 0.025f, 0.000f, -0.024f };
        float[] rr = { 0.0120f, 0.0126f, 0.0120f, 0.0105f };
        float[] tip = { 228f, 234f, 226f, 212f };
        for (int f = 0; f < 4; f++)
        {
            float rad = R + rr[f] + 0.0008f;
            Vector3[] ctrl =
            {
                Arc(10f, rad + 0.007f, ys[f] + 0.002f),
                Arc(60f, rad + 0.001f, ys[f]),
                Arc(120f, rad, ys[f]),
                Arc(180f, rad - 0.0005f, ys[f] - 0.001f),
                Arc(tip[f], rad - 0.001f, ys[f] - 0.003f)
            };
            Finger(ctrl, new[] { rr[f] * 1.05f, rr[f], rr[f] * 0.97f, rr[f] * 0.92f, rr[f] * 0.88f }, Skin);
        }

        // 손등과 손바닥 두툼한 곳: 부드러운 큰 타원체
        TurnedBlob(Arc(-8f, R + 0.016f, 0.010f), -8f, new Vector3(0.018f, 0.058f, 0.031f), 6, 12, Skin);
        TurnedBlob(Arc(-58f, R + 0.013f, -0.024f), -58f, new Vector3(0.017f, 0.038f, 0.022f), 5, 10, Skin);

        // 엄지: 카메라 쪽 면을 비스듬히 올라가 손가락 끝을 덮는다
        Vector3 t0 = Arc(-48f, R + 0.020f, -0.034f), t1 = Arc(-92f, R + 0.024f, -0.006f), t2 = Arc(-130f, R + 0.023f, 0.022f);
        Finger(new[] { t0, t1, t2 }, new[] { 0.0150f, 0.0138f, 0.0122f }, Skin);

        // 손목, 소맷부리, 소매: 손등 아래에서 오른쪽 아래 뒤(화면 밖)로
        Vector3 w0 = Arc(-20f, R + 0.012f, -0.046f);
        Vector3 d = new Vector3(0.36f, -1f, -0.52f).normalized;
        Sweep(new[] { w0 - d * 0.01f, w0 + d * 0.066f }, new[] { 0.027f, 0.029f }, 14, Skin, false);
        Sweep(new[] { w0 + d * 0.060f, w0 + d * 0.092f }, new[] { 0.036f, 0.037f }, 14, Cream, false);     // 소맷부리
        Sweep(new[] { w0 + d * 0.090f, w0 + d * 0.42f }, new[] { 0.039f, 0.047f }, 14, Green, false);      // 스웨터 소매
        for (int i = 0; i < 3; i++)                                                                       // 소매 골지
            Sweep(new[] { w0 + d * (0.13f + i * 0.09f), w0 + d * (0.135f + i * 0.09f) }, new[] { 0.041f + i * 0.0018f, 0.041f + i * 0.0018f }, 14, SleeveRib, false);
    }
}
