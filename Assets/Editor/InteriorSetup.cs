using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 집 안(HutInterior) 씬을 에셋 팩의 통나무 오두막(6m x 6m)으로 꾸민다.
/// 바닥, 벽, 창, 문, 지붕과 가구(침대, 책상과 편지, 선반, 보관 상자, 통, 러그, 촛불)를 놓고 따뜻한 조명을 켠다.
/// 작업대(Workbench)와 그 위의 병들은 그대로 두고, 문(SceneDoor)과 도착 지점은 다시 만든다.
/// 메뉴: 유리병 정원 > 집 안 꾸미기. 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class InteriorSetup
{
    private const string InteriorPath = "Assets/Scenes/HutInterior.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string Kit = "Assets/ThirdParty/MedievalTownExteriors/Prefabs/";
    private const string DecoFolder = "Assets/Models/Deco/";

    [MenuItem("유리병 정원/집 안 꾸미기")]
    public static string Build()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // 예전에 만든 것(큐브 바닥과 벽, 문, 도착 지점, 가구)을 지운다
        foreach (string n in new[] { "Floor", "WallNorth", "WallSouth", "WallEast", "WallWest", "Door", "Spawn_FromIsland", "Cabin", "Room", "Furniture", "Lights" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }

        // 중세풍 방 (바닥 윗면이 높이 0, 문은 남쪽)
        GameObject doorObject = BuildRoom();

        // 문: 바라보고 E를 누르면 섬으로 나간다
        {
            doorObject.name = "Door";
            SceneDoor door = doorObject.AddComponent<SceneDoor>();
            SerializedObject so = new SerializedObject(door);
            so.FindProperty("targetScene").stringValue = "Island";
            so.FindProperty("spawnId").stringValue = "FromHut";
            so.FindProperty("label").stringValue = "밖으로 나가기";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 도착 지점: 문 안쪽, 방 안을 바라본다
        GameObject spawn = new GameObject("Spawn_FromIsland");
        spawn.transform.SetPositionAndRotation(new Vector3(0f, 1f, -1.9f), Quaternion.identity);
        SpawnPoint sp = spawn.AddComponent<SpawnPoint>();
        SerializedObject sps = new SerializedObject(sp);
        sps.FindProperty("id").stringValue = "FromIsland";
        sps.ApplyModifiedPropertiesWithoutUndo();

        // 작업대는 북쪽 벽 앞에 둔다
        GameObject bench = GameObject.Find("Workbench");
        if (bench != null) bench.transform.position = new Vector3(0f, 0f, 2.0f);

        BuildFurniture(bench != null ? bench.transform.position : Vector3.zero);
        BuildLighting();

        // 이미 서 있는 플레이어(이 씬의 GameRoot)를 도착 지점으로 옮긴다
        foreach (GameRoot root in Object.FindObjectsByType<GameRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform player = root.transform.Find("Player");
            if (player != null) player.position = spawn.transform.position;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        return "집 안 꾸미기 완료";
    }

    // ───────── 방 (바깥 중세 집과 같은 색: 회벽, 돌 밑단, 짙은 갈색 목조 뼈대) ─────────

    private const float Half = 3f;       // 방 안쪽 반 폭 (6m x 6m)
    private const float Height = 3.2f;
    private const float DoorWidth = 1.2f;
    private const float DoorHeight = 2.3f;

    /// <summary>방을 만들고 문 오브젝트(SceneDoor를 붙일 자리)를 돌려준다.</summary>
    private static GameObject BuildRoom()
    {
        GameObject room = new GameObject("Room");
        Material plaster = Mat("MedPlaster", new Color(0.88f, 0.85f, 0.78f));
        Material stone = Mat("MedStone", new Color(0.5f, 0.5f, 0.52f));
        Material timber = Mat("MedTimber", new Color(0.33f, 0.2f, 0.12f));
        Material floorMat = Mat("MedFloor", new Color(0.5f, 0.35f, 0.22f));
        Material ceilingMat = Mat("MedCeiling", new Color(0.4f, 0.27f, 0.17f));
        Material doorMat = Mat("MedDoor", new Color(0.42f, 0.27f, 0.15f));
        Material glass = Mat("MedWindowGlow", new Color(1f, 0.85f, 0.5f));
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.3f) * 1.4f);

        float t = 0.2f;              // 벽 두께
        float outer = Half + t / 2f; // 벽 중심

        // 바닥과 천장 (천장은 충돌 없음)
        Cube(room, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(Half * 2f + 0.6f, 0.2f, Half * 2f + 0.6f), floorMat);
        Cube(room, "Ceiling", new Vector3(0f, Height + 0.1f, 0f), new Vector3(Half * 2f + 0.6f, 0.2f, Half * 2f + 0.6f), ceilingMat, false);
        foreach (float z in new[] { -2f, 0f, 2f })
            Cube(room, "CeilingBeam", new Vector3(0f, Height - 0.1f, z), new Vector3(Half * 2f, 0.2f, 0.25f), timber, false);

        // 벽: 북, 동, 서는 통벽. 남쪽은 문 자리를 비운다
        float wallLen = Half * 2f + t * 2f;
        Cube(room, "WallNorth", new Vector3(0f, Height / 2f, outer), new Vector3(wallLen, Height, t), plaster);
        Cube(room, "WallEast", new Vector3(outer, Height / 2f, 0f), new Vector3(t, Height, wallLen), plaster);
        Cube(room, "WallWest", new Vector3(-outer, Height / 2f, 0f), new Vector3(t, Height, wallLen), plaster);
        float sideLen = Half - DoorWidth / 2f + t;
        float sideCenter = DoorWidth / 2f + sideLen / 2f;
        Cube(room, "WallSouthL", new Vector3(-sideCenter, Height / 2f, -outer), new Vector3(sideLen, Height, t), plaster);
        Cube(room, "WallSouthR", new Vector3(sideCenter, Height / 2f, -outer), new Vector3(sideLen, Height, t), plaster);
        Cube(room, "DoorLintel", new Vector3(0f, (Height + DoorHeight) / 2f, -outer), new Vector3(DoorWidth, Height - DoorHeight, t), plaster);

        // 문: 짙은 갈색 널문 (바라보고 E)
        GameObject door = Cube(room, "DoorSlab", new Vector3(0f, DoorHeight / 2f, -Half - 0.02f), new Vector3(DoorWidth - 0.05f, DoorHeight - 0.05f, 0.1f), doorMat);
        Cube(door.transform.parent.gameObject, "DoorFrameL", new Vector3(-DoorWidth / 2f - 0.07f, DoorHeight / 2f, -Half + 0.02f), new Vector3(0.14f, DoorHeight, 0.18f), timber, false);
        Cube(door.transform.parent.gameObject, "DoorFrameR", new Vector3(DoorWidth / 2f + 0.07f, DoorHeight / 2f, -Half + 0.02f), new Vector3(0.14f, DoorHeight, 0.18f), timber, false);
        Cube(door.transform.parent.gameObject, "DoorFrameTop", new Vector3(0f, DoorHeight + 0.07f, -Half + 0.02f), new Vector3(DoorWidth + 0.28f, 0.14f, 0.18f), timber, false);

        // 벽 안쪽 장식: 돌 밑단, 가로 보, 모서리 기둥, 세로 기둥
        float face = Half - 0.02f;
        foreach (int side in new[] { 0, 1, 2, 3 }) // 0 북, 1 동, 2 남, 3 서
        {
            Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
            GameObject wall = new GameObject("WallTrim" + side);
            wall.transform.SetParent(room.transform, false);
            wall.transform.rotation = rot;
            // 이 벽을 바라볼 때 +z가 벽 쪽: 벽의 안쪽 면은 z = face
            Cube(wall, "StoneBase", new Vector3(0f, 0.45f, face), new Vector3(Half * 2f, 0.9f, 0.05f), stone, false);
            Cube(wall, "BeamLow", new Vector3(0f, 0.93f, face - 0.01f), new Vector3(Half * 2f, 0.12f, 0.1f), timber, false);
            Cube(wall, "BeamHigh", new Vector3(0f, Height - 0.2f, face - 0.01f), new Vector3(Half * 2f, 0.2f, 0.1f), timber, false);
            foreach (float s in new[] { -2f, 2f })
                Cube(wall, "Stud", new Vector3(s, Height / 2f, face - 0.01f), new Vector3(0.15f, Height, 0.1f), timber, false);
        }
        foreach (Vector2 c in new[] { new Vector2(-Half, -Half), new Vector2(Half, -Half), new Vector2(-Half, Half), new Vector2(Half, Half) })
            Cube(room, "CornerPost", new Vector3(c.x - Mathf.Sign(c.x) * 0.1f, Height / 2f, c.y - Mathf.Sign(c.y) * 0.1f), new Vector3(0.25f, Height, 0.25f), timber, false);

        // 창: 북쪽(작업대 뒤), 동쪽(침대 위), 서쪽(책상 위). 따뜻한 빛이 들어오는 느낌
        Window(room, glass, timber, new Vector3(0f, 1.75f, Half - 0.03f), 0f);
        Window(room, glass, timber, new Vector3(Half - 0.03f, 1.75f, -1.1f), 90f);
        Window(room, glass, timber, new Vector3(-Half + 0.03f, 1.75f, -1.2f), 270f);

        return door;
    }

    /// <summary>창틀과 따뜻하게 빛나는 유리. yaw는 창이 바라보는 방향(방 안쪽)이다.</summary>
    private static void Window(GameObject parent, Material glass, Material timber, Vector3 pos, float yaw)
    {
        GameObject w = new GameObject("Window");
        w.transform.SetParent(parent.transform, false);
        w.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        Cube(w, "Glass", Vector3.zero, new Vector3(0.9f, 0.8f, 0.04f), glass, false);
        Cube(w, "FrameTop", new Vector3(0f, 0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameBottom", new Vector3(0f, -0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameL", new Vector3(-0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "FrameR", new Vector3(0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "BarV", new Vector3(0f, 0f, -0.02f), new Vector3(0.06f, 0.8f, 0.08f), timber, false);
        Cube(w, "BarH", new Vector3(0f, 0f, -0.02f), new Vector3(0.9f, 0.06f, 0.08f), timber, false);
        PointLight(w.transform, "WindowLight", new Vector3(0f, 0f, -0.5f), new Color(1f, 0.85f, 0.6f), 0.9f, 4f);
    }

    // ───────── 가구 ─────────

    private static void BuildFurniture(Vector3 benchPosition)
    {
        GameObject root = new GameObject("Furniture");

        Material wood = Mat("FurnitureWood", new Color(0.69f, 0.51f, 0.35f));
        Material darkWood = Mat("FurnitureDarkWood", new Color(0.47f, 0.33f, 0.23f));
        Material cream = Mat("FurnitureCream", new Color(0.96f, 0.93f, 0.85f));
        Material blanket = Mat("FurnitureBlanket", new Color(0.32f, 0.44f, 0.56f));
        Material pillow = Mat("FurniturePillow", new Color(0.99f, 0.97f, 0.94f));
        Material rugMat = Mat("FurnitureRug", new Color(0.6f, 0.3f, 0.27f));
        Material paper = Mat("FurniturePaper", new Color(1f, 0.99f, 0.95f));

        // 러그: 방 가운데
        Cylinder(root, "Rug", new Vector3(0f, 0.012f, -0.4f), new Vector3(2.6f, 0.012f, 2.6f), rugMat, false);

        // 침대 (동쪽 벽, 남쪽 절반). 바라보고 E를 누르면 잔다 (Bed).
        GameObject bed = new GameObject("Bed");
        bed.transform.SetParent(root.transform, false);
        bed.AddComponent<Bed>();
        Cube(bed, "BedFrame", new Vector3(2.15f, 0.18f, -1.1f), new Vector3(1.1f, 0.36f, 2.1f), darkWood);
        Cube(bed, "BedMattress", new Vector3(2.15f, 0.46f, -1.1f), new Vector3(1.0f, 0.2f, 2.0f), cream);
        Cube(bed, "BedBlanket", new Vector3(2.15f, 0.6f, -1.55f), new Vector3(1.04f, 0.1f, 1.2f), blanket);
        Cube(bed, "BedPillow", new Vector3(2.15f, 0.62f, -0.35f), new Vector3(0.8f, 0.12f, 0.4f), pillow);
        Cube(bed, "BedHeadboard", new Vector3(2.15f, 0.55f, 0.0f), new Vector3(1.1f, 0.9f, 0.08f), wood);

        // 책상과 편지 (서쪽 벽, 남쪽)
        Cube(root, "DeskTop", new Vector3(-2.3f, 0.76f, -1.7f), new Vector3(0.7f, 0.06f, 1.3f), wood);
        foreach (Vector2 leg in new[] { new Vector2(-2.58f, -2.28f), new Vector2(-2.02f, -2.28f), new Vector2(-2.58f, -1.12f), new Vector2(-2.02f, -1.12f) })
            Cube(root, "DeskLeg", new Vector3(leg.x, 0.37f, leg.y), new Vector3(0.06f, 0.74f, 0.06f), darkWood);
        MakeLetterReadable(Cube(root, "Letter", new Vector3(-2.3f, 0.795f, -1.7f), new Vector3(0.3f, 0.006f, 0.21f), paper, false));

        // 책상 위 촛불 (모델 + 불빛)
        GameObject candle = Deco(root, "deco_candle", new Vector3(-2.3f, 0.79f, -2.1f), 6f);
        PointLight(candle.transform, "CandleLight", new Vector3(0f, 0.25f, 0f), new Color(1f, 0.82f, 0.55f), 1.2f, 3.5f);

        BuildShelf(root.transform, wood, darkWood);

        // 보관 상자와 통
        Kitpiece(root, "Crate", new Vector3(1.6f, 0f, 2.3f), 15f);
        Kitpiece(root, "Crate", new Vector3(1.6f, 0.8f, 2.3f), 40f);
        Kitpiece(root, "Barrel", new Vector3(2.35f, 0f, 2.3f), 0f);
        Kitpiece(root, "Barrel", new Vector3(2.4f, 0f, 1.25f), 40f);
        Kitpiece(root, "Crate", new Vector3(-1.3f, 0f, -2.7f), 80f); // 서쪽 벽은 긴 선반이 차지해서 상자는 남쪽 벽으로 옮겼다
        Kitpiece(root, "Bucket", new Vector3(-2.3f, 0f, 3.0f - 0.35f), 0f);

        // 천장에 매단 랜턴 (작업대 위)
        GameObject lantern = Kitpiece(root, "Lantern", new Vector3(0f, 2.3f, 1.2f), 0f, false);
        PointLight(lantern.transform, "LanternLight", new Vector3(0f, 0.3f, 0f), new Color(1f, 0.78f, 0.45f), 1.0f, 5f);
    }

    /// <summary>책상 위 편지를 바라보고 E로 읽을 수 있게 한다. (얇은 편지라서 바라보기 쉽게 Collider를 크게 둔다)</summary>
    private static void MakeLetterReadable(GameObject letter)
    {
        if (letter == null || letter.GetComponent<DeskLetter>() != null) return;
        BoxCollider box = letter.GetComponent<BoxCollider>();
        if (box == null) box = letter.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.4f, 45f, 1.5f); // 편지 칸이 얇아서(0.006m) 세로를 크게 키웠다 (약 0.27m)
        box.center = new Vector3(0f, 22f, 0f);
        letter.AddComponent<DeskLetter>();
    }

    /// <summary>집 안의 다른 것은 그대로 두고 책상 위 편지만 읽을 수 있게 한다. (메뉴: 유리병 정원 > 책상 편지 읽게 하기)</summary>
    [MenuItem("유리병 정원/책상 편지 읽게 하기")]
    public static string AddLetterInteraction()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != InteriorPath)
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);
        }
        GameObject furniture = GameObject.Find("Furniture");
        Transform letter = furniture != null ? furniture.transform.Find("Letter") : null;
        if (letter == null) return "책상 위 편지(Furniture/Letter)를 찾지 못했어요.";
        MakeLetterReadable(letter.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        return "책상 위 편지를 읽을 수 있게 했어요";
    }

    // 선반: 서쪽 벽을 따라 긴 선반 하나. 4단 x 6칸 = 24칸이고 아래 단부터 6칸씩 열린다. (완성한 병 6개마다 한 단씩. BottleStore.ShelfCapacity)
    private const float ShelfLength = 3.1f, ShelfDepth = 0.3f;
    private static readonly Vector3 ShelfCenter = new Vector3(-2.58f, 0f, 0.7f);
    private static readonly float[] ShelfBoardY = { 0.25f, 0.8f, 1.35f, 1.9f, 2.45f };
    private const int ShelfRows = 4, ShelfCols = 6;

    /// <summary>선반의 판, 기둥, 뒤판과 병 칸 자리(BottleShelf)를 만든다. 병 칸 번호는 아래 단 왼쪽부터 (단 x 6 + 칸).</summary>
    private static void BuildShelf(Transform furniture, Material wood, Material darkWood)
    {
        GameObject root = furniture.gameObject;
        foreach (float y in ShelfBoardY)
            Cube(root, "ShelfBoard", ShelfCenter + new Vector3(0f, y, 0f), new Vector3(ShelfDepth, 0.04f, ShelfLength), wood);
        foreach (float z in new[] { -ShelfLength / 2f + 0.03f, 0f, ShelfLength / 2f - 0.03f })
            Cube(root, "ShelfPost", ShelfCenter + new Vector3(0f, 1.27f, z), new Vector3(ShelfDepth, 2.5f, 0.05f), darkWood);
        Cube(root, "ShelfBack", ShelfCenter + new Vector3(-0.13f, 1.27f, 0f), new Vector3(0.03f, 2.5f, ShelfLength), darkWood, false);

        GameObject shelfRoot = new GameObject("BottleShelf");
        shelfRoot.transform.SetParent(furniture, false);
        var slotList = new System.Collections.Generic.List<Transform>();
        float spacing = (ShelfLength - 0.2f) / ShelfCols;
        for (int row = 0; row < ShelfRows; row++)
        {
            for (int col = 0; col < ShelfCols; col++)
            {
                GameObject slot = new GameObject("Slot" + (row * ShelfCols + col));
                slot.transform.SetParent(shelfRoot.transform, false);
                float z = ShelfCenter.z + (col - (ShelfCols - 1) / 2f) * spacing;
                slot.transform.position = new Vector3(ShelfCenter.x + 0.02f, ShelfBoardY[row] + 0.02f, z); // 판 윗면
                slotList.Add(slot.transform);
            }
        }
        // 선반 말고도 병을 둘 자리 5곳: 책상 위(24번), 북쪽 창턱 둘(25, 26번), 동쪽 창턱 둘(27, 28번). 선반이 안 열려도 처음부터 쓸 수 있다.
        // (BottleStore.ExtraSlotStart, ExtraSlotCount와 맞춰야 한다)
        Cube(root, "SillBoard", new Vector3(0f, 1.30f, Half - 0.15f), new Vector3(1.25f, 0.04f, 0.26f), wood, false);
        Cube(root, "SillBoard", new Vector3(Half - 0.15f, 1.30f, -1.1f), new Vector3(0.26f, 0.04f, 1.25f), wood, false);
        Vector3[] extra =
        {
            new Vector3(-2.3f, 0.79f, -1.25f),
            new Vector3(-0.28f, 1.32f, Half - 0.15f), new Vector3(0.28f, 1.32f, Half - 0.15f),
            new Vector3(Half - 0.15f, 1.32f, -1.4f), new Vector3(Half - 0.15f, 1.32f, -0.8f)
        };
        for (int i = 0; i < extra.Length; i++)
        {
            GameObject slot = new GameObject("Slot" + (BottleStore.ExtraSlotStart + i));
            slot.transform.SetParent(shelfRoot.transform, false);
            slot.transform.position = extra[i];
            slotList.Add(slot.transform);
        }

        BottleShelf bottleShelf = shelfRoot.AddComponent<BottleShelf>();
        SerializedObject shelfSo = new SerializedObject(bottleShelf);
        SerializedProperty slotsProp = shelfSo.FindProperty("slots");
        slotsProp.arraySize = slotList.Count;
        for (int i = 0; i < slotList.Count; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotList[i];
        shelfSo.FindProperty("station").objectReferenceValue = Object.FindFirstObjectByType<BottleStation>(FindObjectsInactive.Include);
        shelfSo.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 집 안의 다른 것은 그대로 두고 선반만 다시 만든다. (메뉴: 유리병 정원 > 선반 다시 만들기)
    /// 이전 선반의 판과 기둥과 병 자리를 지우고 새로 만든다. 서쪽 벽의 상자(Crate)가 선반과 겹치면 남쪽 벽으로 옮긴다.
    /// </summary>
    [MenuItem("유리병 정원/선반 다시 만들기")]
    public static string RebuildShelf()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != InteriorPath)
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);
        }
        GameObject furniture = GameObject.Find("Furniture");
        if (furniture == null) return "Furniture를 찾지 못했어요. 먼저 집 안 꾸미기를 하세요.";

        for (int i = furniture.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = furniture.transform.GetChild(i);
            bool shelfPart = child.name == "ShelfBoard" || child.name == "ShelfPost" || child.name == "ShelfBack" || child.name == "BottleShelf" || child.name == "SillBoard";
            // 예전 위치(-2.2, -0.1)에 있던 상자는 새 선반과 겹쳐서 옮긴다
            bool oldCrate = child.name.StartsWith("Crate") && Vector3.Distance(child.position, new Vector3(-2.2f, 0f, -0.1f)) < 0.5f;
            if (oldCrate) child.position = new Vector3(-1.3f, 0f, -2.7f);
            if (shelfPart) Object.DestroyImmediate(child.gameObject);
        }
        Material wood = Mat("FurnitureWood", new Color(0.69f, 0.51f, 0.35f));
        Material darkWood = Mat("FurnitureDarkWood", new Color(0.47f, 0.33f, 0.23f));
        BuildShelf(furniture.transform, wood, darkWood);

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        return "선반을 24칸(4단 x 6칸)과 창가, 책상 자리 5곳으로 다시 만들었어요";
    }

    private static void BuildLighting()
    {
        // 따뜻한 환경광 (지붕 때문에 해가 들어오는 창가 말고는 어두워서)
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.56f, 0.5f);

        GameObject lights = new GameObject("Lights");
        PointLight(lights.transform, "CeilingLight", new Vector3(0f, 3.1f, -0.2f), new Color(1f, 0.9f, 0.72f), 2.2f, 8f);
        PointLight(lights.transform, "WorkbenchLight", new Vector3(0f, 2.2f, 1.6f), new Color(1f, 0.88f, 0.7f), 1.4f, 5f);
    }

    // ───────── 도우미 ─────────

    private static Material Mat(string name, Color color)
    {
        return BottleSetup.GetOrCreateMaterial(name, color);
    }

    private static GameObject Cube(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, parent, name, pos, scale, mat, collider);
        return go;
    }

    private static GameObject Cylinder(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, parent, name, pos, scale, mat, collider);
        return go;
    }

    private static void Setup(GameObject go, GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat, bool collider)
    {
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    /// <summary>오두막 키트의 소품(통, 상자)을 놓는다.</summary>
    private static GameObject Kitpiece(GameObject parent, string prefabName, Vector3 pos, float yaw, bool collider = true)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + prefabName + ".prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation;
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (r.name.Contains("LOD1") || r.name.Contains("LOD2") || r.name.Contains("LOD3")) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        foreach (Collider old in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(old);
        if (!collider) return go;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        return go;
    }

    /// <summary>작은 장식 모델(deco_*)을 크게 키워 소품으로 놓는다.</summary>
    private static GameObject Deco(GameObject parent, string model, Vector3 pos, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DecoFolder + model + ".obj");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = model;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    private static void PointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }
}
