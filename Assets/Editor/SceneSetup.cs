using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 한 번만 실행하는 설정: 한 씬(SampleScene)을 섬(Island)과 집 안(HutInterior) 두 씬으로 나눈다.
/// 플레이어, 가방, UI는 GameRoot 프리팹으로 묶어 두 씬에 하나씩 넣는다.
/// 이미 만들어져 있으면 아무것도 하지 않는다.
/// </summary>
public static class SceneSetup
{
    private const string OldPath = "Assets/Scenes/SampleScene.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string InteriorPath = "Assets/Scenes/HutInterior.unity";
    private const string PrefabPath = "Assets/Prefabs/GameRoot.prefab";
    private const string RenderTexturePath = "Assets/Data/Placeholder/BottleView.renderTexture";

    [MenuItem("유리병 정원/씬 나누기 (한 번만)")]
    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(InteriorPath) != null)
            return "이미 만들어져 있어요: " + InteriorPath;

        // 1) 현재 씬 이름을 Island로 바꾼다
        Scene island = EditorSceneManager.GetActiveScene();
        if (island.path == OldPath)
        {
            EditorSceneManager.SaveScene(island);
            string error = AssetDatabase.MoveAsset(OldPath, IslandPath);
            if (!string.IsNullOrEmpty(error)) return "씬 이름 바꾸기 실패: " + error;
            island = EditorSceneManager.GetActiveScene();
        }
        else if (island.path != IslandPath)
        {
            return "예상하지 못한 씬이 열려 있어요: " + island.path;
        }

        // 2) UI를 새 구조로 다시 만들고, 플레이어·UI·EventSystem을 GameRoot로 묶어 프리팹으로 만든다
        string uiResult = UISetup.Build();
        GameObject player = GameObject.Find("Player");
        GameObject ui = GameObject.Find("UI");
        GameObject eventSystem = GameObject.Find("EventSystem");

        GameObject rootObject = new GameObject("GameRoot");
        player.transform.SetParent(rootObject.transform, true);
        ui.transform.SetParent(rootObject.transform, true);
        eventSystem.transform.SetParent(rootObject.transform, true);
        GameRoot gameRoot = rootObject.AddComponent<GameRoot>();
        UISetup.SetRef(gameRoot, "player", player.transform);
        UISetup.SetRef(gameRoot, "bottlePanel", ui.GetComponent<BottlePanel>());

        Directory.CreateDirectory("Assets/Prefabs");
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootObject, PrefabPath, InteractionMode.AutomatedAction);

        // 3) 섬에 집 건물, 문, 도착 지점을 만든다
        Material wall = GetOrCreateMaterial("WallCream", new Color(0.93f, 0.89f, 0.8f));
        Material roof = GetOrCreateMaterial("RoofDark", new Color(0.4f, 0.28f, 0.25f));
        Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Placeholder/WoodTable.mat");

        GameObject hut = new GameObject("Hut");
        hut.transform.position = new Vector3(0f, 0f, 14f);
        Cube(hut.transform, "Body", new Vector3(0f, 1.5f, 0f), new Vector3(6f, 3f, 5f), wall);
        Cube(hut.transform, "Roof", new Vector3(0f, 3.2f, 0f), new Vector3(6.6f, 0.4f, 5.6f), roof);
        GameObject outerDoor = Cube(hut.transform, "Door", new Vector3(0f, 1.1f, -2.55f), new Vector3(1.2f, 2.2f, 0.15f), wood);
        AddDoor(outerDoor, "HutInterior", "FromIsland", "집에 들어가기");
        AddSpawn("FromHut", new Vector3(0f, 1f, 10.2f), 180f);

        // 4) 집 안 씬을 만든다
        SceneManager.SetActiveScene(island);
        Scene interior = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // 작업대(병 포함)를 집 안으로 옮긴다
        GameObject bench = GameObject.Find("Workbench");
        EditorSceneManager.MoveGameObjectToScene(bench, interior);
        bench.transform.position = new Vector3(0f, 0f, 2f);
        SetupBottle(bench);

        // 빛과 Volume을 복사해 온다
        CopyTo(GameObject.Find("Directional Light"), interior);
        CopyTo(GameObject.Find("Global Volume"), interior);

        // 바닥, 벽, 문, 도착 지점
        SceneManager.SetActiveScene(interior);
        Cube(null, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f), wood);
        Cube(null, "WallNorth", new Vector3(0f, 1.5f, 4f), new Vector3(8.2f, 3f, 0.2f), wall);
        Cube(null, "WallSouth", new Vector3(0f, 1.5f, -4f), new Vector3(8.2f, 3f, 0.2f), wall);
        Cube(null, "WallEast", new Vector3(4f, 1.5f, 0f), new Vector3(0.2f, 3f, 8.2f), wall);
        Cube(null, "WallWest", new Vector3(-4f, 1.5f, 0f), new Vector3(0.2f, 3f, 8.2f), wall);
        GameObject innerDoor = Cube(null, "Door", new Vector3(0f, 1.1f, -3.85f), new Vector3(1.2f, 2.2f, 0.15f), wood);
        AddDoor(innerDoor, "Island", "FromHut", "밖으로 나가기");
        AddSpawn("FromIsland", new Vector3(0f, 1f, -2.6f), 0f);

        // 집 안 씬에도 GameRoot를 하나 넣는다(여기서 바로 Play해도 시작되도록. 중복이면 스스로 사라진다)
        GameObject interiorRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, interior);
        interiorRoot.transform.Find("Player").position = new Vector3(0f, 1.1f, -2.6f);

        // 5) 저장하고 Build Settings에 두 씬을 넣는다
        EditorSceneManager.SaveScene(interior, InteriorPath);
        EditorSceneManager.CloseScene(interior, true);
        SceneManager.SetActiveScene(island);
        EditorSceneManager.SaveScene(island);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(IslandPath, true),
            new EditorBuildSettingsScene(InteriorPath, true)
        };
        AssetDatabase.SaveAssets();
        return "씬 나누기 완료 (" + uiResult + ")";
    }

    // ───────── 도우미 ─────────

    private static void SetupBottle(GameObject bench)
    {
        int layer = UISetup.EnsureLayer(UISetup.BottleLayerName);
        Transform bottle = bench.transform.Find("Bottle");
        SetLayerRecursive(bottle.gameObject, layer);

        CapsuleCollider col = bottle.GetComponent<CapsuleCollider>();
        if (col == null) col = bottle.gameObject.AddComponent<CapsuleCollider>();
        col.isTrigger = true; col.center = new Vector3(0f, 0.3f, 0f); col.radius = 0.22f; col.height = 0.62f;

        if (bottle.GetComponent<BottleViewer>() == null) bottle.gameObject.AddComponent<BottleViewer>();
        BottleObject bottleObject = bottle.GetComponent<BottleObject>();
        if (bottleObject == null) bottleObject = bottle.gameObject.AddComponent<BottleObject>();

        // 병 카메라: 병 레이어만, 단색 배경, RenderTexture로 출력. 평소엔 꺼 둔다.
        Camera cam = bench.transform.Find("DecorationCamera").GetComponent<Camera>();
        cam.cullingMask = 1 << layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.92f, 0.93f, 1f);
        cam.targetTexture = GetOrCreateRenderTexture();
        cam.enabled = false;
        UISetup.SetRef(bottleObject, "bottleCamera", cam);
    }

    private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void AddDoor(GameObject go, string scene, string spawnId, string label)
    {
        SceneDoor door = go.AddComponent<SceneDoor>();
        SerializedObject so = new SerializedObject(door);
        so.FindProperty("targetScene").stringValue = scene;
        so.FindProperty("spawnId").stringValue = spawnId;
        so.FindProperty("label").stringValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSpawn(string id, Vector3 pos, float yaw)
    {
        GameObject go = new GameObject("Spawn_" + id);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        SpawnPoint sp = go.AddComponent<SpawnPoint>();
        SerializedObject so = new SerializedObject(sp);
        so.FindProperty("id").stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyTo(GameObject source, Scene scene)
    {
        GameObject copy = Object.Instantiate(source);
        copy.name = source.name;
        copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        EditorSceneManager.MoveGameObjectToScene(copy, scene);
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = "Assets/Data/Placeholder/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static RenderTexture GetOrCreateRenderTexture()
    {
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        if (rt != null) return rt;
        rt = new RenderTexture(768, 768, 24) { name = "BottleView" };
        AssetDatabase.CreateAsset(rt, RenderTexturePath);
        return rt;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }
}
