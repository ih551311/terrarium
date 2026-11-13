using UnityEngine;

/// <summary>
/// 오두막 앞의 우체통(상점). 바라보고 E를 누르면 우체통 패널이 열린다.
/// 도착한 것이 있으면 안내 글자에 알려 준다. 우체통 오브젝트에 붙이고 Collider를 둔다.
/// </summary>
public class Mailbox : MonoBehaviour, IInteractable
{
    public string Prompt => GameState.HasMail ? "[E] 우체통 열기 (도착한 것이 있어요!)" : "[E] 우체통 열기";

    public void Interact(Interactor who)
    {
        GameRoot.Instance.MailboxPanel.Open();
    }
}
