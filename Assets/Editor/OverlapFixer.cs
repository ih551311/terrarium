using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 열려 있는 섬 씬에서 서로 겹치거나(나무 속 눈사람, 울타리 속 눈 더미, 같은 자리의 재료 두 개) 집, 선착장 같은 큰 물건 속에 들어간
/// 나무, 바위, 눈 더미, 소품, 재료를 가까운 빈 자리로 옮긴다. 빈 자리가 없으면 나무, 바위, 눈 더미, 소품은 지운다.
/// 마을(눈사람, 울타리, 돌담, 팻말, 징검돌)은 제자리에 두고 나머지가 비켜 선다. 여러 번 눌러도 안전하다.
/// 메뉴: 유리병 정원 > 겹친 물건 고치기 (열려 있는 씬) / 모든 섬 겹침 고치기
/// </summary>
public static class OverlapFixer
{
    private static readonly string[] Groups = { "Forest", "Mounds", "Decor", "Village", "Pickups", "Trees", "Rocks", "Props" };

    // 먼저 자리를 잡는 것부터 (앞쪽이 우선, 뒤쪽이 비켜선다)
    private static int Priority(string group, string name)
    {
        if (group == "Village") return 0;
        if (group == "Rocks") return 1;
        if (group == "Trees" || group == "Forest") return 2;
        if (group == "Mounds") return 3;
        if (group == "Decor" || group == "Props") return 4;
        return 5; // Pickups
    }

    private class Item
    {
        public Transform t;
        public string group;
        public int priority;
        public float radius;
        public bool movable;
        public bool isPickup;
        public bool elongated;   // 울타리, 돌담처럼 길쭉한 것은 원 대신 직사각형으로 본다
        public Rect rect;
        public Vector2 Pos => new Vector2(t.position.x, t.position.z);
    }

    [MenuItem("유리병 정원/겹친 물건 고치기")]
    public static void FixMenu()
    {
        Debug.Log(FixOpenScene(false));
    }

    [MenuItem("유리병 정원/모든 섬 겹침 고치기")]
    public static void FixAllMenu()
    {
        Debug.Log(FixAllScenes(false));
    }

    public static string FixAllScenes(bool dryRun)
    {
        EditorSceneManager.SaveOpenScenes();
        var sb = new StringBuilder();
        foreach (string path in new[] { "Assets/Scenes/SnowIsland.unity", "Assets/Scenes/Island.unity", "Assets/Scenes/DesertIsland.unity" })
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("== " + path);
            sb.AppendLine(FixOpenScene(dryRun));
        }
        return sb.ToString();
    }

    public static string FixOpenScene(bool dryRun)
    {
        Physics.SyncTransforms();
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return "지형이 없어서 건너뜀";

        var items = Collect();
        items.Sort((a, b) => a.priority.CompareTo(b.priority));
        var itemRoots = new HashSet<Transform>();
        foreach (Item it in items) itemRoots.Add(it.t);

        var accepted = new List<Item>();
        int moved = 0, removed = 0, kept = 0;
        var log = new StringBuilder();
        var toDelete = new List<GameObject>();

        foreach (Item it in items)
        {
            why = null;
            if (!Conflicts(it, it.Pos, accepted, itemRoots, terrain))
            {
                accepted.Add(it);
                kept++;
                continue;
            }
            string reason = why;
            if (!it.movable) { accepted.Add(it); log.AppendLine("고정 물건이 겹침(그대로 둠): " + it.t.name + " " + it.Pos); continue; }

            Vector2 target;
            if (FindSpot(it, accepted, itemRoots, terrain, out target))
            {
                moved++;
                log.AppendLine("옮김: " + it.t.name + " (" + it.group + ") " + it.Pos + " -> " + target + "  [" + reason + "]");
                if (!dryRun) Move(it, target, terrain);
                else { accepted.Add(new Item { t = it.t, group = it.group, radius = it.radius, movable = false, isPickup = it.isPickup }); continue; }
                accepted.Add(it);
            }
            else if (it.isPickup)
            {
                // 재료는 지우지 않고 섬 안의 다른 빈 자리로 보낸다
                if (FindFarSpot(it, accepted, itemRoots, terrain, out target))
                {
                    moved++;
                    log.AppendLine("멀리 옮김: " + it.t.name + " " + it.Pos + " -> " + target);
                    if (!dryRun) Move(it, target, terrain);
                    accepted.Add(it);
                }
                else { removed++; log.AppendLine("지움: " + it.t.name); toDelete.Add(it.t.gameObject); }
            }
            else
            {
                removed++;
                log.AppendLine("지움: " + it.t.name + " (" + it.group + ") " + it.Pos);
                toDelete.Add(it.t.gameObject);
            }
        }

        if (!dryRun)
        {
            foreach (GameObject go in toDelete) if (go != null) Object.DestroyImmediate(go);
            if (moved + removed > 0)
            {
                Physics.SyncTransforms();
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }
        }
        return (dryRun ? "[미리보기] " : "") + "그대로 " + kept + ", 옮김 " + moved + ", 지움 " + removed + "\n" + log;
    }

    private static List<Item> Collect()
    {
        var list = new List<Item>();
        var set = new HashSet<string>(Groups);
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.parent == null || !set.Contains(t.parent.name)) continue;
            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);

            string group = t.parent.name;
            bool pickup = group == "Pickups";
            string n = t.name.ToLower();
            // 마을 안에서도 눈사람과 팻말은 크기를 가진 물건이고, 울타리, 돌담, 징검돌은 줄이라 한 칸씩 본다
            bool path = n.StartsWith("path");
            float radius;
            if (pickup) radius = 0.35f;
            else if (path) radius = 0.45f;
            else radius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z) * 0.5f, 0.3f, 3f);
            if (n.StartsWith("fence") || n.StartsWith("wall")) radius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z) * 0.6f, 0.3f, 1.2f);

            list.Add(new Item
            {
                t = t,
                group = group,
                priority = Priority(group, n),
                radius = radius,
                movable = group != "Village",
                isPickup = pickup,
                elongated = !pickup && Mathf.Max(b.extents.x, b.extents.z) > Mathf.Min(b.extents.x, b.extents.z) * 1.8f && (n.StartsWith("fence") || n.StartsWith("wall")),
                rect = new Rect(b.min.x, b.min.z, b.size.x, b.size.z)
            });
        }
        return list;
    }

    private static string why;

    private static bool IsTree(string group) { return group == "Forest" || group == "Trees" || group == "Decor"; }

    private static float Ground(Terrain terrain, Vector2 p)
    {
        return terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
    }

    /// <summary>이 자리에 놓으면 이미 자리 잡은 물건이나 집, 선착장 같은 큰 물건과 겹치는가</summary>
    private static bool Conflicts(Item it, Vector2 p, List<Item> accepted, HashSet<Transform> itemRoots, Terrain terrain)
    {
        foreach (Item o in accepted)
        {
            if (o.t == it.t) continue;
            if (o.elongated)
            {
                float dx = Mathf.Max(o.rect.xMin - p.x, 0f, p.x - o.rect.xMax);
                float dz = Mathf.Max(o.rect.yMin - p.y, 0f, p.y - o.rect.yMax);
                if (dx * dx + dz * dz < (it.radius + 0.2f) * (it.radius + 0.2f)) return true;
                continue;
            }
            float need = it.radius + o.radius;
            if (it.isPickup && o.isPickup) need = 0.8f; // 재료끼리는 겹쳐 놓지 않는다
            else if (it.isPickup && IsTree(o.group)) need = Mathf.Min(o.radius, 0.9f) + it.radius; // 재료는 나뭇잎 아래는 괜찮고 줄기만 피한다
            else if (IsTree(it.group) && IsTree(o.group)) need = Mathf.Min(it.radius, 0.9f) + Mathf.Min(o.radius, 0.9f) + 0.3f; // 나무끼리는 잎이 겹쳐도 되고 줄기만 떨어져 있으면 된다
            if ((o.Pos - p).sqrMagnitude < need * need) { why = o.t.name + " (" + o.group + ") d=" + (o.Pos - p).magnitude.ToString("F1") + " need=" + need.ToString("F1"); return true; }
        }
        // 집, 선착장, 다리 같은 그 밖의 단단한 물건 속에 있는가
        float g = Ground(terrain, p);
        Collider[] hits = Physics.OverlapSphere(new Vector3(p.x, g + 1.0f, p.y), Mathf.Max(0.5f, it.radius * 0.8f));
        foreach (Collider c in hits)
        {
            if (c is TerrainCollider || c.isTrigger) continue;
            Transform root = c.transform;
            bool own = false, listed = false;
            for (Transform up = c.transform; up != null; up = up.parent)
            {
                if (up == it.t) own = true;
                if (itemRoots.Contains(up)) listed = true;
                if (up.name == "Plants") listed = true; // 풀, 고사리, 작은 꽃: 스치고 지나가도 이상하지 않다
            }
            if (own || listed) continue;
            string n = c.name.ToLower();
            if (n.Contains("player") || n.Contains("sea") || n.Contains("water") || n.Contains("ground") || n.Contains("terrain")) continue;
            why = "collider " + c.name + " (" + c.GetType().Name + ")";
            return true;
        }
        return false;
    }

    private static bool FindSpot(Item it, List<Item> accepted, HashSet<Transform> itemRoots, Terrain terrain, out Vector2 spot)
    {
        Vector2 origin = it.Pos;
        float g0 = Ground(terrain, origin);
        float minG = Mathf.Min(g0, 1.7f) - 0.3f;
        int seed = Mathf.Abs(it.t.name.GetHashCode());
        for (float r = 0.8f; r <= 12f; r += 0.7f)
        {
            int steps = Mathf.Max(8, Mathf.CeilToInt(r * 6f));
            for (int i = 0; i < steps; i++)
            {
                float a = (i + (seed % 7) * 0.13f) / steps * Mathf.PI * 2f;
                Vector2 p = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Ground(terrain, p) < minG) continue;
                if (!Conflicts(it, p, accepted, itemRoots, terrain)) { spot = p; return true; }
            }
        }
        spot = origin;
        return false;
    }

    private static bool FindFarSpot(Item it, List<Item> accepted, HashSet<Transform> itemRoots, Terrain terrain, out Vector2 spot)
    {
        Vector2 origin = it.Pos;
        float g0 = Ground(terrain, origin);
        float minG = Mathf.Min(g0, 1.7f) - 0.3f;
        for (float r = 12.5f; r <= 40f; r += 2.5f)
        {
            int steps = Mathf.CeilToInt(r * 2f);
            for (int i = 0; i < steps; i++)
            {
                float a = i / (float)steps * Mathf.PI * 2f;
                Vector2 p = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Ground(terrain, p) < minG) continue;
                if (!Conflicts(it, p, accepted, itemRoots, terrain)) { spot = p; return true; }
            }
        }
        spot = origin;
        return false;
    }

    private static void Move(Item it, Vector2 target, Terrain terrain)
    {
        float gOld = Ground(terrain, it.Pos);
        float gNew = Ground(terrain, target);
        Vector3 pos = it.t.position;
        it.t.position = new Vector3(target.x, pos.y + (gNew - gOld), target.y);
        Physics.SyncTransforms();
    }
}
