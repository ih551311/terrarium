using UnityEngine;

/// <summary>
/// 선착장. 바라보고 E를 누른다.
/// - 처음에는 부서진 선착장이라 코인을 내고 고쳐야 한다 (needsPurchase). 고치면 저장되고(GameState.DockOpen) 배가 나타난다.
/// - 고친 뒤에는 E로 배를 타고 다른 섬으로 간다 (targetScene의 spawnId 자리에 선다).
/// 눈 섬 쪽 선착장은 이미 열려 있어서(needsPurchase 끔) 언제든 배를 타고 돌아올 수 있다.
/// 부서진 모습(closedGroup)과 고친 모습(openGroup)은 DockSetup이 만든다.
/// </summary>
public class Dock : MonoBehaviour, IInteractable
{
    [SerializeField] private string targetScene = "SnowIsland";
    [SerializeField] private string spawnId = "FromDock";
    [SerializeField] private string travelLabel = "배 타고 눈 섬으로 가기";
    [SerializeField] private bool needsPurchase = true;
    [SerializeField] private int cost = 150;

    [Tooltip("부서진 선착장 모습 (고치기 전에만 보인다)")]
    [SerializeField] private GameObject closedGroup;
    [Tooltip("고친 선착장 모습: 긴 부두와 배 (고친 뒤에 보인다)")]
    [SerializeField] private GameObject openGroup;

    public bool IsOpen => !needsPurchase || GameState.DockOpen;
    public int Cost => cost;

    public string Prompt => IsOpen ? "[E] " + travelLabel : "[E] 부서진 선착장 고치기 (" + cost + "코인)";

    private void Update()
    {
        bool open = IsOpen;
        if (closedGroup != null && closedGroup.activeSelf == open) closedGroup.SetActive(!open);
        if (openGroup != null && openGroup.activeSelf != open) openGroup.SetActive(open);
    }

    public void Interact(Interactor who)
    {
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

        GameState.OpenDock();
        GameAudio.Play(GameAudio.Sfx.Coin, 1f);
        who.ShowMessage("선착장을 고쳤어요! 이제 배를 타고 눈 섬에 갈 수 있어요");
        if (GameRoot.Instance != null) GameRoot.Instance.SaveNow();
    }
}
