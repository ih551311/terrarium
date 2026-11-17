using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 섬 바닥에 놓인 줍는 재료(Pickup)가 너무 작아서 안 보이지 않게, 모델의 가장 긴 변이 MinSize(미터)보다 작으면 키운다.
/// (모래 달러, 바다 유리, 풍뎅이, 청개구리처럼 원래 모델이 몇 cm인 것들). 키운 뒤 바닥이 땅에 닿게 다시 놓고 바라보기 상자도 맞춘다.
/// 섬 만들기(눈 섬, 사막 섬)와 섬에 새 재료 놓기가 끝에 부른다. 메뉴: 유리병 정원 > 줍는 재료 크기 맞추기 (지금 열린 씬)
/// </summary>
public static class PickupSize
{
    public const float MinSize = 0.28f;

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>줍는 자리 하나(go)의 모델(model)을 최소 크기로 맞춘다. 키웠으면 true.</summary>
    public static bool Fit(GameObject go, GameObject model)
    {
        if (model.GetComponentsInChildren<Renderer>().Length == 0) return false;
        Bounds b = BoundsOf(model);
        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        bool grown = false;
        if (maxDim > 0.0001f && maxDim < MinSize)
        {
            model.transform.localScale *= MinSize / maxDim;
            grown = true;
        }
        b = BoundsOf(model);
        model.transform.position += Vector3.up * (go.transform.position.y - b.min.y);
        b = BoundsOf(model);
        BoxCollider box = go.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(Mathf.Max(b.size.x, 0.4f), Mathf.Max(b.size.y, 0.4f), Mathf.Max(b.size.z, 0.4f));
        }
        return grown;
    }

    [MenuItem("유리병 정원/줍는 재료 크기 맞추기")]
    public static string FixOpenScene()
    {
        int grown = 0, total = 0;
        foreach (Pickup p in Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform model = p.transform.Find("Model");
            if (model == null) continue;
            total++;
            if (Fit(p.gameObject, model.gameObject)) grown++;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        return "줍는 재료 " + total + "곳 중 " + grown + "곳을 키웠어요";
    }
}
