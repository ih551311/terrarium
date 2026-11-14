using UnityEngine;

/// <summary>
/// 오두막 앞의 우체통(상점). 바라보고 E를 누르면 우체통 패널이 열린다.
/// 병 주문 편지가 와 있으면 먼저 그 편지 화면(RequestPanel)이 열리고, 거기서 우체통 패널로 갈 수 있다.
/// 도착한 것이 있으면 안내 글자에 알려 준다. 우체통 오브젝트에 붙이고 Collider를 둔다.
/// </summary>
public class Mailbox : MonoBehaviour, IInteractable
{
    public string Prompt
    {
        get
        {
            if (GameState.HasUnreadRequest) return "[E] 우체통 열기 (편지가 왔어요!)";
            return GameState.HasMail ? "[E] 우체통 열기 (도착한 것이 있어요!)" : "[E] 우체통 열기";
        }
    }

    public void Interact(Interactor who)
    {
        if (RequestPanel.TryOpen()) return;
        GameRoot.Instance.MailboxPanel.Open();
    }
}
