using UnityEngine;

/// <summary>
/// 섬에 놓인, 주울 수 있는 재료. 재료 오브젝트(또는 그 부모)에 붙이고 Collider가 있어야 한다.
/// 주운 자리는 비어 있다가 다음 날 아침에 다시 채워진다. 집에 들어갔다 나와도 비어 있는 채로 남는다.
/// 모델은 나중에 이 오브젝트의 자식을 바꿔 끼우면 된다.
/// </summary>
public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private MaterialData material;

    private string id;

    public MaterialData Material => material;

    public string Prompt =>
        material != null ? "[E] " + material.displayName + " 줍기" : null;

    private void Awake()
    {
        Vector3 p = transform.position;
        id = "p_" + p.x.ToString("F1") + "_" + p.z.ToString("F1");
        if (GameState.IsTaken(id)) gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameState.DayStarted += Refill;
    }

    private void OnDisable()
    {
        GameState.DayStarted -= Refill;
    }

    public void Interact(Interactor who)
    {
        if (who.Bag.TryAdd(material))
            Collect();
        else
            who.ShowMessage("가방이 가득 찼어요");
    }

    /// <summary>주웠을 때 호출. 숨기고, 오늘 가져간 자리로 적어 둔다.</summary>
    public void Collect()
    {
        GameState.MarkTaken(id);
        gameObject.SetActive(false);
    }

    /// <summary>다음 날 아침에 다시 나타난다. (숨겨진 오브젝트는 이 함수를 못 받으므로, 같은 씬에서 자고 일어났을 때만 쓰인다)</summary>
    private void Refill()
    {
        gameObject.SetActive(true);
    }
}
