using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 눈 섬(SnowIsland 씬)을 만든다. 지름 약 190m의 작은 눈 섬:
///   가운데 평평한 눈 마을 터(눈사람, 울타리, 돌담, 팻말. 건물은 재료로 만들어 우체통에서 판다), 얼어붙은 연못, 북쪽 소나무 숲, 동쪽 선착장.
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
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Snowman.prefab") == null)
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

        // 선착장 자리(동쪽 물가)와 부두 길이를 먼저 정한다: 마을에서 선착장까지 가는 길의 끝이 되기 때문
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

        // 선착장: 동쪽 바닷가 (이미 열려 있다)
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
        int floes = ScatterFloes(rt, landing2);
        BuildFishing(rt);
        BuildAurora(rt);
        Physics.SyncTransforms();
        int pickups = PlacePickups(rt);
        PickupSize.FixOpenScene(); // 작은 재료가 눈 위에서 안 보이지 않게

        // 지도: 지도 그림을 만들고, 이 씬의 지도 정보(SceneMapInfo)를 둔다
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
        return "눈 섬 만들기 완료: 소나무 " + pines + "그루, 눈 더미 " + mounds + "개, 소품 " + deco + "개, 얼음덩이 " + floes + "개, 재료 줍는 자리 " + pickups + "개. " + VerifyTerrainOnDisk();
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

    /// <summary>해안선이 동그랗지 않고 굽이치게 하는 값: 각도에 따라 섬의 반지름이 0.8배에서 1.2배 사이로 변한다.</summary>
    private static float CoastFactor(float angle)
    {
        return 1f + 0.10f * Mathf.Sin(3f * angle + 1f) + 0.07f * Mathf.Sin(5f * angle + 2f) + 0.04f * Mathf.Sin(8f * angle + 4f);
    }

    private static float HeightAt(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = p.magnitude / (IslandRadius * CoastFactor(Mathf.Atan2(z, x)));

        float land = 1f - Smooth(0.55f, 1.0f, r);
        float h = Mathf.Pow(land, 1.3f) * 7.5f;
        if (r > 1f) h = -(r - 1f) * IslandRadius * 0.12f;

        float noise = Mathf.PerlinNoise(x * 0.02f + 10f, z * 0.02f + 10f) * 2f - 1f;
        float fine = Mathf.PerlinNoise(x * 0.08f + 70f, z * 0.08f + 70f) * 2f - 1f;
        h += (noise * 2.4f + fine * 0.5f) * Smooth(0.1f, 0.6f, 1f - r);

        // 북쪽 산등성이 (소나무 숲이 있는 곳)
        float dx = (x - Ridge.x) / 45f, dz = (z - Ridge.y) / 22f;
        h += 9f * Mathf.Exp(-(dx * dx + dz * dz) / 2f) * Smooth(0f, 0.45f, 1f - r); // 해안으로 가면 낮아진다 (절벽이 되지 않게)

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
        // 높이와 칠을 메모리에서 모두 채운 다음 마지막에 파일로 저장한다. (파일을 먼저 만들고 채우면 높이가 파일에 안 남는 일이 있었다)

        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int iz = 0; iz < HeightmapResolution; iz++)
            for (int ix = 0; ix < HeightmapResolution; ix++)
            {
                float x = -TerrainSize / 2f + ix * TerrainSize / (HeightmapResolution - 1);
                float z = -TerrainSize / 2f + iz * TerrainSize / (HeightmapResolution - 1);
                heights[iz, ix] = Mathf.Clamp01((HeightAt(x, z) - TerrainBaseY) / TerrainHeight);
            }
        td.SetHeights(0, 0, heights);
        td.SyncHeightmap(); // 높이를 파일에 확실히 저장하려면 필요하다 (없으면 껐다 켰을 때 평평해진다)

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

    private static void AddHarvestable(GameObject go, AcquireMethod tool, Color emptyTint, MaterialData fixedMaterial = null)
    {
        Harvestable h = go.AddComponent<Harvestable>();
        SerializedObject so = new SerializedObject(h);
        so.FindProperty("tool").enumValueIndex = (int)tool;
        so.FindProperty("zone").enumValueIndex = (int)MaterialZone.Snow;
        so.FindProperty("emptyTint").colorValue = emptyTint;
        if (fixedMaterial != null) so.FindProperty("fixedMaterial").objectReferenceValue = fixedMaterial;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────── 마을 ─────────

    private static void BuildVillage(Transform parent)
    {
        GameObject group = new GameObject("Village");
        group.transform.SetParent(parent, false);
        Transform g = group.transform;

        // 건물(집 3채, 우물)은 눈 섬에 놓지 않는다. 병 속에 놓는 꾸밈 재료로 우체통에서 산다 (BuildingMaterialSetup)

        // 눈사람 여섯 개: 곡괭이로 캐면 미니 눈사람(병 속 재료)이 나온다. 캐면 푸르스름하게 바래고 다음 날 아침 돌아온다.
        MaterialData miniSnowman = AssetDatabase.LoadAssetAtPath<MaterialData>(MaterialFolder + "mini_snowman.asset");
        Vector2[] snowmen = { new Vector2(8f, -7f), new Vector2(-11f, 7f), new Vector2(-15f, -10f), new Vector2(17f, 11f), new Vector2(-3f, 15f), new Vector2(31f, -12f) };
        for (int i = 0; i < snowmen.Length; i++)
        {
            GameObject sm = Instantiate("Snowman", g, new Vector3(snowmen[i].x, GroundY(snowmen[i].x, snowmen[i].y), snowmen[i].y), Rand() * 360f, Range(3.4f, 4.6f));
            if (sm == null) continue;
            sm.name = "Snowman_" + i;
            AddTrunk(sm, 0.5f, 1.6f);
            AddHarvestable(sm, AcquireMethod.Pickaxe, new Color(0.58f, 0.66f, 0.80f, 1f), miniSnowman);
        }
        GameObject sign1 = Instantiate("Sign1", g, new Vector3(5f, GroundY(5f, 4f), 4f), 20f, 3f);
        GameObject sign2 = Instantiate("Sign2", g, new Vector3(34f, GroundY(34f, 6f), 6f), 100f, 3f);
        foreach (GameObject s in new[] { sign1, sign2 }) if (s != null) AddTrunk(s, 0.2f, 2f);

        // 울타리와 낮은 돌담: 집 뒤와 길가
        Fence(g, "Fence_1", new Vector2(-24f, 4f), new Vector2(-24f, 18f), 4.4f * FenceScale / 2.4f);
        Fence(g, "Fence_2", new Vector2(8f, 20f), new Vector2(24f, 20f), 4.4f * FenceScale / 2.4f);
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

    // 울타리 크기: 2.4배였을 때 2.2m로 사람(1.8m)보다 커서, 허리 높이(약 1.2m)가 되게 줄였다
    private const float FenceScale = 1.3f;

    private static void Fence(Transform parent, string prefab, Vector2 a, Vector2 b, float segment)
    {
        float len = Vector2.Distance(a, b);
        int count = Mathf.Max(1, Mathf.RoundToInt(len / segment));
        float yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg + 90f; // 프리팹은 x 방향으로 길다
        for (int i = 0; i < count; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, (i + 0.5f) / count);
            GameObject f = Instantiate(prefab, parent, new Vector3(p.x, GroundY(p.x, p.y), p.y), yaw, FenceScale);
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
            { "holly_berry", 9 }, { "frost_leaf", 9 }, { "snowball", 10 }, { "winter_bell", 4 }, { "snow", 14 }, { "snow_flower", 10 }
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
                // 방울은 마을 가까이에, 나머지는 어디서나 (미니 눈사람은 눈사람에서 캐고, 벙어리장갑은 우체통에서 산다)
                bool nearVillage = pair.Key == "winter_bell";
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

        GameObject model;
        if (data.placedPrefab != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(data.placedPrefab);
            model.transform.SetParent(go.transform, false);
            model.transform.localScale = Vector3.one * data.pickupScale * 1.9f; // 하얀 눈 위에서는 작은 재료가 안 보여서 크게
        }
        else
        {
            // 모델이 없는 재료(바닥재 눈): 눈이 소복이 쌓인 하얀 더미. 흰 눈 위에서도 보이게 푸르스름한 그늘색으로 한다.
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.transform.SetParent(go.transform, false);
            model.transform.localScale = new Vector3(0.9f, 0.4f, 0.9f);
            model.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().sharedMaterial = BottleSetup.GetOrCreateMaterial("SnowPile", new Color(0.80f, 0.88f, 0.98f));
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

    // ───────── 연못 낚시 ─────────

    private static void BuildFishing(Transform parent)
    {
        GameObject group = new GameObject("Fishing");
        group.transform.SetParent(parent, false);
        // 연못 위의 낚시 구멍 세 곳 (얼음에 뚫은 구멍). 모양은 FishingHoleBuilder가 만든다.
        Vector2[] holes = { Pond + new Vector2(6f, 3f), Pond + new Vector2(-5f, 6f), Pond + new Vector2(0f, -7.5f) };
        for (int i = 0; i < holes.Length; i++)
        {
            float gy = GroundY(holes[i].x, holes[i].y);
            GameObject root = new GameObject("FishingHole_" + i);
            root.transform.SetParent(group.transform, false);
            root.transform.position = new Vector3(holes[i].x, gy, holes[i].y);
            Transform bobber = FishingHoleBuilder.BuildVisuals(root.transform, i);

            SphereCollider hit = root.AddComponent<SphereCollider>();
            hit.isTrigger = true; // 걸어 다니는 데 걸리지 않게. 바라보는 광선은 맞는다
            hit.center = new Vector3(0f, 0.3f, 0f);
            hit.radius = 1.3f;

            FishingSpot spot = root.AddComponent<FishingSpot>();
            SerializedObject so = new SerializedObject(spot);
            so.FindProperty("bobber").objectReferenceValue = bobber;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 낚시터 팻말
        Vector2 signPos = Pond + new Vector2(10f, -3f);
        GameObject sign = Instantiate("Sign2", group.transform, new Vector3(signPos.x, GroundY(signPos.x, signPos.y), signPos.y), 120f, 3f);
        if (sign != null) AddTrunk(sign, 0.2f, 2f);
    }

    private static void Disc(Transform parent, string name, float diameter, float thickness, float lift, Material mat)
    {
        GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(0f, lift, 0f);
        d.transform.localScale = new Vector3(diameter, thickness, diameter);
        Object.DestroyImmediate(d.GetComponent<Collider>());
        d.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ───────── 오로라 하늘 ─────────

    private static void BuildAurora(Transform parent)
    {
        const string matPath = "Assets/Resources/AuroraMat.mat";
        Directory.CreateDirectory("Assets/Resources");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("유리병 정원/Aurora");
        if (shader == null) { Debug.LogWarning("오로라 셰이더를 찾지 못했어요."); return; }
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, matPath); }
        m.shader = shader;
        EditorUtility.SetDirty(m);

        GameObject sky = new GameObject("AuroraSky");
        sky.transform.SetParent(parent, false);
        sky.AddComponent<AuroraSky>();
    }

    // ───────── 바다 위 얼음덩이 (섬이 바다에 떠 있는 느낌) ─────────

    private static int ScatterFloes(Transform parent, Vector2 landing)
    {
        GameObject group = new GameObject("IceFloes");
        group.transform.SetParent(parent, false);
        var placedAt = new List<Vector2>();
        int placed = 0, tries = 0;
        while (placed < 22 && tries++ < 1200)
        {
            float ang = Rand() * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float shore = 0f;
            for (float r = 30f; r < 170f; r += 1f)
                if (HeightAt(dir.x * r, dir.y * r) <= 0f) { shore = r; break; }
            if (shore <= 0f) continue;
            Vector2 p = dir * (shore + Range(10f, 55f));
            if (Mathf.Abs(p.y - landing.y) < 16f && p.x > landing.x - 8f) continue; // 선착장 앞 바다는 비운다
            bool tooClose = false;
            foreach (Vector2 q in placedAt) if ((q - p).sqrMagnitude < 14f * 14f) { tooClose = true; break; }
            if (tooClose) continue;
            string prefab = "Snow" + (1 + (int)(Rand() * 4f));
            GameObject f = Instantiate(prefab, group.transform, new Vector3(p.x, -0.5f, p.y), Rand() * 360f, Range(5f, 9f));
            if (f == null) continue;
            f.name = "Floe_" + placed;
            placedAt.Add(p);
            placed++;
        }
        return placed;
    }

    // ───────── 지도 그림 ─────────

    private const string MapImagePath = "Assets/Data/SnowIslandMap.png";
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

    private static Texture2D BakeMap(Transform root, Vector2 landing, float pierLen)
    {
        var px = new Color[MapSize * MapSize];
        Color snow = new Color(0.96f, 0.975f, 1f), ice = new Color(0.78f, 0.92f, 0.97f), pondC = new Color(0.66f, 0.85f, 0.95f), trod = new Color(0.84f, 0.86f, 0.90f);
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
                    c = Color.Lerp(new Color(0.66f, 0.86f, 0.93f), new Color(0.30f, 0.52f, 0.72f), depth);
                }
                else
                {
                    float dx = HeightAt(wx + 2f, wz) - HeightAt(wx - 2f, wz), dz = HeightAt(wx, wz + 2f) - HeightAt(wx, wz - 2f);
                    float shade = Mathf.Clamp((dx * 0.7f - dz * 0.7f) * 0.04f, -0.10f, 0.10f);
                    c = snow;
                    c = Color.Lerp(c, ice, 1f - Smooth(0.6f, 1.9f, h)); // 얼어붙은 물가
                    Vector2 wp = new Vector2(wx, wz);
                    if (DistanceToPath(wx, wz) < 1.8f || Vector2.Distance(wp, Village) < 9f) c = trod;
                    if (Vector2.Distance(wp, Pond) < PondRadius + 0.5f) c = pondC;
                    c = new Color(Mathf.Clamp01(c.r + shade), Mathf.Clamp01(c.g + shade), Mathf.Clamp01(c.b + shade), 1f);
                }
                px[y * MapSize + x] = c;
            }
        }

        // 해안선 둘레의 어두운 윤곽 (섬이 바다에서 또렷하게 보이게)
        var outlined = (Color[])px.Clone();
        float step = MapHalf * 2f / MapSize * 2.5f;
        for (int y = 2; y < MapSize - 2; y++)
            for (int x = 2; x < MapSize - 2; x++)
            {
                float wx = ((x + 0.5f) / MapSize - 0.5f) * 2f * MapHalf, wz = ((y + 0.5f) / MapSize - 0.5f) * 2f * MapHalf;
                if (HeightAt(wx, wz) >= 0.25f) continue;
                bool near = HeightAt(wx + step, wz) >= 0.25f || HeightAt(wx - step, wz) >= 0.25f || HeightAt(wx, wz + step) >= 0.25f || HeightAt(wx, wz - step) >= 0.25f;
                if (near) outlined[y * MapSize + x] = Color.Lerp(px[y * MapSize + x], new Color(0.2f, 0.42f, 0.62f), 0.75f);
            }
        px = outlined;

        // 소나무 (짙은 초록 삼각형), 집 (갈색 네모), 우물, 선착장
        Transform forest = root.Find("Forest");
        if (forest != null)
            foreach (Transform t in forest)
            {
                Vector2 c = ToPx(t.position.x, t.position.z);
                for (int dy = -4; dy <= 4; dy++)
                {
                    float half = (4f - dy) * 0.45f;
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        if (Mathf.Abs(dx) > half) continue;
                        PutPx(px, (int)c.x + dx + 1, (int)c.y + dy - 1, new Color(0.1f, 0.2f, 0.25f), 0.18f);
                        PutPx(px, (int)c.x + dx, (int)c.y + dy, Mathf.Abs(dx) > half - 0.8f ? new Color(0.06f, 0.24f, 0.18f) : new Color(0.13f, 0.38f, 0.26f));
                    }
                }
            }
        Transform village = root.Find("Village");
        if (village != null)
            foreach (Transform t in village)
            {
                if (!t.name.StartsWith("Cottage") && t.name != "Well") continue;
                Vector2 c = ToPx(t.position.x, t.position.z);
                bool house = t.name.StartsWith("Cottage");
                int r = house ? 6 : 3;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        bool edge = Mathf.Abs(dx) == r || Mathf.Abs(dy) == r;
                        PutPx(px, (int)c.x + dx, (int)c.y + dy, house ? (edge ? new Color(0.36f, 0.2f, 0.12f) : new Color(0.7f, 0.42f, 0.25f)) : (edge ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.55f, 0.58f, 0.65f)));
                    }
            }
        for (float d = 0f; d < pierLen; d += 0.3f) // 부두
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

    /// <summary>
    /// 지형 파일을 디스크에서 새로 읽어 높이가 정말 저장됐는지 확인한다. (메모리에만 남아 있고 파일에는 평평하게 저장되는 문제가 있었다)
    /// </summary>
    private static string VerifyTerrainOnDisk()
    {
        Object[] objs = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(TerrainFolder + "/SnowIslandTerrain.asset");
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