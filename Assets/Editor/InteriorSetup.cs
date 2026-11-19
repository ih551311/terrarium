using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬 씬(Island) 안, 집(Cabin) 속에 방을 짓는다. 집 안은 더 이상 따로 떨어진 씬이 아니다.
/// 바깥 집(중세 마을 팩의 Building_b)은 속이 빈 껍데기라서, 그 안쪽 바닥에 방(HouseInterior)을 만든다.
/// 바닥, 벽, 창, 지붕 속 천장과 가구(침대, 책상과 편지, 선반, 보관 상자, 통, 러그, 탁자, 촛불)를 놓고, 집 안 영역(IndoorZone)을 둔다.
/// 작업대(Workbench)와 그 위의 병들은 새로 만들지 않고 섬에 있는 것을 방 안으로 옮겨 놓는다. (병 만들기 메뉴가 작업대의 병을 만든다)
/// 문은 HouseDoorBuilder가 만든 열리는 현관문(HouseDoor)이다. 이 방의 남쪽 벽은 그 문 자리를 비워 둔다.
/// 메뉴: 유리병 정원 > 집 안 꾸미기 (먼저 집 문 만들기). 여러 번 눌러도 같은 결과가 나온다.
/// </summary>
public static class InteriorSetup
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string Kit = "Assets/ThirdParty/MedievalTownExteriors/Prefabs/";
    private const string DecoFolder = "Assets/Models/Deco/";

    // 방 크기: 안쪽 반 폭(동서), 반 깊이(남북). 바깥 집 안쪽(12m x 9.6m)에 꼭 맞게 들어간다.
    private const float HalfX = 5.4f;
    private const float HalfZ = 4.4f;
    private const float Height = 3.2f;
    private const float DoorWidth = 1.36f;   // 바깥 문 구멍(x -0.68 ~ 0.68)과 같다
    private const float DoorHeight = 2.5f;
    private const float WallThickness = 0.2f;

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
        foreach (string n in new[] { "HouseInterior" })
        {
            GameObject old = GameObject.Find(n);
            while (old != null) { Object.DestroyImmediate(old); old = GameObject.Find(n); }
        }

        // 방 뿌리: 문턱 높이(집 밑동 + 0.41 + 0.02), 문(월드 x = 0) 바로 안쪽에서 남쪽 안벽이 시작한다
        float southInnerZ = house.min.z + 0.65f;      // 문 두께 바로 뒤 (문짝이 놓인 벽 안쪽 면)
        houseRoot = new GameObject("HouseInterior");
        houseRoot.transform.position = new Vector3(0f, house.min.y + 0.43f, southInnerZ + HalfZ);
        houseRoot.transform.rotation = Quaternion.identity;

        BuildRoom();

        // 작업대는 북쪽 벽 앞에
        bench.transform.SetParent(houseRoot.transform, false);
        bench.transform.localPosition = new Vector3(0f, 0f, HalfZ - 1.2f);
        bench.transform.localRotation = Quaternion.identity;

        BuildFurniture();
        BuildLighting();

        // 집 안 영역
        IndoorZone zone = houseRoot.AddComponent<IndoorZone>();
        SerializedObject zso = new SerializedObject(zone);
        zso.FindProperty("center").vector3Value = new Vector3(0f, Height * 0.5f, 0f);
        zso.FindProperty("size").vector3Value = new Vector3(HalfX * 2f + 0.2f, Height + 0.4f, HalfZ * 2f + 0.2f);
        zso.FindProperty("indoorSky").colorValue = new Color(0.68f, 0.62f, 0.55f);
        zso.FindProperty("indoorEquator").colorValue = new Color(0.64f, 0.58f, 0.51f);
        zso.FindProperty("indoorGround").colorValue = new Color(0.50f, 0.44f, 0.37f);
        zso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "집 안 꾸미기 완료 (방 " + (HalfX * 2f) + "m x " + (HalfZ * 2f) + "m, 뿌리 " + houseRoot.transform.position + ")";
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
        float outerX = HalfX + t / 2f, outerZ = HalfZ + t / 2f; // 벽 중심

        // 바닥과 천장 (천장은 충돌 없음)
        Cube(room, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(HalfX * 2f + 0.6f, 0.2f, HalfZ * 2f + 0.6f), floorMat);
        Cube(room, "Ceiling", new Vector3(0f, Height + 0.1f, 0f), new Vector3(HalfX * 2f + 0.6f, 0.2f, HalfZ * 2f + 0.6f), ceilingMat, false);
        foreach (float z in new[] { -3.2f, -1.07f, 1.07f, 3.2f })
            Cube(room, "CeilingBeam", new Vector3(0f, Height - 0.1f, z), new Vector3(HalfX * 2f, 0.2f, 0.25f), timber, false);

        // 벽: 북, 동, 서는 통벽. 남쪽은 현관문 자리를 비운다
        float wallLenX = HalfX * 2f + t * 2f, wallLenZ = HalfZ * 2f + t * 2f;
        Cube(room, "WallNorth", new Vector3(0f, Height / 2f, outerZ), new Vector3(wallLenX, Height, t), plaster);
        Cube(room, "WallEast", new Vector3(outerX, Height / 2f, 0f), new Vector3(t, Height, wallLenZ), plaster);
        Cube(room, "WallWest", new Vector3(-outerX, Height / 2f, 0f), new Vector3(t, Height, wallLenZ), plaster);
        float sideLen = HalfX - DoorWidth / 2f + t;
        float sideCenter = DoorWidth / 2f + sideLen / 2f;
        Cube(room, "WallSouthL", new Vector3(-sideCenter, Height / 2f, -outerZ), new Vector3(sideLen, Height, t), plaster);
        Cube(room, "WallSouthR", new Vector3(sideCenter, Height / 2f, -outerZ), new Vector3(sideLen, Height, t), plaster);
        Cube(room, "DoorLintel", new Vector3(0f, (Height + DoorHeight) / 2f, -outerZ), new Vector3(DoorWidth, Height - DoorHeight, t), plaster);

        // 벽 안쪽 장식: 돌 밑단, 가로 보, 모서리 기둥, 세로 기둥
        foreach (int side in new[] { 0, 1, 2, 3 }) // 0 북, 1 동, 2 남, 3 서
        {
            bool alongX = side == 0 || side == 2;
            float length = alongX ? HalfX : HalfZ;       // 벽 반 길이
            float face = (alongX ? HalfZ : HalfX) - 0.02f; // 벽 안쪽 면까지의 거리
            Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
            GameObject wall = new GameObject("WallTrim" + side);
            wall.transform.SetParent(room.transform, false);
            wall.transform.rotation = rot;
            // 이 벽을 바라볼 때 +z가 벽 쪽: 벽의 안쪽 면은 z = face
            if (side == 2)
            {
                // 남쪽 벽은 문 자리를 비운다 (돌 밑단과 보가 문 아래쪽을 가리지 않게 문틀 양옆만 놓는다)
                float gap = DoorWidth / 2f + 0.06f;
                float segLen = length - gap, segCenter = gap + segLen / 2f;
                foreach (float sx in new[] { -segCenter, segCenter })
                {
                    Cube(wall, "StoneBase", new Vector3(sx, 0.45f, face), new Vector3(segLen, 0.9f, 0.05f), stone, false);
                    Cube(wall, "BeamLow", new Vector3(sx, 0.93f, face - 0.01f), new Vector3(segLen, 0.12f, 0.1f), timber, false);
                }
            }
            else
            {
                Cube(wall, "StoneBase", new Vector3(0f, 0.45f, face), new Vector3(length * 2f, 0.9f, 0.05f), stone, false);
                Cube(wall, "BeamLow", new Vector3(0f, 0.93f, face - 0.01f), new Vector3(length * 2f, 0.12f, 0.1f), timber, false);
            }
            Cube(wall, "BeamHigh", new Vector3(0f, Height - 0.2f, face - 0.01f), new Vector3(length * 2f, 0.2f, 0.1f), timber, false);
            float studAt = alongX ? 3.4f : 2.9f; // 창과 문 자리를 피해 양쪽에 세로 기둥
            foreach (float s in new[] { -studAt, studAt })
                Cube(wall, "Stud", new Vector3(s, Height / 2f, face - 0.01f), new Vector3(0.15f, Height, 0.1f), timber, false);
        }
        foreach (Vector2 c in new[] { new Vector2(-HalfX, -HalfZ), new Vector2(HalfX, -HalfZ), new Vector2(-HalfX, HalfZ), new Vector2(HalfX, HalfZ) })
            Cube(room, "CornerPost", new Vector3(c.x - Mathf.Sign(c.x) * 0.1f, Height / 2f, c.y - Mathf.Sign(c.y) * 0.1f), new Vector3(0.25f, Height, 0.25f), timber, false);

        // 창: 북쪽(작업대 뒤)과 그 양옆, 동쪽(침대 위), 서쪽(책상 위), 남쪽 문 양옆. 따뜻한 빛이 들어오는 느낌
        Window(room, glass, timber, new Vector3(0f, 1.75f, HalfZ - 0.03f), 0f, true);
        Window(room, glass, timber, new Vector3(-3.3f, 1.75f, HalfZ - 0.03f), 0f, false);
        Window(room, glass, timber, new Vector3(3.3f, 1.75f, HalfZ - 0.03f), 0f, false);
        Window(room, glass, timber, new Vector3(HalfX - 0.03f, 1.75f, -1.1f), 90f, true);
        Window(room, glass, timber, new Vector3(-HalfX + 0.03f, 1.75f, -1.2f), 270f, true);
        Window(room, glass, timber, new Vector3(-3.0f, 1.75f, -HalfZ + 0.03f), 180f, false);
        Window(room, glass, timber, new Vector3(3.0f, 1.75f, -HalfZ + 0.03f), 180f, false);
    }

    /// <summary>창틀과 따뜻하게 빛나는 유리. yaw는 창이 바라보는 방향(방 안쪽)이다.</summary>
    private static void Window(GameObject parent, Material glass, Material timber, Vector3 pos, float yaw, bool withLight)
    {
        GameObject w = new GameObject("Window");
        w.transform.SetParent(parent.transform, false);
        w.transform.localPosition = pos;
        w.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Cube(w, "Glass", Vector3.zero, new Vector3(0.9f, 0.8f, 0.04f), glass, false);
        Cube(w, "FrameTop", new Vector3(0f, 0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameBottom", new Vector3(0f, -0.45f, -0.02f), new Vector3(1.1f, 0.1f, 0.12f), timber, false);
        Cube(w, "FrameL", new Vector3(-0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "FrameR", new Vector3(0.5f, 0f, -0.02f), new Vector3(0.1f, 0.9f, 0.12f), timber, false);
        Cube(w, "BarV", new Vector3(0f, 0f, -0.02f), new Vector3(0.06f, 0.8f, 0.08f), timber, false);
        Cube(w, "BarH", new Vector3(0f, 0f, -0.02f), new Vector3(0.9f, 0.06f, 0.08f), timber, false);
        if (withLight) PointLight(w.transform, "WindowLight", new Vector3(0f, 0f, -0.5f), new Color(1f, 0.85f, 0.6f), 0.9f, 4f);
    }

    // ───────── 가구 ─────────

    private static void BuildFurniture()
    {
        GameObject root = new GameObject("Furniture");
        root.transform.SetParent(houseRoot.transform, false);

        Material wood = Mat("FurnitureWood", new Color(0.69f, 0.51f, 0.35f));
        Material darkWood = Mat("FurnitureDarkWood", new Color(0.47f, 0.33f, 0.23f));
        Material cream = Mat("FurnitureCream", new Color(0.96f, 0.93f, 0.85f));
        Material blanket = Mat("FurnitureBlanket", new Color(0.32f, 0.44f, 0.56f));
        Material pillow = Mat("FurniturePillow", new Color(0.99f, 0.97f, 0.94f));
        Material rugMat = Mat("FurnitureRug", new Color(0.6f, 0.3f, 0.27f));
        Material rugMat2 = Mat("FurnitureRug2", new Color(0.36f, 0.45f, 0.40f));
        Material paper = Mat("FurniturePaper", new Color(1f, 0.99f, 0.95f));

        // 러그: 방 가운데와 작업대 앞
        Cylinder(root, "Rug", new Vector3(0f, 0.012f, -1.3f), new Vector3(3.4f, 0.012f, 3.4f), rugMat, false);
        Cylinder(root, "RugWorkbench", new Vector3(0f, 0.014f, 1.6f), new Vector3(2.0f, 0.012f, 1.4f), rugMat2, false);

        // 침대 (동쪽 벽, 남쪽 절반). 바라보고 E를 누르면 잔다 (Bed).
        float bedX = HalfX - 0.85f;
        GameObject bed = new GameObject("Bed");
        bed.transform.SetParent(root.transform, false);
        bed.AddComponent<Bed>();
        Cube(bed, "BedFrame", new Vector3(bedX, 0.18f, -1.1f), new Vector3(1.1f, 0.36f, 2.1f), darkWood);
        Cube(bed, "BedMattress", new Vector3(bedX, 0.46f, -1.1f), new Vector3(1.0f, 0.2f, 2.0f), cream);
        Cube(bed, "BedBlanket", new Vector3(bedX, 0.6f, -1.55f), new Vector3(1.04f, 0.1f, 1.2f), blanket);
        Cube(bed, "BedPillow", new Vector3(bedX, 0.62f, -0.35f), new Vector3(0.8f, 0.12f, 0.4f), pillow);
        Cube(bed, "BedHeadboard", new Vector3(bedX, 0.55f, 0.0f), new Vector3(1.1f, 0.9f, 0.08f), wood);
        // 침대 옆 작은 상자
        Cube(root, "BedsideTable", new Vector3(bedX, 0.25f, 0.45f), new Vector3(0.5f, 0.5f, 0.45f), wood);

        // 책상과 편지 (서쪽 벽, 남쪽)
        float deskX = -HalfX + 0.7f;
        Cube(root, "DeskTop", new Vector3(deskX, 0.76f, -1.7f), new Vector3(0.7f, 0.06f, 1.3f), wood);
        foreach (Vector2 leg in new[] { new Vector2(deskX - 0.28f, -2.28f), new Vector2(deskX + 0.28f, -2.28f), new Vector2(deskX - 0.28f, -1.12f), new Vector2(deskX + 0.28f, -1.12f) })
            Cube(root, "DeskLeg", new Vector3(leg.x, 0.37f, leg.y), new Vector3(0.06f, 0.74f, 0.06f), darkWood);
        MakeLetterReadable(Cube(root, "Letter", new Vector3(deskX, 0.795f, -1.7f), new Vector3(0.3f, 0.006f, 0.21f), paper, false));
        Cube(root, "DeskStool", new Vector3(deskX + 0.95f, 0.22f, -1.7f), new Vector3(0.4f, 0.44f, 0.4f), darkWood);

        // 책상 위 촛불 (모델 + 불빛)
        GameObject candle = Deco(root, "deco_candle", new Vector3(deskX, 0.79f, -2.1f), 6f);
        PointLight(candle.transform, "CandleLight", new Vector3(0f, 0.25f, 0f), new Color(1f, 0.82f, 0.55f), 1.2f, 3.5f);

        BuildShelf(root.transform, wood, darkWood);

        // 방 가운데 탁자와 의자 둘
        Cube(root, "TableTop", new Vector3(2.2f, 0.74f, 0.9f), new Vector3(1.5f, 0.07f, 0.9f), wood);
        foreach (Vector2 leg in new[] { new Vector2(1.55f, 0.55f), new Vector2(2.85f, 0.55f), new Vector2(1.55f, 1.25f), new Vector2(2.85f, 1.25f) })
            Cube(root, "TableLeg", new Vector3(leg.x, 0.36f, leg.y), new Vector3(0.07f, 0.72f, 0.07f), darkWood);
        Cube(root, "StoolA", new Vector3(1.8f, 0.22f, 1.65f), new Vector3(0.4f, 0.44f, 0.4f), darkWood);
        Cube(root, "StoolB", new Vector3(2.6f, 0.22f, 1.7f), new Vector3(0.4f, 0.44f, 0.4f), darkWood);
        GameObject tableCandle = Deco(root, "deco_candle", new Vector3(2.2f, 0.775f, 0.9f), 6f);
        PointLight(tableCandle.transform, "TableCandleLight", new Vector3(0f, 0.25f, 0f), new Color(1f, 0.82f, 0.55f), 0.9f, 3.5f);

        // 보관 상자와 통 (북동쪽 모퉁이, 남쪽 벽 서쪽)
        Kitpiece(root, "Crate", new Vector3(HalfX - 1.4f, 0f, HalfZ - 0.7f), 15f);
        Kitpiece(root, "Crate", new Vector3(HalfX - 1.4f, 0.8f, HalfZ - 0.7f), 40f);
        Kitpiece(root, "Barrel", new Vector3(HalfX - 0.65f, 0f, HalfZ - 0.7f), 0f);
        Kitpiece(root, "Barrel", new Vector3(HalfX - 0.6f, 0f, HalfZ - 1.75f), 40f);
        Kitpiece(root, "Crate", new Vector3(-2.2f, 0f, -HalfZ + 0.55f), 80f);
        Kitpiece(root, "Barrel", new Vector3(2.2f, 0f, -HalfZ + 0.55f), 20f);
        Kitpiece(root, "Bucket", new Vector3(-HalfX + 0.6f, 0f, HalfZ - 0.55f), 0f);

        // 천장에 매단 랜턴 (작업대 위와 방 가운데)
        GameObject lantern = Kitpiece(root, "Lantern", new Vector3(0f, 2.3f, HalfZ - 2.0f), 0f, false);
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

    // 선반: 서쪽 벽을 따라 긴 선반 하나. 4단 x 6칸 = 24칸이고 아래 단부터 6칸씩 열린다. (완성한 병 6개마다 한 단씩. BottleStore.ShelfCapacity)
    private const float ShelfLength = 3.1f, ShelfDepth = 0.3f;
    private static readonly float[] ShelfBoardY = { 0.25f, 0.8f, 1.35f, 1.9f, 2.45f };
    private const int ShelfRows = 4, ShelfCols = 6;

    /// <summary>선반의 판, 기둥, 뒤판과 병 칸 자리(BottleShelf)를 만든다. 병 칸 번호는 아래 단 왼쪽부터 (단 x 6 + 칸).</summary>
    private static void BuildShelf(Transform furniture, Material wood, Material darkWood)
    {
        Vector3 shelfCenter = new Vector3(-HalfX + 0.42f, 0f, 1.6f);
        GameObject root = furniture.gameObject;
        foreach (float y in ShelfBoardY)
            Cube(root, "ShelfBoard", shelfCenter + new Vector3(0f, y, 0f), new Vector3(ShelfDepth, 0.04f, ShelfLength), wood);
        foreach (float z in new[] { -ShelfLength / 2f + 0.03f, 0f, ShelfLength / 2f - 0.03f })
            Cube(root, "ShelfPost", shelfCenter + new Vector3(0f, 1.27f, z), new Vector3(ShelfDepth, 2.5f, 0.05f), darkWood);
        Cube(root, "ShelfBack", shelfCenter + new Vector3(-0.13f, 1.27f, 0f), new Vector3(0.03f, 2.5f, ShelfLength), darkWood, false);

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
        float deskX = -HalfX + 0.7f;
        Cube(root, "SillBoard", new Vector3(0f, 1.30f, HalfZ - 0.15f), new Vector3(1.25f, 0.04f, 0.26f), wood, false);
        Cube(root, "SillBoard", new Vector3(HalfX - 0.15f, 1.30f, -1.1f), new Vector3(0.26f, 0.04f, 1.25f), wood, false);
        Vector3[] extra =
        {
            new Vector3(deskX, 0.79f, -1.25f),
            new Vector3(-0.28f, 1.32f, HalfZ - 0.15f), new Vector3(0.28f, 1.32f, HalfZ - 0.15f),
            new Vector3(HalfX - 0.15f, 1.32f, -1.4f), new Vector3(HalfX - 0.15f, 1.32f, -0.8f)
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
        PointLight(lights.transform, "CeilingLightA", new Vector3(-2.7f, 3.0f, -0.6f), new Color(1f, 0.9f, 0.72f), 1.6f, 8f);
        PointLight(lights.transform, "CeilingLightB", new Vector3(2.7f, 3.0f, -0.6f), new Color(1f, 0.9f, 0.72f), 1.6f, 8f);
        PointLight(lights.transform, "WorkbenchLight", new Vector3(0f, 2.2f, HalfZ - 2.6f), new Color(1f, 0.88f, 0.7f), 1.4f, 5f);
        PointLight(lights.transform, "EntranceLight", new Vector3(0f, 2.5f, -HalfZ + 1.0f), new Color(1f, 0.85f, 0.62f), 1.1f, 5.5f); // 문 안쪽: 밤에도 들어서는 곳이 어둡지 않게
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
