using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 선착장에서 E를 누르면 열리는 "어느 섬으로 갈까요?" 창. 화면은 Assets/Resources/UI/DockPanel.prefab (캔버스)에 있다.
/// 섬마다 따로 코인을 내고 열고(눈 섬 150코인, 사막 섬 250코인), 열린 섬은 "가기"로 배를 탄다. 열림은 저장된다(GameState.IsDockOpen).
/// 새 섬을 더하려면 아래 Destinations 표에 한 줄을 더하고 GameState에 열림 상태를 더한다.
/// </summary>
public class DockPanel : MonoBehaviour
{
    private struct Destination
    {
        public string name, scene, spawn, blurb;
        public int dockId, cost;
        public Destination(string name, string scene, string spawn, int dockId, int cost, string blurb)
        {
            this.name = name; this.scene = scene; this.spawn = spawn; this.dockId = dockId; this.cost = cost; this.blurb = blurb;
        }
    }

    private static readonly Destination[] Destinations =
    {
        new Destination("눈 섬",   "SnowIsland",   "FromDock", 0, 150, "늘 눈이 내리는 섬. 얼음 낚시와 오로라"),
        new Destination("사막 섬", "DesertIsland", "FromDock", 1, 250, "오아시스와 피라미드가 있는 모래 섬"),
    };

    private static DockPanel instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text status;
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] names;
    [SerializeField] private TMP_Text[] blurbs;
    [SerializeField] private Button closeButton;
    private bool isOpen;
    private int openedFrame = -1; // 여는 데 쓴 E 키가 같은 프레임에 창을 바로 닫지 않게

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /// <summary>선착장을 바라보고 E를 눌렀을 때 부른다. 처음 부를 때 창을 만든다.</summary>
    public static void Open(Interactor who)
    {
        if (instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("UI/DockPanel");
            if (prefab == null) { Debug.LogError("UI/DockPanel 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
            GameObject go = Instantiate(prefab);
            go.name = "DockPanel";
            DontDestroyOnLoad(go);
            instance = go.GetComponent<DockPanel>();
        }
        instance.Show();
    }

    /// <summary>화면을 굽는 편집기 도구가 쓴다 (UIBake)</summary>
    public static int DestinationCount => Destinations.Length;
    public static string NameOf(int i) => Destinations[i].name;
    public static string BlurbOf(int i) => Destinations[i].blurb;

    private void Awake()
    {
        for (int i = 0; i < Destinations.Length; i++)
        {
            int index = i;
            names[i].text = Destinations[i].name;
            blurbs[i].text = Destinations[i].blurb;
            buttons[i].onClick.AddListener(() => Clicked(index));
        }
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }
    private void Show()
    {
        if (isOpen) return;
        isOpen = true;
        openedFrame = Time.frameCount;
        root.SetActive(true);
        status.text = "코인: " + GameState.Coins;
        Refresh();
        UIFocus.Open();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.6f);
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        root.SetActive(false);
        UIFocus.Close();
    }

    private void Update()
    {
        if (!isOpen || Time.frameCount == openedFrame) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) Close();
    }

    private void Refresh()
    {
        for (int i = 0; i < Destinations.Length; i++)
        {
            Destination d = Destinations[i];
            bool open = GameState.IsDockOpen(d.dockId);
            UIKit.SetButtonText(buttons[i], open ? "가기" : d.cost + "코인으로 열기");
            buttons[i].interactable = open || GameState.Coins >= d.cost;
        }
    }

    private void Clicked(int index)
    {
        Destination d = Destinations[index];
        if (GameState.IsDockOpen(d.dockId))
        {
            Close();
            if (GameRoot.Instance != null) GameRoot.Instance.GoTo(d.scene, d.spawn);
            return;
        }
        if (!GameState.TrySpend(d.cost))
        {
            status.text = "코인이 모자라요";
            return;
        }
        GameState.OpenDock(d.dockId);
        GameAudio.Play(GameAudio.Sfx.Coin, 1f);
        status.text = "코인: " + GameState.Coins;
        Refresh();
        if (GameRoot.Instance != null) GameRoot.Instance.SaveNow();
    }
}
