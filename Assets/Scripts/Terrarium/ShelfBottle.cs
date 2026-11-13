using UnityEngine;

/// <summary>
/// 선반에 전시된 병 하나. 바라보고 E를 누르면 선반에서 내린다. (병은 지워지지 않고 그대로 남는다)
/// 다시 전시하려면 병 꾸미기 화면에서 그 병을 열고 "선반에 전시하기"를 누른다.
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

    public string Prompt => "[E] 선반에서 내리기 - " + displayName;

    public void Interact(Interactor who)
    {
        BottleStore.Undisplay(bottleId);
        who.ShowMessage(displayName + "을(를) 선반에서 내렸어요");
    }
}
