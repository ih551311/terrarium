using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬의 식물 소품 크기를 사람(키 1.8m)에 어울리게 맞춘다. 에셋 팩의 풀, 꽃, 버섯은 사람보다 크게 들어와 있었다.
/// 모델 종류(이름에서 끝 번호를 뗀 것)마다 "평균 높이"를 목표값으로 정하고, 그 종류의 모든 것에 같은 배율을 곱한다
/// (종류 안의 크기 차이는 그대로 남는다). 땅에 닿은 높이는 그대로 두고 키운다.
/// 목표 높이를 기준으로 맞추므로 여러 번 눌러도 같은 결과가 나온다 (두 번째부터는 배율이 1).
/// 메뉴: 유리병 정원 > 식물 크기 맞추기 / 모든 섬 식물 크기 맞추기
/// </summary>
public static class SizeBalancer
{
    private static readonly string[] Groups = { "Plants", "Decor" };

    // 종류별 목표 평균 높이(미터). 앞의 글자가 같은 모든 종류에 쓴다 (예: "Mushroom"은 Mushroom01A~06A).
    private static readonly KeyValuePair<string, float>[] Targets =
    {
        new KeyValuePair<string, float>("Mushroom", 0.45f),   // 버섯: 발목~무릎
        new KeyValuePair<string, float>("Plant01", 0.40f),
        new KeyValuePair<string, float>("Plant02", 0.55f),
        new KeyValuePair<string, float>("Plant04", 0.80f),    // 큰 풀과 꽃: 허리 높이
        new KeyValuePair<string, float>("Plant05", 0.80f),
        new KeyValuePair<string, float>("Plant06", 0.80f),
        new KeyValuePair<string, float>("Plant07", 0.80f),
        new KeyValuePair<string, float>("Plant09", 0.40f),
        new KeyValuePair<string, float>("Plant12", 0.38f),
        new KeyValuePair<string, float>("Plant13", 0.38f),
        new KeyValuePair<string, float>("Plant15", 0.30f),
        new KeyValuePair<string, float>("Plant16", 0.30f),
        new KeyValuePair<string, float>("aloe", 0.50f),       // 사막
        new KeyValuePair<string, float>("deco_dry_branch", 0.65f),
    };

    [MenuItem("유리병 정원/식물 크기 맞추기")]
    public static void BalanceMenu()
    {
        Debug.Log(BalanceOpenScene(false));
    }

    [MenuItem("유리병 정원/모든 섬 식물 크기 맞추기")]
    public static void BalanceAllMenu()
    {
        EditorSceneManager.SaveOpenScenes();
        var sb = new StringBuilder();
        foreach (string path in new[] { "Assets/Scenes/Island.unity", "Assets/Scenes/SnowIsland.unity", "Assets/Scenes/DesertIsland.unity" })
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            sb.AppendLine("== " + path);
            sb.AppendLine(BalanceOpenScene(false));
        }
        Debug.Log(sb.ToString());
    }

    public static string BalanceOpenScene(bool dryRun)
    {
        var set = new HashSet<string>(Groups);
        var items = new Dictionary<string, List<Transform>>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.parent == null || !set.Contains(t.parent.name)) continue;
            string key = Regex.Replace(t.name, @"[ _]?\(?\d+\)?$", "");
            if (!TargetFor(key, out _)) continue;
            if (!items.ContainsKey(key)) items[key] = new List<Transform>();
            items[key].Add(t);
        }

        var sb = new StringBuilder();
        int changed = 0;
        foreach (var kv in items)
        {
            TargetFor(kv.Key, out float target);
            float sum = 0f;
            int n = 0;
            foreach (Transform t in kv.Value)
            {
                if (!TryBounds(t, out Bounds b)) continue;
                sum += b.size.y; n++;
            }
            if (n == 0) continue;
            float avg = sum / n;
            float factor = target / avg;
            sb.AppendLine(kv.Key + " n=" + n + " 평균 " + avg.ToString("F2") + "m -> " + target.ToString("F2") + "m (배율 " + factor.ToString("F2") + ")");
            if (dryRun || Mathf.Abs(factor - 1f) < 0.02f) continue;

            foreach (Transform t in kv.Value)
            {
                if (!TryBounds(t, out Bounds before)) continue;
                t.localScale *= factor;
                if (TryBounds(t, out Bounds after)) t.position += Vector3.up * (before.min.y - after.min.y); // 땅에 닿은 자리를 지킨다
                EditorUtility.SetDirty(t);
                changed++;
            }
        }
        int clearedColliders = dryRun ? 0 : ClearSolidColliders(items);
        if (clearedColliders > 0) { sb.AppendLine("막는 충돌 " + clearedColliders + "개 없앰 (풀과 꽃은 걸어서 지나간다)"); changed++; }

        if (!dryRun && changed > 0)
        {
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        return (dryRun ? "[미리보기] " : "") + "크기를 바꾼 것 " + changed + "개\n" + sb;
    }

    /// <summary>
    /// 풀, 꽃, 버섯, 알로에는 막는 충돌(메시 콜라이더 3개씩)이 달려 있어서 풀밭을 걸으면 자꾸 걸렸고 물리 계산도 무거웠다.
    /// 막는 충돌은 없애고, 캘 수 있는 것(Harvestable)에는 바라볼 때 잡히는 투명 상자(트리거)만 둔다. (이미 있으면 그대로)
    /// </summary>
    private static int ClearSolidColliders(Dictionary<string, List<Transform>> items)
    {
        int removed = 0;
        foreach (var kv in items)
        {
            foreach (Transform t in kv.Value)
            {
                if (t == null) continue;
                Collider[] all = t.GetComponentsInChildren<Collider>(true);
                bool hasTrigger = false;
                foreach (Collider c in all) if (c.isTrigger) hasTrigger = true;
                bool harvestable = t.GetComponentInChildren<Harvestable>(true) != null;
                if (harvestable && !hasTrigger && TryBounds(t, out Bounds b))
                {
                    BoxCollider box = t.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.center = t.InverseTransformPoint(b.center);
                    Vector3 size = t.InverseTransformVector(b.size);
                    box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                }
                foreach (Collider c in all)
                {
                    if (c == null || c.isTrigger) continue;
                    Object.DestroyImmediate(c);
                    removed++;
                }
            }
        }
        return removed;
    }

    private static bool TargetFor(string key, out float target)
    {
        foreach (var kv in Targets)
            if (key.StartsWith(kv.Key)) { target = kv.Value; return true; }
        target = 0f;
        return false;
    }

    private static bool TryBounds(Transform t, out Bounds b)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        b = new Bounds();
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return true;
    }
}
