using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬 씬(Island) 안, 집(Cabin) 속에 방을 짓는다. 집 안은 더 이상 따로 떨어진 씬이 아니다.
/// 바깥 집(중세 마을 팩의 Building_b)은 속이 빈 껍데기이고 모양이 T자다: 문이 있는 가운데가 앞으로 튀어나온 현관 앞방(폭 2.8m, 깊이 3.2m)과
/// 그 뒤의 큰 방(폭 10.9m, 깊이 5.9m)이다. 방도 그 모양대로 짓는다. (직사각형으로 지으면 벽이 집 밖으로 나오고 집 앞 벽의 창틀과 상자가 방에 남는다)
/// 바닥, 벽, 창, 천장과 가구(침대, 책상과 편지, 선반, 탁자, 보관 상자, 통, 러그, 촛불)를 놓고, 집 안 영역(IndoorZone)을 둔다.
/// 작업대(Workbench)와 그 위의 병들은 새로 만들지 않고 섬에 있는 것을 방 안으로 옮겨 놓는다. (병 만들기 메뉴가 작업대의 병을 만든다)
/// 문은 HouseDoorBuilder가 만든 열리는 현관문(HouseDoor)이다. 현관 앞방의 남쪽 벽은 그 문 자리를 비워 둔다.
/// 메뉴: 유리병 정원 > 집 안 꾸미기 (먼저 집 문 만들기). 여러 번 눌러도 같은 결과가 나온다.
/// 좌표는 방 뿌리(큰 방 바닥 한가운데) 기준이다. +z가 북쪽(집 안쪽), 현관 앞방은 z가 작은 쪽이다.
/// </summary>
public static class InteriorSetup
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string Kit = "Assets/ThirdParty/MedievalTownExteriors/Prefabs/";
    private const string DecoFolder = "Assets/Models/Deco/";

    // 큰 방: 안쪽 반 폭(동서), 반 깊이(남북). 바깥 집 몸채 안쪽에 꼭 맞는다. (집 앞 벽 z는 집 맨 앞 + 3.85m, 뒤 벽은 + 9.75m)
    private const float HallHalfX = 5.45f;
    private const float HallHalfZ = 2.95f;
    // 현관 앞방: 안쪽 반 폭, 깊이 (남쪽 안벽에서 큰 방 남쪽 벽까지)
    private const float VestHalfX = 1.4f;
    private const float VestDepth = 3.2f;
    private const float Height = 3.2f;
    private const float DoorWidth = 1.36f;   // 바깥 문 구멍(x -0.68 ~ 0.68)과 같다
    private const float DoorHeight = 2.5f;
    private const float WallThickness = 0.2f;
    public const float HoleHalfW = 0.5f, HoleHalfH = 0.45f;   // 창 구멍 (가로 1.0 x 세로 0.9), 창 가운데 높이는 1.75
    /// <summary>창 자리 (집 뿌리 기준 위치, 방 안쪽을 보는 yaw, 겉벽까지 거리, 불빛 여부). 겉벽 구멍은 CabinWindows가 같은 자리에 낸다.</summary>
    public static readonly (Vector3 pos, float yaw, float outer, bool light)[] WindowSpecs =
    {
        (new Vector3(0f, 1.75f, HallHalfZ - 0.03f), 0f, 0.33f, false),
        (new Vector3(-3.3f, 1.75f, HallHalfZ - 0.03f), 0f, 0.33f, false),
        (new Vector3(3.3f, 1.75f, HallHalfZ - 0.03f), 0f, 0.33f, false),
        (new Vector3(HallHalfX - 0.03f, 1.75f, -1.7f), 90f, 0.33f, true),
        (new Vector3(-HallHalfX + 0.03f, 1.75f, -1.9f), 270f, 0.50f, true),
    };
    private const float VestSouthZ = -(HallHalfZ + VestDepth); // 현관 앞방 남쪽 안벽 (큰 방 뿌리 기준)

    private static GameObject houseRoot;

    [MenuItem("유리병 정원/집 안 꾸미기")]
    public static string Build()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        GameObject cabin = GameObject.Find("Cabin");
        if (cabin == null) return "섬에 Cabin(집)이 없어요.";
        Bounds house = cabin.GetComponentInChildren<Renderer>().bounds;

        // 작업대(병을 꾸미는 자리)는 섬 씬에 이미 있다. 다시 지을 때 같이 지워지지 않게 뿌리에서 떼어 둔다
        GameObject bench = FindWorkbench();
        if (bench == null) return "섬 씬에서 Workbench를 찾지 못했어요. (git 기록의 옛 HutInterior 씬에 있던 작업대를 섬으로 옮겨야 해요)";
        bench.transform.SetParent(null, true);

        // 예전에 지은 방을 지운다
        GameObject old = GameObject.Find("HouseInterior");
        while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find("HouseInterior"); }

        // 방 뿌리: 큰 방 한가운데. 높이는 문턱(집 밑동 + 0.41) + 2cm. 현관 앞방 남쪽 안벽이 문 두께 바로 뒤(집 맨 앞 + 0.65)에 오게 놓는다
        float southInnerZ = house.min.z + 0.65f;
        houseRoot = new GameObject("HouseInterior");
        houseRoot.transform.position = new Vector3(0f, house.min.y + 0.43f, southInnerZ - VestSouthZ);
        houseRoot.transform.rotation = Quaternion.identity;

        BuildRoom();

        // 작업대는 큰 방 북쪽 벽 앞에
        bench.transform.SetParent(houseRoot.transform, false);
        bench.transform.localPosition = new Vector3(0f, 0f, HallHalfZ - 1.0f);
        bench.transform.localRotation = Quaternion.identity;
        DressWorkbench(bench);

        BuildFurniture();
        BuildLighting();
        int cleared = ClearPlantsInside();
        string cabinNote = CabinWindows.Build();
        Debug.Log(cabinNote);

        // 집 안 영역: 큰 방과 현관 앞방
        IndoorZone zone = houseRoot.AddComponent<IndoorZone>();
        SerializedObject zso = new SerializedObject(zone);
        float zh = Height + 0.4f;
        SetVectors(zso.FindProperty("centers"), new[] { new Vector3(0f, Height * 0.5f, 0f), new Vector3(0f, Height * 0.5f, (VestSouthZ - HallHalfZ) * 0.5f) });
        SetVectors(zso.FindProperty("sizes"), new[] { new Vector3(HallHalfX * 2f + 0.2f, zh, HallHalfZ * 2f + 0.2f), new Vector3(VestHalfX * 2f + 0.2f, zh, VestDepth + 0.3f) });
        zso.FindProperty("indoorSky").colorValue = new Color(0.68f, 0.62f, 0.55f);
        zso.FindProperty("indoorEquator").colorValue = new Color(0.64f, 0.58f, 0.51f);
        zso.FindProperty("indoorGround").colorValue = new Color(0.50f, 0.44f, 0.37f);
        zso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "집 안 꾸미기 완료 (큰 방 " + (HallHalfX * 2f) + "m x " + (HallHalfZ * 2f) + "m + 현관 앞방, 뿌리 " + houseRoot.transform.position + ", 집 안에 있던 풀 " + cleared + "개 지움)";
    }

    /// <summary>집 안에 섬 풀과 꽃이 바닥을 뚫고 나와 있지 않게, 집 터 안의 식물 소품(Plants)을 지운다.</summary>
    private static int ClearPlantsInside()
    {
        int removed = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.parent == null || t.parent.name != "Plants") continue; // 지운 식물의 자식은 이미 사라졌다
            Vector3 l = houseRoot.transform.InverseTransformPoint(t.position);
            bool hall = Mathf.Abs(l.x) < HallHalfX + 0.8f && Mathf.Abs(l.z) < HallHalfZ + 0.8f;
            bool vest = Mathf.Abs(l.x) < VestHalfX + 0.8f && l.z > VestSouthZ - 0.8f && l.z < -HallHalfZ + 0.1f;
            if (!hall && !vest) continue;
            Object.DestroyImmediate(t.gameObject);
            removed++;
        }
        return removed;
    }

    private static void SetVectors(SerializedProperty prop, Vector3[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).vector3Value = values[i];
    }

    /// <summary>
    /// 밋밋한 갈색 상자였던 작업대를 꾸민다: 몸통은 짙은 나무, 상판은 밝은 나무로 조금 넓게 덮고, 아래에 선반 판 하나를 둔다.
    /// 병이 놓이는 높이(상판 위 1m)와 충돌 상자는 그대로다. 여러 번 눌러도 겹쳐 쌓이지 않는다.
    /// </summary>
    private static void DressWorkbench(GameObject bench)
    {
        foreach (string n in new[] { "WorkbenchTop", "WorkbenchShelf", "deco_furn_workbench" })
        {
            Transform old = bench.transform.Find(n);
            while (old != null) { Object.DestroyImmediate(old.gameObject); old = bench.transform.Find(n); }
        }
        Material wood = Mat("FurnitureWood", new Color(0.69f, 0.51f, 0.35f));
        Material darkWood = Mat("FurnitureDarkWood", new Color(0.47f, 0.33f, 0.23f));
        Transform body = bench.transform.Find("Table");
        GameObject benchModel = body != null ? Furn(bench, "deco_furn_workbench", new Vector3(body.localPosition.x, 0f, body.localPosition.z), 180f) : null;
        if (benchModel != null)
        {
            // 상자 하나였던 작업대를 로우폴리 작업대 모델로 바꾼다 (문 달린 몸통, 모서리 기둥, 상판). 막는 상자와 병 자리는 그대로 두고 상자의 겉모습만 숨긴다.
            body.GetComponent<Renderer>().enabled = false;
        }
        else if (body != null)
        {
            body.GetComponent<Renderer>().sharedMaterial = darkWood;
            Vector3 s = body.localScale;
            // 상판: 몸통 위를 살짝 넓게 덮는다 (몸통 윗면이 높이 1m)
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "WorkbenchTop";
            top.transform.SetParent(bench.transform, false);
            top.transform.localPosition = new Vector3(body.localPosition.x, 0.99f, body.localPosition.z);
            top.transform.localScale = new Vector3(s.x + 0.14f, 0.05f, s.z + 0.12f);
            top.GetComponent<Renderer>().sharedMaterial = wood;
            Object.DestroyImmediate(top.GetComponent<Collider>());
            // 아래 선반 판
            GameObject shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "WorkbenchShelf";
            shelf.transform.SetParent(bench.transform, false);
            shelf.transform.localPosition = new Vector3(body.localPosition.x, 0.28f, body.localPosition.z - s.z * 0.5f - 0.015f);
            shelf.transform.localScale = new Vector3(s.x - 0.2f, 0.05f, 0.03f);
            shelf.GetComponent<Renderer>().sharedMaterial = wood;
            Object.DestroyImmediate(shelf.GetComponent<Collider>());
        }
    }

    private static GameObject FindWorkbench()
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == "Workbench" && go.GetComponent<BottleStation>() != null) return go;
        return null;
    }

    // ───────── 방 (바깥 중세 집과 같은 색: 회벽, 돌 밑단, 짙은 갈색 목조 뼈대) ─────────

    private static void BuildRoom()
    {
        GameObject room = new GameObject("Room");
        room.transform.SetParent(houseRoot.transform, false);
        Material plaster = Mat("MedPlaster", new Color(0.88f, 0.85f, 0.78f));
        Material stone = Mat("MedStone", new Color(0.5f, 0.5f, 0.52f));
        Material timber = Mat("MedTimber", new Color(0.33f, 0.2f, 0.12f));
        Material floorMat = Mat("MedFloor", new Color(0.5f, 0.35f, 0.22f));
        Material ceilingMat = Mat("MedCeiling", new Color(0.4f, 0.27f, 0.17f));
        Material glass = Mat("MedWindowGlow", new Color(1f, 0.85f, 0.5f));
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.3f) * 1.4f);

        float t = WallThickness;
        float hx = HallHalfX, hz = HallHalfZ, vx = VestHalfX, vs = VestSouthZ;

        // 바닥과 천장 (천장은 충돌 없음). 바깥 벽보다 안쪽에 머물도록 벽 밖으로 0.15m만 나온다
        float vestLen = -hz - vs + 0.1f;               // 현관 앞방 바닥 길이 (큰 방 바닥과 살짝 겹친다)
        float vestMid = (vs - 0.1f + -hz) * 0.5f + 0.05f;
        Cube(room, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(hx * 2f + 0.3f, 0.2f, hz * 2f + 0.3f), floorMat);
        Cube(room, "FloorVestibule", new Vector3(0f, -0.1f, vestMid), new Vector3(vx * 2f + 0.3f, 0.2f, vestLen + 0.1f), floorMat);
        Cube(room, "Ceiling", new Vector3(0f, Height + 0.1f, 0f), new Vector3(hx * 2f + 0.3f, 0.2f, hz * 2f + 0.3f), ceilingMat, false);
        Cube(room, "CeilingVestibule", new Vector3(0f, Height + 0.1f, vestMid), new Vector3(vx * 2f + 0.3f, 0.2f, vestLen + 0.1f), ceilingMat, false);
        foreach (float z in new[] { -1.7f, 0f, 1.7f })
            Cube(room, "CeilingBeam", new Vector3(0f, Height - 0.1f, z), new Vector3(hx * 2f, 0.2f, 0.25f), timber, false);
        Cube(room, "CeilingBeam", new Vector3(0f, Height - 0.1f, vestMid), new Vector3(vx * 2f, 0.2f, 0.25f), timber, false);

        // ── 큰 방 벽: 북, 동, 서는 통벽. 남쪽은 현관 앞방으로 열린다
        WallWithHoles(room, "WallNorth", true, hz + t / 2f, t, hx + t, new[] { -3.3f, 0f, 3.3f }, plaster);       // 창 자리는 구멍을 낸다
        WallWithHoles(room, "WallEast", false, hx + t / 2f, t, hz + t, new[] { -1.7f }, plaster);
        WallWithHoles(room, "WallWest", false, -hx - t / 2f, t, hz + t, new[] { -1.9f }, plaster);
        // 집(Cabin) 자체에도 같은 자리에 현관 벽이 있어서, 우리 벽을 2cm 안쪽으로 넣어 면이 겹쳐 줄무늬가 지지 않게 한다
        const float inset = 0.02f;
        float segLen = hx + t - (vx - inset);
        foreach (float sign in new[] { -1f, 1f })
            Cube(room, "WallHallSouth", new Vector3(sign * (vx - inset + segLen / 2f), Height / 2f, -hz - t / 2f), new Vector3(segLen, Height, t), plaster);

        // ── 현관 앞방 벽: 동, 서, 남(문). 문 자리는 비운다
        float sideLen = -hz - vs + t;                 // 문 벽 바깥면부터 큰 방 남쪽 벽까지
        float sideZ = (vs - t + -hz) * 0.5f;
        foreach (float sign in new[] { -1f, 1f })
            Cube(room, "WallVestibule", new Vector3(sign * (vx - inset + t / 2f), Height / 2f, sideZ), new Vector3(t, Height, sideLen), plaster);
        float doorSegW = vx + t - inset - DoorWidth / 2f;
        foreach (float sign in new[] { -1f, 1f })
            Cube(room, "WallDoorSide", new Vector3(sign * (DoorWidth / 2f + doorSegW / 2f), Height / 2f, vs - t / 2f), new Vector3(doorSegW, Height, t), plaster);
        Cube(room, "DoorLintel", new Vector3(0f, (Height + DoorHeight) / 2f, vs - t / 2f), new Vector3(DoorWidth, Height - DoorHeight, t), plaster);

        // ── 벽 안쪽 장식: 돌 밑단, 가로 보, 세로 기둥 (벽 한 장씩: 안쪽 면 한가운데 x, z, 바깥쪽 방향, 반 길이, 세로 기둥 자리)
        float gap = DoorWidth / 2f + 0.06f;
        Trim(room, stone, timber, "TrimNorth", new Vector2(0f, hz), 0f, hx, new[] { -4.9f, -1.65f, 1.65f, 4.9f });
        Trim(room, stone, timber, "TrimEast", new Vector2(hx, 0f), 90f, hz, new[] { -2.6f, 2.6f });
        Trim(room, stone, timber, "TrimWest", new Vector2(-hx, 0f), 270f, hz, new[] { -2.75f, 2.75f });
        float hallSegCenter = (vx + hx) / 2f, hallSegHalf = (hx - vx) / 2f;
        Trim(room, stone, timber, "TrimHallSouthW", new Vector2(-hallSegCenter, -hz), 180f, hallSegHalf, new float[0]);
        Trim(room, stone, timber, "TrimHallSouthE", new Vector2(hallSegCenter, -hz), 180f, hallSegHalf, new float[0]);
        float vestMidZ = (vs + -hz) * 0.5f;
        Trim(room, stone, timber, "TrimVestWest", new Vector2(-vx + inset, vestMidZ), 270f, VestDepth / 2f, new float[0]);
        Trim(room, stone, timber, "TrimVestEast", new Vector2(vx - inset, vestMidZ), 90f, VestDepth / 2f, new float[0]);
        float doorSide = (vx - gap) / 2f;
        Trim(room, stone, timber, "TrimDoorW", new Vector2(-(gap + doorSide), vs), 180f, doorSide, new float[0]);
        Trim(room, stone, timber, "TrimDoorE", new Vector2(gap + doorSide, vs), 180f, doorSide, new float[0]);

        // 모서리 기둥: 큰 방 네 모퉁이, 현관 앞방 앞쪽 두 모퉁이
        foreach (Vector2 c in new[] { new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(-hx, hz), new Vector2(hx, hz) })
            Cube(room, "CornerPost", new Vector3(c.x - Mathf.Sign(c.x) * 0.1f, Height / 2f, c.y - Mathf.Sign(c.y) * 0.1f), new Vector3(0.25f, Height, 0.25f), timber, false);
        foreach (float sign in new[] { -1f, 1f })
            Cube(room, "CornerPost", new Vector3(sign * (vx - 0.1f), Height / 2f, vs + 0.1f), new Vector3(0.25f, Height, 0.25f), timber, false);
        // 현관 앞방과 큰 방이 만나는 모퉁이 기둥 (문 모양 입구의 양쪽)
        foreach (float sign in new[] { -1f, 1f })
            Cube(room, "CornerPost", new Vector3(sign * (vx - 0.1f), Height / 2f, -hz - 0.1f), new Vector3(0.25f, Height, 0.25f), timber, false);

        // 창: 큰 방 북쪽(작업대 뒤와 양옆), 동쪽(침대 위), 서쪽(책상 위). 투명한 유리로 밖이 보인다
        // 창 불빛은 둘만 켠다. 한 오브젝트가 받는 추가 불빛이 8개를 넘으면(URP) 그중 하나가 깜빡이며 꺼졌다 켜진다
        Material clear = AssetDatabase.LoadAssetAtPath<Material>(DecoFolder + "JarGlass.mat");
        if (clear == null) clear = glass;
        foreach (var w in WindowSpecs) Window(room, clear, timber, w.pos, w.yaw, w.light, w.outer);    }

    /// <summary>
    /// 벽 한 장의 안쪽 장식. (x, z)는 벽 안쪽 면 한가운데, yaw는 벽 쪽(바깥)을 바라보는 방향(0 북, 90 동, 180 남, 270 서), halfLength는 벽의 반 길이,
    /// studs는 세로 기둥을 세울 자리(벽을 따라 가운데에서 잰 거리).
    /// </summary>
    private static void Trim(GameObject room, Material stone, Material timber, string name, Vector2 center, float yaw, float halfLength, float[] studs)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(room.transform, false);
        wall.transform.localPosition = new Vector3(center.x, 0f, center.y);
        wall.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        const float face = -0.02f; // 벽 안쪽 면에서 방 쪽으로 2cm
        Cube(wall, "StoneBase", new Vector3(0f, 0.45f, face), new Vector3(halfLength * 2f, 0.9f, 0.05f), stone, false);
        Cube(wall, "BeamLow", new Vector3(0f, 0.93f, face - 0.01f), new Vector3(halfLength * 2f, 0.12f, 0.1f), timber, false);
        Cube(wall, "BeamHigh", new Vector3(0f, Height - 0.2f, face - 0.01f), new Vector3(halfLength * 2f, 0.2f, 0.1f), timber, false);
        foreach (float s in studs)
            Cube(wall, "Stud", new Vector3(s, Height / 2f, face - 0.01f), new Vector3(0.15f, Height, 0.1f), timber, false);
    }

    /// <summary>창틀과 투명한 유리 (겉벽 쪽에도 틀을 붙인다). yaw는 창이 바라보는 방향(방 안쪽)이다.</summary>
    private static void Window(GameObject parent, Material glass, Material timber, Vector3 pos, float yaw, bool withLight, float outer)
    {
        GameObject w = new GameObject("Window");
        w.transform.SetParent(parent.transform, false);
        w.transform.localPosition = pos;
        w.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Cube(w, "Glass", Vector3.zero, new Vector3(0.9f, 0.8f, 0.04f), glass, true);   // 눈에 안 띄는 막이 (창으로 못 나간다)
        Cube(w, "FrameTop", new Vector3(0f, 0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameBottom", new Vector3(0f, -0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameL", new Vector3(-0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "FrameR", new Vector3(0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "BarV", new Vector3(0f, 0f, -0.02f), new Vector3(0.06f, 0.8f, 0.08f), timber, false);
        Cube(w, "BarH", new Vector3(0f, 0f, -0.02f), new Vector3(0.9f, 0.06f, 0.08f), timber, false);
        // 바깥 틀
        outer += 0.02f; // 겉벽의 면과 같은 평면에 놓이지 않게
        Cube(w, "OuterTop", new Vector3(0f, 0.5f, outer), new Vector3(1.2f, 0.1f, 0.1f), timber, false);
        Cube(w, "OuterBottom", new Vector3(0f, -0.5f, outer), new Vector3(1.2f, 0.1f, 0.1f), timber, false);
        Cube(w, "OuterL", new Vector3(-0.55f, 0f, outer), new Vector3(0.1f, 1.0f, 0.1f), timber, false);
        Cube(w, "OuterR", new Vector3(0.55f, 0f, outer), new Vector3(0.1f, 1.0f, 0.1f), timber, false);
        if (withLight) PointLight(w.transform, "WindowLight", new Vector3(0f, 0f, -0.5f), new Color(1f, 0.85f, 0.6f), 0.9f, 4f);
    }

    /// <summary>벽 한 장을 창 자리마다 구멍을 내서 짓는다 (alongX: 벽이 x 방향으로 길다, fixedCoord: 벽 가운데의 z(또는 x)).</summary>
    private static void WallWithHoles(GameObject room, string name, bool alongX, float fixedCoord, float thick, float halfLen, float[] holes, Material mat)
    {
        System.Array.Sort(holes);
        float bottom = 1.75f - HoleHalfH, top = 1.75f + HoleHalfH;
        System.Action<string, float, float, float, float> piece = (n, a, b, y0, y1) =>
        {
            float mid = (a + b) * 0.5f, len = b - a, ymid = (y0 + y1) * 0.5f, ht = y1 - y0;
            if (len <= 0.001f || ht <= 0.001f) return;
            if (alongX) Cube(room, n, new Vector3(mid, ymid, fixedCoord), new Vector3(len, ht, thick), mat);
            else Cube(room, n, new Vector3(fixedCoord, ymid, mid), new Vector3(thick, ht, len), mat);
        };
        float cursor = -halfLen;
        foreach (float h in holes)
        {
            piece(name, cursor, h - HoleHalfW, 0f, Height);
            piece(name, h - HoleHalfW, h + HoleHalfW, 0f, bottom);
            piece(name, h - HoleHalfW, h + HoleHalfW, top, Height);
            cursor = h + HoleHalfW;
        }
        piece(name, cursor, halfLen, 0f, Height);
    }
    // ───────── 가구 ─────────

    private static void BuildFurniture()
    {
        GameObject root = new GameObject("Furniture");
        root.transform.SetParent(houseRoot.transform, false);
        float hx = HallHalfX, hz = HallHalfZ;

        Material wood = Mat("FurnitureWood", new Color(0.69f, 0.51f, 0.35f));
        Material darkWood = Mat("FurnitureDarkWood", new Color(0.47f, 0.33f, 0.23f));
        Material cream = Mat("FurnitureCream", new Color(0.96f, 0.93f, 0.85f));
        Material blanket = Mat("FurnitureBlanket", new Color(0.32f, 0.44f, 0.56f));
        Material pillow = Mat("FurniturePillow", new Color(0.99f, 0.97f, 0.94f));
        Material paper = Mat("FurniturePaper", new Color(1f, 0.99f, 0.95f));

        // 바닥 카펫은 두지 않는다 (맨 나무 바닥)

        // 침대 (동쪽 벽, 남쪽 절반). 바라보고 E를 누르면 잔다 (Bed).
        float bedX = hx - 0.85f, bedZ = -1.7f;
        GameObject bed = new GameObject("Bed");
        bed.transform.SetParent(root.transform, false);
        bed.AddComponent<Bed>();
        Furn(bed, "deco_furn_bed", new Vector3(bedX, 0f, bedZ), 0f);                         // 로우폴리 침대 모델 (머리판이 북쪽)
        Solid(bed, "BedBody", new Vector3(bedX, 0.30f, bedZ), new Vector3(1.1f, 0.6f, 2.1f)); // 바라보고 E, 막는 상자
        Solid(bed, "BedHead", new Vector3(bedX, 0.55f, bedZ + 1.0f), new Vector3(1.1f, 1.1f, 0.12f));

        // 책상과 편지 (서쪽 벽, 남쪽)
        float deskX = -hx + 0.7f, deskZ = -1.9f;
        Furn(root, "deco_furn_desk", new Vector3(deskX, 0f, deskZ), 0f, new Vector3(1.4f, 1f, 1.5f));                    // 로우폴리 책상 (서랍이 방 쪽)
        Solid(root, "DeskBody", new Vector3(deskX, 0.40f, deskZ), new Vector3(1.04f, 0.80f, 2.0f));
        // 책상 위의 도감: 보이는 것은 책 모델이고, 바라보고 E를 누르는 상자는 눈에 안 보이게 둔다 (DeskLetter가 도감을 연다)
        GameObject letter = Cube(root, "Letter", new Vector3(deskX, 0.80f, deskZ + 0.02f), new Vector3(0.51f, 0.01f, 0.375f), paper, false);
        Object.DestroyImmediate(letter.GetComponent<MeshRenderer>());
        MakeLetterReadable(letter);
        Furn(root, "deco_furn_book", new Vector3(deskX, 0.80f, deskZ + 0.02f), 90f, Vector3.one * 1.5f);

        // 책상 위 촛불 (모델 + 불빛)
        GameObject candle = Deco(root, "deco_candle", new Vector3(deskX, 0.79f, deskZ - 0.7f), 6f);
        PointLight(candle.transform, "CandleLight", new Vector3(0f, 0.25f, 0f), new Color(1f, 0.82f, 0.55f), 1.2f, 3.5f);

        BuildShelf(root.transform, wood, darkWood, deskX, deskZ);

        // 방 가운데는 비워 둔다: 문에서 작업대(병)로 가는 길에 탁자나 의자를 두지 않는다

        // 보관 상자와 통: 북동쪽 모퉁이, 남쪽 벽, 현관 앞방, 양동이는 북서쪽 모퉁이
        Kitpiece(root, "Crate", new Vector3(hx - 1.4f, 0f, hz - 0.7f), 15f);
        Kitpiece(root, "Crate", new Vector3(hx - 1.4f, 0.8f, hz - 0.7f), 40f);
        Kitpiece(root, "Barrel", new Vector3(hx - 0.5f, 0f, hz - 0.7f), 0f); // 상자 더미와 겹치지 않게 벽쪽으로 0.15m
        // 침대 머리맡에는 통을 두지 않는다
        // 책상 앞과 침대 앞에는 상자와 통을 두지 않는다
        // 현관 앞방(입구)에는 아무것도 두지 않는다
        Kitpiece(root, "Bucket", new Vector3(-hx + 0.6f, 0f, hz - 0.55f), 0f);

        // 천장에 매단 랜턴: 작업대 위, 현관 앞방
        GameObject lantern = Kitpiece(root, "Lantern", new Vector3(0f, 2.3f, hz - 1.6f), 0f, false);
        PointLight(lantern.transform, "LanternLight", new Vector3(0f, 0.3f, 0f), new Color(1f, 0.78f, 0.45f), 1.0f, 5f);
        GameObject entryLantern = Kitpiece(root, "Lantern", new Vector3(0f, 2.3f, VestSouthZ + 1.6f), 0f, false);
        PointLight(entryLantern.transform, "EntranceLight", new Vector3(0f, 0.3f, 0f), new Color(1f, 0.82f, 0.55f), 1.1f, 5.5f);
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

    // 선반: 서쪽 벽을 따라 긴 선반 하나. 4단 x 6칸 = 24칸이고 아래 단부터 6칸씩 열린다. (완성한 병 6개마다 한 단씩. BottleStore.ShelfCapacity)
    private const float ShelfLength = 3.1f, ShelfDepth = 0.3f;
    private static readonly float[] ShelfBoardY = { 0.25f, 0.8f, 1.35f, 1.9f, 2.45f };
    private const int ShelfRows = 4, ShelfCols = 6;

    /// <summary>선반의 판, 기둥, 뒤판과 병 칸 자리(BottleShelf)를 만든다. 병 칸 번호는 아래 단 왼쪽부터 (단 x 6 + 칸).</summary>
    private static void BuildShelf(Transform furniture, Material wood, Material darkWood, float deskX, float deskZ)
    {
        Vector3 shelfCenter = new Vector3(-HallHalfX + 0.42f, 0f, 0.8f);
        GameObject root = furniture.gameObject;
        // 로우폴리 선반 모델(판, 기둥, 뒤판, 윗 띠, 밑 받침). 판의 높이는 ShelfBoardY 그대로라 병 자리(슬롯)가 맞는다.
        Furn(root, "deco_furn_shelf", shelfCenter + new Vector3(0.01f, 0f, 0f), 0f);
        Solid(root, "ShelfBody", shelfCenter + new Vector3(0f, 1.3f, 0f), new Vector3(ShelfDepth + 0.03f, 2.6f, ShelfLength));

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
                float z = shelfCenter.z + (col - (ShelfCols - 1) / 2f) * spacing;
                slot.transform.localPosition = new Vector3(shelfCenter.x + 0.02f, ShelfBoardY[row] + 0.02f, z); // 판 윗면
                slotList.Add(slot.transform);
            }
        }
        // 선반 말고도 병을 둘 자리 5곳: 책상 위(24번), 북쪽 창턱 둘(25, 26번), 동쪽 창턱 둘(27, 28번). 선반이 안 열려도 처음부터 쓸 수 있다.
        // (BottleStore.ExtraSlotStart, ExtraSlotCount와 맞춰야 한다)
        float bedWindowZ = -1.7f;
        Furn(root, "deco_furn_sill", new Vector3(0f, 1.145f, HallHalfZ - 0.15f), 180f);   // 창턱: 받침 보가 달린 로우폴리 판
        Furn(root, "deco_furn_sill", new Vector3(HallHalfX - 0.15f, 1.145f, bedWindowZ), -90f);
        Vector3[] extra =
        {
            new Vector3(deskX, 0.79f, deskZ + 0.65f),
            new Vector3(-0.28f, 1.32f, HallHalfZ - 0.15f), new Vector3(0.28f, 1.32f, HallHalfZ - 0.15f),
            new Vector3(HallHalfX - 0.15f, 1.32f, bedWindowZ - 0.3f), new Vector3(HallHalfX - 0.15f, 1.32f, bedWindowZ + 0.3f)
        };
        for (int i = 0; i < extra.Length; i++)
        {
            GameObject slot = new GameObject("Slot" + (BottleStore.ExtraSlotStart + i));
            slot.transform.SetParent(shelfRoot.transform, false);
            slot.transform.localPosition = extra[i];
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

    private static void BuildLighting()
    {
        // 환경광은 집 안 영역(IndoorZone)이 들어올 때 바꾼다. 여기서는 불빛만.
        GameObject lights = new GameObject("Lights");
        lights.transform.SetParent(houseRoot.transform, false);
        PointLight(lights.transform, "CeilingLightA", new Vector3(-2.9f, 3.0f, -0.2f), new Color(1f, 0.9f, 0.72f), 1.6f, 8f);
        PointLight(lights.transform, "CeilingLightB", new Vector3(2.9f, 3.0f, -0.2f), new Color(1f, 0.9f, 0.72f), 1.6f, 8f);
        PointLight(lights.transform, "WorkbenchLight", new Vector3(0f, 2.2f, HallHalfZ - 1.8f), new Color(1f, 0.88f, 0.7f), 1.4f, 5f);
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

    /// <summary>가구 모델(deco_furn_*, 실제 크기)을 놓는다. 앞쪽이 +z인 모델을 yaw만큼 돌린다.</summary>
    private static GameObject Furn(GameObject parent, string model, Vector3 pos, float yaw, Vector3? scale = null)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DecoFolder + model + ".obj");
        if (prefab == null) { Debug.LogWarning("가구 모델이 없어요: " + model + " (메뉴 가구 모델 만들기 다음 장식 모델 가져오기 설정)"); return null; }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = model;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (scale.HasValue) go.transform.localScale = scale.Value;
        return go;
    }

    /// <summary>눈에 안 보이는 막는 상자 (가구 모델은 모양만 있고 충돌은 이것이 맡는다)</summary>
    private static GameObject Solid(GameObject parent, string name, Vector3 center, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = center;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = size;
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
