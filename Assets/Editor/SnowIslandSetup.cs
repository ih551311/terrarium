using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 눈 섬(SnowIsland 씬)을 만든다. 지름 약 190m의 작은 눈 섬:
///   가운데 평평한 눈 마을(집 세 채, 우물, 눈사람, 울타리, 팻말), 얼어붙은 연못, 북쪽 소나무 숲, 동쪽 선착장.
/// 모양과 건물은 에셋 "Cozy Snowy Environment"(Assets/HobiSoloved)로 꾸미고, 지형(Terrain)과 바다는 코드로 만든다.
/// 눈 섬은 언제나 눈이 내리고(SceneClimate), 눈 섬 재료(MaterialZone.Snow)가 소나무(도끼), 눈 더미(곡괭이), 바닥(줍기)에서 난다.
/// 처음 만들 때는 Island 씬을 복사해서 시작한다(플레이어, 하늘, 조명, 후처리가 이미 들어 있다). 다시 누르면 눈 섬 내용만 새로 만든다.
/// 메뉴: 유리병 정원 > 눈 섬 만들기 (눈 에셋 재질 바꾸기, 눈 섬 재료 만들기를 먼저 해 둘 것)
/// </summary>
public static class SnowIslandSetup
{
    // ───────── 섬 모양 (미터) ─────────
    private const float TerrainSize = 360f;
    private const float TerrainHeight = 40f;
    private const float TerrainBaseY = -6f;
    private const int HeightmapResolution = 257;
    private const int AlphamapResolution = 256;

    private const float IslandRadius = 100f;
    private const float VillageHeight = 6f;
    private static readonly Vector2 Village = new Vector2(0f, 0f);
    private static readonly Vector2 Pond = new Vector2(-40f, -24f);
    private const float PondRadius = 13f;
    private const float PondHeight = 3.3f;
    private static readonly Vector2 Ridge = new Vector2(0f, 68f);

    // 마을에서 선착장까지 가는 길 (눈이 다져진 길과 징검돌)
    private static readonly Vector2[] Path = { new Vector2(0f, 0f), new Vector2(26f, 8f), new Vector2(56f, -3f), new Vector2(90f, 0f) };

    private const string RootName = "SnowContent";
    private const string ScenePath = "Assets/Scenes/SnowIsland.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string Prefabs = SnowAssetSetup.Folder + "/Prefabs/";
    private const string TerrainFolder = "Assets/Terrain";
    private const string MaterialFolder = "Assets/Data/Materials/";

    private static System.Random rng;
    private static Terrain terrain;
    private static float Rand() { return (float)rng.NextDouble(); }
    private static float Range(float a, float b) { return Mathf.Lerp(a, b, Rand()); }

    [MenuItem("유리병 정원/눈 섬 만들기")]
    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Cottage1.prefab") == null)
            return "눈 에셋(Assets/HobiSoloved)을 찾지 못했어요. 먼저 에셋을 가져오세요.";
        SnowAssetSetup.ConvertMaterials();

        EditorSceneManager.SaveOpenScenes();
        if (!File.Exists(ScenePath)) AssetDatabase.CopyAsset(IslandPath, ScenePath); // 플레이어, 하늘, 조명이 들어 있는 채로 시작
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        rng = new System.Random(20261120);

        foreach (string n in new[] { "IslandContent", "Spawn_FromHut", "MainDock", "Spawn_FromSnow", RootName, "SceneClimate" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }

        GameObject root = new GameObject(RootName);
        Transform rt = root.transform;
        terrain = BuildTerrain(rt);
        BuildSea(rt);

        // 선착장: 동쪽 바닷가 (이미 열려 있다)
        Vector2 landing2 = FindShore(0f, 0.35f);
        float pierLen = 16f;
        for (float d = 0f; d < 60f; d += 0.5f)
            if (HeightAt(landing2.x + d, landing2.y) <= -0.6f) { pierLen = Mathf.Clamp(d + 3f, 14f, 36f); break; }
        Vector3 landingSpot;
        float landY = HeightAt(landing2.x, landing2.y);
        GameObject dockRoot = DockSetup.Build(rt, "Dock", landing2, Vector3.right, false, "Island", "FromSnow", "배 타고 돌아가기", out landingSpot, landY, pierLen);
        GameObject spawn = new GameObject("Spawn_FromDock");
        spawn.transform.SetParent(rt, false);
        spawn.transform.SetPositionAndRotation(landingSpot, Quaternion.Euler(0f, -90f, 0f));
        SpawnPoint sp = spawn.AddComponent<SpawnPoint>();
        SerializedObject sps = new SerializedObject(sp);
        sps.FindProperty("id").stringValue = "FromDock";
        sps.ApplyModifiedPropertiesWithoutUndo();

        // 이 섬은 늘 눈이 온다
        GameObject climate = new GameObject("SceneClimate");
        SceneClimate sc = climate.AddComponent<SceneClimate>();
        SerializedObject cso = new SerializedObject(sc);
        cso.FindProperty("weather").enumValueIndex = (int)WeatherKind.Snow;
        cso.ApplyModifiedPropertiesWithoutUndo();

        // 마을과 나무, 눈 더미
        BuildVillage(rt);
        int pines = ScatterForest(rt);
        int mounds = ScatterMounds(rt);
        int deco = ScatterDecor(rt);
        Physics.SyncTransforms();
        int pickups = PlacePickups(rt);

        // 플레이어를 선착장 도착 자리로
        foreach (GameRoot gr in Object.FindObjectsByType<GameRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform player = gr.transform.Find("Player");
            if (player != null) player.position = landingSpot;
        }

        // 안개 (날씨가 덮어쓰지만, 편집 화면에서도 차갑게 보이게)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.80f, 0.86f, 0.92f);
        RenderSettings.fogStartDistance = 120f;
        RenderSettings.fogEndDistance = 900f;

        AddToBuildSettings();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "눈 섬 만들기 완료: 소나무 " + pines + "그루, 눈 더미 " + mounds + "개, 소품 " + deco + "개, 재료 줍는 자리 " + pickups + "개";
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

    private static float HeightAt(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = p.magnitude / IslandRadius;

        float land = 1f - Smooth(0.55f, 1.0f, r);
        float h = Mathf.Pow(land, 1.3f) * 7.5f;
        if (r > 1f) h = -(r - 1f) * IslandRadius * 0.12f;

        float noise = Mathf.PerlinNoise(x * 0.02f + 10f, z * 0.02f + 10f) * 2f - 1f;
        float fine = Mathf.PerlinNoise(x * 0.08f + 70f, z * 0.08f + 70f) * 2f - 1f;
        h += (noise * 2.4f + fine * 0.5f) * Smooth(0.1f, 0.6f, 1f - r);

        // 북쪽 산등성이 (소나무 숲이 있는 곳)
        float dx = (x - Ridge.x) / 45f, dz = (z - Ridge.y) / 22f;
        h += 9f * Mathf.Exp(-(dx * dx + dz * dz) / 2f);

        // 마을 터: 평평하게 고른다
        float dv = Vector2.Distance(p, Village);
        h = Mathf.Lerp(h, VillageHeight, 1f - Smooth(20f, 32f, dv));

        // 얼어붙은 연못: 평평하게 고른다
        float dp = Vector2.Distance(p, Pond);
        h = Mathf.Lerp(h, PondHeight, 1f - Smooth(PondRadius - 2f, PondRadius + 6f, dp));
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

    /// <summary>중심에서 angle도 방향으로 나가다가 땅이 처음 maxHeight 이하가 되는 자리 (선착장 땅 쪽 끝)</summary>
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

    private static Texture2D MakeTexture(string file, Color baseColor, Color variation, float streak, float sparkle, int seed)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        var local = new System.Random(seed);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 이어 붙여도 티 나지 않게 주기가 맞는 소리(노이즈)
                float u = x / (float)size, v = y / (float)size;
                float n = Mathf.PerlinNoise(Mathf.Cos(u * Mathf.PI * 2f) * 2f + 5f, Mathf.Sin(u * Mathf.PI * 2f) * 2f + v * 4f) * 0.5f
                        + Mathf.PerlinNoise(Mathf.Cos(v * Mathf.PI * 2f) * 4f + 9f, Mathf.Sin(v * Mathf.PI * 2f) * 4f + u * 6f) * 0.5f;
                Color c = Color.Lerp(baseColor, variation, Mathf.Clamp01((n - 0.35f) * 2.2f));
                c = Color.Lerp(c, baseColor, 0.2f);
                float streaks = Mathf.PerlinNoise(x * 0.015f + 3f, y * 0.12f + 3f) * streak;
                c = Color.Lerp(c, variation, streaks);
                if ((float)local.NextDouble() < sparkle) c = Color.Lerp(c, Color.white, 0.7f); // 반짝이는 눈 알갱이
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        Directory.CreateDirectory(TerrainFolder + "/Snow");
        string path = TerrainFolder + "/Snow/" + file + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static TerrainLayer MakeLayer(string name, Texture2D tex, float tile, float smoothness)
    {
        string path = TerrainFolder + "/Snow/" + name + ".terrainlayer";
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
        string path = TerrainFolder + "/SnowIslandTerrain.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(td, path);

        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int iz = 0; iz < HeightmapResolution; iz++)
            for (int ix = 0; ix < HeightmapResolution; ix++)
            {
                float x = -TerrainSize / 2f + ix * TerrainSize / (HeightmapResolution - 1);
                float z = -TerrainSize / 2f + iz * TerrainSize / (HeightmapResolution - 1);
                heights[iz, ix] = Mathf.Clamp01((HeightAt(x, z) - TerrainBaseY) / TerrainHeight);
            }
        td.SetHeights(0, 0, heights);

        // 칠하는 층: 눈, 얼음, 다져진 눈(길). 층 이름의 snow/ice/path가 발소리를 정한다 (FootstepPlayer)
        TerrainLayer snow = MakeLayer("Snow_Layer", MakeTexture("snow_diffuse", new Color(0.95f, 0.97f, 1f), new Color(0.84f, 0.90f, 0.98f), 0.25f, 0.004f, 1), 9f, 0.25f);
        TerrainLayer ice = MakeLayer("Ice_Layer", MakeTexture("ice_diffuse", new Color(0.70f, 0.87f, 0.95f), new Color(0.88f, 0.96f, 1f), 0.6f, 0.002f, 2), 12f, 0.7f);
        TerrainLayer trod = MakeLayer("Path_Layer", MakeTexture("trod_diffuse", new Color(0.80f, 0.83f, 0.88f), new Color(0.68f, 0.72f, 0.80f), 0.35f, 0.006f, 3), 5f, 0.15f);
        td.terrainLayers = new[] { snow, ice, trod };

        float[,,] alpha = new float[AlphamapResolution, AlphamapResolution, 3];
        for (int iz = 0; iz < AlphamapResolution; iz++)
            for (int ix = 0; ix < AlphamapResolution; ix++)
            {
                float x = -TerrainSize / 2f + (ix + 0.5f) * TerrainSize / AlphamapResolution;
                float z = -TerrainSize / 2f + (iz + 0.5f) * TerrainSize / AlphamapResolution;
                float h = HeightAt(x, z);
                float wob = (Mathf.PerlinNoise(x * 0.06f, z * 0.06f) - 0.5f) * 3f;

                float wIce = 1f - Smooth(PondRadius - 1f, PondRadius + 3f, Vector2.Distance(new Vector2(x, z), Pond) + wob); // 연못
                wIce = Mathf.Max(wIce, 1f - Smooth(0.8f, 1.9f, h + wob * 0.3f));                                          // 얼어붙은 물가
                float plaza = 1f - Smooth(7f, 11f, Vector2.Distance(new Vector2(x, z), Village) + wob);                   // 우물 둘레 광장
                float wTrod = Mathf.Max(plaza, 1f - Smooth(1.6f, 3.4f, DistanceToPath(x, z) + wob * 0.5f)) * (1f - wIce);
                float wSnow = Mathf.Max(0f, 1f - wIce - wTrod);
                float sum = wSnow + wIce + wTrod;
                alpha[iz, ix, 0] = wSnow / sum; alpha[iz, ix, 1] = wIce / sum; alpha[iz, ix, 2] = wTrod / sum;
            }
        td.SetAlphamaps(0, 0, alpha);
        EditorUtility.SetDirty(td);
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

    // ───────── 바다 (차가운 푸른 회색) ─────────

    private static void BuildSea(Transform parent)
    {
        // 원래 섬과 같은 메쉬를 쓰고 색만 차갑게 한다. (이름이 SeaCoast여야 SkyController가 안개 색으로 물들인다)
        Mesh coastMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Data/Placeholder/SeaCoast.asset");
        Mesh farMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Data/Placeholder/SeaFar.asset");
        Material srcCoast = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Placeholder/SeaCoast.mat");
        if (coastMesh == null || farMesh == null || srcCoast == null)
        {
            Debug.LogWarning("원래 섬의 바다 재료(SeaCoast, SeaFar)를 찾지 못했어요. 먼저 '바다 다시 만들기'를 하세요.");
            return;
        }

        const string coastPath = "Assets/Data/Placeholder/SnowSeaCoast.mat";
        Material coastMat = AssetDatabase.LoadAssetAtPath<Material>(coastPath);
        if (coastMat == null) { coastMat = new Material(srcCoast); AssetDatabase.CreateAsset(coastMat, coastPath); }
        Color deep = new Color(0.20f, 0.38f, 0.52f, 1f);
        coastMat.SetColor("Color_F01C36BF", new Color(0.72f, 0.90f, 0.95f, 0.8f)); // 얕은 곳: 얼음빛
        coastMat.SetColor("Color_7D9A58EC", deep);                                  // 깊은 곳
        EditorUtility.SetDirty(coastMat);

        GameObject coast = new GameObject("SeaCoast");
        coast.transform.SetParent(parent, false);
        coast.transform.position = Vector3.zero;
        coast.AddComponent<MeshFilter>().sharedMesh = coastMesh;
        MeshRenderer cr = coast.AddComponent<MeshRenderer>();
        cr.sharedMaterial = coastMat;
        cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        const string farPath = "Assets/Data/Placeholder/SnowSeaFar.mat";
        Material farMat = AssetDatabase.LoadAssetAtPath<Material>(farPath);
        if (farMat == null)
        {
            farMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(farMat, farPath);
        }
        farMat.SetColor("_BaseColor", deep);
        farMat.SetFloat("_Smoothness", 0.25f);
        farMat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(farMat);

        GameObject far = new GameObject("SeaFar");
        far.transform.SetParent(parent, false);
        far.transform.position = new Vector3(0f, -3f, 0f);
        far.AddComponent<MeshFilter>().sharedMesh = farMesh;
        MeshRenderer fr = far.AddComponent<MeshRenderer>();
        fr.sharedMaterial = farMat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ───────── 놓기 도우미 ─────────

    private static GameObject Instantiate(string prefabName, Transform parent, Vector3 pos, float yaw, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + prefabName + ".prefab");
        if (prefab == null) return null;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        go.transform.localScale = prefab.transform.localScale * scale;
        go.transform.position = pos;
        // 바닥(모델 맨 아래)이 땅에 닿게
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            go.transform.position += Vector3.up * (pos.y - b.min.y - 0.02f);
        }
        return go;
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>모델을 감싸는 상자 Collider (걸어서 통과하지 못하는 건물과 소품)</summary>
    private static void AddSolidBox(GameObject go, float shrink = 0.9f)
    {
        Bounds b = BoundsOf(go);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 s = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) * shrink;
        box.size = new Vector3(Mathf.Max(box.size.x, 0.1f), Mathf.Max(box.size.y, 0.1f), Mathf.Max(box.size.z, 0.1f));
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

    private static void AddHarvestable(GameObject go, AcquireMethod tool, Color emptyTint)
    {
        Harvestable h = go.AddComponent<Harvestable>();
        SerializedObject so = new SerializedObject(h);
        so.FindProperty("tool").enumValueIndex = (int)tool;
        so.FindProperty("zone").enumValueIndex = (int)MaterialZone.Snow;
        so.FindProperty("emptyTint").colorValue = emptyTint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────── 마을 ─────────

    private static void BuildVillage(Transform parent)
    {
        GameObject group = new GameObject("Village");
        group.transform.SetParent(parent, false);
        Transform g = group.transform;

        // 우물: 마을 한가운데
        GameObject well = Instantiate("Well", g, new Vector3(0f, GroundY(0f, 0f), 0f), 0f, 2.4f);
        if (well != null) AddSolidBox(well, 0.85f);

        // 집 세 채: 광장을 둘러싸고 우물을 바라본다 (모델은 키가 1.8m라 3.4배로 키워 6m 남짓)
        Vector3[] house = { new Vector3(-17f, 0f, 9f), new Vector3(15f, 0f, 13f), new Vector3(-3f, 0f, -19f) };
        float[] yaw = new float[3];
        string[] names = { "Cottage1", "Cottage2", "Cottage3" };
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(house[i].x, GroundY(house[i].x, house[i].z), house[i].z);
            Vector3 toWell = (Vector3.zero - p); toWell.y = 0f;
            yaw[i] = Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg; // 집의 앞(창)은 -z 쪽이라, 우물을 바라보게 돌린다
            GameObject c = Instantiate(names[i], g, p, yaw[i], 3.4f);
            if (c == null) continue;
            c.name = names[i];
            AddSolidBox(c, 0.92f);
        }

        // 눈사람과 팻말
        GameObject snowman = Instantiate("Snowman", g, new Vector3(8f, GroundY(8f, -7f), -7f), 200f, 4f);
        if (snowman != null) AddTrunk(snowman, 0.5f, 1.6f);
        GameObject sign1 = Instantiate("Sign1", g, new Vector3(5f, GroundY(5f, 4f), 4f), 20f, 3f);
        GameObject sign2 = Instantiate("Sign2", g, new Vector3(34f, GroundY(34f, 6f), 6f), 100f, 3f);
        foreach (GameObject s in new[] { sign1, sign2 }) if (s != null) AddTrunk(s, 0.2f, 2f);

        // 울타리와 낮은 돌담: 집 뒤와 길가
        Fence(g, "Fence_1", new Vector2(-24f, 4f), new Vector2(-24f, 18f), 4.4f);
        Fence(g, "Fence_2", new Vector2(8f, 20f), new Vector2(24f, 20f), 4.4f);
        Wall(g, "Wall1", new Vector2(-12f, -24f), new Vector2(6f, -24f), 3.6f);
        Wall(g, "Wall3", new Vector2(22f, -2f), new Vector2(22f, -12f), 3.6f);

        // 징검돌: 마을에서 선착장까지 가는 길
        float walked = 0f; int n = 0;
        for (int i = 0; i < Path.Length - 1; i++)
        {
            Vector2 a = Path[i], b = Path[i + 1];
            float len = Vector2.Distance(a, b);
            for (float d = walked; d < len; d += 2.6f)
            {
                Vector2 p = Vector2.Lerp(a, b, d / len);
                Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized * (n % 2 == 0 ? 0.55f : -0.55f);
                p += side;
                if (HeightAt(p.x, p.y) < 1.2f) break;
                if (Vector2.Distance(p, Village) < 9f) continue;
                Instantiate("Path" + (1 + n % 9), g, new Vector3(p.x, GroundY(p.x, p.y), p.y), Rand() * 360f, 3.3f);
                n++;
            }
            walked = Mathf.Max(0f, walked + Mathf.Ceil((len - walked) / 2.6f) * 2.6f - len);
        }
    }

    private static void Fence(Transform parent, string prefab, Vector2 a, Vector2 b, float segment)
    {
        float len = Vector2.Distance(a, b);
        int count = Mathf.Max(1, Mathf.RoundToInt(len / segment));
        float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg + 90f; // 프리팹은 x 방향으로 길다
        for (int i = 0; i < count; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (i + 0.5f) / count);
            GameObject f = Instantiate(prefab, parent, new Vector3(p.x, GroundY(p.x, p.y), p.y), yaw, 2.4f);
            if (f != null) AddSolidBox(f, 1f);
        }
    }

    private static void Wall(Transform parent, string prefab, Vector2 a, Vector2 b, float segment)
    {
        float len = Vector2.Distance(a, b);
        int count = Mathf.Max(1, Mathf.RoundToInt(len / segment));
        float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg + 90f;
        for (int i = 0; i < count; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (i + 0.5f) / count);
            GameObject w = Instantiate(prefab, parent, new Vector3(p.x, GroundY(p.x, p.y), p.y), yaw, 2.3f);
            if (w != null) AddSolidBox(w, 1f);
        }
    }

    // ───────── 숲, 눈 더미, 소품 ─────────

    private static readonly List<Vector2> taken = new List<Vector2>();

    private static bool Free(float x, float z, float spacing)
    {
        foreach (Vector2 t in taken) if ((t.x - x) * (t.x - x) + (t.y - z) * (t.y - z) < spacing * spacing) return false;
        return true;
    }

    /// <summary>나무나 소품을 놓아도 되는 자리인가: 땅이고, 마을 터, 연못, 길에서 떨어져 있다.</summary>
    private static bool Allowed(float x, float z, float villageClear, float pathClear)
    {
        float h = HeightAt(x, z);
        if (h < 1.7f) return false;
        if (Vector2.Distance(new Vector2(x, z), Village) < villageClear) return false;
        if (Vector2.Distance(new Vector2(x, z), Pond) < PondRadius + 3f) return false;
        if (DistanceToPath(x, z) < pathClear) return false;
        return true;
    }

    private static int ScatterForest(Transform parent)
    {
        taken.Clear();
        GameObject group = new GameObject("Forest");
        group.transform.SetParent(parent, false);
        string[] pines = { "Pine1", "Pine2", "Pine3", "Pine4", "Pine5" };
        float[] scale = { 11f, 9.5f, 9.5f, 7f, 6.6f };  // 소나무 모델은 0.3~1.1m라서 3~7m 키로 키운다
        int placed = 0, tries = 0;
        while (placed < 190 && tries++ < 6000)
        {
            float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius - 6f);
            float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
            if (!Allowed(x, z, 36f, 5f)) continue;
            // 숲이 덩어리지게: 소리(노이즈)가 높은 곳과 북쪽 산등성이에 촘촘하게
            float forest = Mathf.PerlinNoise(x * 0.035f + 200f, z * 0.035f + 200f) + Mathf.Clamp01((z - 20f) / 60f) * 0.45f;
            if (forest < 0.62f) continue;
            if (!Free(x, z, 3.6f)) continue;

            int k = Mathf.Min(pines.Length - 1, (int)(Rand() * pines.Length));
            GameObject tree = Instantiate(pines[k], group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, scale[k] * Range(0.85f, 1.15f));
            if (tree == null) continue;
            tree.name = pines[k] + "_" + placed;
            AddTrunk(tree, 0.4f, 2.2f);
            // 소나무의 일부는 도끼로 캘 수 있다 (솔잎 가지, 눈 덮인 솔방울, 장작). 비면 눈이 덮인 색이 바랜다
            if (Rand() < 0.45f) AddHarvestable(tree, AcquireMethod.Axe, new Color(0.55f, 0.62f, 0.74f, 1f));
            taken.Add(new Vector2(x, z));
            placed++;
        }
        return placed;
    }

    private static int ScatterMounds(Transform parent)
    {
        GameObject group = new GameObject("Mounds");
        group.transform.SetParent(parent, false);
        int placed = 0, tries = 0;
        while (placed < 34 && tries++ < 4000)
        {
            float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius - 10f);
            float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
            if (!Allowed(x, z, 26f, 4f) || !Free(x, z, 3.2f)) continue;
            string prefab = "Snow" + (1 + (int)(Rand() * 4f));
            GameObject m = Instantiate(prefab, group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, Range(3.2f, 4.6f));
            if (m == null) continue;
            m.name = prefab + "_" + placed;
            AddSolidBox(m, 0.8f);
            // 눈 더미는 곡괭이로 캘 수 있다 (얼음 조각, 고드름). 캐면 흰색이 푸르스름하게 바랜다
            AddHarvestable(m, AcquireMethod.Pickaxe, new Color(0.58f, 0.68f, 0.82f, 1f));
            taken.Add(new Vector2(x, z));
            placed++;
        }
        return placed;
    }

    private static int ScatterDecor(Transform parent)
    {
        GameObject group = new GameObject("Decor");
        group.transform.SetParent(parent, false);
        int placed = 0, tries = 0;
        string[] things = { "Tree1", "Tree2", "DeadTree" };
        float[] scale = { 3.2f, 3.2f, 3.6f };
        while (placed < 60 && tries++ < 4000)
        {
            float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius - 8f);
            float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
            if (!Allowed(x, z, 30f, 4f) || !Free(x, z, 4.2f)) continue;
            int k = (int)(Rand() * things.Length);
            GameObject o = Instantiate(things[k], group.transform, new Vector3(x, GroundY(x, z), z), Rand() * 360f, scale[k] * Range(0.85f, 1.2f));
            if (o == null) continue;
            AddTrunk(o, 0.3f, 2f);
            taken.Add(new Vector2(x, z));
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
        // 재료마다 놓을 곳의 수 (눈꽃은 원래 꽃 언덕의 재료지만 눈 오는 날에만 피는 꽃이라 여기도 핀다)
        var want = new Dictionary<string, int>
        {
            { "holly_berry", 9 }, { "frost_leaf", 9 }, { "snowball", 10 }, { "winter_bell", 4 }, { "mitten", 4 }, { "mini_snowman", 3 }, { "snow_flower", 10 }
        };
        foreach (KeyValuePair<string, int> pair in want)
        {
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(MaterialFolder + pair.Key + ".asset");
            if (data == null) { Debug.LogWarning("재료를 찾지 못했어요: " + pair.Key + " (눈 섬 재료를 먼저 만드세요)"); continue; }
            int placed = 0, tries = 0;
            while (placed < pair.Value && tries++ < 3000)
            {
                float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius - 8f);
                float x = Mathf.Cos(ang) * rad, z = Mathf.Sin(ang) * rad;
                // 방울, 장갑, 눈사람은 마을 가까이에, 나머지는 어디서나
                bool nearVillage = pair.Key == "winter_bell" || pair.Key == "mitten" || pair.Key == "mini_snowman";
                float d = Vector2.Distance(new Vector2(x, z), Village);
                if (nearVillage && (d < 11f || d > 48f)) continue;
                if (!Allowed(x, z, 10f, 2f)) continue;
                if (Physics.CheckSphere(new Vector3(x, GroundY(x, z) + 1.4f, z), 0.9f)) continue;
                MakePickup(group.transform, data, x, z, placed);
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

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(data.placedPrefab);
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * data.pickupScale * 1.9f; // 하얀 눈 위에서는 작은 재료가 안 보여서 크게
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
}
