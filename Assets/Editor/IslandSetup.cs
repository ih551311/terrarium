using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬(Island 씬)을 만든다. 지름 약 300m의 섬:
///   가운데 평평한 언덕 위의 오두막, 남쪽 바닷가, 북쪽 작은 숲, 북동쪽 꽃 언덕, 구역 사이의 흙길.
/// 지형(Terrain)과 바다, 오두막(CabinBuilder), 나무, 바위, 풀, 버섯, 재료 줍는 자리를 만든다.
/// 재료는 각 재료 데이터(MaterialData)의 구역(zone)과 얻는 방법(acquire)에 따라 놓는다. 상점 품목은 놓지 않는다.
/// 메뉴: 유리병 정원 > 섬 만들기. 여러 번 눌러도 같은 결과가 나온다(같은 난수 씨앗을 쓴다).
/// 섬 모양과 개수는 아래 숫자만 고치고 다시 누르면 바뀐다.
/// </summary>
public static class IslandSetup
{
    // ───────── 섬 모양 (미터) ─────────
    private const float TerrainSize = 640f;          // 지형 한 변
    private const float TerrainHeight = 60f;         // 지형 높이 범위
    private const float TerrainBaseY = -8f;          // 지형 맨 아래의 세계 높이 (바다 밑바닥). 바다 높이는 0
    private const int HeightmapResolution = 513;
    private const int AlphamapResolution = 512;

    private static readonly Vector2 IslandCenter = new Vector2(0f, 30f);
    private const float IslandRadius = 150f;         // 섬 지름 약 300m (걷기 약 1분 30초, 달리기 약 45초)
    private static readonly Vector2 HutPlateau = new Vector2(0f, 10f);
    private const float HutHeight = 11f;             // 오두막이 있는 언덕 높이
    private static readonly Vector2 FlowerHill = new Vector2(90f, 85f);
    private const float FlowerHillRadius = 45f;
    private static readonly Vector3 HutPosition = new Vector3(0f, 0f, 14f); // y는 지형에서 읽는다

    private static readonly Vector2[][] Paths =
    {
        new[] { new Vector2(0f, 6f), new Vector2(0f, -28f), new Vector2(-6f, -68f), new Vector2(-4f, -112f) },       // 바닷가로
        new[] { new Vector2(0f, 28f), new Vector2(5f, 55f), new Vector2(-8f, 85f), new Vector2(-18f, 115f) },        // 작은 숲으로
        new[] { new Vector2(12f, 8f), new Vector2(34f, 28f), new Vector2(62f, 52f), new Vector2(82f, 74f) },         // 꽃 언덕으로
    };

    private const string RootName = "IslandContent";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string TerrainFolder = "Assets/Terrain";
    private const string Pack = "Assets/ThirdParty/LowPolyNaturePack/";
    private const string DecoFolder = "Assets/Models/Deco/";
    private const string MaterialFolder = "Assets/Data/Materials/";

    private enum Zone { Sea, Beach, Hut, Hill, Forest, Meadow }

    private static System.Random rng;
    private static Terrain terrain;

    [MenuItem("유리병 정원/섬 만들기")]
    public static string Build()
    {
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        rng = new System.Random(20261114);

        // 1) 예전에 만든 것을 지운다 (큐브 바닥, 임시 집, 기둥, 시험용 재료, 이전에 만든 섬)
        foreach (string n in new[] { "Ground", "Hut", "Marker0", "Marker1", "Marker2", "TestMaterials", "Spawn_FromHut", RootName })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }
        GameObject root = new GameObject(RootName);

        // 2) 지형과 바다
        terrain = BuildTerrain(root.transform);
        BuildSea(root.transform);

        // 3) 오두막
        float hutY = GroundY(HutPosition.x, HutPosition.z);
        BuildHouse(root.transform, new Vector3(HutPosition.x, hutY, HutPosition.z));
        BuildHutYard(root.transform, hutY);

        // 4) 도착 지점과 플레이어
        GameObject spawn = new GameObject("Spawn_FromHut");
        spawn.transform.SetPositionAndRotation(new Vector3(0f, hutY + 1f, 9f), Quaternion.Euler(0f, 180f, 0f));
        SpawnPoint sp = spawn.AddComponent<SpawnPoint>();
        SerializedObject sps = new SerializedObject(sp);
        sps.FindProperty("id").stringValue = "FromHut";
        sps.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameRoot gr in Object.FindObjectsByType<GameRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform player = gr.transform.Find("Player");
            if (player != null) player.position = new Vector3(0f, hutY + 1.1f, 2f);
        }

        // 5) 나무, 바위, 풀, 버섯
        int trees = ScatterTrees(root.transform);
        int rocks = ScatterRocks(root.transform);
        int plants = ScatterPlants(root.transform);

        // 6) 재료 줍는 자리
        int pickups = PlacePickups(root.transform);

        // 7) 하늘의 구름과 먼 곳의 안개 (바다 끝이 부드럽게 하늘과 이어지게)
        AddClouds(root.transform);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.87f, 0.92f);
        RenderSettings.fogStartDistance = 260f;
        RenderSettings.fogEndDistance = 1100f;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        return "섬 만들기 완료: 나무 " + trees + "그루, 바위 " + rocks + "개, 풀과 버섯 " + plants + "개, 재료 줍는 자리 " + pickups + "개";
    }

    // ───────── 지형 ─────────

    private static float Smooth(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    /// <summary>세계 좌표 (x, z)에서의 땅 높이(미터). 바다는 0.</summary>
    private static float HeightAt(float x, float z)
    {
        Vector2 p = new Vector2(x, z);
        float r = Vector2.Distance(p, IslandCenter) / IslandRadius;

        // 섬 모양: 가운데가 높고 가장자리로 갈수록 낮아져 바다로 들어간다
        float land = 1f - Smooth(0.45f, 1.0f, r);
        float h = Mathf.Pow(land, 1.35f) * 13f; // 거듭제곱으로 해안 쪽을 완만하게 (작은 섬에서도 모래사장이 넓게)
        if (r > 1f) h = -(r - 1f) * IslandRadius * 0.12f;

        // 완만한 굴곡 (해안선 근처는 줄인다)
        float noise = Mathf.PerlinNoise(x * 0.018f + 100f, z * 0.018f + 100f) * 2f - 1f;
        float fine = Mathf.PerlinNoise(x * 0.07f + 50f, z * 0.07f + 50f) * 2f - 1f;
        h += (noise * 2.8f + fine * 0.6f) * Smooth(0.15f, 0.7f, 1f - r);

        // 꽃 언덕: 섬에서 가장 높은 곳
        float dh = Vector2.Distance(p, FlowerHill);
        h += 20f * Mathf.Exp(-(dh * dh) / (2f * 32f * 32f));

        // 오두막 언덕: 평평하게 고른다
        float dp = Vector2.Distance(p, HutPlateau);
        h = Mathf.Lerp(h, HutHeight, 1f - Smooth(22f, 36f, dp));
        return h;
    }

    private static float DistanceToPaths(float x, float z)
    {
        float best = float.MaxValue;
        Vector2 p = new Vector2(x, z);
        foreach (Vector2[] path in Paths)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 a = path[i], b = path[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
        }
        return best;
    }

    private static Zone ZoneAt(float x, float z, float h)
    {
        if (h < 0.4f) return Zone.Sea;
        Vector2 p = new Vector2(x, z);
        if (Vector2.Distance(p, FlowerHill) < FlowerHillRadius) return Zone.Hill;
        if (h < 3.6f) return Zone.Beach;
        if (Vector2.Distance(p, HutPlateau) < 36f) return Zone.Hut;
        return z > 28f ? Zone.Forest : Zone.Meadow;
    }

    private static Terrain BuildTerrain(Transform parent)
    {
        Directory.CreateDirectory(TerrainFolder);
        TerrainData td = new TerrainData { heightmapResolution = HeightmapResolution, alphamapResolution = AlphamapResolution };
        td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

        // 파일을 먼저 만들고 그 뒤에 높이와 칠을 채운다. (먼저 칠하면 칠한 정보가 파일에 저장되지 않는다)
        string path = TerrainFolder + "/IslandTerrain.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(td, path);

        // 높이
        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int iz = 0; iz < HeightmapResolution; iz++)
        {
            for (int ix = 0; ix < HeightmapResolution; ix++)
            {
                float x = -TerrainSize / 2f + ix * TerrainSize / (HeightmapResolution - 1);
                float z = -TerrainSize / 2f + iz * TerrainSize / (HeightmapResolution - 1);
                heights[iz, ix] = Mathf.Clamp01((HeightAt(x, z) - TerrainBaseY) / TerrainHeight);
            }
        }
        td.SetHeights(0, 0, heights);

        // 칠하는 층: 풀, 모래, 흙
        TerrainLayer grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Pack + "Materials/Grass_Mat.terrainlayer");
        TerrainLayer sand = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Pack + "Materials/Sand_Mat.terrainlayer");
        TerrainLayer soil = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Pack + "Materials/Soil_Mat.terrainlayer");
        td.terrainLayers = new[] { grass, sand, soil };

        float[,,] alpha = new float[AlphamapResolution, AlphamapResolution, 3];
        for (int iz = 0; iz < AlphamapResolution; iz++)
        {
            for (int ix = 0; ix < AlphamapResolution; ix++)
            {
                float x = -TerrainSize / 2f + (ix + 0.5f) * TerrainSize / AlphamapResolution;
                float z = -TerrainSize / 2f + (iz + 0.5f) * TerrainSize / AlphamapResolution;
                float h = HeightAt(x, z);
                float wobble = (Mathf.PerlinNoise(x * 0.05f, z * 0.05f) - 0.5f) * 1.4f;

                float wSand = 1f - Smooth(2.4f, 4.2f, h + wobble);                      // 해안은 모래
                float wPath = (1f - Smooth(1.6f, 3.2f, DistanceToPaths(x, z) + wobble * 0.5f)) * (1f - wSand); // 길은 흙
                float wGrass = Mathf.Max(0f, 1f - wSand - wPath);
                float sum = wGrass + wSand + wPath;
                alpha[iz, ix, 0] = wGrass / sum;
                alpha[iz, ix, 1] = wSand / sum;
                alpha[iz, ix, 2] = wPath / sum;
            }
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

    private static void BuildSea(Transform parent)
    {
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Sea";
        sea.transform.SetParent(parent, false);
        sea.transform.position = new Vector3(IslandCenter.x, 0f, IslandCenter.y);
        sea.transform.localScale = new Vector3(600f, 1f, 600f); // 약 6km
        Object.DestroyImmediate(sea.GetComponent<Collider>());
        sea.GetComponent<Renderer>().sharedMaterial = GetOrCreateSeaMaterial();
    }

    /// <summary>파란 반투명 바다 재질. (에셋 팩의 물 재질은 빛 무늬가 너무 밝아 넓은 바다가 하얗게 나온다)</summary>
    private static Material GetOrCreateSeaMaterial()
    {
        const string path = "Assets/Data/Placeholder/SeaWater.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", new Color(0.32f, 0.68f, 0.82f, 0.82f));
        m.SetFloat("_Surface", 1f);    // Transparent
        m.SetFloat("_Blend", 0f);      // Alpha
        m.SetFloat("_SrcBlend", 5f);
        m.SetFloat("_DstBlend", 10f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Smoothness", 0.92f);
        m.SetFloat("_Metallic", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ───────── 오두막 ─────────

    private const string Medieval = "Assets/ThirdParty/MedievalTownExteriors/Prefabs/";

    /// <summary>
    /// 중세 마을 팩의 집(Building_b)을 놓는다. 현관 앞에 문 모양의 충돌 상자를 붙여,
    /// 바라보고 E를 누르면 집 안으로 들어가게 한다. 문은 남쪽(-z)을 향한다.
    /// </summary>
    private static void BuildHouse(Transform parent, Vector3 pos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Medieval + "Building_b.prefab");
        GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        house.name = "Cabin";
        house.transform.SetParent(parent, false);
        house.transform.SetPositionAndRotation(pos, prefab.transform.rotation); // 프리팹 뿌리의 회전(-90도 등)을 그대로 둔다

        // 현관 앞면(z = -0.3)에 붙은 문 상자
        GameObject door = new GameObject("Door");
        door.transform.SetPositionAndRotation(pos + new Vector3(0f, 1.1f, -0.5f), Quaternion.identity);
        door.transform.SetParent(house.transform, true); // 집이 회전되어 있어서 세계 좌표로 놓고 붙인다
        BoxCollider box = door.AddComponent<BoxCollider>();
        box.size = new Vector3(1.6f, 2.2f, 0.5f);
        SceneDoor sceneDoor = door.AddComponent<SceneDoor>();
        SerializedObject so = new SerializedObject(sceneDoor);
        so.FindProperty("targetScene").stringValue = "HutInterior";
        so.FindProperty("spawnId").stringValue = "FromIsland";
        so.FindProperty("label").stringValue = "집에 들어가기";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>집 마당: 통, 상자, 자루, 우물, 가로등, 수레</summary>
    private static void BuildHutYard(Transform parent, float y)
    {
        GameObject yard = new GameObject("HutYard");
        yard.transform.SetParent(parent, false);
        PlaceKit(yard.transform, "Barrel", new Vector3(3.6f, y, 11.2f), 0f);
        PlaceKit(yard.transform, "Barrel", new Vector3(-3.6f, y, 11.4f), 30f);
        PlaceKit(yard.transform, "Crate", new Vector3(-4.7f, y, 11.6f), 15f);
        PlaceKit(yard.transform, "Bucket", new Vector3(4.4f, y, 11.5f), 0f);
        PlaceKit(yard.transform, "Lantern_post", new Vector3(2.4f, y, 9f), 180f);
        PlaceKit(yard.transform, "Well", new Vector3(9.5f, GroundY(9.5f, 8f), 8f), 0f);
        PlaceKit(yard.transform, "Cart_a", new Vector3(-9f, GroundY(-9f, 10f), 10f), 25f);
    }

    private static GameObject PlaceKit(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Medieval + name + ".prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
        if (go.GetComponentInChildren<Collider>() == null) AddBoxCollider(go);
        return go;
    }

    // ───────── 나무, 바위, 풀 ─────────

    private static float Rand() { return (float)rng.NextDouble(); }
    private static float Range(float a, float b) { return Mathf.Lerp(a, b, Rand()); }

    private static GameObject Spawn(Transform parent, string category, string name, float x, float z, float scale, float yawRange = 360f)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Prefabs/" + category + "/" + name + ".prefab");
        if (prefab == null) return null;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(new Vector3(x, GroundY(x, z), z), Quaternion.Euler(0f, Rand() * yawRange, 0f));
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    /// <summary>한 칸(cell)마다 한 번씩 흔들린 자리를 뽑아, 조건이 맞으면 놓는다.</summary>
    private static void Grid(float cell, System.Action<float, float, float, Zone> visit)
    {
        for (float gx = -IslandRadius - 20f; gx <= IslandRadius + 20f; gx += cell)
        {
            for (float gz = IslandCenter.y - IslandRadius - 20f; gz <= IslandCenter.y + IslandRadius + 20f; gz += cell)
            {
                float x = gx + Range(-0.45f, 0.45f) * cell;
                float z = gz + Range(-0.45f, 0.45f) * cell;
                float h = HeightAt(x, z);
                visit(x, z, h, ZoneAt(x, z, h));
            }
        }
    }

    private static int ScatterTrees(Transform parent)
    {
        GameObject group = new GameObject("Trees");
        group.transform.SetParent(parent, false);
        int count = 0;

        string[] forest = { "Tree01A", "Tree01C", "Tree10B", "Tree10C", "Tree12A", "Tree13A", "Tree3A", "Tree4A", "Tree4A", "Tree5A", "Tree5A", "Tree11B", "Tree6A" };
        string[] meadow = { "Tree01A", "Tree01C", "Tree2A", "Tree2B", "Tree3A", "Tree10B" };
        string[] hill = { "Tree2A", "Tree2B", "Tree2C", "Tree3C" };
        string[] palm = { "Tree7A", "Tree7B", "Tree8A", "Tree8B" };

        Grid(10f, (x, z, h, zone) =>
        {
            float path = DistanceToPaths(x, z);
            if (path < 5f) return;
            string[] pool; float prob;
            switch (zone)
            {
                case Zone.Forest: pool = forest; prob = 0.7f; break;
                case Zone.Meadow: pool = meadow; prob = 0.12f; if (Vector2.Distance(new Vector2(x, z), HutPlateau) < 48f) return; break;
                case Zone.Hill: pool = hill; prob = 0.05f; break;
                case Zone.Beach: pool = palm; prob = (h > 1.4f && h < 3.4f) ? 0.2f : 0f; break;
                default: return;
            }
            if (zone == Zone.Forest && Vector2.Distance(new Vector2(x, z), HutPlateau) < 44f) return; // 오두막 주변은 트이게
            if (Rand() > prob) return;
            string name = pool[rng.Next(pool.Length)];
            float scale = zone == Zone.Beach ? Range(0.9f, 1.2f) : Range(0.85f, 1.35f);
            GameObject go = Spawn(group.transform, "Trees", name, x, z, scale);
            if (go == null) return;
            AddTrunkCollider(go, scale);
            count++;
        });
        return count;
    }

    private static int ScatterRocks(Transform parent)
    {
        GameObject group = new GameObject("Rocks");
        group.transform.SetParent(parent, false);
        int count = 0;

        string[] boulders = { "Boulder01", "Boulder02", "Boulder03", "Boulder04", "Boulder05", "Boulder06", "Boulder07", "Boulder08" };
        string[] rocks = { "Rock01", "Rock02", "Rock03", "Rock04", "Rock05" };

        Grid(13f, (x, z, h, zone) =>
        {
            if (zone == Zone.Sea || zone == Zone.Hut) return;
            if (DistanceToPaths(x, z) < 4f) return;
            float prob = zone == Zone.Beach ? 0.22f : zone == Zone.Hill ? 0.25f : 0.06f;
            if (Rand() > prob) return;
            float scale = Range(1.2f, 2.6f);
            GameObject go = Spawn(group.transform, "Boulders", boulders[rng.Next(boulders.Length)], x, z, scale);
            if (go == null) return;
            AddBoxCollider(go);
            count++;
        });

        Grid(48f, (x, z, h, zone) =>
        {
            if (zone == Zone.Sea || zone == Zone.Hut) return;
            if (DistanceToPaths(x, z) < 8f) return;
            if (Rand() > (zone == Zone.Beach || zone == Zone.Hill ? 0.45f : 0.2f)) return;
            GameObject go = Spawn(group.transform, "Rocks", rocks[rng.Next(rocks.Length)], x, z, Range(0.8f, 1.3f));
            if (go == null) return;
            AddBoxCollider(go);
            count++;
        });

        // 바다에 솟은 절벽 바위 3개 (멀리서 섬 풍경이 되게)
        float[] angles = { 150f, 235f, 320f };
        foreach (float a in angles)
        {
            float x = IslandCenter.x + Mathf.Cos(a * Mathf.Deg2Rad) * (IslandRadius + 30f);
            float z = IslandCenter.y + Mathf.Sin(a * Mathf.Deg2Rad) * (IslandRadius + 30f);
            GameObject cliff = Spawn(group.transform, "Cliffs", "Cliff0" + (1 + (int)(a / 100f) % 4), x, z, 0.45f);
            if (cliff != null) { cliff.transform.position = new Vector3(x, -3f, z); count++; }
        }
        return count;
    }

    private static int ScatterPlants(Transform parent)
    {
        GameObject group = new GameObject("Plants");
        group.transform.SetParent(parent, false);
        int count = 0;

        string[] leafy = { "Plant01A", "Plant01B", "Plant01C", "Plant02A", "Plant02B", "Plant04A", "Plant05A", "Plant05B", "Plant06B", "Plant07B", "Plant09A" };
        string[] tufts = { "Plant12A", "Plant12B", "Plant13A", "Plant13B", "Plant15A", "Plant15B", "Plant16A", "Plant16B" };
        string[] mushrooms = { "Mushroom01A", "Mushroom02A", "Mushroom03A", "Mushroom04A", "Mushroom05A", "Mushroom06A" };

        Grid(6f, (x, z, h, zone) =>
        {
            if (zone == Zone.Sea || zone == Zone.Beach) return;
            if (DistanceToPaths(x, z) < 2.5f) return;
            float d = Vector2.Distance(new Vector2(x, z), HutPlateau);
            if (d < 9f) return; // 오두막 바로 앞은 비운다
            float prob = zone == Zone.Forest ? 0.6f : zone == Zone.Hill ? 0.5f : 0.35f;
            if (Rand() > prob) return;
            string[] pool = Rand() < 0.45f ? leafy : tufts;
            GameObject go = Spawn(parent.Find("Plants"), "Plants", pool[rng.Next(pool.Length)], x, z, Range(0.9f, 1.6f));
            if (go != null) count++;
        });

        Grid(16f, (x, z, h, zone) =>
        {
            if (zone != Zone.Forest) return;
            if (DistanceToPaths(x, z) < 3f || Rand() > 0.3f) return;
            GameObject go = Spawn(group.transform, "Mushrooms", mushrooms[rng.Next(mushrooms.Length)], x, z, Range(1.2f, 2.2f));
            if (go != null) count++;
        });
        return count;
    }

    private static void AddClouds(Transform parent)
    {
        GameObject group = new GameObject("Clouds");
        group.transform.SetParent(parent, false);
        string[] names = { "Cloud01", "Cloud02", "Cloud03", "Cloud04", "Cloud05" };
        for (int i = 0; i < names.Length; i++)
        {
            float a = (i / (float)names.Length) * Mathf.PI * 2f + 0.6f;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Prefabs/Clouds/" + names[i] + ".prefab");
            if (prefab == null) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(group.transform, false);
            go.transform.position = new Vector3(Mathf.Cos(a) * 350f, 170f + i * 12f, IslandCenter.y + Mathf.Sin(a) * 350f);
            go.transform.localScale = Vector3.one * 0.7f;
            go.transform.rotation = Quaternion.Euler(0f, i * 67f, 0f);
        }
    }

    // ───────── 재료 줍는 자리 ─────────

    private static int PlacePickups(Transform parent)
    {
        GameObject group = new GameObject("Pickups");
        group.transform.SetParent(parent, false);
        int count = 0;
        Physics.SyncTransforms(); // 방금 놓은 나무와 바위의 Collider를 반영한다

        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData", new[] { MaterialFolder }))
        {
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(guid));
            // 상점 품목과 희귀 재료는 섬에 놓지 않는다 (희귀 재료는 도끼와 곡괭이로 칠 때 나온다)
            if (data.zone == MaterialZone.Shop || data.zone == MaterialZone.Anywhere || data.isRare) continue;

            Zone target = data.zone == MaterialZone.Forest ? Zone.Forest : data.zone == MaterialZone.Beach ? Zone.Beach : Zone.Hill;
            int want = data.acquire == AcquireMethod.Hand ? 9 : 6; // 도끼, 곡괭이 재료는 11단계 전까지 줍는 임시 자리
            int placed = 0, tries = 0;
            while (placed < want && tries++ < 4000)
            {
                float x, z;
                if (!RandomPointIn(target, out x, out z)) continue;
                if (DistanceToPaths(x, z) < 2f) continue;
                // 나무, 바위와 겹치지 않게. (지형 자체에 걸리지 않도록 땅에서 1.4m 위에서 검사한다)
                if (Physics.CheckSphere(new Vector3(x, GroundY(x, z) + 1.4f, z), 0.9f)) continue;
                MakePickup(group.transform, data, x, z, placed);
                placed++; count++;
            }
        }
        return count;
    }

    /// <summary>구역 안의 무작위 지점을 고른다. 못 찾으면 false.</summary>
    private static bool RandomPointIn(Zone target, out float x, out float z)
    {
        x = z = 0f;
        float ang = Rand() * Mathf.PI * 2f, rad = Mathf.Sqrt(Rand()) * (IslandRadius + 10f);
        x = IslandCenter.x + Mathf.Cos(ang) * rad;
        z = IslandCenter.y + Mathf.Sin(ang) * rad;
        float h = HeightAt(x, z);
        if (target == Zone.Beach && (h < 0.9f || h > 3.0f)) return false;
        if (ZoneAt(x, z, h) != target) return false;
        return true;
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
            model.transform.localScale = Vector3.one * data.pickupScale;
        }
        else
        {
            // 모델이 없는 재료(모래)는 모래 색 더미로 보인다
            model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.transform.SetParent(go.transform, false);
            model.transform.localScale = new Vector3(0.6f, 0.2f, 0.6f);
            model.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().sharedMaterial = BottleSetup.GetOrCreateMaterial("SandTan", data.placeholderColor);
        }
        model.name = "Model";

        // 바닥(모델 맨 아래)이 땅에 닿게 올리고, 바라보기 쉽게 상자 Collider를 붙인다
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        model.transform.position += Vector3.up * (go.transform.position.y - b.min.y);
        b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true; // 걸어 다니는 데 걸리지 않게. 바라보는 광선은 Trigger도 맞는다.
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = b.size;
        box.size = new Vector3(Mathf.Max(size.x, 0.4f), Mathf.Max(size.y, 0.4f), Mathf.Max(size.z, 0.4f));
    }

    // ───────── Collider 도우미 ─────────

    /// <summary>나무에 줄기 Collider(캡슐)가 없으면 붙인다. 잎 사이로는 지나갈 수 있고 줄기에는 막힌다.</summary>
    private static void AddTrunkCollider(GameObject tree, float scale)
    {
        if (tree.GetComponentInChildren<Collider>() != null) return;
        CapsuleCollider cap = tree.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1.8f, 0f);
        cap.radius = 0.5f;
        cap.height = 3.6f;
    }

    private static void AddBoxCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>().Where(r => !r.name.Contains("LOD1") && !r.name.Contains("LOD2") && !r.name.Contains("LOD3")).ToArray();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }
}
