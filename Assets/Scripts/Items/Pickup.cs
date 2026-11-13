using UnityEngine;

/// <summary>
/// 섬에 놓인, 채취하거나 주울 수 있는 재료. 재료 오브젝트(또는 그 부모)에 붙이고 Collider가 있어야 한다.
/// - 식물과 이끼는 "채취"하고, 조개나 모래 같은 것은 "줍는다". 얻으면 "고사리 +1"처럼 알림이 뜬다.
/// - 달맞이꽃처럼 밤에만 나는 재료는 밤이 되어야 나타나고, 아침이 되면 사라진다. (하늘 시간과 연결)
/// - 채취한 자리는 비어 있다가 다음 날 아침에 다시 채워진다. 집에 들어갔다 나와도 비어 있는 채로 남는다.
/// 모델은 나중에 이 오브젝트의 자식을 바꿔 끼우면 된다.
/// </summary>
public class Pickup : MonoBehaviour, IInteractable
{
    /// <summary>하루 중 이 시간(0~1)부터는 밤이다. (SkyController의 밤 하늘로 섞이기 시작하는 때와 같다)</summary>
    public const float NightStart = 0.78f;

    [SerializeField] private MaterialData material;

    private string id;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool shown = true;
    private bool collected; // 오늘 이미 채취했는가 (오브젝트는 켜 둔 채 모습만 숨겨서, 아침 알림을 계속 받는다)

    public MaterialData Material => material;

    /// <summary>지금 채취할 수 있는가. 밤에만 나는 재료는 밤에만 true다.</summary>
    public bool Available => material != null && !collected &&
        (material.condition != MaterialCondition.NightOnly || GameState.TimeOfDay >= NightStart);

    private bool IsPlantLike => material != null && (material.kind == MaterialKind.Plant || material.kind == MaterialKind.Cover);

    public string Prompt
    {
        get
        {
            if (material == null || !Available) return null;
            return "[E] " + material.displayName + (IsPlantLike ? " 채취" : " 줍기");
        }
    }

    private void Awake()
    {
        Vector3 p = transform.position;
        id = "p_" + p.x.ToString("F1") + "_" + p.z.ToString("F1");
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        collected = GameState.IsTaken(id);
    }

    private void OnEnable()
    {
        GameState.DayStarted += Refill;
        ApplyVisibility(true);
    }

    private void OnDisable()
    {
        GameState.DayStarted -= Refill;
    }

    private void Update()
    {
        // 밤에만 나는 재료는 밤이 되면 나타나고 아침이 되면 사라진다
        bool should = Available;
        if (should != shown) ApplyVisibility(should);
    }

    private void ApplyVisibility(bool visible)
    {
        shown = visible && Available;
        if (renderers == null) return;
        foreach (Renderer r in renderers) if (r != null) r.enabled = shown;
        foreach (Collider c in colliders) if (c != null) c.enabled = shown;
    }

    public void Interact(Interactor who)
    {
        if (!Available) return;
        if (who.Bag.TryAdd(material))
        {
            who.ShowMessage(material.displayName + " +1");
            Collect();
        }
        else
        {
            who.ShowMessage("가방이 가득 찼어요");
        }
    }

    /// <summary>채취했을 때 호출. 숨기고, 오늘 가져간 자리로 적어 둔다.</summary>
    public void Collect()
    {
        GameState.MarkTaken(id);
        collected = true;
        ApplyVisibility(false);
    }

    /// <summary>다음 날 아침에 다시 나타난다.</summary>
    private void Refill()
    {
        collected = false;
        ApplyVisibility(true);
    }
}
