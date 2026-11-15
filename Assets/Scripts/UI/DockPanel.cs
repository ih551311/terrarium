using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 선착장에서 E를 누르면 열리는 "어느 섬으로 갈까요?" 창. 코드로 만든다(UIKit).
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

    private GameObject root;
    private TMP_Text status;
    private Button[] buttons;
    private TMP_Text[] blurbs;
    private bool isOpen;

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
            GameObject go = new GameObject("DockPanel");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DockPanel>();
            instance.Build();
        }
        instance.Show();
    }

    private void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("DockPanelCanvas", 380);
        canvas.transform.SetParent(transform, false);
        root = new GameObject("Root", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        UIKit.Backdrop(root.transform);
        RectTransform panel = UIKit.Panel(root.transform, "Panel", new Vector2(760f, 200f + Destinations.Length * 150f));
        float top = panel.sizeDelta.y * 0.5f;

        UIKit.Label(panel, "어느 섬으로 갈까요?", 46f, new Vector2(0f, top - 60f), new Vector2(680f, 70f));
        buttons = new Button[Destinations.Length];
        blurbs = new TMP_Text[Destinations.Length];
        for (int i = 0; i < Destinations.Length; i++)
        {
            int index = i;
            float y = top - 160f - i * 150f;
            UIKit.Label(panel, Destinations[i].name, 38f, new Vector2(-170f, y + 22f), new Vector2(340f, 50f), TextAlignmentOptions.Left);
            blurbs[i] = UIKit.Label(panel, Destinations[i].blurb, 22f, new Vector2(-170f, y - 28f), new Vector2(340f, 60f), TextAlignmentOptions.TopLeft);
            buttons[i] = UIKit.MakeButton(panel, "", new Vector2(220f, y), new Vector2(280f, 84f), () => Clicked(index), 28f);
        }
        status = UIKit.Label(panel, "", 26f, new Vector2(0f, -top + 90f), new Vector2(680f, 40f));
        UIKit.MakeButton(panel, "닫기", new Vector2(0f, -top + 38f), new Vector2(220f, 56f), Close, 28f);
        root.SetActive(false);
    }

    private void Show()
    {
        if (isOpen) return;
        isOpen = true;
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
        if (!isOpen) return;
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
            status.text = "코인이 모자라요 (" + d.cost + "코인 필요, 지금 " + GameState.Coins + "코인)";
            return;
        }
        GameState.OpenDock(d.dockId);
        GameAudio.Play(GameAudio.Sfx.Coin, 1f);
        status.text = d.name + "으로 가는 배가 열렸어요! (코인 " + GameState.Coins + ")";
        Refresh();
        if (GameRoot.Instance != null) GameRoot.Instance.SaveNow();
    }
}
