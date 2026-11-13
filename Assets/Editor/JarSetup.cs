using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 병 3종(둥근 병, 긴 병, 넓은 병)을 만든다.
///  1) 병 모양(유리 모델)의 안쪽 단면을 읽어, 병 모양에 꼭 맞는 흙 모양(메시)을 만든다.
///  2) 병 프리팹을 만든다: 유리, 흙, 코르크 뚜껑(완성하면 보임), 병 카메라, 꾸미기 스크립트.
///  3) 병 데이터(BottleData)를 만든다.
///  4) 집 안 씬의 작업대에 세 병을 놓는다. (처음부터 있는 빈 병 3개)
/// 메뉴: 유리병 정원 > 병 만들기. 여러 번 눌러도 같은 결과가 나온다.
/// 병 모양이나 흙 두께를 바꾸려면 아래 표(Jars)를 고치고 다시 누른다.
/// </summary>
public static class JarSetup
{
    private const string ModelFolder = "Assets/Models/Deco";
    private const string PrefabFolder = "Assets/Prefabs/Bottles";
    private const string DataFolder = "Assets/Data/Bottles";
    private const string InteriorPath = "Assets/Scenes/HutInterior.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string RenderTexturePath = "Assets/Data/Placeholder/BottleView.renderTexture";
    private const string SoilMaterialPath = "Assets/Data/Placeholder/SoilBrown.mat";

    private const int SoilSegments = 32;

    private struct Jar
    {
        public string id, displayName, model;
        public float soilDepth;   // 병 바닥 안쪽에서 흙 표면까지 높이(미터)
        public float tableX;      // 작업대 위 놓는 자리(가로)
        public string bottleId;   // 저장할 때 쓰는 병 이름

        public Jar(string id, string displayName, string model, float soilDepth, float tableX, string bottleId)
        {
            this.id = id; this.displayName = displayName; this.model = model;
            this.soilDepth = soilDepth; this.tableX = tableX; this.bottleId = bottleId;
        }
    }

    private static readonly Jar[] Jars =
    {
        new Jar("round", "둥근 병", "jar_round", 0.030f,  0.00f, "bottle0"),
        new Jar("tall",  "긴 병",   "jar_tall",  0.020f, -0.50f, "bottle1"),
        new Jar("flask", "넓은 병", "jar_flask", 0.025f,  0.50f, "bottle2"),
    };

    [MenuItem("유리병 정원/병 만들기")]
    public static string Build()
    {
        int layer = UISetup.EnsureLayer(UISetup.BottleLayerName);
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(DataFolder);

        var datas = new List<BottleData>();
        foreach (Jar jar in Jars) datas.Add(BuildBottleAsset(jar, layer));
        AssetDatabase.SaveAssets();

        // 집 안 씬: 작업대 위에 세 병을 놓는다
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);
        GameObject bench = GameObject.Find("Workbench");
        if (bench == null) return "집 안 씬에서 Workbench를 찾을 수 없어요.";

        // 예전 임시 병(원통)과 카메라, 이전에 놓은 병을 지운다
        for (int i = bench.transform.childCount - 1; i >= 0; i--)
        {
            string n = bench.transform.GetChild(i).name;
            if (n == "Bottle" || n == "DecorationCamera" || n.StartsWith("Bottle_"))
                Object.DestroyImmediate(bench.transform.GetChild(i).gameObject);
        }
        foreach (BottleContents old in Object.FindObjectsByType<BottleContents>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old != null) Object.DestroyImmediate(old.gameObject);

        // 세 병을 같은 자리에 겹쳐 놓는다. (고른 병만 켜진다 — BottleStation이 맡는다)
        var contentsList = new List<BottleContents>();
        for (int i = 0; i < Jars.Length; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(datas[i].prefab, bench.scene);
            instance.transform.SetParent(bench.transform, false);
            instance.transform.localPosition = new Vector3(0f, 1f, 0f); // 작업대 상판 높이가 1m
            instance.name = "Bottle_" + Jars[i].id;
            BottleContents bc = instance.GetComponent<BottleContents>();
            SetString(bc, "bottleId", Jars[i].bottleId);
            contentsList.Add(bc);
        }

        // 작업대: 바라보고 E를 누르면 병 꾸미기 패널이 열린다
        BottleStation station = bench.GetComponent<BottleStation>();
        if (station == null) station = bench.AddComponent<BottleStation>();
        SerializedObject so = new SerializedObject(station);
        SerializedProperty arr = so.FindProperty("bottles");
        arr.arraySize = contentsList.Count;
        for (int i = 0; i < contentsList.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = contentsList[i];
        so.FindProperty("selectedIndex").intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(bench.scene);
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(IslandPath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        return "병 " + Jars.Length + "종 만들기 완료";
    }

    private static BottleData BuildBottleAsset(Jar jar, int layer)
    {
        GameObject glassModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + jar.model + ".obj");
        GameObject corkModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + jar.model + "_cork.obj");
        Mesh glassMesh = glassModel.GetComponentInChildren<MeshFilter>().sharedMesh;
        Bounds glassBounds = glassMesh.bounds;
        float top = corkModel.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.max.y; // 뚜껑 윗면 = 병 전체 높이

        // 1) 흙 모양: 안쪽 바닥에서 흙 표면까지, 병 안쪽 단면에 맞춘다
        float floorY = FindFloorY(glassMesh);
        float surfaceY = floorY + jar.soilDepth;
        Mesh soilMesh = BuildSoilMesh(glassMesh, floorY, surfaceY, out float surfaceRadius);
        Debug.Log("[JarSetup] " + jar.id + ": 바닥 " + (floorY * 100f).ToString("0.0") + "cm, 흙 표면 " + (surfaceY * 100f).ToString("0.0") +
                  "cm, 표면 반지름 " + (surfaceRadius * 100f).ToString("0.0") + "cm");
        string meshPath = DataFolder + "/" + jar.id + "_soil.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(soilMesh, meshPath);

        // 병 데이터 (프리팹은 아래에서 만든 뒤 연결한다)
        string dataPath = DataFolder + "/bottle_" + jar.id + ".asset";
        BottleData data = AssetDatabase.LoadAssetAtPath<BottleData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BottleData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        data.id = jar.id;
        data.displayName = jar.displayName;

        // 2) 병 프리팹
        GameObject root = new GameObject("Bottle_" + jar.id);
        root.AddComponent<BottleViewer>();

        GameObject glass = (GameObject)PrefabUtility.InstantiatePrefab(glassModel);
        glass.name = "Glass";
        glass.transform.SetParent(root.transform, false);

        GameObject soil = new GameObject("Soil");
        soil.transform.SetParent(root.transform, false);
        soil.AddComponent<MeshFilter>().sharedMesh = soilMesh;
        MeshRenderer soilRenderer = soil.AddComponent<MeshRenderer>();
        soilRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SoilMaterialPath);

        GameObject lid = (GameObject)PrefabUtility.InstantiatePrefab(corkModel);
        lid.name = "Lid";
        lid.transform.SetParent(root.transform, false);
        lid.SetActive(false); // 완성하면 켜진다

        // 병 카메라: 병 전체가 한눈에 보이는 거리. 평소엔 꺼 둔다.
        float centerY = top * 0.5f;
        float distance = top * 1.3f; // 아래가 넓은 병도 화면에서 잘리지 않을 만큼 물러선다
        GameObject camGo = new GameObject("BottleCamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = new Vector3(0f, centerY + top * 0.1f, -distance);
        camGo.transform.LookAt(root.transform.TransformPoint(0f, centerY, 0f));
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 5f;
        cam.cullingMask = 1 << layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.92f, 0.93f, 1f);
        cam.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        cam.enabled = false;

        // 바라보고 E를 누를 수 있게 병을 감싸는 Collider (Is Trigger)
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = glassBounds.extents.x;
        col.height = top;
        col.center = new Vector3(0f, top * 0.5f, 0f);

        BottleContents contents = root.AddComponent<BottleContents>();

        UISetup.SetRef(contents, "data", data);
        UISetup.SetRef(contents, "viewCamera", cam);
        UISetup.SetRef(contents, "groundRenderer", soilRenderer);
        UISetup.SetRef(contents, "lid", lid);
        UISetup.SetRef(contents, "ghostMaterial", BottleSetup.GetOrCreateGhostMaterial());
        SetFloat(contents, "surfaceLocalY", surfaceY);
        SetFloat(contents, "soilRadius", surfaceRadius * 0.92f); // 유리에 너무 붙지 않게 조금 안쪽까지만
        SetFloat(contents, "zoomLookHeight", surfaceY + top * 0.1f);
        SetString(contents, "bottleId", jar.bottleId);

        SetLayerRecursive(root, layer);

        string prefabPath = PrefabFolder + "/Bottle_" + jar.id + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // 3) 병 데이터에 프리팹을 연결한다
        data.prefab = prefab;
        EditorUtility.SetDirty(data);
        return data;
    }

    /// <summary>병 안쪽 바닥의 높이. 중심축에 있는 정점 중 맨 아래(바깥 바닥)를 뺀 가장 낮은 것.</summary>
    private static float FindFloorY(Mesh mesh)
    {
        float floor = float.MaxValue;
        foreach (Vector3 p in mesh.vertices)
            if (new Vector2(p.x, p.z).magnitude < 0.0005f && p.y > 0.001f && p.y < floor) floor = p.y;
        return floor == float.MaxValue ? 0.004f : floor;
    }

    /// <summary>
    /// 병 모델의 높이 y에서의 안쪽 반지름.
    /// 같은 높이의 정점들 중 가장 안쪽 반지름을 "단면"으로 보고, 곧게 뻗은 벽처럼 중간에 정점이 없는
    /// 높이는 바로 위아래 단면 사이를 이어서(보간) 구한다.
    /// </summary>
    private static float InteriorRadius(Vector3[] vertices, float y, float floorY)
    {
        // 높이(0.1mm 단위)마다 가장 안쪽 반지름
        var levels = new SortedDictionary<int, float>();
        foreach (Vector3 p in vertices)
        {
            if (p.y < floorY - 0.0005f) continue; // 병 바깥 바닥 쪽은 뺀다
            float r = new Vector2(p.x, p.z).magnitude;
            if (r <= 0.002f) continue;            // 중심축 정점은 뺀다
            int key = Mathf.RoundToInt(p.y * 10000f);
            if (!levels.TryGetValue(key, out float current) || r < current) levels[key] = r;
        }

        float below = float.NaN, above = float.NaN, belowY = 0f, aboveY = 0f;
        foreach (KeyValuePair<int, float> level in levels)
        {
            float ly = level.Key / 10000f;
            if (ly <= y) { below = level.Value; belowY = ly; }
            if (ly >= y) { above = level.Value; aboveY = ly; break; }
        }
        if (float.IsNaN(below)) return above;
        if (float.IsNaN(above) || Mathf.Approximately(aboveY, belowY)) return below;
        return Mathf.Lerp(below, above, (y - belowY) / (aboveY - belowY));
    }

    /// <summary>안쪽 바닥(floorY)에서 표면(surfaceY)까지, 단면에 맞는 흙 모양(회전체)을 만든다.</summary>
    private static Mesh BuildSoilMesh(Mesh glassMesh, float floorY, float surfaceY, out float surfaceRadius)
    {
        Vector3[] glassVertices = glassMesh.vertices;
        int rings = Mathf.Max(3, Mathf.CeilToInt((surfaceY - floorY) / 0.004f) + 1);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        surfaceRadius = 0f;

        // 옆면: 아래에서 위로 반지름이 병 안쪽을 따라간다 (유리에 파묻히지 않게 4% 줄인다)
        for (int i = 0; i < rings; i++)
        {
            float y = Mathf.Lerp(floorY, surfaceY, i / (float)(rings - 1));
            float r = InteriorRadius(glassVertices, y, floorY) * 0.96f;
            if (i == rings - 1) surfaceRadius = r;
            for (int s = 0; s < SoilSegments; s++)
            {
                float a = s * Mathf.PI * 2f / SoilSegments;
                vertices.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                normals.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
            }
        }
        for (int i = 0; i < rings - 1; i++)
        {
            for (int s = 0; s < SoilSegments; s++)
            {
                int s1 = (s + 1) % SoilSegments;
                int v00 = i * SoilSegments + s, v01 = i * SoilSegments + s1;
                int v10 = (i + 1) * SoilSegments + s, v11 = (i + 1) * SoilSegments + s1;
                triangles.Add(v00); triangles.Add(v10); triangles.Add(v01);
                triangles.Add(v01); triangles.Add(v10); triangles.Add(v11);
            }
        }

        // 윗면(평평, 위를 향함): 가운데 점에서 부채꼴로
        int centerIndex = vertices.Count;
        vertices.Add(new Vector3(0f, surfaceY, 0f));
        normals.Add(Vector3.up);
        int capStart = vertices.Count;
        for (int s = 0; s < SoilSegments; s++)
        {
            float a = s * Mathf.PI * 2f / SoilSegments;
            vertices.Add(new Vector3(Mathf.Cos(a) * surfaceRadius, surfaceY, Mathf.Sin(a) * surfaceRadius));
            normals.Add(Vector3.up);
        }
        for (int s = 0; s < SoilSegments; s++)
        {
            int s1 = (s + 1) % SoilSegments;
            triangles.Add(centerIndex); triangles.Add(capStart + s1); triangles.Add(capStart + s);
        }

        Mesh mesh = new Mesh { name = "SoilMesh" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetFloat(Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }
}
