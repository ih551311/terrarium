using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병 속 흙과 모래의 겉모습. 흙 그림(색과 울퉁불퉁함)을 코드로 만들어서 병 속 흙 재질에 입힌다.
/// - 흙: 짙고 옅은 갈색 얼룩, 알갱이, 작은 자갈, 부스러진 덩어리 느낌. 윤기는 없다.
/// - 모래: 고운 알갱이 느낌
/// 흙 모양(메시)에는 그림을 입히려면 필요한 UV와 접선이 없어서, 쓸 때 복사해서 만들어 준다. (원래 메시 에셋은 건드리지 않는다)
/// BottleContents가 흙 모양과 바닥재를 바꿀 때 부른다. 그림은 처음 쓸 때 한 번만 만든다.
/// </summary>
public static class SoilLook
{
    private const int Size = 256;
    private const float TileMeters = 0.07f; // 그림 한 장이 덮는 길이 (7cm). 작을수록 알갱이가 곱다

    private static Texture2D soilAlbedo, soilNormal, sandAlbedo, sandNormal;
    private static readonly Dictionary<Mesh, Mesh> styled = new Dictionary<Mesh, Mesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        soilAlbedo = soilNormal = sandAlbedo = sandNormal = null;
        styled.Clear();
    }

    /// <summary>흙 렌더러에 흙(ground가 null) 또는 모래 등의 바닥재 모습을 입힌다.</summary>
    public static void Apply(Renderer renderer, MaterialData ground)
    {
        if (renderer == null) return;
        EnsureTextures();

        Material m = renderer.material; // 이 병 전용 복사본
        bool soil = ground == null;
        Texture2D albedo = soil ? soilAlbedo : sandAlbedo;
        Texture2D normal = soil ? soilNormal : sandNormal;

        m.mainTexture = albedo;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
        if (m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", soil ? 1.3f : 0.8f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", soil ? 0.04f : 0.12f); // 흙은 윤기 없이
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
        // 그림에 색이 다 들어 있으니 재질 색은 흰색. 다른 바닥재는 그 재료 색을 살짝 입힌다 (모래는 그림이 이미 모래색이라 흰색)
        Color tint = Color.white;
        if (!soil && ground.id != "sand") tint = Color.Lerp(Color.white, ground.placeholderColor, 0.8f);
        m.color = tint;
    }

    /// <summary>
    /// 흙 모양(메시)을 복사해서 그림을 입힐 수 있게 한다: 위에서 내려다본 평면 UV(윗면)와 옆면용 UV, 법선, 접선.
    /// 같은 메시는 한 번만 복사한다.
    /// </summary>
    public static Mesh Styled(Mesh source)
    {
        if (source == null) return null;
        if (styled.TryGetValue(source, out Mesh copy) && copy != null) return copy;

        copy = Object.Instantiate(source);
        copy.name = source.name + "_styled";
        Vector3[] vertices = copy.vertices;
        Vector3[] normals = copy.normals;
        if (normals == null || normals.Length != vertices.Length) { copy.RecalculateNormals(); normals = copy.normals; }

        var uv = new Vector2[vertices.Length];
        float k = 1f / TileMeters;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i], n = normals[i];
            // 위를 보는 면은 위에서 본 평면 UV, 옆을 보는 면은 둘레 방향과 높이 UV (두 방식을 법선으로 섞지 않고 갈라서 쓴다)
            uv[i] = Mathf.Abs(n.y) > 0.5f ? new Vector2(v.x * k, v.z * k) : new Vector2((v.x + v.z) * k * 0.9f, v.y * k);
        }
        copy.uv = uv;
        copy.RecalculateTangents();
        styled[source] = copy;
        return copy;
    }

    // ───────── 그림 만들기 ─────────

    private static void EnsureTextures()
    {
        if (soilAlbedo != null) return;
        BuildSoil();
        BuildSand();
    }

    /// <summary>흙: 얼룩 + 알갱이 + 자갈. 높이 그림도 같이 만들어 울퉁불퉁한 법선 그림을 얻는다.</summary>
    private static void BuildSoil()
    {
        var color = new Color32[Size * Size];
        var height = new float[Size * Size];
        var rnd = new System.Random(101);

        // 1) 얼룩과 덩어리 (큰 것에서 작은 것까지 겹친 부드러운 잡음)
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            float blotch = Fbm(u, v, 3, 4, 7);                 // 큰 얼룩
            float clod = Fbm(u, v, 12, 3, 19);                 // 덩어리
            float grain = Fbm(u, v, 48, 2, 31);                // 고운 결
            float shade = 0.55f + 0.7f * blotch;                // 0.55~1.25 (유리 너머로 보면 뿌옇게 보여서 대비를 크게)
            shade *= 0.85f + 0.3f * clod;
            shade *= 0.92f + 0.16f * grain;
            height[y * Size + x] = clod * 0.55f + grain * 0.45f;

            // 갈색: 기본 흙색에서 붉은 기와 노란 기를 살짝 오간다
            float warm = Fbm(u, v, 5, 2, 43);
            float r = Mathf.Lerp(0.37f, 0.46f, warm) * shade;
            float g = Mathf.Lerp(0.25f, 0.31f, warm) * shade;
            float b = Mathf.Lerp(0.14f, 0.17f, warm) * shade;
            color[y * Size + x] = ToColor32(r, g, b);
        }

        // 2) 알갱이: 밝은 모래알과 어두운 부식토 점
        for (int i = 0; i < Size * Size / 14; i++)
        {
            int x = rnd.Next(Size), y = rnd.Next(Size);
            bool light = rnd.NextDouble() < 0.45;
            float amount = (float)(0.25 + rnd.NextDouble() * 0.4);
            int radius = rnd.NextDouble() < 0.2 ? 2 : 1;
            for (int dy = -radius + 1; dy < radius; dy++)
            for (int dx = -radius + 1; dx < radius; dx++)
            {
                int px = (x + dx + Size) % Size, py = (y + dy + Size) % Size;
                Color32 c = color[py * Size + px];
                float f = light ? 1f + amount : 1f - amount * 0.8f;
                color[py * Size + px] = ToColor32(c.r / 255f * f, c.g / 255f * f, c.b / 255f * f * (light ? 1.05f : 1f));
                height[py * Size + px] += light ? 0.25f : -0.12f;
            }
        }

        // 3) 작은 자갈: 둥글고 회갈색이며 가운데가 볼록하다
        for (int i = 0; i < 70; i++)
        {
            float cx = (float)rnd.NextDouble() * Size, cy = (float)rnd.NextDouble() * Size;
            float radius = 2f + (float)rnd.NextDouble() * 4f;
            float tone = 0.9f + (float)rnd.NextDouble() * 0.5f;
            for (int dy = -8; dy <= 8; dy++)
            for (int dx = -8; dx <= 8; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                if (d >= 1f) continue;
                int px = ((int)cx + dx + Size) % Size, py = ((int)cy + dy + Size) % Size;
                float dome = Mathf.Sqrt(1f - d * d);
                // 빛이 위왼쪽에서 오는 느낌의 음영
                float lit = 0.8f + 0.35f * Mathf.Clamp01(0.5f - (dx + dy) / (radius * 3f));
                Color32 c = color[py * Size + px];
                float mix = Mathf.SmoothStep(0f, 1f, 1.2f - d);
                float r = Mathf.Lerp(c.r / 255f, 0.46f * tone * lit, mix);
                float g = Mathf.Lerp(c.g / 255f, 0.40f * tone * lit, mix);
                float b = Mathf.Lerp(c.b / 255f, 0.34f * tone * lit, mix);
                color[py * Size + px] = ToColor32(r, g, b);
                height[py * Size + px] += dome * 0.9f;
            }
        }

        soilAlbedo = MakeTexture("Soil_Albedo", color, false);
        soilNormal = MakeNormal("Soil_Normal", height, 4.0f);
    }

    /// <summary>모래: 밝은 모래색에 아주 고운 알갱이</summary>
    private static void BuildSand()
    {
        var color = new Color32[Size * Size];
        var height = new float[Size * Size];
        var rnd = new System.Random(202);

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float u = x / (float)Size, v = y / (float)Size;
            float wave = Fbm(u, v, 6, 3, 5);
            float grain = Fbm(u, v, 64, 2, 23);
            float shade = 0.9f + 0.14f * wave;
            shade *= 0.94f + 0.12f * grain;
            height[y * Size + x] = wave * 0.5f + grain * 0.5f;
            color[y * Size + x] = ToColor32(0.92f * shade, 0.83f * shade, 0.62f * shade);
        }
        for (int i = 0; i < Size * Size / 10; i++)
        {
            int x = rnd.Next(Size), y = rnd.Next(Size);
            float f = rnd.NextDouble() < 0.5 ? 1.12f : 0.82f;
            Color32 c = color[y * Size + x];
            color[y * Size + x] = ToColor32(c.r / 255f * f, c.g / 255f * f, c.b / 255f * f);
        }
        sandAlbedo = MakeTexture("Sand_Albedo", color, false);
        sandNormal = MakeNormal("Sand_Normal", height, 1.6f);
    }

    private static Color32 ToColor32(float r, float g, float b)
    {
        return new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
    }

    private static Texture2D MakeTexture(string name, Color32[] pixels, bool linear)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, linear) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        tex.SetPixels32(pixels);
        tex.Apply(true, true);
        return tex;
    }

    /// <summary>높이 그림에서 법선 그림을 만든다 (이웃 높이 차). 가장자리는 반대편과 이어져서 이음매가 안 보인다.</summary>
    private static Texture2D MakeNormal(string name, float[] height, float strength)
    {
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float hl = height[y * Size + (x + Size - 1) % Size], hr = height[y * Size + (x + 1) % Size];
            float hd = height[((y + Size - 1) % Size) * Size + x], hu = height[((y + 1) % Size) * Size + x];
            Vector3 n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
            pixels[y * Size + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
        }
        return MakeTexture(name, pixels, true);
    }

    // ───────── 이어 붙여도 이음매가 없는 부드러운 잡음 ─────────

    private static float Fbm(float u, float v, int cells, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Value(u, v, cells << o, seed + o * 13) * amp;
            norm += amp;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    /// <summary>격자(cells x cells)의 무작위 값을 부드럽게 이은 값. 격자가 돌아서 이어지므로 그림을 반복해도 티가 안 난다.</summary>
    private static float Value(float u, float v, int cells, int seed)
    {
        float x = u * cells, y = v * cells;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        float a = Hash(x0 % cells, y0 % cells, seed), b = Hash((x0 + 1) % cells, y0 % cells, seed);
        float c = Hash(x0 % cells, (y0 + 1) % cells, seed), d = Hash((x0 + 1) % cells, (y0 + 1) % cells, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
