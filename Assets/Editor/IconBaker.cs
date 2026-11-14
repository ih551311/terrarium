using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 재료 아이콘을 3D 모델에서 찍어 만든다: Assets/Data/Icons/(재료 id).png
/// 재료마다 placedPrefab(병 안에 놓이는 3D 모델)을 투명 배경으로 비스듬히 찍고, 둘레에 짙은 윤곽선을 둘러서
/// 3D 모습과 똑같은 로우폴리 아이콘을 만든다. 만든 그림은 MaterialData.icon에 연결한다.
/// 모델이 없는 재료(모래)는 모래색 더미를 만들어 찍는다.
/// 메뉴: 유리병 정원 > 재료 아이콘 만들기. 재료나 모델을 바꿨으면 다시 누른다.
/// </summary>
public static class IconBaker
{
    public const string Folder = "Assets/Data/Icons";
    private const int Size = 256;          // 아이콘 크기
    private const int Super = 2;           // 2배로 그려서 줄이면 가장자리가 부드럽다
    private const int OutlinePx = 5;       // 윤곽선 두께 (Super 배 화면 기준)
    private static readonly Color32 OutlineColor = new Color32(70, 52, 44, 255);

    [MenuItem("유리병 정원/재료 아이콘 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShaderUtil.allowAsyncCompilation = false;

        // 하늘이 없는 작은 촬영 스튜디오: 부드러운 환경광과 해 하나
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.74f);
        Light light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        light.intensity = 1.1f;
        light.shadows = LightShadows.None;

        Camera cam = new GameObject("Cam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView = 24f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 50f;
        int render = Size * Super;
        var rt = new RenderTexture(render, render, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        int made = 0;
        var report = new System.Text.StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData", new[] { "Assets/Data/Materials" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
            if (data == null) continue;

            GameObject model = MakeModel(data);
            model.transform.position = new Vector3(100f, 0f, 0f);
            model.transform.rotation = Quaternion.Euler(0f, -32f, 0f);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

            // 모델 크기에 맞춰 화면에 가득 차게 (여백을 조금 둔다)
            float radius = b.extents.magnitude;
            float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            Vector3 dir = new Vector3(0.28f, 0.34f, -1f).normalized;
            cam.transform.position = b.center + dir * distance;
            cam.transform.LookAt(b.center);
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(render, render, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, render, render), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            Texture2D final = Finish(tex);
            string file = Folder + "/" + data.id + ".png";
            File.WriteAllBytes(file, final.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(final);
            Object.DestroyImmediate(model);
            made++;
        }

        cam.targetTexture = null; // 카메라가 쥔 채로 지우면 오류가 난다
        Object.DestroyImmediate(rt);
        AssetDatabase.Refresh();

        // 스프라이트로 가져오고 재료에 연결한다
        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData", new[] { "Assets/Data/Materials" }))
        {
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(guid));
            string file = Folder + "/" + data.id + ".png";
            var importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            data.icon = AssetDatabase.LoadAssetAtPath<Sprite>(file);
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();

        EditorSceneManager.OpenScene("Assets/Scenes/Island.unity");
        return "재료 아이콘 " + made + "장을 만들고 재료에 연결했어요: " + Folder;
    }

    private static GameObject MakeModel(MaterialData data)
    {
        if (data.placedPrefab != null) return (GameObject)PrefabUtility.InstantiatePrefab(data.placedPrefab);

        // 모델이 없는 재료(모래): 모래색 둥근 더미 세 덩어리
        var root = new GameObject("SandPile");
        Material mat = BottleSetup.GetOrCreateMaterial("SandIcon", data.placeholderColor);
        foreach (var (x, y, z, s) in new[] { (0f, 0f, 0f, 1f), (0.55f, -0.05f, 0.1f, 0.65f), (-0.5f, -0.08f, -0.15f, 0.55f) })
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localPosition = new Vector3(x, y, z);
            sphere.transform.localScale = new Vector3(s * 1.3f, s * 0.55f, s * 1.1f);
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
        }
        return root;
    }

    /// <summary>
    /// 찍은 그림(투명 배경)을 정리한다: 모델이 차지한 부분을 가운데로 모아 정사각형에 맞추고, 둘레에 윤곽선을 두른 뒤
    /// Size x Size로 줄인다.
    /// </summary>
    private static Texture2D Finish(Texture2D src)
    {
        int w = src.width, h = src.height;
        Color32[] px = src.GetPixels32();

        // 1) 윤곽선: 불투명 부분에서 OutlinePx 안쪽에 있는 투명한 칸을 윤곽선 색으로 칠한다
        var outlined = new Color32[px.Length];
        int r = OutlinePx;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 c = px[y * w + x];
                if (c.a > 40) { outlined[y * w + x] = c; continue; }

                bool near = false;
                for (int dy = -r; dy <= r && !near; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= h) continue;
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy > r * r) continue;
                        int xx = x + dx; if (xx < 0 || xx >= w) continue;
                        if (px[yy * w + xx].a > 140) { near = true; break; }
                    }
                }
                outlined[y * w + x] = near ? OutlineColor : new Color32(0, 0, 0, 0);
            }
        }

        // 2) 그려진 부분의 범위를 찾아 정사각형으로 자르고 가운데에 맞춘다 (여백 6%)
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (outlined[y * w + x].a > 0) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
        if (maxX < 0) { minX = 0; minY = 0; maxX = w - 1; maxY = h - 1; }

        int side = Mathf.CeilToInt(Mathf.Max(maxX - minX + 1, maxY - minY + 1) * 1.12f);
        int ox = (minX + maxX) / 2 - side / 2, oy = (minY + maxY) / 2 - side / 2;

        // 3) side x side를 Size x Size로 줄이면서 (상자 평균) 부드럽게 만든다. 투명한 칸은 색이 번지지 않게 가중 평균한다.
        var result = new Texture2D(Size, Size, TextureFormat.ARGB32, false);
        var outPx = new Color32[Size * Size];
        float scale = side / (float)Size;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int x0 = Mathf.FloorToInt(ox + x * scale), x1 = Mathf.Max(x0 + 1, Mathf.FloorToInt(ox + (x + 1) * scale));
                int y0 = Mathf.FloorToInt(oy + y * scale), y1 = Mathf.Max(y0 + 1, Mathf.FloorToInt(oy + (y + 1) * scale));
                float a = 0f, rr = 0f, gg = 0f, bb = 0f; int count = 0;
                for (int yy = y0; yy < y1; yy++)
                {
                    for (int xx = x0; xx < x1; xx++)
                    {
                        count++;
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        Color32 c = outlined[yy * w + xx];
                        float ca = c.a / 255f;
                        a += ca; rr += c.r * ca; gg += c.g * ca; bb += c.b * ca;
                    }
                }
                if (a <= 0f || count == 0) { outPx[y * Size + x] = new Color32(0, 0, 0, 0); continue; }
                outPx[y * Size + x] = new Color32((byte)(rr / a), (byte)(gg / a), (byte)(bb / a), (byte)Mathf.Clamp(a / count * 255f, 0f, 255f));
            }
        }
        result.SetPixels32(outPx);
        result.Apply();
        return result;
    }
}
