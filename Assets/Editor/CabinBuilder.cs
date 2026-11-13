using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에셋 팩의 오두막 키트(바닥, 벽, 창, 문, 지붕, 굴뚝)로 6m x 6m 통나무 오두막을 조립한다.
/// 섬 바깥(외부)과 집 안(내부) 양쪽에 같은 오두막을 써서 겉과 속이 똑같다.
/// 오두막의 기준점은 바닥 윗면 가운데이고, 문은 남쪽(-z)에 있다.
/// 벽의 창과 문 구멍은 벽 모델에 광선을 쏴서 자동으로 찾고, 그 자리에 창과 문 조각을 끼운다.
/// </summary>
public static class CabinBuilder
{
    private const string Kit = "Assets/Low Poly Nature Pack/Prefabs/Cabin/";
    private const float WallOffset = 2.8f;   // 가운데에서 벽 가운데까지 (에셋 팩 데모와 같은 값)
    private const float RoofHeight = 4.12f;  // 바닥 윗면에서 지붕 기준점까지

    public class Cabin
    {
        public GameObject root;
        public GameObject door;   // 문 조각 (SceneDoor를 붙이는 곳)
    }

    private class WallInfo
    {
        public float bottom;                  // 벽 기준점에서 벽 맨 아래까지(음수)
        public List<float> doors = new List<float>();
        public List<float> windows = new List<float>();
    }

    private static readonly Dictionary<string, WallInfo> cache = new Dictionary<string, WallInfo>();

    /// <summary>오두막을 parent 아래에 조립한다. withFlue가 켜지면 지붕에 굴뚝도 올린다.</summary>
    public static Cabin Build(Transform parent, Vector3 localPosition, float yaw, bool withFlue)
    {
        var cabin = new Cabin();
        GameObject root = new GameObject("Cabin");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        cabin.root = root;

        // 바닥: 윗면이 높이 0이 되도록 기준점을 0.2 내린다
        GameObject floor = Piece(root.transform, "Floor01", new Vector3(0f, -0.2f, 0f), 0f);
        AddBoxCollider(floor);

        // 네 벽: 서(창), 동(창), 남(문과 창), 북(창)
        BuildWall(cabin, "Wall03", new Vector3(-WallOffset, 0f, 0f), 0f);
        BuildWall(cabin, "Wall03", new Vector3(WallOffset, 0f, 0f), 0f);
        BuildWall(cabin, "Wall09", new Vector3(0f, 0f, -WallOffset), 90f);
        BuildWall(cabin, "Wall07", new Vector3(0f, 0f, WallOffset), 90f);

        // 지붕과 굴뚝
        Piece(root.transform, "Roof01", new Vector3(0f, RoofHeight, 0f), 0f);
        if (withFlue)
        {
            GameObject flue = Piece(root.transform, "Flue", new Vector3(-0.94f, 4.6f, 0.7f), 0f);
            flue.transform.localScale = Vector3.one * 0.75f;
        }
        return cabin;
    }

    private static void BuildWall(Cabin cabin, string wallName, Vector3 position, float yaw)
    {
        WallInfo info = Analyze(wallName);
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Transform root = cabin.root.transform;

        // 벽: 맨 아래가 바닥 윗면(높이 0)에 닿도록 기준점을 올린다
        Vector3 wallPos = position + new Vector3(0f, -info.bottom, 0f);
        GameObject wall = Piece(root, wallName, wallPos, yaw);
        foreach (MeshFilter mf in LodZero(wall)) AddMeshCollider(mf.gameObject);

        // 창: 구멍 가운데, 벽 맨 아래에서 1.5m 높이
        foreach (float z in info.windows)
        {
            GameObject window = Piece(root, "Window02", wallPos + rot * new Vector3(0.04f, info.bottom + 1.5f, z), yaw);
            AddBoxCollider(window);
        }

        // 문: 구멍 가운데에 문 조각을 끼운다 (문 조각의 기준점은 경첩 쪽 끝이라 0.5m 옮긴다)
        foreach (float z in info.doors)
        {
            GameObject door = Piece(root, "Door", wallPos + rot * new Vector3(0f, info.bottom + 1.0f, z + 0.5f), yaw);
            AddBoxCollider(door);
            cabin.door = door;
        }
    }

    // ───────── 벽 분석 (창과 문 구멍 찾기) ─────────

    /// <summary>벽 모델에 광선을 쏴서 창과 문 구멍의 가운데 위치(벽 기준 z)를 찾는다.</summary>
    private static WallInfo Analyze(string wallName)
    {
        if (cache.TryGetValue(wallName, out WallInfo cached)) return cached;

        // 다른 물체와 섞이지 않게 아주 먼 곳에 임시로 세운다
        Vector3 origin = new Vector3(20000f, 0f, 20000f);
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit + wallName + ".prefab"));
        temp.transform.position = origin;

        var colliders = new List<MeshCollider>();
        Bounds bounds = new Bounds(origin, Vector3.zero);
        bool first = true;
        foreach (MeshFilter mf in LodZero(temp))
        {
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            colliders.Add(mc);
            Renderer r = mf.GetComponent<Renderer>();
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
        }
        Physics.SyncTransforms();

        var info = new WallInfo { bottom = bounds.min.y };
        float zMin = bounds.min.z, zMax = bounds.max.z;

        List<float[]> doorGaps = Gaps(origin, bounds.min.y + 0.3f, zMin, zMax);   // 바닥에서 30cm 높이에서 뚫려 있으면 문
        List<float[]> windowGaps = Gaps(origin, bounds.min.y + 1.5f, zMin, zMax); // 1.5m 높이에서 뚫려 있으면 문이나 창

        foreach (float[] g in doorGaps) info.doors.Add((g[0] + g[1]) * 0.5f);
        foreach (float[] g in windowGaps)
        {
            float center = (g[0] + g[1]) * 0.5f;
            if (info.doors.Any(d => Mathf.Abs(d - center) < 0.6f)) continue; // 문 구멍은 창이 아니다
            info.windows.Add(center);
        }

        Object.DestroyImmediate(temp);
        cache[wallName] = info;
        return info;
    }

    /// <summary>높이 y에서 벽을 따라(z) 광선을 쏴서, 아무것도 안 맞는 구간(구멍)의 시작과 끝을 돌려준다.</summary>
    private static List<float[]> Gaps(Vector3 origin, float y, float zMin, float zMax)
    {
        var gaps = new List<float[]>();
        float start = float.NaN;
        for (float z = zMin + 0.05f; z <= zMax - 0.05f; z += 0.05f)
        {
            bool hit = Physics.Raycast(new Vector3(origin.x + 3f, y, z), Vector3.left, 6f);
            if (!hit && float.IsNaN(start)) start = z;
            if (hit && !float.IsNaN(start))
            {
                if (z - start > 0.5f) gaps.Add(new[] { start - origin.z, z - origin.z });
                start = float.NaN;
            }
        }
        return gaps;
    }

    // ───────── 도우미 ─────────

    private static GameObject Piece(Transform parent, string prefabName, Vector3 localPosition, float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + prefabName + ".prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    /// <summary>가장 자세한 모양(LOD0)의 MeshFilter만 돌려준다.</summary>
    private static List<MeshFilter> LodZero(GameObject go)
    {
        return go.GetComponentsInChildren<MeshFilter>(true)
                 .Where(m => m.sharedMesh != null && !m.name.Contains("LOD1") && !m.name.Contains("LOD2") && !m.name.Contains("LOD3"))
                 .ToList();
    }

    private static void AddMeshCollider(GameObject go)
    {
        if (go.GetComponent<MeshCollider>() == null) go.AddComponent<MeshCollider>();
    }

    /// <summary>조각 전체를 감싸는 상자 Collider를 붙인다.</summary>
    private static void AddBoxCollider(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>().Where(r => !r.name.Contains("LOD1") && !r.name.Contains("LOD2") && !r.name.Contains("LOD3")).ToArray();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = go.transform.InverseTransformVector(b.size);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }
}
