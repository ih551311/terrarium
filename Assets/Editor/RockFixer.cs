using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 바위 모델 중에는 덩어리 몇 개가 아무것에도 닿지 않고 허공에 떠 있는 것이 있다. 그런 덩어리를 지운 복사본 메쉬를 만들어 바위에 연결한다.
/// 원본 모델은 그대로 두고, 고친 메쉬는 Assets/Models/Cleaned 에 저장한다. 열려 있는 씬 하나를 처리한다.
///  - 덩어리: 꼭짓점이 이어진 면의 묶음
///  - 떠 있음: 덩어리 아래(0.12 안)에 다른 덩어리의 면이 없고, 바닥에서도 떨어져 있음
/// 메뉴: 유리병 정원 > 떠 있는 바위 덩어리 지우기 (열린 씬)
/// </summary>
public static class RockFixer
{
    private const string OutFolder = "Assets/Models/Cleaned";

    [MenuItem("유리병 정원/떠 있는 바위 덩어리 지우기 (열린 씬)")]
    public static string FixOpenScene() => FixRoots(new[] { "Rocks", "Ruins" });

    public static string FixRoots(string[] rootNames)
    {
        Directory.CreateDirectory(OutFolder);
        var cache = new Dictionary<Mesh, Mesh>();
        var report = new System.Text.StringBuilder();
        int replaced = 0;
        foreach (string rn in rootNames)
        {
            GameObject root = GameObject.Find(rn);
            if (root == null) continue;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh src = mf.sharedMesh;
                if (src == null || !src.isReadable) continue;
                if (!cache.TryGetValue(src, out Mesh fixedMesh))
                {
                    fixedMesh = Clean(src, mf.name, report);
                    cache[src] = fixedMesh;
                }
                if (fixedMesh != null && fixedMesh != src)
                {
                    mf.sharedMesh = fixedMesh;
                    MeshCollider mc = mf.GetComponent<MeshCollider>();
                    if (mc != null && mc.sharedMesh == src) mc.sharedMesh = fixedMesh;
                    EditorUtility.SetDirty(mf);
                    replaced++;
                }
            }
        }
        AssetDatabase.SaveAssets();
        return "떠 있는 덩어리를 지운 메쉬로 " + replaced + "개 물체를 바꿨어요.\n" + report;
    }

    private static Mesh Clean(Mesh src, string label, System.Text.StringBuilder report)
    {
        Vector3[] v = src.vertices;
        int[] tris = src.triangles;
        if (src.subMeshCount > 1 || v.Length > 20000) return null;

        // 덩어리 나누기 (같은 자리 꼭짓점은 하나로 본다)
        int[] parent = new int[v.Length];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        var byPos = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < v.Length; i++)
        {
            var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 1000f), Mathf.RoundToInt(v[i].y * 1000f), Mathf.RoundToInt(v[i].z * 1000f));
            if (byPos.TryGetValue(k, out int o)) parent[Find(i)] = Find(o); else byPos[k] = i;
        }
        for (int i = 0; i < tris.Length; i += 3) { parent[Find(tris[i + 1])] = Find(tris[i]); parent[Find(tris[i + 2])] = Find(tris[i]); }

        var comps = new Dictionary<int, List<int>>(); // 덩어리 -> 삼각형 번호들
        for (int t = 0; t < tris.Length / 3; t++)
        {
            int r = Find(tris[t * 3]);
            if (!comps.TryGetValue(r, out List<int> list)) comps[r] = list = new List<int>();
            list.Add(t);
        }
        if (comps.Count < 2 || comps.Count > 40) return null;

        var bounds = new Dictionary<int, Bounds>();
        foreach (var kv in comps)
        {
            Bounds b = new Bounds(v[tris[kv.Value[0] * 3]], Vector3.zero);
            foreach (int t in kv.Value) for (int k = 0; k < 3; k++) b.Encapsulate(v[tris[t * 3 + k]]);
            bounds[kv.Key] = b;
        }
        float meshMin = float.MaxValue;
        foreach (var b in bounds.Values) meshMin = Mathf.Min(meshMin, b.min.y);

        var floating = new HashSet<int>();
        foreach (var kv in comps)
        {
            Bounds a = bounds[kv.Key];
            if (a.min.y < meshMin + 0.15f) continue; // 바닥에 닿아 있음
            // 아래로 쏜 선이 다른 덩어리의 면에 0.12 안에 닿으면 올려 놓인 것
            bool supported = false;
            foreach (int t in kv.Value)
            {
                for (int k = 0; k < 3 && !supported; k++)
                {
                    Vector3 p = v[tris[t * 3 + k]];
                    if (p.y > a.min.y + 0.12f) continue;
                    if (HitsBelow(p, v, tris, comps, kv.Key, 0.12f)) supported = true;
                }
                if (supported) break;
            }
            if (!supported) floating.Add(kv.Key);
        }
        if (floating.Count == 0) return null;

        // 떠 있는 덩어리를 뺀 새 메쉬
        var keep = new List<int>();
        foreach (var kv in comps) if (!floating.Contains(kv.Key)) keep.AddRange(kv.Value);
        Mesh m = new Mesh { name = src.name + "_clean" };
        m.indexFormat = src.indexFormat;
        var remap = new Dictionary<int, int>();
        var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nuv = new List<Vector2>(); var nc = new List<Color>();
        Vector3[] sn = src.normals; Vector2[] suv = src.uv; Color[] sc = src.colors;
        var nt = new List<int>();
        foreach (int t in keep)
        {
            for (int k = 0; k < 3; k++)
            {
                int old = tris[t * 3 + k];
                if (!remap.TryGetValue(old, out int idx))
                {
                    idx = nv.Count; remap[old] = idx;
                    nv.Add(v[old]);
                    if (sn.Length == v.Length) nn.Add(sn[old]);
                    if (suv.Length == v.Length) nuv.Add(suv[old]);
                    if (sc.Length == v.Length) nc.Add(sc[old]);
                }
                nt.Add(idx);
            }
        }
        m.SetVertices(nv);
        if (nn.Count == nv.Count) m.SetNormals(nn);
        if (nuv.Count == nv.Count) m.SetUVs(0, nuv);
        if (nc.Count == nv.Count) m.SetColors(nc);
        m.SetTriangles(nt, 0);
        m.RecalculateBounds();
        if (nn.Count != nv.Count) m.RecalculateNormals();

        string path = OutFolder + "/" + SafeName(src.name) + "_" + v.Length + "_" + comps.Count + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        report.Append("  ").Append(label).Append(" (").Append(src.name).Append(" ").Append(v.Length).Append("개 꼭짓점): 떠 있는 덩어리 ").Append(floating.Count).Append("개 지움\n");
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static string SafeName(string n)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        return n;
    }

    /// <summary>p 바로 아래 maxDist 안에 다른 덩어리의 삼각형이 있는가</summary>
    private static bool HitsBelow(Vector3 p, Vector3[] v, int[] tris, Dictionary<int, List<int>> comps, int self, float maxDist)
    {
        Vector3 down = Vector3.down;
        foreach (var kv in comps)
        {
            if (kv.Key == self) continue;
            foreach (int t in kv.Value)
            {
                Vector3 a = v[tris[t * 3]], b = v[tris[t * 3 + 1]], c = v[tris[t * 3 + 2]];
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < p.y - maxDist) continue;   // 너무 아래
                if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > p.y + 0.02f) continue;     // 위에 있음
                // 위에서 본(xz) 삼각형 안에 있는가
                if (!InsideXZ(p, a, b, c)) continue;
                // 그 자리의 면 높이
                if (!HeightAt(p, a, b, c, out float y)) continue;
                float d = p.y - y;
                if (d >= -0.05f && d <= maxDist) return true;
            }
        }
        return false;
    }

    private static bool InsideXZ(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    private static float Sign(Vector3 p, Vector3 a, Vector3 b) => (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.z - b.z);

    private static bool HeightAt(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out float y)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        y = 0f;
        if (Mathf.Abs(n.y) < 1e-6f) return false;
        y = a.y - (n.x * (p.x - a.x) + n.z * (p.z - a.z)) / n.y;
        return true;
    }
}
