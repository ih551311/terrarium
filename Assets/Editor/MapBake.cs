using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬 지도 그림(Assets/Data/IslandMap.png)을 만든다.
/// 1) 지형 계산(IslandSetup)으로 바다, 모래사장, 풀밭, 숲, 꽃 언덕을 칠하고, 땅 높이에 따라 음영(입체감)을 준다.
/// 2) 섬 씬의 실제 나무와 바위 위치를 하나씩 그려 넣는다. (소나무는 짙은 삼각, 활엽수는 둥근 덩어리, 야자수는 별, 바위는 회색)
/// 길은 그리지 않는다. 섬이나 나무를 다시 만들었으면 이 메뉴를 다시 누른다.
/// 메뉴: 유리병 정원 > 섬 지도 만들기.
/// </summary>
public static class MapBake
{
    /// <summary>지도가 덮는 땅의 가운데(세계 x, z)</summary>
    public static readonly Vector2 Center = new Vector2(0f, 30f);

    /// <summary>지도 한 변의 절반(미터). 섬 반지름 150m에 바다 여유를 더했다.</summary>
    public const float HalfExtent = 175f;

    public const string ImagePath = "Assets/Data/IslandMap.png";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const int Size = 1024;

    [MenuItem("유리병 정원/섬 지도 만들기")]
    public static string Build()
    {
        float pxPerMeter = Size / (HalfExtent * 2f);
        var pixels = new Color[Size * Size];

        // 1) 지형 색 + 음영. 해가 왼쪽 위(북서)에서 비치는 것처럼, 땅이 북서쪽으로 올라갈수록 어둡고 남동쪽으로 올라갈수록 밝다.
        const float Step = 2f; // 기울기를 재는 간격(미터)
        for (int y = 0; y < Size; y++)
        {
            float z = Center.y + ((y + 0.5f) / Size - 0.5f) * 2f * HalfExtent; // 위쪽이 북쪽(+z)
            for (int x = 0; x < Size; x++)
            {
                float wx = Center.x + ((x + 0.5f) / Size - 0.5f) * 2f * HalfExtent;
                Color c = IslandSetup.MapColorAt(wx, z);

                float h = IslandSetup.MapHeightAt(wx, z);
                if (h > 0.4f)
                {
                    float dx = IslandSetup.MapHeightAt(wx + Step, z) - IslandSetup.MapHeightAt(wx - Step, z);
                    float dz = IslandSetup.MapHeightAt(wx, z + Step) - IslandSetup.MapHeightAt(wx, z - Step);
                    float shade = Mathf.Clamp((dx * 0.7f - dz * 0.7f) * 0.05f, -0.22f, 0.22f); // 동쪽과 남쪽으로 오르막이면 밝게
                    c = new Color(Mathf.Clamp01(c.r + shade), Mathf.Clamp01(c.g + shade), Mathf.Clamp01(c.b + shade), 1f);
                }
                pixels[y * Size + x] = c;
            }
        }

        // 2) 해안선 둘레에 부드러운 어두운 윤곽 (섬이 바다에서 또렷하게 떠 보이게)
        var outlined = (Color[])pixels.Clone();
        for (int y = 2; y < Size - 2; y++)
        {
            for (int x = 2; x < Size - 2; x++)
            {
                float wx = Center.x + ((x + 0.5f) / Size - 0.5f) * 2f * HalfExtent;
                float wz = Center.y + ((y + 0.5f) / Size - 0.5f) * 2f * HalfExtent;
                bool land = IslandSetup.MapHeightAt(wx, wz) > 0.4f;
                if (land) continue;
                float wx2 = wx + 2.2f, wz2 = wz + 2.2f;
                bool nearLand = IslandSetup.MapHeightAt(wx2, wz) > 0.4f || IslandSetup.MapHeightAt(wx - 2.2f, wz) > 0.4f
                             || IslandSetup.MapHeightAt(wx, wz2) > 0.4f || IslandSetup.MapHeightAt(wx, wz - 2.2f) > 0.4f;
                if (nearLand) outlined[y * Size + x] = Color.Lerp(pixels[y * Size + x], new Color(0.2f, 0.45f, 0.62f), 0.7f);
            }
        }
        pixels = outlined;

        // 2.5) 꽃 언덕에 작은 꽃들을 흩뿌린다 (밋밋한 색 대신 꽃밭처럼 보이게)
        var flowerRng = new System.Random(77);
        Color[] petals = { new Color(1f, 1f, 0.95f), new Color(0.55f, 0.7f, 1f), new Color(1f, 0.85f, 0.35f), new Color(1f, 0.6f, 0.7f) };
        for (int i = 0; i < 900; i++)
        {
            float a = (float)(flowerRng.NextDouble() * Mathf.PI * 2f), rr = Mathf.Sqrt((float)flowerRng.NextDouble()) * 44f;
            float fx = IslandSetup.MapFlowerHill.x + Mathf.Cos(a) * rr, fz = IslandSetup.MapFlowerHill.y + Mathf.Sin(a) * rr;
            if (IslandSetup.MapHeightAt(fx, fz) < 0.5f) continue;
            Vector2 fp = ToPixel(fx, fz);
            Color pc = petals[flowerRng.Next(petals.Length)];
            Put(pixels, (int)fp.x, (int)fp.y, pc);
            Put(pixels, (int)fp.x + 1, (int)fp.y, pc, 0.8f);
        }

        // 3) 섬 씬의 나무와 바위를 그려 넣는다
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        int trees = 0, rocks = 0;

        foreach (Harvestable h in Object.FindObjectsByType<Harvestable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Vector3 p = h.transform.position;
            Vector2 px = ToPixel(p.x, p.z);
            float scale = Mathf.Max(0.8f, h.transform.localScale.x);
            if (h.name.StartsWith("Pine"))
            {
                Triangle(pixels, px, 5.5f * Mathf.Sqrt(scale), new Color(0.12f, 0.36f, 0.2f), new Color(0.05f, 0.2f, 0.12f));
                trees++;
            }
            else if (h.name.StartsWith("Palm"))
            {
                Star(pixels, px, 7f, new Color(0.18f, 0.55f, 0.28f));
                trees++;
            }
            else if (h.name.StartsWith("BasicTree"))
            {
                Disc(pixels, px, 5.5f * Mathf.Sqrt(scale), new Color(0.22f, 0.5f, 0.2f), new Color(0.1f, 0.3f, 0.12f));
                trees++;
            }
            else
            {
                Disc(pixels, px, 3.6f * Mathf.Sqrt(scale), new Color(0.62f, 0.62f, 0.66f), new Color(0.38f, 0.38f, 0.42f)); // 바위
                rocks++;
            }
        }

        var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(ImagePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ImagePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ImagePath);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        return "섬 지도 만들기 완료: 나무 " + trees + "그루, 바위 " + rocks + "개를 그렸어요 (" + ImagePath + ")";
    }

    // ───────── 그리기 도우미 ─────────

    private static Vector2 ToPixel(float wx, float wz)
    {
        float u = (wx - Center.x) / (HalfExtent * 2f) + 0.5f;
        float v = (wz - Center.y) / (HalfExtent * 2f) + 0.5f;
        return new Vector2(u * Size, v * Size);
    }

    private static void Put(Color[] px, int x, int y, Color c, float alpha = 1f)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return;
        px[y * Size + x] = Color.Lerp(px[y * Size + x], c, alpha);
    }

    /// <summary>둥근 덩어리: 안쪽은 fill, 가장자리는 edge. 그림자를 남동쪽에 살짝 둔다.</summary>
    private static void Disc(Color[] px, Vector2 center, float r, Color fill, Color edge)
    {
        int R = Mathf.CeilToInt(r + 2f);
        for (int dy = -R; dy <= R + 2; dy++)
            for (int dx = -R; dx <= R + 2; dx++)
            {
                // 그림자(아래 오른쪽으로 2픽셀)
                if ((dx - 2) * (dx - 2) + (dy - 2) * (dy - 2) <= r * r) Put(px, (int)center.x + dx, (int)center.y + dy, new Color(0.1f, 0.2f, 0.1f), 0.25f);
            }
        for (int dy = -R; dy <= R; dy++)
            for (int dx = -R; dx <= R; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r) continue;
                float light = 1f - (dx - dy) * 0.5f / r * 0.18f; // 왼쪽 위가 살짝 밝다
                Color c = d > r - 1.3f ? edge : fill * light;
                c.a = 1f;
                Put(px, (int)center.x + dx, (int)center.y + dy, c);
            }
    }

    /// <summary>위가 뾰족한 삼각형 (소나무)</summary>
    private static void Triangle(Color[] px, Vector2 center, float r, Color fill, Color edge)
    {
        int R = Mathf.CeilToInt(r + 3f);
        for (int dy = -R; dy <= R; dy++)
        {
            float t = (dy + r) / (2f * r); // 0 아래 ~ 1 위
            if (t < 0f || t > 1f) continue;
            float half = r * (1f - t) * 0.95f;
            for (int dx = -R; dx <= R; dx++)
            {
                if (Mathf.Abs(dx) > half) continue;
                // 그림자
                Put(px, (int)center.x + dx + 2, (int)center.y + dy - 1, new Color(0.1f, 0.2f, 0.1f), 0.15f);
            }
        }
        for (int dy = -R; dy <= R; dy++)
        {
            float t = (dy + r) / (2f * r);
            if (t < 0f || t > 1f) continue;
            float half = r * (1f - t) * 0.95f;
            for (int dx = -R; dx <= R; dx++)
            {
                if (Mathf.Abs(dx) > half) continue;
                bool border = Mathf.Abs(dx) > half - 1.2f || dy < -r + 1.5f;
                Color c = border ? edge : fill * (1f + (dx < 0 ? 0.12f : -0.05f));
                c.a = 1f;
                Put(px, (int)center.x + dx, (int)center.y + dy, c);
            }
        }
    }

    /// <summary>다섯 갈래 별 (야자수 잎)</summary>
    private static void Star(Color[] px, Vector2 center, float r, Color color)
    {
        for (int arm = 0; arm < 5; arm++)
        {
            float a = arm * 2f * Mathf.PI / 5f + Mathf.PI / 2f;
            for (float s = 0f; s <= r; s += 0.5f)
            {
                int x = (int)(center.x + Mathf.Cos(a) * s), y = (int)(center.y + Mathf.Sin(a) * s);
                float w = Mathf.Lerp(1.8f, 0.6f, s / r);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (dx * dx + dy * dy <= w * w) Put(px, x + dx, y + dy, color);
            }
        }
        Put(px, (int)center.x, (int)center.y, new Color(0.45f, 0.3f, 0.15f));
    }
}