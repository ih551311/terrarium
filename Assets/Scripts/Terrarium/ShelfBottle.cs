using UnityEngine;

/// <summary>
/// 선반에 전시된 병 하나. 바라보고 E를 누르면 병을 크게 보여 준다. (자세히 보기 화면에서 돌려 보고, 선반에서 내릴 수도 있다)
/// BottleShelf가 병을 선반에 올릴 때 붙인다.
/// </summary>
public class ShelfBottle : MonoBehaviour, IInteractable
{
    private string bottleId;
    private string displayName;

    public void Init(string id, string name)
    {
        bottleId = id;
        displayName = name;
    }

    public string Prompt => "[E] 자세히 보기 - " + displayName;

    public void Interact(Interactor who)
    {
        BottleDetailPanel panel = GameRoot.Instance != null ? GameRoot.Instance.BottleDetailPanel : null;
        if (panel == null) return;
        string problem = panel.Open(bottleId);
        if (problem != null) who.ShowMessage(problem);
    }
}
