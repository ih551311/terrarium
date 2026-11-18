using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 눈 섬 연못의 낚시 구멍을 만든다: 들쭉날쭉한 두꺼운 얼음 테두리와 안쪽 벽, 어두운 물,
/// 물 위에 뜬 얼음 조각, 빨강 하양 찌. 구멍마다 모양이 조금씩 다르다(구멍 번호가 씨앗).
/// 얼음 테두리 메쉬는 Assets/Models/Fishing/에 저장한다(같은 파일 안에서 내용만 바꾼다).
/// 눈 섬 만들기(SnowIslandSetup)가 쓰고, "낚시 구멍 다시 만들기"는 열려 있는 눈 섬 씬의 구멍 모양만 바꾼다(섬의 다른 것은 그대로).
/// </summary>
public static class FishingHoleBuilder
{
    private const string MeshFolder = "Assets/Models/Fishing";
    private const string ScenePath = "Assets/Scenes/SnowIsland.unity";

    [MenuItem("유리병 정원/낚시 구멍 다시 만들기")]
    public static string Rebuild()
    {
        if (EditorApplication.isPlaying) return "Play 중에는 만들 수 없어요";
        string previous = SceneManager_ActivePath();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int count = 0;
        foreach (FishingSpot spot in Object.FindObjectsByType<FishingSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform root = spot.transform;
            var old = new List<GameObject>();
            foreach (Transform child in root) old.Add(child.gameObject);
            foreach (GameObject go in old) Object.DestroyImmediate(go);

            int index = 0;
            int.TryParse(root.name.Substring(root.name.LastIndexOf('_') + 1), out index);
            Transform bobber = BuildVisuals(root, index);

            SerializedObject so = new SerializedObject(spot);
            so.FindProperty("bobber").objectReferenceValue = bobber;
            so.ApplyModifiedPropertiesWithoutUndo();
            count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(previous) && previous != ScenePath) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        return "낚시 구멍 " + count + "곳을 다시 만들었어요";
    }

    private static string SceneManager_ActivePath()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
    }

    /// <summary>구멍 하나의 모습을 root 아래에 만든다. 찌(Bobber)를 돌려준다.</summary>
    public static Transform BuildVisuals(Transform root, int index)
    {
        Material iceTop = Mat("FishIceTop", new Color(0.74f, 0.90f, 0.98f));
        Material iceSide = Mat("FishIceSide", new Color(0.52f, 0.74f, 0.92f));
        Material iceWall = Mat("FishIceWall", new Color(0.30f, 0.62f, 0.84f));
        Material water = Mat("FishHoleWater", new Color(0.03f, 0.11f, 0.26f));
                                        Material red = Mat("FishBobber", new Color(0.92f, 0.22f, 0.20f));
        Material white = Mat("FishBobberWhite", new Color(0.97f, 0.97f, 0.95f));
        var rnd = new System.Random(1000 + index * 37);

        // 두꺼운 얼음 테두리 (위, 바깥 비탈, 안쪽 벽)
        GameObject ring = new GameObject("IceRing");
        ring.transform.SetParent(root, false);
        ring.AddComponent<MeshFilter>().sharedMesh = IceMesh(index);
        ring.AddComponent<MeshRenderer>().sharedMaterials = new[] { iceTop, iceSide, iceWall };

        // 어두운 물 (테두리 아래, 안쪽 벽 사이로 보인다)
        Disc(root, "Water", 1.5f, 0.01f, 0.035f, water); // 눈 바닥(0) 위, 테두리 윗면(0.13) 아래

        // 물 위에 뜬 얼음 조각
        for (int i = 0; i < 3; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 0.15f + (float)rnd.NextDouble() * 0.22f;
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "IceChip";
            chip.transform.SetParent(root, false);
            chip.transform.localPosition = new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
            chip.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            chip.transform.localScale = new Vector3(0.10f + (float)rnd.NextDouble() * 0.08f, 0.025f, 0.08f + (float)rnd.NextDouble() * 0.06f);
            Object.DestroyImmediate(chip.GetComponent<Collider>());
            chip.GetComponent<Renderer>().sharedMaterial = iceTop;
        }

        // 찌: 빨강 몸에 하얀 띠와 막대
        GameObject bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bobber.name = "Bobber";
        bobber.transform.SetParent(root, false);
        bobber.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        bobber.transform.localScale = Vector3.one * 0.15f;
        Object.DestroyImmediate(bobber.GetComponent<Collider>());
        bobber.GetComponent<Renderer>().sharedMaterial = red;
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "BobberBand";
        band.transform.SetParent(bobber.transform, false);
        band.transform.localScale = new Vector3(1.02f, 0.045f, 1.02f);
        Object.DestroyImmediate(band.GetComponent<Collider>());
        band.GetComponent<Renderer>().sharedMaterial = white;
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stick.name = "BobberStick";
        stick.transform.SetParent(bobber.transform, false);
        stick.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        stick.transform.localScale = new Vector3(0.12f, 0.38f, 0.12f);
        Object.DestroyImmediate(stick.GetComponent<Collider>());
        stick.GetComponent<Renderer>().sharedMaterial = white;
        return bobber.transform;
    }

    private static Material Mat(string name, Color color)
    {
        Material m = BottleSetup.GetOrCreateMaterial(name, color);
        if (m.GetColor("_BaseColor") != color) { m.SetColor("_BaseColor", color); EditorUtility.SetDirty(m); } // 색을 고치면 바로 반영
        return m;
    }

    private static void Disc(Transform parent, string name, float diameter, float thickness, float y, Material mat)
    {
        GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(0f, y, 0f);
        d.transform.localScale = new Vector3(diameter, thickness, diameter);
        Object.DestroyImmediate(d.GetComponent<Collider>());
        d.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ───────── 얼음 테두리 메쉬 ─────────

    private static Mesh IceMesh(int seed)
    {
        const int N = 20;
        const float TopY = 0.13f, BaseY = 0f, FloorY = -0.20f;
        var rnd = new System.Random(seed * 131 + 5);
        var ro = new float[N]; var ri = new float[N];
        for (int i = 0; i < N; i++)
        {
            ro[i] = 1.00f * (1f + ((float)rnd.NextDouble() - 0.5f) * 0.20f);
            ri[i] = 0.56f * (1f + ((float)rnd.NextDouble() - 0.5f) * 0.12f);
        }

        var verts = new List<Vector3>();
        var subs = new[] { new List<int>(), new List<int>(), new List<int>() };

        Vector3 P(float r, float a, float y) { return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
        void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 desired)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            int s = verts.Count;
            if (Vector3.Dot(n, desired) >= 0f) { verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d); }
            else { verts.Add(a); verts.Add(d); verts.Add(c); verts.Add(b); }
            subs[sub].AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }

        for (int i = 0; i < N; i++)
        {
            int j = (i + 1) % N;
            float a0 = Mathf.PI * 2f * i / N, a1 = Mathf.PI * 2f * (i + 1) / N;
            Vector3 mid = P(1f, (a0 + a1) * 0.5f, 0f);
            // 위쪽 면 (살짝 둥글게 가장자리가 낮아진다)
            Quad(0, P(ri[i], a0, TopY), P(ro[i] * 0.92f, a0, TopY - 0.005f), P(ro[j] * 0.92f, a1, TopY - 0.005f), P(ri[j], a1, TopY), Vector3.up);
            // 바깥 비탈
            Quad(1, P(ro[i] * 0.92f, a0, TopY - 0.005f), P(ro[i], a0, BaseY), P(ro[j], a1, BaseY), P(ro[j] * 0.92f, a1, TopY - 0.005f), mid);
            // 안쪽 벽 (아래로 갈수록 조금 좁아진다)
            Quad(2, P(ri[i], a0, TopY), P(ri[i] * 0.94f, a0, FloorY), P(ri[j] * 0.94f, a1, FloorY), P(ri[j], a1, TopY), -mid);
        }

        var mesh = new Mesh { name = "ice_hole_" + seed };
        mesh.SetVertices(verts);
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(subs[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Directory.CreateDirectory(MeshFolder);
        string path = MeshFolder + "/ice_hole_" + seed + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
