using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 병 4종(둥근 병, 긴 병, 넓은 병, 누운 병)을 만든다.
///  1) 병 모양(유리 모델)에서 안쪽 반지름 표를 읽는다. (재료가 병 밖으로 나가지 않게 검사하는 데 쓴다)
///  2) 세운 병은 안쪽 단면에 맞춘 흙 모양을 만들고, 누운 병은 함께 온 모래 바닥 모델을 쓴다.
///  3) 병 프리팹을 만든다: 유리, 흙, 코르크 뚜껑(완성하면 보임), 병 카메라, 꾸미기 스크립트.
///  4) 병 데이터(BottleData)를 만든다.
///  5) 집 안 씬의 작업대에 네 병을 같은 자리에 겹쳐 놓는다. (고른 병만 켜진다)
/// 메뉴: 유리병 정원 > 병 만들기. 여러 번 눌러도 같은 결과가 나온다.
/// 병 크기는 아래 BottleScale 한 숫자로 바꾼다. (병만 커지고, 재료 크기는 그대로다)
/// </summary>
public static class JarSetup
{
    /// <summary>병을 몇 배로 키울지. 클수록 재료를 놓을 자리가 넓어진다.</summary>
    private const float BottleScale = 2f;

    private const string ModelFolder = "Assets/Models/Deco";
    private const string PrefabFolder = "Assets/Prefabs/Bottles";
    private const string DataFolder = "Assets/Data/Bottles";
    private const string InteriorPath = "Assets/Scenes/HutInterior.unity";
    private const string IslandPath = "Assets/Scenes/Island.unity";
    private const string RenderTexturePath = "Assets/Data/Placeholder/BottleView.renderTexture";
    private const string SoilMaterialPath = "Assets/Data/Placeholder/SoilBrown.mat";

    private const int SoilSegments = 32;
    private const float ProfileStep = 0.002f; // 안쪽 반지름 표의 간격(모델 크기 기준, 미터)

    private struct Jar
    {
        public string id, displayName, model, bottleId;
        public float soilDepth;  // 세운 병: 병 바닥 안쪽에서 흙 표면까지 높이(모델 크기 기준, 미터)
        public bool sideways;    // 누운 병 (함께 온 모래 바닥 모델 <model>_floor 를 쓴다)

        public Jar(string id, string displayName, string model, string bottleId, float soilDepth, bool sideways)
        {
            this.id = id; this.displayName = displayName; this.model = model; this.bottleId = bottleId;
            this.soilDepth = soilDepth; this.sideways = sideways;
        }
    }

    private static readonly Jar[] Jars =
    {
        new Jar("round", "둥근 병", "jar_round", "bottle0", 0.030f, false),
        new Jar("tall",  "긴 병",   "jar_tall",  "bottle1", 0.020f, false),
        new Jar("flask", "넓은 병", "jar_flask", "bottle2", 0.025f, false),
        new Jar("side",  "누운 병", "jar_side",  "bottle3", 0f,     true),
    };

    /// <summary>높이(또는 가로 위치)마다 가장 안쪽 반지름. 정점이 없는 곳은 위아래를 이어서(보간) 구한다.</summary>
    private class Profile
    {
        private readonly List<float> positions = new List<float>();
        private readonly List<float> radii = new List<float>();

        public float First => positions[0];
        public float Last => positions[positions.Count - 1];

        public Profile(Vector3[] vertices, bool alongX, Vector3 origin, float minAlong)
        {
            var levels = new SortedDictionary<int, float>();
            foreach (Vector3 p in vertices)
            {
                float along = alongX ? p.x : p.y;
                if (along < minAlong) continue; // 세운 병의 바깥 바닥 쪽은 뺀다
                Vector3 d = p - origin;
                float away = alongX ? new Vector2(d.y, d.z).magnitude : new Vector2(d.x, d.z).magnitude;
                if (away <= 0.002f) continue;   // 축 위의 정점은 뺀다
                int key = Mathf.RoundToInt(along * 10000f);
                if (!levels.TryGetValue(key, out float current) || away < current) levels[key] = away;
            }
            foreach (KeyValuePair<int, float> level in levels)
            {
                positions.Add(level.Key / 10000f);
                radii.Add(level.Value);
            }
        }

        public float Radius(float along)
        {
            int below = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i] <= along) below = i;
                if (positions[i] >= along)
                {
                    if (below < 0) return radii[i];
                    if (Mathf.Approximately(positions[i], positions[below])) return radii[below];
                    return Mathf.Lerp(radii[below], radii[i], (along - positions[below]) / (positions[i] - positions[below]));
                }
            }
            return radii[radii.Count - 1];
        }
    }

    [MenuItem("유리병 정원/병 만들기")]
    public static string Build()
    {
        int layer = UISetup.EnsureLayer(UISetup.BottleLayerName);
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(DataFolder);

        var datas = new List<BottleData>();
        foreach (Jar jar in Jars) datas.Add(BuildBottleAsset(jar, layer));
        AssetDatabase.SaveAssets();

        // 집 안 씬: 작업대 위에 병들을 놓는다
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(InteriorPath, OpenSceneMode.Single);
        GameObject bench = GameObject.Find("Workbench");
        if (bench == null) return "집 안 씬에서 Workbench를 찾을 수 없어요.";

        // 예전 임시 병과 이전에 놓은 병을 지운다
        for (int i = bench.transform.childCount - 1; i >= 0; i--)
        {
            string n = bench.transform.GetChild(i).name;
            if (n == "Bottle" || n == "DecorationCamera" || n.StartsWith("Bottle_"))
                Object.DestroyImmediate(bench.transform.GetChild(i).gameObject);
        }
        foreach (BottleContents old in Object.FindObjectsByType<BottleContents>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old != null) Object.DestroyImmediate(old.gameObject);

        // 병들을 같은 자리에 겹쳐 놓는다. (고른 병만 켜진다 — BottleStation이 맡는다)
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
        return "병 " + Jars.Length + "종 만들기 완료 (병 크기 " + BottleScale + "배)";
    }

    private static BottleData BuildBottleAsset(Jar jar, int layer)
    {
        float S = BottleScale;
        GameObject glassModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + jar.model + ".obj");
        GameObject corkModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + jar.model + "_cork.obj");
        Mesh glassMesh = glassModel.GetComponentInChildren<MeshFilter>().sharedMesh;
        Mesh corkMesh = corkModel.GetComponentInChildren<MeshFilter>().sharedMesh;
        Vector3[] glassVertices = glassMesh.vertices;

        // 병 전체(유리 + 뚜껑)의 크기와 중심 — 모델 크기 기준
        Bounds whole = glassMesh.bounds;
        whole.Encapsulate(corkMesh.bounds);

        // 축: 세운 병은 위(y), 누운 병은 가로(x). 안쪽 반지름은 이 축에서 잰다.
        bool alongX = jar.sideways;
        Vector3 axisOrigin = alongX ? new Vector3(0f, glassMesh.bounds.center.y, glassMesh.bounds.center.z) : Vector3.zero;

        // 흙 표면의 높이와 가운데 — 모델 크기 기준
        float floorY = alongX ? 0f : FindFloorY(glassMesh);
        float surfaceY;
        Vector2 surfaceCenter = Vector2.zero;
        Mesh floorMesh = null;
        if (alongX)
        {
            GameObject floorModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + jar.model + "_floor.obj");
            floorMesh = floorModel.GetComponentInChildren<MeshFilter>().sharedMesh;
            surfaceY = floorMesh.bounds.max.y;
            surfaceCenter = new Vector2(floorMesh.bounds.center.x, floorMesh.bounds.center.z);
        }
        else
        {
            surfaceY = floorY + jar.soilDepth;
        }

        // 1) 안쪽 반지름 표
        Profile profile = new Profile(glassVertices, alongX, axisOrigin, alongX ? float.MinValue : floorY - 0.0005f);
        int samples = Mathf.CeilToInt((profile.Last - profile.First) / ProfileStep) + 1;
        float[] profileRadii = new float[samples];
        for (int i = 0; i < samples; i++)
            profileRadii[i] = profile.Radius(profile.First + i * ProfileStep) * S;

        // 2) 흙 모양 (세운 병: 안쪽 단면에 맞춘 회전체 / 누운 병: 함께 온 모래 바닥 모델)
        Mesh soilMesh;
        float surfaceRadius = 0f;
        if (alongX)
        {
            soilMesh = floorMesh;
        }
        else
        {
            soilMesh = BuildSoilMesh(profile, floorY, surfaceY, S, out surfaceRadius);
            string meshPath = DataFolder + "/" + jar.id + "_soil.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(soilMesh, meshPath);
        }
        Debug.Log("[JarSetup] " + jar.id + ": 바닥 " + (floorY * S * 100f).ToString("0.0") + "cm, 흙 표면 " + (surfaceY * S * 100f).ToString("0.0") +
                  "cm, 병 크기 " + (whole.size.x * S * 100f).ToString("0") + "x" + (whole.size.y * S * 100f).ToString("0") + "cm" +
                  (alongX ? "" : ", 표면 반지름 " + (surfaceRadius * 100f).ToString("0.0") + "cm") + ", 표 " + samples + "칸");

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

        // 3) 병 프리팹
        GameObject root = new GameObject("Bottle_" + jar.id);
        root.AddComponent<BottleViewer>();

        GameObject glass = (GameObject)PrefabUtility.InstantiatePrefab(glassModel);
        glass.name = "Glass";
        glass.transform.SetParent(root.transform, false);
        glass.transform.localScale = Vector3.one * S;

        GameObject soil = new GameObject("Soil");
        soil.transform.SetParent(root.transform, false);
        soil.AddComponent<MeshFilter>().sharedMesh = soilMesh;
        MeshRenderer soilRenderer = soil.AddComponent<MeshRenderer>();
        soilRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SoilMaterialPath);
        if (alongX) soil.transform.localScale = Vector3.one * S; // 세운 병의 흙은 이미 키운 크기로 만들었다

        GameObject lid = (GameObject)PrefabUtility.InstantiatePrefab(corkModel);
        lid.name = "Lid";
        lid.transform.SetParent(root.transform, false);
        lid.transform.localScale = Vector3.one * S;
        lid.SetActive(false); // 완성하면 켜진다

        // 병 카메라: 병 전체가 한눈에 보이는 거리. 평소엔 꺼 둔다.
        Vector3 center = whole.center * S;
        float extent = Mathf.Max(whole.size.x, whole.size.y) * S; // 가장 긴 쪽
        GameObject camGo = new GameObject("BottleCamera");
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.localPosition = center + new Vector3(0f, extent * 0.1f, -extent * 1.3f);
        camGo.transform.LookAt(root.transform.TransformPoint(center));
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 8f;
        cam.cullingMask = 1 << layer;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.92f, 0.93f, 1f);
        cam.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        cam.enabled = false;

        // 바라보고 E를 누를 수 있게 병을 감싸는 상자 Collider (Is Trigger)
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = center;
        col.size = whole.size * S;

        BottleContents contents = root.AddComponent<BottleContents>();
        UISetup.SetRef(contents, "data", data);
        UISetup.SetRef(contents, "viewCamera", cam);
        UISetup.SetRef(contents, "groundRenderer", soilRenderer);
        UISetup.SetRef(contents, "lid", lid);
        UISetup.SetRef(contents, "ghostMaterial", BottleSetup.GetOrCreateGhostMaterial());
        SetString(contents, "bottleId", jar.bottleId);
        SetFloat(contents, "surfaceLocalY", surfaceY * S);
        SetBool(contents, "axisAlongX", alongX);
        SetVector3(contents, "axisOrigin", axisOrigin * S);
        SetFloat(contents, "profileStart", profile.First * S);
        SetFloat(contents, "profileStep", ProfileStep * S);
        SetFloatArray(contents, "profileRadii", profileRadii);
        SetVector2(contents, "surfaceCenter", surfaceCenter * S);
        SetVector3(contents, "zoomLookPoint", new Vector3(surfaceCenter.x * S, surfaceY * S + extent * 0.1f, surfaceCenter.y * S));

        SetLayerRecursive(root, layer);

        string prefabPath = PrefabFolder + "/Bottle_" + jar.id + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // 4) 병 데이터에 프리팹을 연결한다
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

    /// <summary>안쪽 바닥(floorY)에서 표면(surfaceY)까지, 단면에 맞는 흙 모양(회전체)을 scale배 크기로 만든다.</summary>
    private static Mesh BuildSoilMesh(Profile profile, float floorY, float surfaceY, float scale, out float surfaceRadius)
    {
        int rings = Mathf.Max(3, Mathf.CeilToInt((surfaceY - floorY) / 0.004f) + 1);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        surfaceRadius = 0f;

        // 옆면: 아래에서 위로 반지름이 병 안쪽을 따라간다 (유리에 파묻히지 않게 4% 줄인다)
        for (int i = 0; i < rings; i++)
        {
            float y = Mathf.Lerp(floorY, surfaceY, i / (float)(rings - 1));
            float r = profile.Radius(y) * 0.96f * scale;
            if (i == rings - 1) surfaceRadius = r;
            for (int s = 0; s < SoilSegments; s++)
            {
                float a = s * Mathf.PI * 2f / SoilSegments;
                vertices.Add(new Vector3(Mathf.Cos(a) * r, y * scale, Mathf.Sin(a) * r));
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
        vertices.Add(new Vector3(0f, surfaceY * scale, 0f));
        normals.Add(Vector3.up);
        int capStart = vertices.Count;
        for (int s = 0; s < SoilSegments; s++)
        {
            float a = s * Mathf.PI * 2f / SoilSegments;
            vertices.Add(new Vector3(Mathf.Cos(a) * surfaceRadius, surfaceY * scale, Mathf.Sin(a) * surfaceRadius));
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

    private static void SetBool(Object target, string field, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector3(Object target, string field, Vector3 value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).vector3Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Object target, string field, Vector2 value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloatArray(Object target, string field, float[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).floatValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }
}
