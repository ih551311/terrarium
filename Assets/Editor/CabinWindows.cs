using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 집(Cabin) 겉벽은 창이 없는 통짜 메쉬라서, 집 안 창문으로 밖이 보이게 하려면 창 자리의 면을 잘라 내야 한다.
/// 원본 메쉬(Building_b_nodoor)는 건드리지 않고, 창 구멍을 낸 복사본(Building_b_windows)을 만들어 Cabin에 연결한다.
/// 창 자리는 InteriorSetup.WindowSpecs 와 같다. 집 안 방을 다시 짓는 InteriorSetup.Build() 끝에서 자동으로 부른다.
/// </summary>
public static class CabinWindows
{
    private const string SourcePath = "Assets/Models/House/Building_b_nodoor.asset";
    private const string OutPath = "Assets/Models/House/Building_b_windows.asset";

    private struct Vtx
    {
        public Vector3 p, n;
        public Vector2 uv;
        public static Vtx Lerp(Vtx a, Vtx b, float t) => new Vtx { p = Vector3.Lerp(a.p, b.p, t), n = Vector3.Lerp(a.n, b.n, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
    }

    [MenuItem("유리병 정원/집 겉벽에 창 구멍 내기")]
    public static string Build()
    {
        GameObject cabin = GameObject.Find("Cabin");
        GameObject house = GameObject.Find("HouseInterior");
        if (cabin == null || house == null) return "Cabin 또는 HouseInterior가 없어요.";
        Mesh src = AssetDatabase.LoadAssetAtPath<Mesh>(SourcePath);
        if (src == null) return "원본 집 메쉬를 못 찾았어요: " + SourcePath;

        Transform tw = cabin.transform;
        Vector3[] sv = src.vertices, sn = src.normals;
        Vector2[] suv = src.uv;
        int[] st = src.triangles;
        bool hasN = sn.Length == sv.Length, hasUv = suv.Length == sv.Length;

        // 창 구멍 상자 (월드 좌표, 가로축 w = 0 x / 2 z, 세로 y, 깊이는 벽을 뚫고 지나게 넉넉히)
        var boxes = new List<Bounds>();
        foreach (var spec in InteriorSetup.WindowSpecs)
        {
            Vector3 c = house.transform.TransformPoint(spec.pos);
            bool alongX = Mathf.Abs(Mathf.Sin(spec.yaw * Mathf.Deg2Rad)) < 0.5f; // yaw 0: 북쪽 벽(가로가 x)
            Vector3 size = alongX
                ? new Vector3(InteriorSetup.HoleHalfW * 2f, InteriorSetup.HoleHalfH * 2f, 1.4f)
                : new Vector3(1.4f, InteriorSetup.HoleHalfH * 2f, InteriorSetup.HoleHalfW * 2f);
            boxes.Add(new Bounds(c, size));
        }

        var outV = new List<Vector3>();
        var outN = new List<Vector3>();
        var outUv = new List<Vector2>();
        var outT = new List<int>();
        int cut = 0;

        HashSet<int> duplicates = CoplanarCheck.HiddenDuplicates(src, tw);   // 같은 자리에 겹쳐 그려지는 면은 뺀다 (줄무늬 방지)
        for (int i = 0; i < st.Length; i += 3)
        {
            if (duplicates.Contains(i / 3)) continue;
            var tri = new List<Vtx>(3);
            for (int k = 0; k < 3; k++)
            {
                int vi = st[i + k];
                tri.Add(new Vtx { p = tw.TransformPoint(sv[vi]), n = hasN ? tw.TransformDirection(sn[vi]) : Vector3.up, uv = hasUv ? suv[vi] : Vector2.zero });
            }
            var pieces = new List<List<Vtx>> { tri };
            foreach (Bounds box in boxes)
            {
                var next = new List<List<Vtx>>();
                foreach (var poly in pieces)
                {
                    Bounds pb = new Bounds(poly[0].p, Vector3.zero);
                    foreach (Vtx v in poly) pb.Encapsulate(v.p);
                    if (!pb.Intersects(box)) { next.Add(poly); continue; }
                    cut++;
                    SplitOutside(poly, box, next);
                }
                pieces = next;
            }
            foreach (var poly in pieces)
            {
                if (poly.Count < 3) continue;
                int baseIdx = outV.Count;
                foreach (Vtx v in poly) { outV.Add(tw.InverseTransformPoint(v.p)); outN.Add(tw.InverseTransformDirection(v.n)); outUv.Add(v.uv); }
                for (int k = 1; k < poly.Count - 1; k++) { outT.Add(baseIdx); outT.Add(baseIdx + k); outT.Add(baseIdx + k + 1); }
            }
        }

        Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(OutPath);
        bool isNew = m == null;
        if (isNew) m = new Mesh();
        m.Clear();
        m.indexFormat = outV.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(outV);
        m.SetNormals(outN);
        m.SetUVs(0, outUv);
        m.SetTriangles(outT, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        m.name = "Building_b_windows";
        if (isNew) AssetDatabase.CreateAsset(m, OutPath); else EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();

        cabin.GetComponent<MeshFilter>().sharedMesh = m;
        EditorUtility.SetDirty(cabin);
        return "집 겉벽에 창 구멍을 냈어요 (잘린 면 조각 " + cut + "개, 겹친 면 " + duplicates.Count + "개 뺌, 삼각형 " + st.Length / 3 + " → " + outT.Count / 3 + ")";
    }

    /// <summary>다각형에서 상자 바깥 조각들만 next에 담는다 (상자 안쪽은 버린다).</summary>
    private static void SplitOutside(List<Vtx> poly, Bounds box, List<List<Vtx>> next)
    {
        List<Vtx> rest = poly;
        for (int axis = 0; axis < 3; axis++)
        {
            float lo = box.min[axis], hi = box.max[axis];
            var below = Clip(rest, axis, lo, true);
            if (below.Count >= 3) next.Add(below);
            rest = Clip(rest, axis, lo, false);
            var above = Clip(rest, axis, hi, false);
            if (above.Count >= 3) next.Add(above);
            rest = Clip(rest, axis, hi, true);
            if (rest.Count < 3) return;
        }
        // rest는 상자 안쪽 → 버림
    }

    /// <summary>평면 axis=value 의 한쪽만 남긴다 (keepLess: 값 이하 쪽).</summary>
    private static List<Vtx> Clip(List<Vtx> poly, int axis, float value, bool keepLess)
    {
        var res = new List<Vtx>();
        for (int i = 0; i < poly.Count; i++)
        {
            Vtx a = poly[i], b = poly[(i + 1) % poly.Count];
            float da = (a.p[axis] - value) * (keepLess ? 1f : -1f), db = (b.p[axis] - value) * (keepLess ? 1f : -1f);
            bool ain = da <= 0f, bin = db <= 0f;
            if (ain) res.Add(a);
            if (ain != bin) res.Add(Vtx.Lerp(a, b, da / (da - db)));
        }
        return res;
    }
}
