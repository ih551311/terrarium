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

    // 길은 없다. (예전에는 바닷가, 작은 숲, 꽃 언덕으로 가는 흙길이 있었지만 지웠다. 비어 있으면 길 때문에 비워 두는 자리도 없다)
    private static readonly Vector2[][] Paths = new Vector2[0][];

    private const string RootName = "IslandContent";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string TerrainFolder = "Assets/Terrain";
    private const string Pack = "Assets/ThirdParty/LowPolyNaturePack/";
    private const string TreePack = "Assets/Low Poly Tree Mega Pack by MysticForge/Prefabs/URP/";
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
        ToolSetup.EnsureHiddenLeafMaterial(); // 캔 나무의 잎을 숨길 때 쓰는 투명 재질

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
        // (구름은 만들지 않는다. 하늘은 낮과 밤 하늘 그림이 맡는다 - SkySetup)
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

    /// <summary>
    /// 지도(MapBake)용 색. 세계 좌표 (x, z)가 바다인지, 모래사장, 풀밭, 숲, 꽃 언덕인지에 따라 칠하고
    /// 높이에 따라 살짝 밝고 어둡게 한다. 길은 그리지 않는다. 지형과 같은 계산을 쓰므로 섬을 다시 만들지 않아도 된다.
    /// </summary>
    public static Color MapColorAt(float x, float z)
    {
        float h = HeightAt(x, z);
        if (h < 0.4f)
        {
            float depth = Mathf.Clamp01(-h / 6f); // 먼 바다일수록 짙게
            return Color.Lerp(new Color(0.55f, 0.82f, 0.90f), new Color(0.30f, 0.56f, 0.76f), depth);
        }

        // 구역 경계가 칼로 자른 듯하지 않게, 구역마다 가중치를 주어 부드럽게 섞는다
        // 경계가 자로 잰 듯하지 않게 노이즈로 구불구불하게 흔든다
        float wob = (Mathf.PerlinNoise(x * 0.035f + 40f, z * 0.035f + 40f) - 0.5f) * 26f;
        float wob2 = (Mathf.PerlinNoise(x * 0.06f + 80f, z * 0.06f + 80f) - 0.5f) * 8f;
        Vector2 p = new Vector2(x, z);
        float forestW = Smooth(22f, 34f, z + wob + wob2) * Smooth(34f, 44f, Vector2.Distance(p, HutPlateau) + wob * 0.5f); // 북쪽은 숲, 오두막 언덕은 초원
        float hillW = 1f - Smooth(FlowerHillRadius - 3f, FlowerHillRadius + 3f, Vector2.Distance(p, FlowerHill) + wob * 0.35f);
        float beachW = (h < 3.6f ? 1f : 0f) * (1f - hillW); // 모래사장 경계는 땅이 완만해서 높이로 섞으면 번지므로 또렷하게

        Color c = Color.Lerp(new Color(0.60f, 0.80f, 0.42f), new Color(0.36f, 0.62f, 0.34f), forestW);
        c = Color.Lerp(c, new Color(0.82f, 0.85f, 0.52f), hillW);
        c = Shade(c, h);

        // 물가는 하얗게, 나머지 모래사장은 모래색
        Color sand = Color.Lerp(new Color(0.97f, 0.90f, 0.66f), new Color(1f, 0.98f, 0.9f), 1f - Smooth(0.4f, 0.9f, h));
        c = Color.Lerp(c, sand, beachW);

        // 아주 은은한 얼룩 (단색이라 밋밋해 보이지 않게)
        float n = Mathf.PerlinNoise(x * 0.05f + 300f, z * 0.05f + 300f) * 0.08f - 0.04f;
        return new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), 1f);
    }

    /// <summary>꽃 언덕의 가운데 (지도에 꽃을 흩뿌릴 때 쓴다)</summary>
    public static Vector2 MapFlowerHill => FlowerHill;

    /// <summary>지도용 땅 높이 (지도에 음영을 줄 때 쓴다)</summary>
    public static float MapHeightAt(float x, float z)
    {
        return HeightAt(x, z);
    }

    private static Color Shade(Color c, float h)
    {
        float k = 0.9f + 0.2f * Mathf.Clamp01(h / 22f); // 높은 곳이 조금 더 밝다
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
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
        BuildMailbox(yard.transform, new Vector3(-2.2f, y, 11.6f));
    }

    /// <summary>
    /// 문 옆 우체통(상점): 나무 기둥 위의 붉은 우편함과 올라간 깃발. 바라보고 E를 누르면 우체통 패널이 열린다.
    /// 큐브로 만든 임시 모양이다 (나중에 모델로 바꿀 수 있다).
    /// </summary>
    private static void BuildMailbox(Transform parent, Vector3 pos)
    {
        GameObject box = new GameObject("Mailbox");
        box.transform.SetParent(parent, false);
        box.transform.SetPositionAndRotation(pos, Quaternion.identity);
        box.AddComponent<Mailbox>();

        Material wood = BottleSetup.GetOrCreateMaterial("MailboxWood", new Color(0.33f, 0.2f, 0.12f));
        Material red = BottleSetup.GetOrCreateMaterial("MailboxRed", new Color(0.72f, 0.2f, 0.18f));
        Material gold = BottleSetup.GetOrCreateMaterial("MailboxFlag", new Color(0.97f, 0.8f, 0.3f));

        MailboxPart(box.transform, "Post", new Vector3(0f, 0.55f, 0f), new Vector3(0.14f, 1.1f, 0.14f), wood);
        MailboxPart(box.transform, "Body", new Vector3(0f, 1.25f, 0f), new Vector3(0.5f, 0.36f, 0.72f), red);
        MailboxPart(box.transform, "Roof", new Vector3(0f, 1.47f, 0f), new Vector3(0.56f, 0.07f, 0.78f), wood);
        MailboxPart(box.transform, "FlagPole", new Vector3(0.3f, 1.4f, 0.12f), new Vector3(0.04f, 0.3f, 0.04f), wood);
        MailboxPart(box.transform, "Flag", new Vector3(0.34f, 1.52f, 0.12f), new Vector3(0.13f, 0.1f, 0.03f), gold);
    }

    private static void MailboxPart(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
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
        // 나무는 Low Poly Tree Mega Pack의 것을 쓴다 (이름: Palm n, Pine n, BasicTree n). 그 밖의 것은 자연 팩.
        string path = Pack + "Prefabs/" + category + "/" + name + ".prefab";
        if (name.StartsWith("Palm ")) path = TreePack + "Palm Trees/" + name + ".prefab";
        else if (name.StartsWith("Pine ")) path = TreePack + "Pine Trees/" + name + ".prefab";
        else if (name.StartsWith("BasicTree ")) path = TreePack + "Woodland/" + name + ".prefab";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
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

        // Low Poly Tree Mega Pack에서 모양이 좋은 것만 골랐다 (번호는 팩의 프리팹 번호)
        string[] pines = { "Pine 1", "Pine 10", "Pine 20", "Pine 30", "Pine 40", "Pine 50", "Pine 60", "Pine 70", "Pine 90", "Pine 110", "Pine 170" };
        string[] woods = { "BasicTree 40", "BasicTree 50", "BasicTree 60", "BasicTree 70", "BasicTree 90", "BasicTree 150", "BasicTree 210", "BasicTree 270", "BasicTree 330", "BasicTree 450" };
        string[] hillWoods = { "BasicTree 40", "BasicTree 50", "BasicTree 60", "BasicTree 70", "BasicTree 90", "BasicTree 270" };
        string[] palms = { "Palm 20", "Palm 30", "Palm 50", "Palm 60", "Palm 90", "Palm 110", "Palm 130", "Palm 170" };

        // 나무를 자연스럽게 놓는 규칙
        // 1) 숲은 덩어리(grove)로 모여 있고 그 사이에 빈터가 있다. (느린 노이즈로 덩어리를 정한다)
        // 2) 숲 덩어리마다 소나무 숲 또는 활엽수 숲으로 나뉜다. (다른 노이즈) 섞이는 나무는 조금만.
        // 3) 나무끼리 겹치지 않는다. (잎 반지름 기준으로 간격을 잰다)
        // 4) 숲 가장자리의 나무는 조금 작다.
        // 5) 야자수는 해변에 몇 그루씩 모여 선다.
        const float PineRadius = 2.6f, WoodRadius = 3.2f, PalmRadius = 2.8f;
        var placed = new List<KeyValuePair<Vector2, float>>(); // (자리, 잎 반지름)
        System.Func<Vector2, float, bool> isFree = (p, r) =>
        {
            foreach (KeyValuePair<Vector2, float> q in placed)
                if (Vector2.Distance(p, q.Key) < (r + q.Value) * 0.85f) return false;
            return true;
        };

        Grid(4.5f, (x, z, h, zone) =>
        {
            Vector2 pos = new Vector2(x, z);
            float grove = Mathf.PerlinNoise(x * 0.030f + 500f, z * 0.030f + 500f); // 숲 덩어리
            float kind = Mathf.PerlinNoise(x * 0.022f + 900f, z * 0.022f + 900f);  // 소나무 숲인가 활엽수 숲인가
            string name; float density;

            switch (zone)
            {
                case Zone.Forest:
                    if (Vector2.Distance(pos, HutPlateau) < 44f) return; // 오두막 주변은 트이게
                    density = Smooth(0.32f, 0.52f, grove) * 1.0f;
                    bool pineGrove = kind > 0.5f;
                    name = Rand() < (pineGrove ? 0.9f : 0.1f) ? pines[rng.Next(pines.Length)] : woods[rng.Next(woods.Length)];
                    break;
                case Zone.Meadow:
                    if (Vector2.Distance(pos, HutPlateau) < 48f) return;
                    density = Smooth(0.56f, 0.74f, grove) * 0.5f;
                    name = woods[rng.Next(woods.Length)];
                    break;
                case Zone.Hill:
                    density = Smooth(0.62f, 0.80f, grove) * 0.3f;
                    name = hillWoods[rng.Next(hillWoods.Length)];
                    break;
                case Zone.Beach:
                    if (h < 1.4f || h > 3.4f) return;
                    density = Smooth(0.5f, 0.7f, Mathf.PerlinNoise(x * 0.05f + 300f, z * 0.05f + 300f)) * 0.55f; // 몇 그루씩 모여서
                    name = palms[rng.Next(palms.Length)];
                    break;
                default: return;
            }
            if (Rand() > density) return;

            bool isPine = name.StartsWith("Pine ");
            bool isPalm = name.StartsWith("Palm ");
            float radius = isPine ? PineRadius : isPalm ? PalmRadius : WoodRadius;
            // 소나무는 원래 키가 커서(7~12m) 조금 줄이고, 활엽수와 야자는 비슷하게 키운다. 숲 가장자리는 조금 작게.
            float edge = Mathf.Lerp(0.8f, 1.05f, Smooth(0.4f, 0.7f, grove));
            float scale = (isPine ? Range(0.8f, 1.1f) : isPalm ? Range(0.9f, 1.2f) : Range(0.9f, 1.3f)) * edge;
            if (!isFree(pos, radius * scale)) return;

            GameObject go = Spawn(group.transform, "Trees", name, x, z, scale);
            if (go == null) return;
            AddTrunkCollider(go, scale);
            AddHarvestable(go, AcquireMethod.Axe, zone);
            placed.Add(new KeyValuePair<Vector2, float>(pos, radius * scale));
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
            AddHarvestable(go, AcquireMethod.Pickaxe, zone);
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
            AddHarvestable(go, AcquireMethod.Pickaxe, zone);
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
            // 도끼와 곡괭이로 얻는 재료는 바닥에 놓지 않고, 나무와 돌(Harvestable)에서 나온다
            if (data.acquire == AcquireMethod.Axe || data.acquire == AcquireMethod.Pickaxe) continue;

            Zone target = data.zone == MaterialZone.Forest ? Zone.Forest : data.zone == MaterialZone.Beach ? Zone.Beach : Zone.Hill;
            int want = 9;
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

    // ───────── 캘 수 있는 나무와 돌 ─────────

    /// <summary>나무나 돌을 도끼나 곡괭이로 캘 수 있게 한다. 구역에 맞는 재료가 먼저 나온다.</summary>
    private static void AddHarvestable(GameObject go, AcquireMethod tool, Zone zone)
    {
        Harvestable h = go.AddComponent<Harvestable>();
        SerializedObject so = new SerializedObject(h);
        so.FindProperty("tool").enumValueIndex = (int)tool;
        MaterialZone mz = zone == Zone.Forest ? MaterialZone.Forest
                        : zone == Zone.Beach ? MaterialZone.Beach
                        : zone == Zone.Hill ? MaterialZone.FlowerHill : MaterialZone.Anywhere;
        so.FindProperty("zone").enumValueIndex = (int)mz;
        so.ApplyModifiedPropertiesWithoutUndo();
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
