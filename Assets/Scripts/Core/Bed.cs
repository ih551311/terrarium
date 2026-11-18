using UnityEngine;

/// <summary>
/// 침대. 바라보고 E를 누르면 자고 일어나 다음 날이 된다.
/// 보낸 재료의 코인과 주문한 물건이 우체통에 도착한다. (밤 화면, 저장, 식물 성장은 이후 단계)
/// 침대 오브젝트(Collider가 있는 것을 자식으로 가진 뿌리)에 붙인다.
/// </summary>
public class Bed : MonoBehaviour, IInteractable
{
    public string Prompt => "[E] 자기";

    public void Interact(Interactor who)
    {
        bool mail = GameState.Sleep();
        string text = GameState.Day + "일째 아침";
        if (GameState.NewRequestToday) text += "\n우체통에 편지가 와 있어요";
        else if (mail) text += "\n우체통에 도착한 게 있어요";

        // 자고 일어나면 저장한다 (안 될 때만 알린다)
        if (!SaveSystem.Save(who.Bag)) text += "\n저장하지 못했어요";
        who.ShowMessage(text);
    }
}
