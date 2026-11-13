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
    private const float ViewTilt = 28f; // 서 있는 병을 카메라 쪽으로 기울이는 각도(도)
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
        new Jar("round", "둥근 병", "jar_round", "bottle0", 0.036f, false),
        new Jar("tall",  "긴 병",   "jar_tall",  "bottle1", 0.028f, false),
        new Jar("flask", "넓은 병", "jar_flask", "bottle2", 0.034f, false),
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
            surfaceY = floorMesh.bounds.max.y + 0.012f; // 둥근 안쪽을 따라 채우므로 예전 판판한 모래판보다 조금 더 두껍게
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
        float seed = jar.id[0] * 0.37f; // 병마다 굴곡 모양이 다르게
        if (alongX)
        {
            soilMesh = BuildSideSoilMesh(profile, axisOrigin, surfaceY, seed);
        }
        else
        {
            soilMesh = BuildSoilMesh(profile, floorY, surfaceY, S, seed, out surfaceRadius);
        }
        string meshPath = DataFolder + "/" + jar.id + "_soil.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(soilMesh, meshPath);
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
        BottleViewer viewer = root.AddComponent<BottleViewer>();
        SetFloat(viewer, "tiltDegrees", alongX ? 0f : ViewTilt); // 서 있는 병은 입구를 카메라 쪽으로 살짝 기울여 흙 윗면이 보이게

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

    /// <summary>흙 윗면의 굴곡(언덕과 골). 모델 크기 기준 좌표를 받아 높이 차(모델 크기 기준)를 돌려준다.</summary>
    private static float Bump(float x, float z, float seed)
    {
        const float Amp = 0.0028f;
        return Amp * (Mathf.Sin(x * 85f + seed) * Mathf.Cos(z * 75f + seed * 1.7f) * 0.6f + Mathf.Sin(x * 47f - z * 61f + seed * 0.5f) * 0.4f);
    }

    /// <summary>
    /// 세운 병의 흙: 안쪽 단면을 따라가는 회전체. 바닥 모서리는 둥글게, 윗면은 언덕과 골이 있고 가장자리는 살짝 솟는다.
    /// 안쪽 바닥(floorY)에서 표면(surfaceY)까지, scale배 크기로 만든다. surfaceY는 굴곡의 평균 높이(재료를 놓는 기준)다.
    /// </summary>
    private static Mesh BuildSoilMesh(Profile profile, float floorY, float surfaceY, float scale, float seed, out float surfaceRadius)
    {
        const int CapRings = 6;
        const float EdgeRise = 0.0035f;   // 가장자리가 유리를 타고 살짝 올라온 높이
        const float RoundHeight = 0.012f; // 바닥 모서리를 둥글게 깎는 높이

        int rings = Mathf.Max(4, Mathf.CeilToInt((surfaceY - floorY) / 0.003f) + 1);

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        surfaceRadius = 0f;

        // 윗면 높이 (rf: 0 가운데 ~ 1 가장자리)
        System.Func<float, float, float, float> top = (rf, px, pz) =>
            surfaceY + Bump(px, pz, seed) * (1f - 0.5f * rf) + EdgeRise * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, rf));

        // 옆면: 아래에서 위로 반지름이 병 안쪽을 따라간다 (유리에 파묻히지 않게 2% 줄이고, 바닥 모서리는 둥글게)
        for (int i = 0; i < rings; i++)
        {
            float y = Mathf.Lerp(floorY, surfaceY, i / (float)(rings - 1));
            float round = Mathf.Clamp01((y - floorY) / RoundHeight);
            float r = profile.Radius(y) * 0.98f * Mathf.Lerp(0.45f, 1f, Mathf.Sqrt(1f - (1f - round) * (1f - round)));
            if (i == rings - 1) surfaceRadius = r;
            for (int s = 0; s < SoilSegments; s++)
            {
                float a = s * Mathf.PI * 2f / SoilSegments;
                float px = Mathf.Cos(a) * r, pz = Mathf.Sin(a) * r;
                float vy = i == rings - 1 ? top(1f, px, pz) : y;
                vertices.Add(new Vector3(px * scale, vy * scale, pz * scale));
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

        // 윗면: 바깥 고리에서 가운데로 겹겹이 (옆면과 따로 만들어 가장자리에 살짝 모서리가 생긴다)
        int capBase = vertices.Count;
        for (int c = 0; c <= CapRings; c++)
        {
            float rf = 1f - c / (float)CapRings;
            for (int s = 0; s < SoilSegments; s++)
            {
                float a = s * Mathf.PI * 2f / SoilSegments;
                float px = Mathf.Cos(a) * surfaceRadius * rf, pz = Mathf.Sin(a) * surfaceRadius * rf;
                vertices.Add(new Vector3(px * scale, top(rf, px, pz) * scale, pz * scale));
            }
        }
        for (int c = 0; c < CapRings; c++)
        {
            for (int s = 0; s < SoilSegments; s++)
            {
                int s1 = (s + 1) % SoilSegments;
                int o00 = capBase + c * SoilSegments + s, o01 = capBase + c * SoilSegments + s1;
                int i00 = capBase + (c + 1) * SoilSegments + s, i01 = capBase + (c + 1) * SoilSegments + s1;
                triangles.Add(o00); triangles.Add(o01); triangles.Add(i00);
                triangles.Add(o01); triangles.Add(i01); triangles.Add(i00);
            }
        }

        // 바닥면: 병을 뒤집어 돌려도 흙이 사라지지 않게 아래도 막는다 (뒷면은 그려지지 않아서, 막지 않으면 아래에서 볼 때 텅 비어 보인다)
        int bottomCenter = vertices.Count;
        vertices.Add(new Vector3(0f, floorY * scale, 0f));
        int bottomRing = vertices.Count;
        for (int s = 0; s < SoilSegments; s++) vertices.Add(vertices[s]); // 첫 고리와 같은 자리
        for (int s = 0; s < SoilSegments; s++)
        {
            int s1 = (s + 1) % SoilSegments;
            triangles.Add(bottomCenter); triangles.Add(bottomRing + s1); triangles.Add(bottomRing + s);
        }

        Mesh mesh = new Mesh { name = "SoilMesh" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// 누운 병의 흙: 병 안쪽(원통)의 아래쪽을 윗면 높이(surfaceY)까지 채운 모양. 아래는 병의 둥근 안쪽을 따라가고
    /// 윗면에는 굴곡이 있다. 모델 크기 기준으로 만든다 (놓을 때 scale을 곱한다).
    /// </summary>
    private static Mesh BuildSideSoilMesh(Profile profile, Vector3 origin, float surfaceY, float seed)
    {
        const int ArcN = 18, TopN = 10;
        float d = surfaceY - origin.y; // 병 축에서 흙 표면까지 (아래쪽이므로 음수)

        var xs = new List<float>();
        for (float x = profile.First; x <= profile.Last; x += 0.004f)
            if (profile.Radius(x) * 0.98f > -d + 0.004f) xs.Add(x); // 표면 높이보다 넓은 몸통 구간에만 흙이 있다

        int ring = ArcN + 1 + TopN;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (float x in xs)
        {
            float R = profile.Radius(x) * 0.98f;
            float phi = Mathf.Asin(Mathf.Clamp(d / R, -1f, 1f));
            float a0 = Mathf.PI - phi, a1 = 2f * Mathf.PI + phi, w = R * Mathf.Cos(phi);
            for (int j = 0; j <= ArcN; j++) // 왼쪽 끝에서 바닥을 지나 오른쪽 끝까지
            {
                float t = Mathf.Lerp(a0, a1, j / (float)ArcN);
                vertices.Add(new Vector3(x, origin.y + R * Mathf.Sin(t), origin.z + R * Mathf.Cos(t)));
            }
            for (int j = 1; j <= TopN; j++) // 윗면: 오른쪽에서 왼쪽으로
            {
                float z = Mathf.Lerp(origin.z + w, origin.z - w, j / (float)(TopN + 1));
                float edge = 1f - Mathf.Pow(Mathf.Abs(z - origin.z) / Mathf.Max(w, 0.0001f), 2f);
                vertices.Add(new Vector3(x, surfaceY + Bump(x, z, seed) * edge, z));
            }
        }
        for (int i = 0; i < xs.Count - 1; i++)
        {
            for (int k = 0; k < ring; k++)
            {
                int k1 = (k + 1) % ring;
                int a = i * ring + k, b = i * ring + k1, c = (i + 1) * ring + k, dd = (i + 1) * ring + k1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(dd);
            }
        }

        Mesh mesh = new Mesh { name = "SideSoilMesh" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
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
