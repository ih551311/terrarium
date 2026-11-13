using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 재료 데이터(기획서 5장의 재료 14종 + 희귀 2종)를 만들고, 각 재료에 모델을 연결한다.
/// 섬의 시험용 재료(TestMaterials)도 같은 모델로 바꾸고, 재료마다 하나씩 시험용으로 놓는다.
/// 메뉴: 유리병 정원 > 재료 데이터 만들기. 여러 번 눌러도 같은 결과가 나온다.
/// 어떤 재료에 어떤 모델을 쓸지는 아래 표(Entries)만 고치면 된다.
/// </summary>
public static class MaterialSetup
{
    private const string DataFolder = "Assets/Data/Materials";
    private const string ModelFolder = "Assets/Models/Deco";
    private const string IslandPath = "Assets/Scenes/Island.unity";

    private struct Entry
    {
        public string id, displayName, model;
        public MaterialKind kind;
        public Color color; // 모델이 없을 때와 미리보기(그림자)에 쓰는 색

        public Entry(string id, string displayName, MaterialKind kind, string model, Color color)
        {
            this.id = id; this.displayName = displayName; this.kind = kind; this.model = model; this.color = color;
        }
    }

    // 기획서 5장의 재료 표 + 희귀 재료 2종. model은 Assets/Models/Deco 안의 파일 이름(확장자 없이).
    private static readonly Entry[] Entries =
    {
        new Entry("moss",             "이끼",       MaterialKind.Cover,      "deco_moss_light",    new Color(0.44f, 0.64f, 0.36f)),
        new Entry("fern",             "고사리",     MaterialKind.Plant,      "deco_fern",          new Color(0.45f, 0.70f, 0.42f)),
        new Entry("twig",             "나뭇가지",   MaterialKind.Decoration, "deco_twig",          new Color(0.70f, 0.55f, 0.35f)),
        new Entry("pinecone",         "솔방울",     MaterialKind.Decoration, "deco_pine_cone",     new Color(0.55f, 0.35f, 0.20f)),
        new Entry("mushroom",         "버섯",       MaterialKind.Decoration, "deco_mushroom_red",  new Color(0.85f, 0.35f, 0.33f)),
        new Entry("seashell",         "조개껍데기", MaterialKind.Decoration, "deco_seashell",      new Color(0.96f, 0.82f, 0.79f)),
        new Entry("pebble",           "조약돌",     MaterialKind.Decoration, "deco_pebble_gray",   new Color(0.62f, 0.62f, 0.65f)),
        new Entry("driftwood",        "유목",       MaterialKind.Decoration, "deco_driftwood",     new Color(0.69f, 0.51f, 0.35f)),
        new Entry("white_flower",     "흰 들꽃",    MaterialKind.Plant,      "deco_flower_white",  new Color(0.96f, 0.95f, 0.92f)),
        new Entry("blue_flower",      "파란 들꽃",  MaterialKind.Plant,      "deco_flower_blue",   new Color(0.49f, 0.66f, 0.89f)),
        new Entry("clover",           "클로버",     MaterialKind.Plant,      "deco_clover",        new Color(0.64f, 0.83f, 0.51f)),
        new Entry("small_rock",       "작은 바위",  MaterialKind.Decoration, "deco_rock_small",    new Color(0.62f, 0.62f, 0.65f)),
        new Entry("evening_primrose", "달맞이꽃",   MaterialKind.Plant,      "deco_flower_yellow", new Color(0.97f, 0.85f, 0.40f)),
        new Entry("amber",            "호박 조각",  MaterialKind.Decoration, "deco_amber",         new Color(0.93f, 0.65f, 0.20f)),
        new Entry("crystal",          "수정 조각",  MaterialKind.Decoration, "deco_crystal",       new Color(0.75f, 0.67f, 0.95f)),
        // 모래는 바닥재라서 병 안에 놓는 모델이 없다 (BottleSetup이 따로 만든다)
    };

    [MenuItem("유리병 정원/재료 데이터 만들기")]
    public static string Build()
    {
        // 1) 재료 데이터
        int created = 0, missingModel = 0;
        foreach (Entry e in Entries)
        {
            string path = DataFolder + "/" + e.id + ".asset";
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MaterialData>();
                AssetDatabase.CreateAsset(data, path);
                created++;
            }

            data.id = e.id;
            data.displayName = e.displayName;
            data.kind = e.kind;
            data.placeholderColor = e.color;
            data.placedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + e.model + ".obj");
            if (data.placedPrefab == null) missingModel++;
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();

        // 2) 섬의 시험용 재료
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        SetupIslandPickups();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        return "재료 데이터 " + Entries.Length + "종 (새로 " + created + "개), 모델 못 찾음 " + missingModel + "개";
    }

    private static void SetupIslandPickups()
    {
        GameObject root = GameObject.Find("TestMaterials");
        if (root == null) return;

        // 이미 놓여 있던 시험용 재료(솔방울, 나뭇가지)의 모습을 모델로 바꾼다.
        foreach (Pickup pickup in root.GetComponentsInChildren<Pickup>(true))
        {
            MaterialData data = pickup.Material;
            if (data == null || data.placedPrefab == null) continue;
            BuildPickupVisual(pickup.gameObject, data);
            Vector3 pos = pickup.transform.position;
            pickup.transform.position = new Vector3(pos.x, 0f, pos.z); // 모델은 바닥이 맨 아래에 있다
        }

        // 재료마다 하나씩, 시험용으로 일렬로 놓는다. (섬을 에셋으로 꾸미는 7단계 전까지 임시)
        Transform showcase = root.transform.Find("Showcase");
        if (showcase != null) Object.DestroyImmediate(showcase.gameObject);
        GameObject row = new GameObject("Showcase");
        row.transform.SetParent(root.transform);

        float x = -6.6f;
        foreach (Entry e in Entries)
        {
            if (e.id == "pinecone" || e.id == "twig") continue; // 이미 따로 놓여 있다
            MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(DataFolder + "/" + e.id + ".asset");
            GameObject go = new GameObject("Pickup_" + e.id);
            go.transform.SetParent(row.transform);
            go.transform.position = new Vector3(x, 0f, 2.2f);
            Pickup pickup = go.AddComponent<Pickup>();
            UISetup.SetRef(pickup, "material", data);
            BuildPickupVisual(go, data);
            x += 1.1f;
        }
    }

    /// <summary>
    /// 주울 수 있는 재료의 모습을 모델로 만든다. 기본 도형이 있으면 지우고,
    /// 모델을 키워 자식으로 넣고, 바라보기 쉽게 모델을 감싸는 상자(Collider)를 붙인다.
    /// </summary>
    private static void BuildPickupVisual(GameObject root, MaterialData data)
    {
        // 예전 기본 도형(Cube/Sphere/Capsule)과 이전에 만든 모델 제거
        foreach (Component c in root.GetComponents<Component>())
            if (c is MeshRenderer || c is MeshFilter || c is Collider) Object.DestroyImmediate(c);
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            if (root.transform.GetChild(i).name == "Model") Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(data.placedPrefab);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localScale = Vector3.one * data.pickupScale;
        model.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);

        // 바닥(모델의 맨 아래)이 오브젝트 위치에 오도록 맞춘다
        Bounds b = WorldBounds(model);
        model.transform.position += Vector3.up * (root.transform.position.y - b.min.y);

        // 얇은 재료(나뭇가지 등)도 바라보기 쉽게 상자를 조금 키운다
        b = WorldBounds(model);
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(b.center);
        Vector3 size = b.size;
        box.size = new Vector3(Mathf.Max(size.x, 0.25f), Mathf.Max(size.y, 0.25f), Mathf.Max(size.z, 0.25f));
    }

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }
}
