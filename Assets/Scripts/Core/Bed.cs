using UnityEngine;

/// <summary>
/// 침대. 바라보고 E를 누르면 잔다. 화면이 어두워졌다가(SleepSequence) 아침이 되어 다음 날이 된다.
/// 보낸 재료의 코인과 주문한 물건이 우체통에 도착하고, 자고 일어나면 저장한다.
/// 침대 오브젝트(Collider가 있는 것을 자식으로 가진 뿌리)에 붙인다.
/// </summary>
public class Bed : MonoBehaviour, IInteractable
{
    public string Prompt => SleepSequence.Sleeping ? string.Empty : "[E] 자기";

    public void Interact(Interactor who)
    {
        SleepSequence.Begin(who);
    }
}
