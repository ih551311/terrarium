using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 지도와 작은 지도에 쓰는 표시 그림 세 장을 코드로 그린다: Assets/Data/MapMarkers
///  - marker_player: 내 위치. 주황 핀(동그란 몸통에 뾰족한 끝). 뾰족한 쪽이 바라보는 방향이다.
///  - marker_hut: 오두막. 빨간 지붕의 작은 집.
///  - marker_boat: 선착장. 돛단배.
/// 재료 아이콘처럼 짙은 갈색 윤곽선을 둘러서 게임 그림과 어울리게 했다. UI 만들기가 부른다(없으면 만든다).
/// 메뉴: 유리병 정원 > 지도 표시 그림 만들기
/// </summary>
public static class MapMarkerMaker
{
    private const string Folder = "Assets/Data/MapMarkers";
    public const string PlayerPath = Folder + "/marker_player.png";
    public const string HutPath = Folder + "/marker_hut.png";
    public const string BoatPath = Folder + "/marker_boat.png";

    private static readonly Color Outline = new Color32(70, 52, 44, 255);
    private const int S = 256;        // 그리는 크기 (반으로 줄여 저장해서 가장자리가 부드럽다)
    private const int Border = 16;    // 윤곽선 두께 (그리는 크기 기준)

    [MenuItem("유리병 정원/지도 표시 그림 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        Make(PlayerPath, Player);
        Make(HutPath, Hut);
        Make(BoatPath, Boat);
        return "지도 표시 그림 3장을 만들었어요: " + Folder;
    }

    public static Sprite Load(string path)
    {
        if (!File.Exists(path)) Build();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ───────── 모양 (u, v는 0~1, v는 위쪽) ─────────

    private static bool InTri(float u, float v, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (u - b.x) * (a.y - b.y) - (a.x - b.x) * (v - b.y);
        float d2 = (u - c.x) * (b.y - c.y) - (b.x - c.x) * (v - c.y);
        float d3 = (u - a.x) * (c.y - a.y) - (c.x - a.x) * (v - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static bool InCircle(float u, float v, float cu, float cv, float r)
    {
        return (u - cu) * (u - cu) + (v - cv) * (v - cv) <= r * r;
    }

    private static Color? Player(float u, float v)
    {
        bool body = InCircle(u, v, 0.5f, 0.40f, 0.25f) || InTri(u, v, new Vector2(0.5f, 0.93f), new Vector2(0.29f, 0.53f), new Vector2(0.71f, 0.53f));
        if (!body) return null;
        if (InCircle(u, v, 0.5f, 0.40f, 0.10f)) return new Color(1f, 0.96f, 0.86f);   // 가운데 밝은 점
        return new Color(0.95f, 0.50f, 0.40f);                                          // 주황 핀
    }

    private static Color? Hut(float u, float v)
    {
        if (u >= 0.43f && u <= 0.57f && v >= 0.12f && v <= 0.38f) return new Color(0.50f, 0.33f, 0.22f);             // 문
        if (InTri(u, v, new Vector2(0.5f, 0.90f), new Vector2(0.10f, 0.54f), new Vector2(0.90f, 0.54f))) return new Color(0.82f, 0.40f, 0.32f); // 지붕
        if (u >= 0.20f && u <= 0.80f && v >= 0.12f && v <= 0.56f) return new Color(0.98f, 0.92f, 0.80f);             // 벽
        return null;
    }

    private static Color? Boat(float u, float v)
    {
        if (InTri(u, v, new Vector2(0.55f, 0.88f), new Vector2(0.55f, 0.40f), new Vector2(0.86f, 0.42f))) return new Color(0.98f, 0.95f, 0.88f); // 돛
        if (u >= 0.47f && u <= 0.54f && v >= 0.32f && v <= 0.90f) return new Color(0.40f, 0.27f, 0.18f);                                        // 돛대
        if (v >= 0.12f && v <= 0.34f)                                                                                                             // 배 몸통 (아래가 좁다)
        {
            float t = (v - 0.12f) / 0.22f;
            float half = Mathf.Lerp(0.25f, 0.40f, t);
            if (Mathf.Abs(u - 0.5f) <= half) return new Color(0.62f, 0.42f, 0.28f);
        }
        return null;
    }

    // ───────── 그리기 ─────────

    private static void Make(string path, Func<float, float, Color?> shape)
    {
        var fill = new Color[S * S];
        var inside = new bool[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color? c = shape((x + 0.5f) / S, (y + 0.5f) / S);
                if (c.HasValue) { fill[y * S + x] = c.Value; inside[y * S + x] = true; }
            }

        // 윤곽선: 모양 밖이면서 모양에서 Border 안에 있는 곳
        var big = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int i = y * S + x;
                if (inside[i]) { big[i] = fill[i]; continue; }
                bool near = false;
                for (int dy = -Border; dy <= Border && !near; dy++)
                {
                    int yy = y + dy;
                    if (yy < 0 || yy >= S) continue;
                    for (int dx = -Border; dx <= Border; dx++)
                    {
                        int xx = x + dx;
                        if (xx < 0 || xx >= S || dx * dx + dy * dy > Border * Border) continue;
                        if (inside[yy * S + xx]) { near = true; break; }
                    }
                }
                big[i] = near ? Outline : new Color(0f, 0f, 0f, 0f);
            }

        // 반으로 줄인다 (네 칸 평균, 투명한 칸의 색은 섞지 않는다)
        int H = S / 2;
        var tex = new Texture2D(H, H, TextureFormat.RGBA32, false);
        var px = new Color[H * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < H; x++)
            {
                Color sum = Color.clear; float a = 0f;
                for (int k = 0; k < 4; k++)
                {
                    Color c = big[(y * 2 + k / 2) * S + x * 2 + k % 2];
                    sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, 0f);
                    a += c.a;
                }
                px[y * H + x] = a > 0f ? new Color(sum.r / a, sum.g / a, sum.b / a, a / 4f) : Color.clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
