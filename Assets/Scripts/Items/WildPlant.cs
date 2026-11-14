using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 섬에 그냥 자란 풀. 바라보고 E를 누르면 뜯어서, 이 구역에서 나는 식물 중 무작위로 하나가 가방으로 들어온다.
/// 뜯은 풀은 모습이 사라졌다가 다음 날 아침에 다시 자란다. 집에 들어갔다 나와도 뜯은 채로 남는다. (GameState가 기억한다)
/// 나오는 재료: 얻는 방법이 Wild인 재료, 그리고 맨손으로 줍는 식물(식물, 바닥 덮기)이 항상 나오는 것들.
/// 구역에 맞는 것과 어느 구역에서나 나는 것이 섞여 나온다. zone이 Anywhere인 풀(가운데 풀밭)에서는 모든 구역의 것이 나온다.
/// 풀 오브젝트(Collider가 있는 것)에 붙인다. 섬 만들기(IslandSetup)가 붙여 준다.
/// </summary>
public class WildPlant : MonoBehaviour, IInteractable
{
    [Tooltip("이 풀이 있는 구역. Anywhere면 모든 구역의 재료가 나온다")]
    [SerializeField] private MaterialZone zone = MaterialZone.Anywhere;

    private string id;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool collected;

    public string Prompt => collected ? null : "[E] 풀 뜯기";

    private void Awake()
    {
        Vector3 p = transform.position;
        id = "w_" + p.x.ToString("F1") + "_" + p.z.ToString("F1");
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        collected = GameState.IsTaken(id);
        ApplyVisibility();
    }

    private void OnEnable() { GameState.DayStarted += Regrow; }
    private void OnDisable() { GameState.DayStarted -= Regrow; }

    public void Interact(Interactor who)
    {
        if (collected) return;

        // 가방에 넣을 수 있는 것만 후보로 한다 (이미 가진 재료거나 빈 칸이 있을 때)
        var candidates = new List<MaterialData>();
        foreach (MaterialData m in Pool())
            if (who.Bag.CanAdd(m)) candidates.Add(m);
        if (candidates.Count == 0)
        {
            who.ShowMessage("가방이 가득 찼어요");
            return;
        }

        MaterialData got = candidates[Random.Range(0, candidates.Count)];
        if (!who.Bag.TryAdd(got)) { who.ShowMessage("가방이 가득 찼어요"); return; }
        who.ShowMessage(got.displayName + " +1");

        GameState.MarkTaken(id);
        collected = true;
        ApplyVisibility();
    }

    /// <summary>이 풀에서 나올 수 있는 재료들</summary>
    private List<MaterialData> Pool()
    {
        var list = new List<MaterialData>();
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null || db.materials == null) return list;

        foreach (MaterialData m in db.materials)
        {
            if (m == null || m.isRare || m.condition != MaterialCondition.Always) continue;
            bool wild = m.acquire == AcquireMethod.Wild;
            bool plantLike = m.acquire == AcquireMethod.Hand && (m.kind == MaterialKind.Plant || m.kind == MaterialKind.Cover);
            if (!wild && !plantLike) continue;
            if (zone != MaterialZone.Anywhere && m.zone != zone && m.zone != MaterialZone.Anywhere) continue;
            list.Add(m);
        }
        return list;
    }

    private void Regrow()
    {
        collected = false;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        bool show = !collected;
        if (renderers != null) foreach (Renderer r in renderers) if (r != null) r.enabled = show;
        if (colliders != null) foreach (Collider c in colliders) if (c != null) c.enabled = show;
    }
}
