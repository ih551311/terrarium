using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 같은 평면에 겹쳐 있는 면(두 면이 같은 방향을 보고, 같은 깊이에서 겹침)을 찾는다. 이런 면은 카메라가 움직이면 서로 깜빡이거나 줄무늬(z-fighting)로 보인다.
/// 한 물체 안(Mesh)과 서로 다른 물체끼리(월드 좌표) 모두 검사한다. 메뉴: 유리병 정원 > 겹친 면 검사
/// </summary>
public static class CoplanarCheck
{
    private struct Tri { public Vector3 a, b, c, n; public float d; public Bounds box; public int owner; public float area; }

    private const float PlaneTol = 0.012f;   // 같은 평면으로 보는 거리
    private const float MinOverlap = 0.004f; // 겹침 넓이(m²)

    [MenuItem("유리병 정원/겹친 면 검사 (선택한 물체 또는 Cabin)")]
    public static string Menu()
    {
        string r = Scan(Selection.activeGameObject != null ? Selection.activeGameObject : GameObject.Find("Cabin"), null);
        Debug.Log(r);
        return r;
    }

    /// <summary>root 아래 모든 렌더러의 면을 모아 겹친 쌍을 센다. others가 있으면 그 물체들의 면과도 비교한다.</summary>
    public static string Scan(GameObject root, GameObject[] others)
    {
        var tris = new List<Tri>();
        var owners = new List<string>();
        Collect(root, tris, owners);
        if (others != null) foreach (var o in others) if (o != null) Collect(o, tris, owners);

        var sb = new StringBuilder();
        int pairs = 0;
        var perOwner = new Dictionary<string, int>();
        for (int i = 0; i < tris.Count; i++)
        {
            for (int j = i + 1; j < tris.Count; j++)
            {
                Tri p = tris[i], q = tris[j];
                if (!p.box.Intersects(q.box)) continue;
                if (Vector3.Dot(p.n, q.n) < 0.995f) continue;                 // 같은 방향을 볼 때만
                if (Mathf.Abs(p.d - q.d) > PlaneTol) continue;                // 같은 평면
                if (p.owner == q.owner && IsSharedEdgeOnly(p, q)) continue;
                float area = OverlapArea(p, q);
                if (area < MinOverlap) continue;
                pairs++;
                string key = owners[p.owner] + " × " + owners[q.owner];
                perOwner[key] = perOwner.TryGetValue(key, out int n) ? n + 1 : 1;
                if (pairs <= 12 || (key.StartsWith("Cabin × ") && key != "Cabin × Cabin" && sb.Length < 6000)) sb.Append("  ").Append(key).Append(" 중심 ").Append(((p.box.center + q.box.center) * 0.5f).ToString("F2")).Append(" 넓이 ").Append(area.ToString("F3")).Append('\n');
            }
        }
        var head = new StringBuilder("겹친 면 쌍 " + pairs + "개 (면 " + tris.Count + "개 검사)\n");
        foreach (var kv in perOwner) head.Append("  ").Append(kv.Key).Append(": ").Append(kv.Value).Append('\n');
        return head + sb.ToString();
    }

    private static void Collect(GameObject root, List<Tri> tris, List<string> owners)
    {
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            Renderer r = mf.GetComponent<Renderer>();
            if (r == null || !r.enabled || !mf.gameObject.activeInHierarchy || mf.sharedMesh == null) continue;
            Mesh m = mf.sharedMesh;
            if (!m.isReadable) continue;
            Vector3[] v = m.vertices;
            int[] t = m.triangles;
            Transform tr = mf.transform;
            int ownerId = owners.Count;
            owners.Add(mf.name);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = tr.TransformPoint(v[t[i]]), b = tr.TransformPoint(v[t[i + 1]]), c = tr.TransformPoint(v[t[i + 2]]);
                Vector3 cross = Vector3.Cross(b - a, c - a);
                float len = cross.magnitude;
                if (len < 1e-5f) continue;
                Vector3 n = cross / len;
                var box = new Bounds(a, Vector3.zero); box.Encapsulate(b); box.Encapsulate(c);
                box.Expand(0.002f);
                tris.Add(new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), box = box, owner = ownerId, area = len * 0.5f });
            }
        }
    }

    /// <summary>한 메쉬 안에서 같은 평면에 같은 방향으로 겹친 면 중, 작은 쪽(겹침이 25% 이상인 것)의 삼각형 번호를 모은다. 이 면들은 줄무늬(z-fighting)의 원인이라 지워도 모양이 변하지 않는다.</summary>
    public static HashSet<int> HiddenDuplicates(Mesh mesh, Transform tr)
    {
        var tris = new List<Tri>();
        Vector3[] v = mesh.vertices; int[] t = mesh.triangles;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 a = tr.TransformPoint(v[t[i]]), b = tr.TransformPoint(v[t[i + 1]]), c = tr.TransformPoint(v[t[i + 2]]);
            Vector3 cross = Vector3.Cross(b - a, c - a);
            float len = cross.magnitude;
            var box = new Bounds(a, Vector3.zero); box.Encapsulate(b); box.Encapsulate(c); box.Expand(0.002f);
            Vector3 n = len < 1e-6f ? Vector3.up : cross / len;
            tris.Add(new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), box = box, owner = i / 3, area = len * 0.5f });
        }
        var drop = new HashSet<int>();
        for (int i = 0; i < tris.Count; i++)
        {
            if (tris[i].area < 1e-5f || drop.Contains(i)) continue;
            for (int j = i + 1; j < tris.Count; j++)
            {
                Tri p = tris[i], q = tris[j];
                if (q.area < 1e-5f || drop.Contains(j) || drop.Contains(i)) continue;
                if (!p.box.Intersects(q.box) || Vector3.Dot(p.n, q.n) < 0.995f || Mathf.Abs(p.d - q.d) > PlaneTol) continue;
                float area = OverlapArea(p, q);
                if (area < MinOverlap || area < Mathf.Min(p.area, q.area) * 0.25f) continue;
                drop.Add(p.area < q.area ? i : j);
            }
        }
        return drop;
    }

    private static bool IsSharedEdgeOnly(Tri p, Tri q) => false;

    /// <summary>두 삼각형(같은 평면)이 겹치는 넓이. 평면 위 2D로 옮겨서 자른다.</summary>
    private static float OverlapArea(Tri p, Tri q)
    {
        Vector3 n = p.n;
        Vector3 u = Mathf.Abs(n.y) < 0.9f ? Vector3.Cross(n, Vector3.up).normalized : Vector3.Cross(n, Vector3.right).normalized;
        Vector3 w = Vector3.Cross(n, u);
        var poly = new List<Vector2> { P(p.a, u, w), P(p.b, u, w), P(p.c, u, w) };
        var clip = new List<Vector2> { P(q.a, u, w), P(q.b, u, w), P(q.c, u, w) };
        if (Signed(poly) < 0f) poly.Reverse();
        if (Signed(clip) < 0f) clip.Reverse();
        for (int i = 0; i < clip.Count && poly.Count > 0; i++)
        {
            Vector2 e0 = clip[i], e1 = clip[(i + 1) % clip.Count];
            var next = new List<Vector2>();
            for (int k = 0; k < poly.Count; k++)
            {
                Vector2 s = poly[k], f = poly[(k + 1) % poly.Count];
                bool sIn = Side(e0, e1, s) >= 0f, fIn = Side(e0, e1, f) >= 0f;
                if (sIn) next.Add(s);
                if (sIn != fIn)
                {
                    float ds = Side(e0, e1, s), df = Side(e0, e1, f);
                    next.Add(Vector2.Lerp(s, f, ds / (ds - df)));
                }
            }
            poly = next;
        }
        return poly.Count < 3 ? 0f : Mathf.Abs(Signed(poly));
    }

    private static Vector2 P(Vector3 x, Vector3 u, Vector3 w) => new Vector2(Vector3.Dot(x, u), Vector3.Dot(x, w));
    private static float Side(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    private static float Signed(List<Vector2> pts)
    {
        float s = 0f;
        for (int i = 0; i < pts.Count; i++) { Vector2 a = pts[i], b = pts[(i + 1) % pts.Count]; s += a.x * b.y - b.x * a.y; }
        return s * 0.5f;
    }
}
