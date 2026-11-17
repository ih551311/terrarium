using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 사막 섬(DesertIsland 씬)을 만든다. 지름 약 210m의 모래 언덕 섬:
///   가운데 유목민 야영지(천막, 모닥불, 낙타), 서쪽 오아시스(연못과 야자나무), 북쪽 피라미드 언덕, 동쪽 선착장,
///   섬 곳곳의 선인장, 알로에, 바위(에셋 POLYDesert, Assets/Runemark Studio).
/// 지형(Terrain)과 바다는 코드로 만들고, 야자나무, 천막, 낙타, 피라미드는 사막 재료 모델(deco_*)을 크게 키워서 쓴다.
/// 사막 재료(MaterialZone.Desert)는 선인장과 야자나무(도끼, 하나씩 정해짐), 바위(곡괭이), 바닥(줍기)에서 난다.
/// 처음 만들 때는 Island 씬을 복사해서 시작한다(플레이어, 하늘, 조명, 후처리가 이미 들어 있다). 다시 누르면 사막 섬 내용만 새로 만든다.
/// 메뉴: 유리병 정원 > 사막 섬 만들기 (사막 에셋 재질 바꾸기, 사막 재료 모델 만들기, 재료 데이터 만들기를 먼저 해 둘 것)
/// </summary>
public static class DesertIslandSetup
{
    // ───────── 섬 모양 (미터) ─────────
    private const float TerrainSize = 360f;
    private const float TerrainHeight = 40f;
    private const float TerrainBaseY = -6f;
    private const int HeightmapResolution = 257;
    private const int AlphamapResolution = 256;

    private const float IslandRadius = 105f;
    private const float CampHeight = 4.2f;
    private static readonly Vector2 Camp = new Vector2(0f, 0f);
    private static readonly Vector2 Oasis = new Vector2(-40f, -22f);
    private const float PondRadius = 13f;
    private const float OasisHeight = 3.3f;
    private const float WaterY = 2.95f;
    private static readonly Vector2 Ruins = new Vector2(8f, 68f);
    private const float RuinsHeight = 5.4f;

    private static readonly Vector2[] Path = { new Vector2(0f, 0f), new Vector2(26f, 8f), new Vector2(56f, -3f), new Vector2(90f, 0f) };

    private const string RootName = "DesertContent";
    private const string ScenePath = "Assets/Scenes/DesertIsland.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string AssetPrefabs = "Assets/Runemark Studio/Freebies/PolyDesert Mobile/Prefabs/";
    private const string DecoFolder = "Assets/Models/Deco/";
    private const string TerrainFolder = "Assets/Terrain";
    private const string MaterialFolder = "Assets/Data/Materials/";

    private static System.Random rng;
    private static Terrain terrain;
    private static float Rand() { return (float)rng.NextDouble(); }
    private static float Range(float a, float b) { return Mathf.Lerp(a, b, Rand()); }

    /// <summary>사막 에셋의 재질은 기본(Standard) 셰이더라 URP에서는 분홍색으로 보이므로 URP Lit으로 바꾼다.</summary>
    [MenuItem("유리병 정원/사막 에셋 재질 바꾸기")]
    public static string ConvertMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return "URP Lit 셰이더를 찾을 수 없어요.";
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Runemark Studio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == lit) continue;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            m.shader = lit;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.05f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            n++;
        }
        AssetDatabase.SaveAssets();
        return "사막 에셋 재질 " + n + "개를 URP로 바꿨어요";
    }

    [MenuItem("유리병 정원/사막 섬 만들기")]
    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(AssetPrefabs + "cactus01_m.prefab") == null)
            return "사막 에셋(Assets/Runemark Studio)을 찾지 못했어요. 먼저 POLYDesert를 가져오세요.";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DecoFolder + "deco_palm_tree.obj") == null)
            return "사막 재료 모델이 없어요. 먼저 '사막 재료 모델 만들기'를 누르세요.";
        ConvertMaterials();

        EditorSceneManager.SaveOpenScenes();
        if (!File.Exists(ScenePath)) AssetDatabase.CopyAsset(IslandPath, ScenePath); // 플레이어, 하늘, 조명이 들어 있는 채로 시작
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        rng = new System.Random(20261201);

        foreach (string n in new[] { "IslandContent", "Spawn_FromHut", "MainDock", "DesertDock", "Spawn_FromSnow", "Spawn_FromDesert", RootName, "SnowContent", "SceneClimate", "SceneMapInfo" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }

        Vector2 landing2 = FindShore(0f, 0.25f);
        float pierLen = 16f;
        for (float d = 0f; d < 60f; d += 0.5f)
            if (HeightAt(landing2.x + d, landing2.y) <= -0.6f) { pierLen = Mathf.Clamp(d + 3f, 14f, 36f); break; }
        Path[Path.Length - 1] = new Vector2(landing2.x - 5f, landing2.y);
        Path[Path.Length - 2] = new Vector2(Mathf.Max(30f, landing2.x * 0.6f), -3f);

        GameObject root = new GameObject(RootName);
        Transform rt = root.transform;
        terrain = BuildTerrain(rt);
        BuildSea(rt);

        Vector3 landingSpot;
        float landY = HeightAt(landing2.x, landing2.y);
        DockSetup.Build(rt, "Dock", landing2, Vector3.right, false, "Island", "FromDesert", "배 타고 돌아가기", out landingSpot, landY, pierLen);
        GameObject spawn = new GameObject("Spawn_FromDock");
        spawn.transform.SetParent(rt, false);
        spawn.transform.SetPositionAndRotation(landingSpot, Quaternion.Euler(0f, -90f, 0f));
        SpawnPoint sp = spawn.AddComponent<SpawnPoint>();
        SerializedObject sps = new SerializedObject(sp);
        sps.FindProperty("id").stringValue = "FromDock";
        sps.ApplyModifiedPropertiesWithoutUndo();

        // 이 섬은 늘 맑다
        GameObject climate = new GameObject("SceneClimate");
        SceneClimate sc = climate.AddComponent<SceneClimate>();
        SerializedObject cso = new SerializedObject(sc);
        cso.FindProperty("weather").enumValueIndex = (int)WeatherKind.Clear;
        cso.ApplyModifiedPropertiesWithoutUndo();

        taken.Clear();
        BuildCamp(rt);
        int palms = BuildOasis(rt);
        BuildRuins(rt);
        int cacti = ScatterCacti(rt);
        int rocks = ScatterRocks(rt);
        int deco = ScatterDecor(rt);
        Physics.SyncTransforms();
        int pickups = PlacePickups(rt);
        PickupSize.FixOpenScene(); // 풍뎅이처럼 작은 재료가 모래 위에서 안 보이지 않게

        Texture2D mapImage = BakeMap(rt, landing2, pierLen);
        GameObject mapGo = new GameObject("SceneMapInfo");
        SceneMapInfo mapInfo = mapGo.AddComponent<SceneMapInfo>();
        SerializedObject mso = new SerializedObject(mapInfo);
        mso.FindProperty("image").objectReferenceValue = mapImage;
        mso.FindProperty("center").vector2Value = Vector2.zero;
        mso.FindProperty("halfExtent").floatValue = MapHalf;
        mso.FindProperty("homeSpawnId").stringValue = "FromDock";
        mso.FindProperty("homeLabel").stringValue = "선착장으로 돌아가기";
        mso.ApplyModifiedPropertiesWithoutUndo();

        foreach (GameRoot gr in Object.FindObjectsByType<GameRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform player = gr.transform.Find("Player");
            if (player != null) player.position = landingSpot;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.93f, 0.85f, 0.70f);
        RenderSettings.fogStartDistance = 160f;
        RenderSettings.fogEndDistance = 1100f;

        AddToBuildSettings();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "사막 섬 만들기 완료: 야자나무 " + palms + "그루, 선인장 " + cacti + "그루, 바위 " + rocks + "개, 소품 " + deco + "개, 재료 줍는 자리 " + pickups + "개. " + VerifyTerrainOnDisk();
    }

    private static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene s in scenes) if (s.path == ScenePath) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ───────── 지형 ─────────

    private static float Smooth(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static float CoastFactor(float angle)
    {
        return 1f + 0.09f * Mathf.Sin(3f * angle + 2f) + 0.06f * Mathf.Sin(5f * angle + 1f) + 0.04f * Mathf.Sin(7f * angle + 5f);
    }

    /// <summary>모래 언덕: 바람받이는 완만하고 안쪽은 가파른 물결 (바람 방향으로 줄지어 있다)</summary>
    private static float Dune(float x, float z)
    {
        float u = x * 0.82f + z * 0.57f;
        float warp = (Mathf.PerlinNoise(x * 0.012f + 40f, z * 0.012f + 40f) - 0.5f) * 70f;
        float f = ((u + warp) / 26f) % 1f;
        if (f < 0f) f += 1f;
        float profile = f < 0.72f ? Smooth(0f, 0.72f, f) : 1f - (f - 0.72f) / 0.28f;
        float amp = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.018f + 4f, z * 0.018f + 4f);
        return profile * amp * 3.2f;
    }

    private static float HeightAt(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = p.magnitude / (IslandRadius * CoastFactor(Mathf.Atan2(z, x)));

        float land = 1f - Smooth(0.62f, 1.0f, r);
        float h = Mathf.Pow(land, 1.2f) * 5.2f;
        if (r > 1f) h = -(r - 1f) * IslandRadius * 0.12f;

        float fine = Mathf.PerlinNoise(x * 0.09f + 70f, z * 0.09f + 70f) * 2f - 1f;
        h += Dune(x, z) * Smooth(0.12f, 0.58f, 1f - r) + fine * 0.18f * Smooth(0.1f, 0.5f, 1f - r);

        // 북쪽 피라미드 언덕
        float rx = (x - Ruins.x) / 40f, rz = (z - Ruins.y) / 22f;
        h += 3.2f * Mathf.Exp(-(rx * rx + rz * rz) / 2f) * Smooth(0f, 0.4f, 1f - r);
        h = Mathf.Lerp(h, RuinsHeight, 1f - Smooth(14f, 26f, Vector2.Distance(p, Ruins)));

        // 야영지: 평평하게 고른다
        h = Mathf.Lerp(h, CampHeight, 1f - Smooth(18f, 30f, Vector2.Distance(p, Camp)));

        // 오아시스: 둘레를 평평하게, 가운데는 움푹하게 (물이 차 있다)
        float dp = Vector2.Distance(p, Oasis);
        h = Mathf.Lerp(h, OasisHeight, 1f - Smooth(PondRadius + 2f, PondRadius + 14f, dp));
        h = Mathf.Lerp(h, 2.35f, 1f - Smooth(PondRadius - 5f, PondRadius - 1f, dp));
        return h;
    }

    private static float DistanceToPath(float x, float z)
    {
        float best = float.MaxValue;
        Vector2 p = new Vector2(x, z);
        for (int i = 0; i < Path.Length - 1; i++)
        {
            Vector2 a = Path[i], b = Path[i + 1], ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }
        return best;
    }

    private static Vector2 FindShore(float angle, float maxHeight)
    {
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        for (float r = 40f; r < 190f; r += 0.5f)
        {
            Vector2 p = dir * r;
            if (HeightAt(p.x, p.y) <= maxHeight) return p;
        }
        return dir * 95f;
    }

    private static Texture2D MakeTexture(string file, Color baseColor, Color variation, float streak, float speck, int seed)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        var local = new System.Random(seed);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float n = Mathf.PerlinNoise(Mathf.Cos(u * Mathf.PI * 2f) * 2f + 5f, Mathf.Sin(u * Mathf.PI * 2f) * 2f + v * 4f) * 0.5f
                        + Mathf.PerlinNoise(Mathf.Cos(v * Mathf.PI * 2f) * 4f + 9f, Mathf.Sin(v * Mathf.PI * 2f) * 4f + u * 6f) * 0.5f;
                Color c = Color.Lerp(baseColor, variation, Mathf.Clamp01((n - 0.35f) * 2.2f));
                c = Color.Lerp(c, baseColor, 0.2f);
                // 바람이 만든 가는 물결 무늬
                float ripple = Mathf.Sin((x * 0.5f + y * 0.9f) * 0.35f + n * 6f) * 0.5f + 0.5f;
                c = Color.Lerp(c, variation, ripple * streak * 0.5f);
                if ((float)local.NextDouble() < speck) c = Color.Lerp(c, variation, 0.8f); // 모래 알갱이
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        Directory.CreateDirectory(TerrainFolder + "/Desert");
        string path = TerrainFolder + "/Desert/" + file + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static TerrainLayer MakeLayer(string name, Texture2D tex, float tile, float smoothness)
    {
        string path = TerrainFolder + "/Desert/" + name + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, path); }
        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(tile, tile);
        layer.smoothness = smoothness;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static Terrain BuildTerrain(Transform parent)
    {
        Directory.CreateDirectory(TerrainFolder);
        TerrainData td = new TerrainData { heightmapResolution = HeightmapResolution, alphamapResolution = AlphamapResolution };
        td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        string path = TerrainFolder + "/DesertIslandTerrain.asset";
        AssetDatabase.DeleteAsset(path);

        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int iz = 0; iz < HeightmapResolution; iz++)
            for (int ix = 0; ix < HeightmapResolution; ix++)
            {
                float x = -TerrainSize / 2f + ix * TerrainSize / (HeightmapResolution - 1);
                float z = -TerrainSize / 2f + iz * TerrainSize / (HeightmapResolution - 1);
                heights[iz, ix] = Mathf.Clamp01((HeightAt(x, z) - TerrainBaseY) / TerrainHeight);
            }
        td.SetHeights(0, 0, heights);
        td.SyncHeightmap();

        // 칠하는 층: 모래, 붉은 모래, 다져진 길, 오아시스 풀. 층 이름의 sand/path가 발소리를 정한다 (FootstepPlayer)
        TerrainLayer sand = MakeLayer("Sand_Layer", MakeTexture("sand_diffuse", new Color(0.93f, 0.82f, 0.60f), new Color(0.86f, 0.72f, 0.47f), 0.6f, 0.004f, 11), 9f, 0.05f);
        TerrainLayer red = MakeLayer("RedSand_Layer", MakeTexture("redsand_diffuse", new Color(0.88f, 0.62f, 0.38f), new Color(0.78f, 0.48f, 0.28f), 0.5f, 0.004f, 12), 10f, 0.05f);
        TerrainLayer trod = MakeLayer("Path_Layer", MakeTexture("trod_diffuse", new Color(0.80f, 0.68f, 0.50f), new Color(0.70f, 0.57f, 0.40f), 0.25f, 0.006f, 13), 5f, 0.1f);
        TerrainLayer oasis = MakeLayer("OasisGrass_Layer", MakeTexture("oasis_diffuse", new Color(0.52f, 0.68f, 0.34f), new Color(0.38f, 0.56f, 0.28f), 0.3f, 0.003f, 14), 6f, 0.05f);
        td.terrainLayers = new[] { sand, red, trod, oasis };

        float[,,] alpha = new float[AlphamapResolution, AlphamapResolution, 4];
        for (int iz = 0; iz < AlphamapResolution; iz++)
            for (int ix = 0; ix < AlphamapResolution; ix++)
            {
                float x = -TerrainSize / 2f + (ix + 0.5f) * TerrainSize / AlphamapResolution;
                float z = -TerrainSize / 2f + (iz + 0.5f) * TerrainSize / AlphamapResolution;
                Vector2 p = new Vector2(x, z);
                float wob = (Mathf.PerlinNoise(x * 0.06f, z * 0.06f) - 0.5f) * 3f;

                float wGrass = 1f - Smooth(PondRadius + 1f, PondRadius + 9f, Vector2.Distance(p, Oasis) + wob);
                float plaza = 1f - Smooth(8f, 13f, Vector2.Distance(p, Camp) + wob);
                float wTrod = Mathf.Max(plaza, 1f - Smooth(1.6f, 3.4f, DistanceToPath(x, z) + wob * 0.5f)) * (1f - wGrass);
                float redNoise = Smooth(0.58f, 0.74f, Mathf.PerlinNoise(x * 0.028f + 90f, z * 0.028f + 90f));
                float wRed = redNoise * (1f - wGrass) * (1f - wTrod);
                float wSand = Mathf.Max(0f, 1f - wGrass - wTrod - wRed);
                float sum = wSand + wRed + wTrod + wGrass;
                alpha[iz, ix, 0] = wSand / sum; alpha[iz, ix, 1] = wRed / sum; alpha[iz, ix, 2] = wTrod / sum; alpha[iz, ix, 3] = wGrass / sum;
            }
        td.SetAlphamaps(0, 0, alpha);
        AssetDatabase.CreateAsset(td, path);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssetIfDirty(td);
        AssetDatabase.SaveAssets();

        GameObject go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain";
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(-TerrainSize / 2f, TerrainBaseY, -TerrainSize / 2f);
        Terrain t = go.GetComponent<Terrain>();
        t.drawInstanced = true;
        return t;
    }

    private static float GroundY(float x, float z)
    {
        return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
    }

    // ───────── 바다 (원래 섬과 같은 따뜻한 바다) ─────────

    private static void BuildSea(Transform parent)
    {
        Mesh coastMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Data/Placeholder/SeaCoast.asset");
        Mesh farMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Data/Placeholder/SeaFar.asset");
        Material coastMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Placeholder/SeaCoast.mat");
        Material farMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Placeholder/SeaFar.mat");
        if (coastMesh == null || farMesh == null || coastMat == null || farMat == null)
        {
            Debug.LogWarning("원래 섬의 바다 재료(SeaCoast, SeaFar)를 찾지 못했어요. 먼저 '바다 다시 만들기'를 하세요.");
            return;
        }
        // 이름이 SeaCoast, SeaFar여야 SkyController가 안개 색으로 물들인다
        GameObject coast = new GameObject("SeaCoast");
        coast.transform.SetParent(parent, false);
        coast.AddComponent<MeshFilter>().sharedMesh = coastMesh;
        MeshRenderer cr = coast.AddComponent<MeshRenderer>();
        cr.sharedMaterial = coastMat;
        cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject far = new GameObject("SeaFar");
        far.transform.SetParent(parent, false);
        far.transform.position = new Vector3(0f, -3f, 0f);
        far.AddComponent<MeshFilter>().sharedMesh = farMesh;
        MeshRenderer fr = far.AddComponent<MeshRenderer>();
        fr.sharedMaterial = farMat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ───────── 놓기 도우미 ─────────

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>프리팹이나 모델을 놓는다. 크기는 높이(targetHeight, 미터)로 정하고, 바닥이 땅에 닿게 한다.</summary>
    private static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float yaw, float targetHeight)
    {
        if (prefab == null) return null;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one;
        go.transform.position = pos;
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return go;
        float h0 = Mathf.Max(BoundsOf(go).size.y, 0.0001f);
        go.transform.localScale = Vector3.one * (targetHeight / h0);
        go.transform.position = pos;
        // 발밑 땅이 가장 낮은 곳에 맞춰 살짝 묻는다 (비탈에서 한쪽이 공중에 뜨지 않게)
        Bounds bb = BoundsOf(go);
        float rad = Mathf.Clamp(0.35f * Mathf.Min(bb.extents.x, bb.extents.z), 0.4f, 3.5f);
        float low = pos.y;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            low = Mathf.Min(low, GroundY(pos.x + Mathf.Cos(a) * rad, pos.z + Mathf.Sin(a) * rad));
        }
        go.transform.position += Vector3.up * (low - bb.min.y - 0.12f);
        return go;
    }

    private static GameObject Asset(string name, Transform parent, Vector3 pos, float yaw, float targetHeight)
    {
        return Place(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPrefabs + name + ".prefab"), parent, pos, yaw, targetHeight);
    }

    private static GameObject Deco(string name, Transform parent, Vector3 pos, float yaw, float targetHeight)
    {
        return Place(AssetDatabase.LoadAssetAtPath<GameObject>(DecoFolder + name + ".obj"), parent, pos, yaw, targetHeight);
    }

    private static void AddSolidBox(GameObject go, float shrink = 0.9f)
    {
        Bounds b = BoundsOf(go);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 s = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) * shrink;
        box.size = new Vector3(Mathf.Max(box.size.x, 0.1f), Mathf.Max(box.size.y, 0.1f), Mathf.Max(box.size.z, 0.1f));
    }

    /// <summary>모양 그대로의 충돌 (보이지 않는 상자에 막히지 않는다). 모델에 이미 충돌이 있는 부분은 건드리지 않는다.</summary>
    private static void AddMeshCollider(GameObject go)
    {
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
            mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }

    private static void AddTrunk(GameObject go, float radius, float height)
    {
        Bounds b = BoundsOf(go);
        CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
        Vector3 localBase = go.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
        float inv = 1f / Mathf.Max(go.transform.lossyScale.y, 0.0001f);
        cap.center = localBase + Vector3.up * (height * 0.5f) * inv;
        cap.radius = radius * inv;
        cap.height = height * inv;
    }

    private static void AddHarvestable(GameObject go, AcquireMethod tool, Color emptyTint, MaterialData fixedMaterial = null)
    {
        Harvestable h = go.AddComponent<Harvestable>();
        SerializedObject so = new SerializedObject(h);
        so.FindProperty("tool").enumValueIndex = (int)tool;
        so.FindProperty("zone").enumValueIndex = (int)MaterialZone.Desert;
        so.FindProperty("emptyTint").colorValue = emptyTint;
        if (fixedMaterial != null) so.FindProperty("fixedMaterial").objectReferenceValue = fixedMaterial;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static readonly List<Vector2> taken = new List<Vector2>();

    private static bool Free(float x, float z, float spacing)
    {
        foreach (Vector2 t in taken) if ((t.x - x) * (t.x - x) + (t.y - z) * (t.y - z) < spacing * spacing) return false;
        return true;
    }

    /// <summary>놓아도 되는 자리인가: 땅이고, 야영지, 오아시스 물, 길에서 떨어져 있다.</summary>
    private static bool Allowed(float x, float z, float campClear, float pathClear)
    {
        float h = HeightAt(x, z);
        if (h < 1.2f) return false;
        Vector2 p = new Vector2(x, z);
        if (Vector2.Distance(p, Camp) < campClear) return false;
        if (Vector2.Distance(p, Oasis) < PondRadius + 2f) return false;
        if (Vector2.Distance(p, Ruins) < 16f) return false;
        if (DistanceToPath(x, z) < pathClear) return false;
        return true;
    }

    private static Vector2 RandomLand()
    {
        float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius - 8f);
        return new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);
    }

    // ───────── 야영지: 천막, 모닥불, 낙타 ─────────

    private static void BuildCamp(Transform parent)
    {
        GameObject group = new GameObject("Camp");
        group.transform.SetParent(parent, false);
        Transform g = group.transform;

        Vector2[] tents = { new Vector2(-9f, 7f), new Vector2(10f, 8f), new Vector2(-11f, -8f), new Vector2(9f, -10f) };
        for (int i = 0; i < tents.Length; i++)
        {
            Vector2 t = tents[i];
            // 천막은 모닥불을 바라본다
            float yaw = Mathf.Atan2(-t.x, -t.y) * Mathf.Rad2Deg - 90f;
            GameObject tent = Deco("deco_desert_tent", g, new Vector3(t.x, GroundY(t.x, t.y), t.y), yaw, 3.2f); // 기둥까지 3.2m (천막 지붕 약 2.4m)
            if (tent == null) continue;
            tent.name = "Tent_" + i;
            AddMeshCollider(tent);
            taken.Add(t);
        }
        GameObject fire = Deco("deco_campfire", g, new Vector3(0f, GroundY(0f, 0f), 0f), 0f, 0.7f);
        if (fire != null) { fire.name = "Campfire"; taken.Add(Vector2.zero); }

        Vector2[] camels = { new Vector2(-4f, 13f), new Vector2(16f, -3f), new Vector2(-17f, 2f) };
        for (int i = 0; i < camels.Length; i++)
        {
            Vector2 c = camels[i];
            GameObject camel = Deco("deco_camel", g, new Vector3(c.x, GroundY(c.x, c.y), c.y), Rand() * 360f, 2.3f);
            if (camel == null) continue;
            camel.name = "Camel_" + i;
            AddMeshCollider(camel);
            taken.Add(c);
        }
    }

    // ───────── 오아시스: 연못과 야자나무 ─────────

    private static int BuildOasis(Transform parent)
    {
        GameObject group = new GameObject("Oasis");
        group.transform.SetParent(parent, false);

        // 연못의 물 (납작한 원판. 걸어 들어가도 막히지 않는다)
        Material water = BottleSetup.GetOrCreateMaterial("OasisWater", new Color(0.25f, 0.62f, 0.72f));
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Water";
        disc.transform.SetParent(group.transform, false);
        disc.transform.position = new Vector3(Oasis.x, WaterY, Oasis.y);
        disc.transform.localScale = new Vector3(PondRadius * 2f - 1f, 0.02f, PondRadius * 2f - 1f);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        disc.GetComponent<Renderer>().sharedMaterial = water;

        MaterialData dates = AssetDatabase.LoadAssetAtPath<MaterialData>(MaterialFolder + "date_fruit.asset");
        int placed = 0, tries = 0;
        while (placed < 16 && tries++ < 2000)
        {
            float ang = Rand() * Mathf.PI * 2f, rad = Range(PondRadius + 0.5f, PondRadius + 11f);
            float x = Oasis.x + Mathf.Cos(ang) * rad, z = Oasis.y + Mathf.Sin(ang) * rad;
            if (HeightAt(x, z) < WaterY + 0.15f || !Free(x, z, 4.2f)) continue;
            GameObject palm = Deco("deco_palm_tree", group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, Range(6.5f, 9.5f));
            if (palm == null) continue;
            palm.name = "Palm_" + placed;
            AddTrunk(palm, 0.35f, 3f);
            if (Rand() < 0.7f) AddHarvestable(palm, AcquireMethod.Axe, new Color(0.62f, 0.60f, 0.46f, 1f), dates);
            taken.Add(new Vector2(x, z));
            placed++;
        }
        // 야영지 가까이에도 야자나무 두 그루
        foreach (Vector2 q in new[] { new Vector2(-19f, 14f), new Vector2(21f, 12f) })
        {
            GameObject palm = Deco("deco_palm_tree", group.transform, new Vector3(q.x, GroundY(q.x, q.y), q.y), Rand() * 360f, 7.5f);
            if (palm != null) { palm.name = "Palm_camp"; AddTrunk(palm, 0.35f, 3f); taken.Add(q); placed++; }
        }
        // 물가의 알로에 무리와 마른 풀
        for (int i = 0; i < 12; i++)
        {
            float ang = Rand() * Mathf.PI * 2f, rad = Range(PondRadius + 1f, PondRadius + 8f);
            float x = Oasis.x + Mathf.Cos(ang) * rad, z = Oasis.y + Mathf.Sin(ang) * rad;
            if (HeightAt(x, z) < WaterY + 0.15f) continue;
            Asset("aloe_m", group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, Range(0.7f, 1.1f));
        }
        return placed;
    }

    // ───────── 북쪽 피라미드 ─────────

    private static void BuildRuins(Transform parent)
    {
        GameObject group = new GameObject("Ruins");
        group.transform.SetParent(parent, false);
        GameObject pyr = Deco("deco_pyramid_steps", group.transform, new Vector3(Ruins.x, GroundY(Ruins.x, Ruins.y), Ruins.y), 20f, 16f);
        if (pyr != null) { pyr.name = "Pyramid"; AddMeshCollider(pyr); }
        // 피라미드 앞의 큰 바위 몇 개
        for (int i = 0; i < 5; i++)
        {
            float ang = Range(-0.5f, Mathf.PI + 0.5f) + Mathf.PI;
            float rad = Range(24f, 30f);
            float x = Ruins.x + Mathf.Cos(ang) * rad, z = Ruins.y + Mathf.Sin(ang) * rad;
            if (HeightAt(x, z) < 1.2f) continue;
            GameObject rock = Asset("rock0" + (1 + i % 8) + "_m", group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, Range(3f, 4.5f));
            if (rock != null) { AddMeshCollider(rock); taken.Add(new Vector2(x, z)); }
        }
    }

    // ───────── 선인장, 바위, 소품 ─────────

    private static int ScatterCacti(Transform parent)
    {
        GameObject group = new GameObject("Cacti");
        group.transform.SetParent(parent, false);
        MaterialData cactus = AssetDatabase.LoadAssetAtPath<MaterialData>(MaterialFolder + "cactus_piece.asset");
        string[] types = { "cactus01_m", "cactus02_m", "cactus03_m" };
        int placed = 0, tries = 0;
        while (placed < 80 && tries++ < 6000)
        {
            Vector2 q = RandomLand();
            if (!Allowed(q.x, q.y, 28f, 5f)) continue;
            // 덩어리지게: 소리(노이즈)가 높은 곳에 모인다
            if (Mathf.PerlinNoise(q.x * 0.04f + 300f, q.y * 0.04f + 300f) < 0.52f) continue;
            if (!Free(q.x, q.y, 5f)) continue;
            string type = types[(int)(Rand() * types.Length) % types.Length];
            GameObject c = Asset(type, group.transform, new Vector3(q.x, GroundY(q.x, q.y), q.y), Rand() * 360f, Range(2.2f, 5.2f));
            if (c == null) continue;
            c.name = type + "_" + placed;
            Bounds cb = BoundsOf(c);
            AddTrunk(c, Mathf.Clamp(Mathf.Min(cb.extents.x, cb.extents.z) * 0.75f, 0.3f, 1.5f), Mathf.Min(cb.size.y, 3f));
            if (Rand() < 0.6f) AddHarvestable(c, AcquireMethod.Axe, new Color(0.62f, 0.58f, 0.44f, 1f), cactus);
            taken.Add(q);
            placed++;
        }
        return placed;
    }

    private static int ScatterRocks(Transform parent)
    {
        GameObject group = new GameObject("Rocks");
        group.transform.SetParent(parent, false);
        int placed = 0, tries = 0;
        while (placed < 70 && tries++ < 5000)
        {
            Vector2 q = RandomLand();
            if (!Allowed(q.x, q.y, 24f, 4f) || !Free(q.x, q.y, 4f)) continue;
            string type = "rock0" + (1 + (int)(Rand() * 8f) % 8) + "_m";
            GameObject r = Asset(type, group.transform, new Vector3(q.x, GroundY(q.x, q.y), q.y), Rand() * 360f, Range(1.0f, 2.8f));
            if (r == null) continue;
            r.name = type + "_" + placed;
            AddMeshCollider(r);
            // 바위는 곡괭이로 캘 수 있다 (사암 조각, 사막 장미, 가끔 화석). 캐면 색이 바랜다
            AddHarvestable(r, AcquireMethod.Pickaxe, new Color(0.60f, 0.56f, 0.50f, 1f));
            taken.Add(q);
            placed++;
        }
        return placed;
    }

    private static int ScatterDecor(Transform parent)
    {
        GameObject group = new GameObject("Decor");
        group.transform.SetParent(parent, false);
        int placed = 0, tries = 0;
        while (placed < 45 && tries++ < 3000)
        {
            Vector2 q = RandomLand();
            if (!Allowed(q.x, q.y, 20f, 3f) || !Free(q.x, q.y, 3.5f)) continue;
            bool aloe = Rand() < 0.5f;
            GameObject o = aloe ? Asset("aloe_m", group.transform, new Vector3(q.x, GroundY(q.x, q.y), q.y), Rand() * 360f, Range(0.7f, 1.2f))
                                : Deco("deco_dry_branch", group.transform, new Vector3(q.x, GroundY(q.x, q.y), q.y), Rand() * 360f, Range(1.0f, 1.8f));
            if (o == null) continue;
            taken.Add(q);
            placed++;
        }
        return placed;
    }

    // ───────── 재료 줍는 자리 ─────────

    private static int PlacePickups(Transform parent)
    {
        GameObject group = new GameObject("Pickups");
        group.transform.SetParent(parent, false);
        int total = 0;
        var want = new Dictionary<string, int>
        {
            { "cactus_flower", 10 }, { "aloe_leaf", 10 }, { "dry_branch", 10 }, { "pottery_shard", 8 }, { "scarab", 8 }, { "red_sand", 14 }
        };
        foreach (KeyValuePair<string, int> pair in want)
        {
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(MaterialFolder + pair.Key + ".asset");
            if (data == null) { Debug.LogWarning("재료를 찾지 못했어요: " + pair.Key + " (사막 재료를 먼저 만드세요)"); continue; }
            int placed = 0, tries = 0;
            while (placed < pair.Value && tries++ < 3000)
            {
                Vector2 q = RandomLand();
                float d = Vector2.Distance(q, Camp), dr = Vector2.Distance(q, Ruins);
                // 도자기 조각은 피라미드 둘레에, 풍뎅이는 야영지에서 먼 곳에, 붉은 모래는 붉은 모래 지대(RedSand 층이 짙은 곳)에, 나머지는 어디서나
                if (pair.Key == "pottery_shard" && (dr < 18f || dr > 55f)) continue;
                if (pair.Key == "scarab" && d < 30f) continue;
                if (pair.Key == "red_sand" && Mathf.PerlinNoise(q.x * 0.028f + 90f, q.y * 0.028f + 90f) < 0.62f) continue;
                if (pair.Key != "pottery_shard" && !Allowed(q.x, q.y, 10f, 2f)) continue;
                if (pair.Key == "pottery_shard" && HeightAt(q.x, q.y) < 1.2f) continue;
                if (Physics.CheckSphere(new Vector3(q.x, GroundY(q.x, q.y) + 1.4f, q.y), 0.9f)) continue;
                MakePickup(group.transform, data, q.x, q.y, placed);
                placed++; total++;
            }
        }
        return total;
    }

    private static void MakePickup(Transform parent, MaterialData data, float x, float z, int index)
    {
        GameObject go = new GameObject("Pickup_" + data.id + "_" + index);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, GroundY(x, z), z);
        go.transform.rotation = Quaternion.Euler(0f, Rand() * 360f, 0f);

        Pickup pickup = go.AddComponent<Pickup>();
        SerializedObject so = new SerializedObject(pickup);
        so.FindProperty("material").objectReferenceValue = data;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject model;
        if (data.placedPrefab != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(data.placedPrefab);
            model.transform.SetParent(go.transform, false);
            model.transform.localScale = Vector3.one * data.pickupScale * 1.5f; // 모래 위에서도 눈에 띄게 조금 크게
        }
        else
        {
            // 모델이 없는 재료(바닥재 붉은 모래): 붉은 모래가 소복이 쌓인 더미
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.transform.SetParent(go.transform, false);
            model.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f);
            model.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().sharedMaterial = BottleSetup.GetOrCreateMaterial("RedSandPile", new Color(0.72f, 0.36f, 0.20f));
        }
        model.name = "Model";

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        model.transform.position += Vector3.up * (go.transform.position.y - b.min.y);
        b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = b.size;
        box.size = new Vector3(Mathf.Max(size.x, 0.4f), Mathf.Max(size.y, 0.4f), Mathf.Max(size.z, 0.4f));
    }

    // ───────── 지도 그림 ─────────

    private const string MapImagePath = "Assets/Data/DesertIslandMap.png";
    private const int MapSize = 768;
    private const float MapHalf = 125f;

    private static Vector2 ToPx(float wx, float wz)
    {
        return new Vector2((wx / (MapHalf * 2f) + 0.5f) * MapSize, (wz / (MapHalf * 2f) + 0.5f) * MapSize);
    }

    private static void PutPx(Color[] px, int x, int y, Color c, float alpha = 1f)
    {
        if (x < 0 || y < 0 || x >= MapSize || y >= MapSize) return;
        px[y * MapSize + x] = Color.Lerp(px[y * MapSize + x], c, alpha);
    }

    private static void Dot(Color[] px, Vector2 c, int r, Color col)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r) PutPx(px, (int)c.x + dx, (int)c.y + dy, col);
    }

    private static Texture2D BakeMap(Transform root, Vector2 landing, float pierLen)
    {
        var px = new Color[MapSize * MapSize];
        Color sand = new Color(0.95f, 0.86f, 0.64f), red = new Color(0.90f, 0.68f, 0.45f), grass = new Color(0.62f, 0.75f, 0.42f), trod = new Color(0.85f, 0.74f, 0.56f);
        for (int y = 0; y < MapSize; y++)
        {
            float wz = ((y + 0.5f) / MapSize - 0.5f) * 2f * MapHalf;
            for (int x = 0; x < MapSize; x++)
            {
                float wx = ((x + 0.5f) / MapSize - 0.5f) * 2f * MapHalf;
                float h = HeightAt(wx, wz);
                Color c;
                if (h < 0.25f)
                {
                    float depth = Mathf.Clamp01(-h / 6f);
                    c = Color.Lerp(new Color(0.55f, 0.85f, 0.88f), new Color(0.22f, 0.52f, 0.68f), depth);
                }
                else
                {
                    float dx = HeightAt(wx + 2f, wz) - HeightAt(wx - 2f, wz), dz = HeightAt(wx, wz + 2f) - HeightAt(wx, wz - 2f);
                    float shade = Mathf.Clamp((dx * 0.8f - dz * 0.5f) * 0.05f, -0.12f, 0.12f); // 모래 언덕의 그늘
                    c = Color.Lerp(sand, red, Smooth(0.58f, 0.74f, Mathf.PerlinNoise(wx * 0.028f + 90f, wz * 0.028f + 90f)) * 0.6f);
                    Vector2 wp = new Vector2(wx, wz);
                    if (DistanceToPath(wx, wz) < 1.8f || Vector2.Distance(wp, Camp) < 9f) c = trod;
                    float dpool = Vector2.Distance(wp, Oasis);
                    if (dpool < PondRadius + 6f) c = Color.Lerp(c, grass, 1f - Smooth(PondRadius, PondRadius + 6f, dpool));
                    if (dpool < PondRadius - 0.5f) c = new Color(0.40f, 0.74f, 0.82f);
                    c = new Color(Mathf.Clamp01(c.r + shade), Mathf.Clamp01(c.g + shade), Mathf.Clamp01(c.b + shade), 1f);
                }
                px[y * MapSize + x] = c;
            }
        }

        // 해안선 둘레의 어두운 윤곽
        var outlined = (Color[])px.Clone();
        float step = MapHalf * 2f / MapSize * 2.5f;
        for (int y = 2; y < MapSize - 2; y++)
            for (int x = 2; x < MapSize - 2; x++)
            {
                float wx = ((x + 0.5f) / MapSize - 0.5f) * 2f * MapHalf, wz = ((y + 0.5f) / MapSize - 0.5f) * 2f * MapHalf;
                if (HeightAt(wx, wz) >= 0.25f) continue;
                bool near = HeightAt(wx + step, wz) >= 0.25f || HeightAt(wx - step, wz) >= 0.25f || HeightAt(wx, wz + step) >= 0.25f || HeightAt(wx, wz - step) >= 0.25f;
                if (near) outlined[y * MapSize + x] = Color.Lerp(px[y * MapSize + x], new Color(0.15f, 0.40f, 0.55f), 0.7f);
            }
        px = outlined;

        // 야자나무(초록), 선인장(짙은 초록), 바위(회색), 천막(붉은색), 피라미드(황토색), 선착장
        Transform oasis = root.Find("Oasis");
        if (oasis != null)
            foreach (Transform t in oasis) if (t.name.StartsWith("Palm")) Dot(px, ToPx(t.position.x, t.position.z), 4, new Color(0.18f, 0.50f, 0.25f));
        Transform cacti = root.Find("Cacti");
        if (cacti != null)
            foreach (Transform t in cacti) Dot(px, ToPx(t.position.x, t.position.z), 2, new Color(0.20f, 0.42f, 0.28f));
        Transform rocks = root.Find("Rocks");
        if (rocks != null)
            foreach (Transform t in rocks) Dot(px, ToPx(t.position.x, t.position.z), 2, new Color(0.52f, 0.48f, 0.44f));
        Transform camp = root.Find("Camp");
        if (camp != null)
            foreach (Transform t in camp)
            {
                if (!t.name.StartsWith("Tent")) continue;
                Vector2 c = ToPx(t.position.x, t.position.z);
                for (int dy = -5; dy <= 5; dy++)
                    for (int dx = -5; dx <= 5; dx++)
                    {
                        bool edge = Mathf.Abs(dx) == 5 || Mathf.Abs(dy) == 5;
                        PutPx(px, (int)c.x + dx, (int)c.y + dy, edge ? new Color(0.45f, 0.15f, 0.12f) : new Color(0.82f, 0.32f, 0.28f));
                    }
            }
        Vector2 pc = ToPx(Ruins.x, Ruins.y);
        for (int dy = -9; dy <= 9; dy++)
            for (int dx = -9; dx <= 9; dx++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > 12) continue;
                bool edge = Mathf.Abs(dx) == 9 || Mathf.Abs(dy) == 9 || Mathf.Abs(dx) + Mathf.Abs(dy) >= 12;
                PutPx(px, (int)pc.x + dx, (int)pc.y + dy, edge ? new Color(0.55f, 0.38f, 0.18f) : new Color(0.86f, 0.68f, 0.38f));
            }
        for (float d = 0f; d < pierLen; d += 0.3f)
        {
            Vector2 c = ToPx(landing.x + d, landing.y);
            for (int dy = -2; dy <= 2; dy++) PutPx(px, (int)c.x, (int)c.y + dy, new Color(0.45f, 0.3f, 0.18f));
        }

        var tex = new Texture2D(MapSize, MapSize, TextureFormat.RGB24, false);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(MapImagePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(MapImagePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(MapImagePath);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(MapImagePath);
    }

    private static string VerifyTerrainOnDisk()
    {
        Object[] objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(TerrainFolder + "/DesertIslandTerrain.asset");
        foreach (Object o in objs)
        {
            TerrainData d = o as TerrainData;
            if (d == null) continue;
            float[,] h = d.GetHeights(0, 0, d.heightmapResolution, d.heightmapResolution);
            float max = 0f;
            for (int i = 0; i < d.heightmapResolution; i++) for (int j = 0; j < d.heightmapResolution; j++) if (h[i, j] > max) max = h[i, j];
            return max > 0f ? "지형 높이 저장 확인 OK(최고 " + (max * TerrainHeight + TerrainBaseY).ToString("F1") + "m)" : "경고: 지형 높이가 파일에 저장되지 않았어요!";
        }
        return "경고: 지형 파일을 읽지 못했어요.";
    }
}
