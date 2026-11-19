using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬의 집(Cabin, 중세 마을 팩의 Building_b)에서 문짝만 떼어 내서 열리는 문(HouseDoor)으로 만든다.
/// Building_b는 메시 하나인데, 문은 벽 구멍 안에 끼운 얇은 판(널빤지, 쇠 띠, 손잡이)이라 따로 떨어진 조각들로 되어 있다.
/// 꼭짓점 번호로 이어진 덩어리 중 문 자리 상자 안에 통째로 들어가는 것만 문짝으로 뗀다. (문틀과 벽은 상자를 벗어나서 남는다)
/// 문짝은 오른쪽(돌쩌귀가 있는 쪽)을 경첩으로 집 안쪽으로 열린다.
/// 원래 메시 에셋은 건드리지 않고, 잘라 낸 새 메시를 Assets/Models/House 에 저장해 집 오브젝트에 끼운다. 여러 번 눌러도 같은 결과가 나온다.
/// 메뉴: 유리병 정원 > 집 문 만들기
/// </summary>
public static class HouseDoorBuilder
{
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string Folder = "Assets/Models/House";
    private const string OriginalPrefab = "Assets/ThirdParty/MedievalTownExteriors/Prefabs/Building_b.prefab";

    // 문 자리 상자 (집 바깥 문 기준, 월드 좌표를 집 위치를 뺀 값). 아래 Find에서 집 위치에 맞춰 옮긴다
    private const float LeafHalfWidth = 0.71f;   // 문짝 반 폭 (x -0.68 ~ 0.70)
    private const float LeafTop = 13.85f - 11f;  // 문짝 맨 위 (바닥에서 2.85m, 위가 둥근 문)

    [MenuItem("유리병 정원/집 문 만들기")]
    public static string Build()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);

        GameObject cabin = GameObject.Find("Cabin");
        if (cabin == null) return "섬에 Cabin(집)이 없어요.";

        // 예전 문(SceneDoor 상자)과 지난번에 만든 문은 지운다
        foreach (string n in new[] { "Door", "HouseDoor" })
        {
            Transform old = FindDeep(n);
            while (old != null) { Object.DestroyImmediate(old.gameObject); old = FindDeep(n); }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPrefab);
        Mesh original = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        MeshFilter cabinFilter = cabin.GetComponent<MeshFilter>();
        if (cabinFilter == null) cabinFilter = cabin.GetComponentInChildren<MeshFilter>();
        Transform tr = cabinFilter.transform;

        Vector3[] vs = original.vertices;
        Vector3[] ns = original.normals;
        Vector2[] uv = original.uv;
        int[] tris = original.triangles;

        // 1) 꼭짓점 번호로 이어진 덩어리로 묶는다
        int[] parent = new int[vs.Length];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
        for (int t = 0; t < tris.Length; t += 3)
        {
            Union(parent, tris[t], tris[t + 1]);
            Union(parent, tris[t], tris[t + 2]);
        }
        var comps = new Dictionary<int, List<int>>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int r = Find(parent, tris[t]);
            if (!comps.ContainsKey(r)) comps[r] = new List<int>();
            comps[r].Add(t);
        }

        // 문 자리 상자: 문 바닥 높이는 집의 가장 낮은 곳이 아니라 문 앞 땅(= 문턱)이다
        Bounds house = cabinFilter.GetComponent<Renderer>().bounds;
        float doorX = 0f;                              // 문은 월드 x = 0
        float frontZ = house.min.z;                    // 집 정면 면
        float floorY = house.min.y + 0.41f;            // 집 밑동(땅에 묻힌 부분)에서 문턱까지 0.41m

        var leaf = new List<int>();   // 문짝 삼각형 시작 번호
        var rest = new List<int>();
        int leafComps = 0;
        foreach (var kv in comps)
        {
            Bounds b = new Bounds();
            bool first = true;
            foreach (int t in kv.Value)
                for (int k = 0; k < 3; k++)
                {
                    Vector3 w = tr.TransformPoint(vs[tris[t + k]]);
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
                }
            bool inside = b.min.x >= doorX - LeafHalfWidth && b.max.x <= doorX + LeafHalfWidth
                          && b.min.y >= floorY - 0.01f && b.max.y <= floorY + LeafTop
                          && b.min.z >= frontZ + 0.28f && b.max.z <= frontZ + 0.56f;
            if (inside) { leaf.AddRange(kv.Value); leafComps++; }
            else rest.AddRange(kv.Value);
        }
        if (leaf.Count == 0) return "문짝 덩어리를 찾지 못했어요.";

        System.IO.Directory.CreateDirectory(Folder);
        Mesh houseMesh = SubMesh(original, vs, ns, uv, tris, rest, null, Vector3.zero, "Building_b_nodoor");

        // 문짝 메시: 경첩(문짝 오른쪽 아래, 안쪽 면 가운데)이 원점이 되게 옮긴다 (월드 방향 그대로, 회전 없음)
        Vector3 hinge = new Vector3(doorX + LeafHalfWidth - 0.01f, floorY, frontZ + 0.42f);
        Mesh doorMesh = SubMesh(original, vs, ns, uv, tris, leaf, tr, hinge, "Building_b_door");

        AssetDatabase.CreateAsset(houseMesh, Folder + "/Building_b_nodoor.asset");
        AssetDatabase.CreateAsset(doorMesh, Folder + "/Building_b_door.asset");
        AssetDatabase.SaveAssets();
        houseMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Building_b_nodoor.asset");
        doorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Building_b_door.asset");

        // 집에 문짝 없는 메시를 끼운다 (보이는 것과 충돌 모두)
        cabinFilter.sharedMesh = houseMesh;
        foreach (MeshCollider mc in cabinFilter.GetComponents<MeshCollider>()) mc.sharedMesh = houseMesh;

        // 문 오브젝트: 경첩 자리에 놓고, 문짝 메시와 같은 재질을 쓴다
        GameObject door = new GameObject("HouseDoor");
        door.transform.SetParent(cabin.transform.parent, true);
        door.transform.position = hinge;
        door.transform.rotation = Quaternion.identity;
        door.transform.localScale = Vector3.one;
        door.AddComponent<MeshFilter>().sharedMesh = doorMesh;
        MeshRenderer mr = door.AddComponent<MeshRenderer>();
        mr.sharedMaterials = cabinFilter.GetComponent<Renderer>().sharedMaterials;
        MeshCollider col = door.AddComponent<MeshCollider>();
        col.sharedMesh = doorMesh;
        col.convex = false;

        // 문 앞에서 바라볼 때 잡히는 상자 (문이 열려 있어도 "닫기"를 누를 수 있게 늘 켜 둔다, 부딪히지는 않는다)
        GameObject trigger = new GameObject("DoorTrigger");
        trigger.transform.SetParent(door.transform, false);
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(-LeafHalfWidth, LeafTop * 0.5f, -0.42f);
        box.size = new Vector3(LeafHalfWidth * 2f, LeafTop, 0.8f);

        HouseDoor hd = door.AddComponent<HouseDoor>();
        SerializedObject so = new SerializedObject(hd);
        so.FindProperty("leafCollider").objectReferenceValue = col;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        return "문짝 덩어리 " + leafComps + "개(삼각형 " + leaf.Count / 3 + "개)를 떼어 열리는 문을 만들었어요. 문 바닥 높이 " + floorY.ToString("F2") + ", 경첩 " + hinge;
    }

    private static Mesh SubMesh(Mesh src, Vector3[] vs, Vector3[] ns, Vector2[] uv, int[] tris, List<int> triStarts, Transform toWorld, Vector3 origin, string name)
    {
        var map = new Dictionary<int, int>();
        var nv = new List<Vector3>();
        var nn = new List<Vector3>();
        var nuv = new List<Vector2>();
        var nt = new List<int>();
        foreach (int t in triStarts)
            for (int k = 0; k < 3; k++)
            {
                int old = tris[t + k];
                if (!map.TryGetValue(old, out int idx))
                {
                    idx = nv.Count;
                    map[old] = idx;
                    if (toWorld != null)
                    {
                        nv.Add(toWorld.TransformPoint(vs[old]) - origin);
                        nn.Add(ns.Length > old ? toWorld.TransformDirection(ns[old]) : Vector3.up);
                    }
                    else
                    {
                        nv.Add(vs[old]);
                        nn.Add(ns.Length > old ? ns[old] : Vector3.up);
                    }
                    nuv.Add(uv.Length > old ? uv[old] : Vector2.zero);
                }
                nt.Add(idx);
            }
        Mesh m = new Mesh { name = name };
        if (nv.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(nv);
        m.SetNormals(nn);
        m.SetUVs(0, nuv);
        m.SetTriangles(nt, 0);
        m.RecalculateBounds();
        return m;
    }

    private static Transform FindDeep(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && (t.parent == null || t.parent.name == "Cabin" || t.parent.name == "IslandContent" || t.GetComponent<SceneDoor>() != null || t.GetComponent<HouseDoor>() != null))
                return t;
        return null;
    }

    private static int Find(int[] p, int x)
    {
        while (p[x] != x) { p[x] = p[p[x]]; x = p[x]; }
        return x;
    }

    private static void Union(int[] p, int a, int b)
    {
        int ra = Find(p, a), rb = Find(p, b);
        if (ra != rb) p[rb] = ra;
    }
}
