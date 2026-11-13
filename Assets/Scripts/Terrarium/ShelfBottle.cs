using System.Collections;
using UnityEngine;

/// <summary>
/// 선반 위의 병 하나. 바라보고 E를 누르면 물을 준다. 병마다 하루에 한 번이다. (기획서 6.3)
/// 물을 준 다음 날 아침, 병 속 식물이 한 단계 자란다. 물을 안 줘도 시들지 않는다.
/// 식물이 하나도 없거나 모두 다 자랐으면 물을 줄 필요가 없다고 알려 준다.
/// BottleShelf가 병을 선반에 올릴 때 붙인다.
/// </summary>
public class ShelfBottle : MonoBehaviour, IInteractable
{
    private string bottleId;
    private string displayName;
    private Vector3 baseScale;

    public void Init(string id, string name)
    {
        bottleId = id;
        displayName = name;
        baseScale = transform.localScale;
    }

    public string Prompt
    {
        get
        {
            BottleRecord record = BottleStore.Get(bottleId);
            if (record.wateredToday) return displayName + " (오늘은 물을 줬어요)";
            if (!HasGrowingPlant(record)) return displayName + " (자라는 식물이 없어요)";
            return "[E] 물 주기 - " + displayName;
        }
    }

    public void Interact(Interactor who)
    {
        BottleRecord record = BottleStore.Get(bottleId);
        if (record.wateredToday) { who.ShowMessage("오늘은 이미 물을 줬어요"); return; }
        if (!HasGrowingPlant(record))
        {
            bool anyPlant = false;
            foreach (PlacedData item in record.items) anyPlant |= item.material != null && item.material.kind == MaterialKind.Plant;
            who.ShowMessage(anyPlant ? "식물이 모두 다 자랐어요" : "이 병에는 자라는 식물이 없어요");
            return;
        }

        record.wateredToday = true;
        who.ShowMessage("물을 줬어요. 내일 아침 식물이 자라요");
        StopAllCoroutines();
        StartCoroutine(Bounce());
    }

    /// <summary>물이 한 단계 더 필요한 식물이 있는가 (다 자란 식물은 제외)</summary>
    private static bool HasGrowingPlant(BottleRecord record)
    {
        foreach (PlacedData item in record.items)
            if (item.material != null && item.material.kind == MaterialKind.Plant && item.growth < PlacedMaterial.MaxGrowth) return true;
        return false;
    }

    // 물을 받았다는 표시로 병이 살짝 통통 튄다 (물뿌리개 연출은 14단계에서)
    private IEnumerator Bounce()
    {
        const float Duration = 0.35f;
        for (float t = 0f; t < Duration; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / Duration * Mathf.PI * 2f) * (1f - t / Duration);
            transform.localScale = new Vector3(baseScale.x * (1f + 0.06f * k), baseScale.y * (1f - 0.08f * k), baseScale.z * (1f + 0.06f * k));
            yield return null;
        }
        transform.localScale = baseScale;
    }
}
