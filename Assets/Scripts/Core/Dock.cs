using UnityEngine;

/// <summary>
/// 선착장. 바라보고 E를 누른다.
/// - 원래 섬의 선착장은 "허브"(hub)다. E를 누르면 가고 싶은 섬을 고르는 창(DockPanel)이 열리고,
///   섬마다 따로 코인을 내고 열어야 갈 수 있다 (눈 섬 150코인, 사막 섬 250코인. 열림은 저장된다 - GameState.IsDockOpen).
/// - 눈 섬과 사막 섬 쪽 선착장은 길이 하나뿐이라서 E를 누르면 바로 원래 섬으로 돌아간다 (targetScene의 spawnId 자리에 선다).
/// - (예전 방식) needsPurchase를 켜면 부서진 선착장을 고쳐야 그 섬으로 가는 선착장이 된다.
/// 부서진 모습(closedGroup)과 고친 모습(openGroup)은 DockSetup이 만든다.
/// </summary>
public class Dock : MonoBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "SnowIsland";
    [SerializeField] private string spawnId = "FromDock";
    [SerializeField] private string travelLabel = "배 타고 눈 섬으로 가기";
    [SerializeField] private bool needsPurchase = true;
    [SerializeField] private int cost = 150;
    [Tooltip("몇 번 섬의 선착장인가 (0 눈 섬, 1 사막 섬). 열렸는지는 이 번호로 저장된다")]
    [SerializeField] private int dockId;
    [SerializeField] private string islandName = "눈 섬";
    [Tooltip("켜면 E로 가고 싶은 섬을 고르는 창을 연다 (원래 섬의 선착장)")]
    [SerializeField] private bool hub;

    [Tooltip("부서진 선착장 모습 (고치기 전에만 보인다)")]
    [SerializeField] private GameObject closedGroup;
    [Tooltip("고친 선착장 모습: 긴 부두와 배 (고친 뒤에 보인다)")]
    [SerializeField] private GameObject openGroup;

    public bool IsOpen => hub || !needsPurchase || GameState.IsDockOpen(dockId);
    public int Cost => cost;

    public string Prompt
    {
        get
        {
            if (hub) return "[E] 배 타고 다른 섬으로 가기";
            return IsOpen ? "[E] " + travelLabel : "[E] 부서진 선착장 고치기 (" + cost + "코인)";
        }
    }

    private void Update()
    {
        bool open = IsOpen;
        if (closedGroup != null && closedGroup.activeSelf == open) closedGroup.SetActive(!open);
        if (openGroup != null && openGroup.activeSelf != open) openGroup.SetActive(open);
    }

    public void Interact(Interactor who)
    {
        if (hub)
        {
            DockPanel.Open(who);
            return;
        }

        if (IsOpen)
        {
            if (GameRoot.Instance != null) GameRoot.Instance.GoTo(targetScene, spawnId);
            return;
        }

        if (!GameState.TrySpend(cost))
        {
            who.ShowMessage("코인이 모자라요 (" + cost + "코인 필요, 지금 " + GameState.Coins + "코인)");
            return;
        }

        GameState.OpenDock(dockId);
        GameAudio.Play(GameAudio.Sfx.Coin, 1f);
        who.ShowMessage("선착장을 고쳤어요! 이제 배를 타고 " + islandName + "에 갈 수 있어요");
        if (GameRoot.Instance != null) GameRoot.Instance.SaveNow();
    }
}
