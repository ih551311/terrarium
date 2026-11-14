using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 선착장을 만든다. 긴 나무 부두와 작은 배. 두 곳에 놓는다:
///  - 원래 섬(Island)의 남쪽 바닷가: 처음에는 부서져 있고 코인을 내고 고쳐야 배를 탈 수 있다. (Dock.needsPurchase)
///  - 눈 섬(SnowIsland)의 동쪽 바닷가: 이미 열려 있어서 언제든 배를 타고 돌아온다.
/// 메뉴: 유리병 정원 > 선착장 만들기 (원래 섬의 선착장만 다시 만든다. 눈 섬의 것은 눈 섬 만들기가 만든다)
/// </summary>
public static class DockSetup
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const float DeckY = 0.45f;      // 부두 윗면의 높이 (바다는 0). 모래 끝(높이 0.3m)에서 한 걸음으로 올라설 수 있게 낮다
    private static float PierLength = 16f;     // 부두 길이: 물가에서 물속으로 충분히 나가도록 만들 때마다 정한다
    private const float PierWidth = 2.4f;

    private static Material wood, darkWood, rope, brokenWood;

    private static void Materials()
    {
        wood = BottleSetup.GetOrCreateMaterial("DockWood", new Color(0.62f, 0.45f, 0.29f));
        darkWood = BottleSetup.GetOrCreateMaterial("DockDarkWood", new Color(0.38f, 0.26f, 0.17f));
        rope = BottleSetup.GetOrCreateMaterial("DockRope", new Color(0.78f, 0.68f, 0.48f));
        brokenWood = BottleSetup.GetOrCreateMaterial("DockBroken", new Color(0.46f, 0.36f, 0.27f));
    }

    private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static BoxCollider Box(Transform parent, string name, Vector3 center, Vector3 size, bool trigger)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = trigger;
        return box;
    }

    /// <summary>
    /// 선착장 하나를 만든다. landPoint는 땅 쪽 끝(세계 좌표의 x, z), seaDir은 바다 쪽으로 향하는 수평 방향이다.
    /// 만든 Dock 오브젝트의 뿌리를 돌려주고, 땅 쪽 끝에서 한 걸음 안쪽 자리를 landingSpot으로 알려 준다.
    /// </summary>
    public static GameObject Build(Transform parent, string name, Vector2 landPoint, Vector3 seaDir, bool needsPurchase,
                                   string targetScene, string spawnId, string travelLabel, out Vector3 landingSpot, float groundYAtLand, float length = 16f)
    {
        Materials();
        PierLength = length;
        seaDir.y = 0f; seaDir.Normalize();
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(new Vector3(landPoint.x, 0f, landPoint.y), Quaternion.LookRotation(seaDir));

        GameObject open = new GameObject("Open");
        open.transform.SetParent(root.transform, false);
        BuildOpenPier(open.transform);

        GameObject closed = new GameObject("Closed");
        closed.transform.SetParent(root.transform, false);
        if (needsPurchase) BuildBrokenPier(closed.transform);

        Dock dock = root.AddComponent<Dock>();
        SerializedObject so = new SerializedObject(dock);
        so.FindProperty("targetScene").stringValue = targetScene;
        so.FindProperty("spawnId").stringValue = spawnId;
        so.FindProperty("travelLabel").stringValue = travelLabel;
        so.FindProperty("needsPurchase").boolValue = needsPurchase;
        so.FindProperty("cost").intValue = 150;
        so.FindProperty("closedGroup").objectReferenceValue = needsPurchase ? closed : null;
        so.FindProperty("openGroup").objectReferenceValue = open;
        so.ApplyModifiedPropertiesWithoutUndo();

        open.SetActive(!needsPurchase);
        closed.SetActive(needsPurchase);

        landingSpot = root.transform.TransformPoint(new Vector3(0f, 0f, -2.2f));
        landingSpot.y = groundYAtLand + 2.2f; // 조금 위에서 내려서게 (땅은 땅 쪽으로 갈수록 높아진다)
        return root;
    }

    // ───────── 고친 선착장: 긴 부두와 배 ─────────

    private static void BuildOpenPier(Transform t)
    {
        // 윗면 판자: 가로로 놓인 널 (사이사이 틈)
        int planks = Mathf.RoundToInt(PierLength / 0.5f);
        for (int i = 0; i < planks; i++)
            Cube(t, "Plank", new Vector3(0f, DeckY, 0.25f + i * 0.5f), new Vector3(PierWidth, 0.1f, 0.44f), i % 3 == 0 ? wood : (i % 3 == 1 ? darkWood : wood));
        // 걸어 다닐 바닥 (보이지 않는 상자 하나)
        Box(t, "Walk", new Vector3(0f, DeckY - 0.05f, PierLength / 2f), new Vector3(PierWidth, 0.2f, PierLength), false);

        // 받침 기둥 (물속으로) 과 가로 보
        for (int i = 0; 1f + i * 3f < PierLength; i++)
        {
            float z = 1f + i * 3f;
            foreach (float x in new[] { -PierWidth / 2f + 0.1f, PierWidth / 2f - 0.1f })
                Cube(t, "Post", new Vector3(x, (DeckY + 0.3f - 3.0f) / 2f, z), new Vector3(0.24f, DeckY + 0.3f + 3.0f, 0.24f), darkWood);
            Cube(t, "Beam", new Vector3(0f, DeckY - 0.14f, z), new Vector3(PierWidth, 0.12f, 0.16f), darkWood);
        }

        // 난간: 가장자리를 따라 낮은 줄 (바다 쪽 끝은 열어 둔다)
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * (PierWidth / 2f - 0.08f);
            Cube(t, "Rail", new Vector3(x, DeckY + 0.62f, PierLength / 2f - 0.5f), new Vector3(0.07f, 0.07f, PierLength - 1.5f), wood);
            for (int i = 0; 1.2f + i * 3f < PierLength - 1f; i++)
                Cube(t, "RailPost", new Vector3(x, DeckY + 0.33f, 1.2f + i * 3f), new Vector3(0.11f, 0.62f, 0.11f), darkWood);
        }
        // 끝의 계류 기둥 두 개
        foreach (float x in new[] { -0.8f, 0.8f })
            Cube(t, "Bollard", new Vector3(x, DeckY + 0.3f, PierLength - 0.4f), new Vector3(0.2f, 0.6f, 0.2f), darkWood);

        // 작은 배 (부두 오른쪽 옆에 밧줄로 묶여 있다)
        GameObject boat = new GameObject("Boat");
        boat.transform.SetParent(t, false);
        boat.transform.localPosition = new Vector3(2.7f, 0.06f, PierLength - 4.2f);
        boat.transform.localRotation = Quaternion.Euler(0f, 6f, 0f);
        BuildBoat(boat.transform);

        // 바라보는 곳: 배 근처와 부두 끝 (E를 누를 수 있다)
        Box(t, "BoatHit", new Vector3(2.0f, 1.0f, PierLength - 4f), new Vector3(4.2f, 2.2f, 4.8f), true);
    }

    private static void BuildBoat(Transform t)
    {
        Cube(t, "Hull", new Vector3(0f, 0.15f, 0f), new Vector3(1.5f, 0.16f, 3.2f), wood);
        foreach (float s in new[] { -1f, 1f })
        {
            Cube(t, "Side", new Vector3(s * 0.78f, 0.42f, -0.1f), new Vector3(0.12f, 0.5f, 3.2f), darkWood, new Vector3(0f, 0f, -s * 10f));
            Cube(t, "Bow", new Vector3(s * 0.40f, 0.42f, 1.78f), new Vector3(0.95f, 0.5f, 0.12f), darkWood, new Vector3(0f, s * 28f, -s * 0f));
        }
        Cube(t, "Stern", new Vector3(0f, 0.42f, -1.7f), new Vector3(1.6f, 0.5f, 0.12f), darkWood);
        Cube(t, "Bench", new Vector3(0f, 0.5f, 0.2f), new Vector3(1.4f, 0.09f, 0.4f), wood);
        Cube(t, "Oar", new Vector3(0.35f, 0.62f, -0.5f), new Vector3(0.07f, 0.07f, 1.8f), rope, new Vector3(0f, 12f, 8f));
        Cube(t, "Rope", new Vector3(-1.35f, 0.6f, 1.2f), new Vector3(0.05f, 0.05f, 1.6f), rope, new Vector3(0f, 52f, 0f));
    }

    // ───────── 부서진 선착장: 짧은 부두 조각과 안내 팻말 ─────────

    private static void BuildBrokenPier(Transform t)
    {
        // 땅에서 3m까지만 판자가 남아 있고, 그 너머는 기둥 몇 개만 서 있다
        for (int i = 0; i < 6; i++)
            Cube(t, "Plank", new Vector3(0f, DeckY, 0.25f + i * 0.5f), new Vector3(PierWidth, 0.1f, 0.44f), i % 2 == 0 ? brokenWood : wood);
        Box(t, "Walk", new Vector3(0f, DeckY - 0.05f, 1.5f), new Vector3(PierWidth, 0.2f, 3f), false);
        // 부러져 기울어진 판자
        Cube(t, "BrokenPlank", new Vector3(-0.5f, DeckY - 0.25f, 3.45f), new Vector3(1.1f, 0.1f, 0.44f), brokenWood, new Vector3(0f, 0f, 38f));
        Cube(t, "BrokenPlank", new Vector3(0.6f, DeckY - 0.10f, 3.75f), new Vector3(1.4f, 0.1f, 0.44f), brokenWood, new Vector3(18f, 0f, -12f));
        // 남은 기둥들 (기울어져 있다)
        float[] zs = { 3.8f, 6.8f, 9.8f };
        for (int i = 0; i < zs.Length; i++)
            foreach (float x in new[] { -PierWidth / 2f + 0.1f, PierWidth / 2f - 0.1f })
                Cube(t, "Stump", new Vector3(x, (-0.4f + 0.5f - i * 0.25f) / 2f, zs[i]), new Vector3(0.24f, 0.9f - i * 0.25f, 0.24f), brokenWood, new Vector3((i % 2 == 0 ? 5f : -4f), 0f, (x > 0 ? 6f : -5f)));
        // 팻말: "수리 필요" (글자 대신 못과 X 표시가 된 판자)
        Cube(t, "SignPost", new Vector3(-1.9f, 0.9f, 0.3f), new Vector3(0.13f, 1.7f, 0.13f), darkWood);
        Cube(t, "SignBoard", new Vector3(-1.9f, 1.55f, 0.3f), new Vector3(0.12f, 0.6f, 1.0f), wood, new Vector3(0f, 0f, 3f));
        Cube(t, "SignCrossA", new Vector3(-1.82f, 1.55f, 0.3f), new Vector3(0.04f, 0.09f, 0.9f), darkWood, new Vector3(40f, 0f, 0f));
        Cube(t, "SignCrossB", new Vector3(-1.82f, 1.55f, 0.3f), new Vector3(0.04f, 0.09f, 0.9f), darkWood, new Vector3(-40f, 0f, 0f));
        // 연장 상자와 새 판자 더미 (고치려고 가져다 놓은 것)
        Cube(t, "ToolBox", new Vector3(1.8f, 0.55f, 0.6f), new Vector3(0.5f, 0.3f, 0.9f), darkWood);
        for (int i = 0; i < 4; i++)
            Cube(t, "NewPlank", new Vector3(1.8f, 0.78f + i * 0.07f, 0.6f), new Vector3(0.44f, 0.06f, 1.4f - i * 0.1f), wood, new Vector3(0f, i * 7f - 8f, 0f));

        Box(t, "ClosedHit", new Vector3(0f, 1.0f, 2.2f), new Vector3(4.6f, 2.4f, 5.4f), true);
    }

    // ───────── 원래 섬의 선착장 ─────────

    /// <summary>
    /// 원래 섬(Island)의 남쪽 바닷가에 선착장을 만든다. 지형을 읽어 물가를 찾고, 나무와 바위가 없는 자리를 고른다.
    /// 눈 섬으로 돌아왔을 때 서는 자리 Spawn_FromSnow도 함께 만든다.
    /// </summary>
    [MenuItem("유리병 정원/선착장 만들기")]
    public static string BuildMainDock()
    {
        EditorSceneManager.SaveOpenScenes();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != IslandPath)
            EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return "지형(Terrain)을 찾지 못했어요.";

        foreach (string n in new[] { "MainDock", "Spawn_FromSnow" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }
        Physics.SyncTransforms();

        // 섬 가운데(0, 30)에서 남쪽으로 뻗은 여러 방향 중, 물가가 있고 나무와 바위가 없는 곳을 고른다
        Vector2 center = new Vector2(0f, 30f);
        float[] angles = { -90f, -70f, -110f, -55f, -125f, -40f, -140f };
        foreach (float angle in angles)
        {
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            // 땅이 처음 0.35m 아래로 내려가는 자리(물가 바로 안쪽)를 찾는다
            Vector2 land = Vector2.zero; float landY = 0f; bool found = false;
            for (float r = 60f; r < 200f; r += 0.5f)
            {
                Vector2 p = center + dir * r;
                float h = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
                if (h <= 0.35f) { land = p; landY = h; found = true; break; }
            }
            if (!found) continue;

            // 물속(바닥이 물 아래 0.6m)까지 닿도록 부두 길이를 정한다
            float length = 16f;
            for (float d = 0f; d < 60f; d += 0.5f)
            {
                Vector2 q = land + dir * d;
                if (terrain.SampleHeight(new Vector3(q.x, 0f, q.y)) + terrain.transform.position.y <= -0.6f) { length = Mathf.Clamp(d + 3f, 14f, 36f); break; }
            }

            // 부두가 지나갈 길(땅 쪽 6m와 바다 쪽 끝까지)에 나무나 바위가 없어야 한다
            bool blocked = false;
            for (float d = -6f; d < length && !blocked; d += 2f)
            {
                Vector2 p = land + dir * d;
                float gy = terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
                if (Physics.CheckSphere(new Vector3(p.x, Mathf.Max(gy, 0f) + 1.6f, p.y), 1.4f)) blocked = true;
            }
            if (blocked) continue;

            GameObject root = Build(null, "MainDock", land, new Vector3(dir.x, 0f, dir.y), true, "SnowIsland", "FromDock", "배 타고 눈 섬으로 가기", out Vector3 landing, landY, length);
            // 바닷가에 묻히지 않게: 땅 쪽 끝의 부두가 땅 위로 자연스럽게 이어진다 (윗면 0.75m, 땅 0.9m 이하)
            GameObject spawn = new GameObject("Spawn_FromSnow");
            spawn.transform.SetPositionAndRotation(landing, Quaternion.LookRotation(new Vector3(-dir.x, 0f, -dir.y)));
            SpawnPoint sp = spawn.AddComponent<SpawnPoint>();
            SerializedObject sps = new SerializedObject(sp);
            sps.FindProperty("id").stringValue = "FromSnow";
            sps.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            return "선착장을 만들었어요: 방향 " + angle + "도, 물가 (" + land.x.ToString("F0") + ", " + land.y.ToString("F0") + "), 도착 자리 " + landing + ", 부두 길이 " + length;
        }
        return "선착장을 놓을 빈 물가를 찾지 못했어요.";
    }
}
