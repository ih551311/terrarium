using UnityEngine;

/// <summary>
/// 침대. 바라보고 E를 누르면 잔다. 화면이 어두워졌다가(SleepSequence) 아침이 되어 다음 날이 된다.
/// 보낸 재료의 코인과 주문한 물건이 우체통에 도착하고, 자고 일어나면 저장한다.
/// 하루의 6할이 지난 저녁부터 잘 수 있다 (낮에는 "아직 졸리지 않아요"). 시간은 GameState.TimeOfDay(0=아침, 1=밤 끝).
/// 침대 오브젝트(Collider가 있는 것을 자식으로 가진 뿌리)에 붙인다.
/// </summary>
public class Bed : MonoBehaviour, IInteractable
{
    /// <summary>이 시각 이후에 잘 수 있다 (0.6 = 해가 기울기 시작할 무렵)</summary>
    public const float SleepFrom = 0.6f;

    private static bool CanSleep => GameState.TimeOfDay >= SleepFrom;

    public string Prompt => SleepSequence.Sleeping ? string.Empty : CanSleep ? "[E] 자기" : "아직 졸리지 않아요";

    public void Interact(Interactor who)
    {
        if (!CanSleep)
        {
            who.ShowMessage("아직 졸리지 않아요. 해가 기울면 잘 수 있어요");
            return;
        }
        SleepSequence.Begin(who);
    }
}
