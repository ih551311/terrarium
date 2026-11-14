using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 우체통 패널. 처음에는 메뉴(판매하기, 업그레이드, 구매하기, 받기 버튼)가 뜨고, 버튼을 누르면 그 화면만 따로 열린다.
/// - 판매하기: 가방의 재료를 눌러 하나씩 넣는다. 다음 날 아침 코인이 온다.
/// - 업그레이드: 코인을 내고 도끼, 곡괭이, 괭이를 강화한다. 다음 날 아침 도착한다.
/// - 구매하기: 코인을 내고 물건을 고른다. 다음 날 아침 도착한다.
/// - 받기: 도착한 코인, 물건, 강화를 한꺼번에 받는다.
/// 각 화면에는 "우체통 메뉴"로 돌아가는 버튼이 있고, Esc는 한 화면 뒤로(메뉴에서는 닫기)다.
/// 코인은 점수가 아니라서, 이 패널을 열었을 때만 보인다. 패널 화면은 UISetup이 만든다.
/// </summary>
public class MailboxPanel : MonoBehaviour
{
    [SerializeField] private Bag bag;
    [Tooltip("켜고 끄는 패널 화면 전체")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("글자")]
    [Tooltip("코인과 날짜")]
    [SerializeField] private TMP_Text headerText;
    [Tooltip("보낸 것과 주문한 것 요약")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageSeconds = 3f;

    [Header("보내기: 가방 칸마다 하나씩")]
    [SerializeField] private Button[] sendButtons;
    [SerializeField] private TMP_Text[] sendLabels;
    [SerializeField] private Image[] sendIcons;

    [Header("주문하기: 살 수 있는 물건마다 하나씩")]
    [SerializeField] private MaterialData[] orderables;
    [SerializeField] private Button[] orderButtons;
    [SerializeField] private TMP_Text[] orderLabels;
    [SerializeField] private Image[] orderIcons;

    [Header("도구 강화: 0 도끼, 1 곡괭이")]
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] upgradeLabels;

    [Header("받기")]
    [SerializeField] private Button collectButton;
    [SerializeField] private TMP_Text collectLabel;
    [SerializeField] private TMP_Text collectInfoText;

    [Header("화면(쪽): 0 메뉴, 1 판매하기, 2 업그레이드, 3 구매하기, 4 받기")]
    [SerializeField] private GameObject[] pages;
    [Tooltip("메뉴의 큰 버튼 네 개: 판매하기, 업그레이드, 구매하기, 받기")]
    [SerializeField] private Button[] hubButtons;
    [SerializeField] private TMP_Text[] hubLabels;
    [Tooltip("1~4쪽의 우체통 메뉴로 돌아가는 버튼")]
    [SerializeField] private Button[] backButtons;
    [Tooltip("구매하기 화면 아래의 주문 요약")]
    [SerializeField] private TMP_Text orderSummaryText;

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private float messageUntil;
    private int page;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
        for (int i = 0; i < hubButtons.Length; i++)
        {
            int target = i + 1;
            hubButtons[i].onClick.AddListener(() => ShowPage(target));
        }
        for (int i = 0; i < backButtons.Length; i++) backButtons[i].onClick.AddListener(() => ShowPage(0));
        collectButton.onClick.AddListener(Collect);
        for (int i = 0; i < sendButtons.Length; i++)
        {
            int index = i; // 반복 변수를 버튼마다 따로 잡아 둔다
            sendButtons[i].onClick.AddListener(() => Send(index));
        }
        for (int i = 0; i < orderButtons.Length; i++)
        {
            int index = i;
            orderButtons[i].onClick.AddListener(() => Order(index));
        }
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            int index = i;
            upgradeButtons[i].onClick.AddListener(() => Upgrade(index));
        }
    }

    private void OnEnable()
    {
        GameState.Changed += Refresh;
        if (bag != null) bag.Changed += Refresh;
    }

    private void OnDisable()
    {
        GameState.Changed -= Refresh;
        if (bag != null) bag.Changed -= Refresh;
        if (IsOpen) Close();
    }

    private void Update()
    {
        if (!IsOpen) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && !InventoryPanel.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (page != 0) ShowPage(0); // 한 화면 뒤로
            else Close();
        }

        if (messageUntil > 0f && Time.unscaledTime > messageUntil)
        {
            messageText.text = string.Empty;
            messageUntil = 0f;
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        messageText.text = string.Empty;
        root.SetActive(true);
        UIFocus.Open();
        ShowPage(0);
    }

    /// <summary>화면을 바꾼다. 0은 메뉴.</summary>
    private void ShowPage(int index)
    {
        page = index;
        for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
        messageText.text = string.Empty;
        GameAudio.Play(GameAudio.Sfx.Click, 0.5f);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        root.SetActive(false);
        UIFocus.Close();
    }

    // ───────── 하는 일 ─────────

    private void Send(int slotIndex)
    {
        if (slotIndex >= bag.Slots.Count) return;
        MaterialData material = bag.Slots[slotIndex].material;
        if (!bag.TryRemove(material)) return;

        GameState.Send(material);
        GameAudio.Play(GameAudio.Sfx.Paper, 0.6f);
        Say(material.displayName + "을(를) 넣었어요. 내일 아침 " + material.sellPrice + "코인이 와요");
    }

    private void Order(int index)
    {
        MaterialData material = orderables[index];
        if (GameState.TryOrder(material))
        {
            GameAudio.Play(GameAudio.Sfx.Coin);
            Say(material.displayName + "을(를) 주문했어요. 내일 아침 도착해요");
        }
        else
        {
            Say("코인이 모자라요 (" + material.buyPrice + "코인 필요)");
        }
    }

    private void Upgrade(int toolIndex)
    {
        AcquireMethod tool = ToolOf(toolIndex);
        string name = NameOf(toolIndex);
        int cost = GameState.NextUpgradeCost(tool);
        if (cost < 0) { Say(name + "는 이미 최대 단계예요"); return; }
        if (GameState.UpgradePending(tool)) { Say(name + " 강화를 이미 주문했어요"); return; }
        if (GameState.TryOrderUpgrade(tool)) { GameAudio.Play(GameAudio.Sfx.Coin); Say(name + " 강화를 주문했어요. 내일 아침 도착해요"); }
        else Say("코인이 모자라요 (" + cost + "코인 필요)");
    }

    // 강화 버튼 번호: 0 도끼, 1 곡괭이, 2 괭이
    private static AcquireMethod ToolOf(int index) { return index == 0 ? AcquireMethod.Axe : index == 1 ? AcquireMethod.Pickaxe : AcquireMethod.Hoe; }
    private static string NameOf(int index) { return index == 0 ? "도끼" : index == 1 ? "곡괭이" : "괭이"; }

    /// <summary>
    /// 강화 버튼의 글자. 지금 단계와 다음 단계를 견주어 무엇이 좋아지는지 보여 준다.
    /// (치는 횟수는 줄고, 얻는 양과 희귀 재료 확률은 는다)
    /// </summary>
    private static string UpgradeText(string name, AcquireMethod tool)
    {
        int level = GameState.LevelOf(tool);
        int cost = GameState.NextUpgradeCost(tool);
        if (cost < 0) return name + " 최대 단계";
        if (GameState.UpgradePending(tool)) return name + " " + (level + 1) + "단계 주문함";
        ToolSettings.Level now = ToolSettings.Instance.Get(tool, level);
        ToolSettings.Level next = ToolSettings.Instance.Get(tool, level + 1);
        return name + " " + level + "→" + (level + 1) + "단계  (" + cost + "코인)"
             + "\n치기 " + now.hits + "→" + next.hits + "번"
             + "\n얻는 양 " + now.amount + "→" + next.amount + "개"
             + "\n희귀 " + Mathf.RoundToInt(now.rareChance * 100f) + "→" + Mathf.RoundToInt(next.rareChance * 100f) + "%";
    }

    private void Collect()
    {
        if (!GameState.HasMail) return;
        GameState.Collect(bag, out int coins, out int items, out int left);
        if (coins > 0) GameAudio.Play(GameAudio.Sfx.Coin);

        string text = string.Empty;
        if (coins > 0) text += "코인 " + coins + "개 ";
        if (items > 0) text += "물건 " + items + "개 ";
        if (GameState.LastUpgradeCount > 0) text += "강화된 도구 (도끼 " + GameState.AxeLevel + "단계, 곡괭이 " + GameState.PickaxeLevel + "단계, 괭이 " + GameState.HoeLevel + "단계) ";
        text += "받았어요";
        if (left > 0) text += " (가방이 가득 차서 " + left + "개는 우체통에 남았어요)";
        Say(text);
    }

    /// <summary>받기 화면에 보여 줄 도착한 것의 목록</summary>
    private static string CollectInfo()
    {
        if (!GameState.HasMail)
            return "아직 도착한 것이 없어요.\n재료를 판매하거나 물건을 구매하면 다음 날 아침 도착해요.";

        string text = string.Empty;
        if (GameState.ArrivedCoins > 0) text += "코인  " + GameState.ArrivedCoins + "개\n";
        if (GameState.ArrivedItems.Count > 0)
        {
            var names = new System.Collections.Generic.List<string>();
            var counts = new System.Collections.Generic.List<int>();
            foreach (MaterialData m in GameState.ArrivedItems)
            {
                int k = names.IndexOf(m.displayName);
                if (k < 0) { names.Add(m.displayName); counts.Add(1); } else counts[k]++;
            }
            text += "물건  ";
            for (int i = 0; i < names.Count; i++) text += (i > 0 ? ", " : string.Empty) + names[i] + " x" + counts[i];
            text += "\n";
        }
        foreach (AcquireMethod tool in GameState.ArrivedUpgrades)
            text += "강화  " + NameOf(tool == AcquireMethod.Axe ? 0 : tool == AcquireMethod.Pickaxe ? 1 : 2) + " 한 단계\n";
        return text;
    }

    private void Say(string text)
    {
        messageText.text = text;
        messageUntil = Time.unscaledTime + messageSeconds;
    }

    // ───────── 화면 ─────────

    private static void SetIcon(Image[] icons, int i, MaterialData material)
    {
        if (icons == null || i >= icons.Length || icons[i] == null) return;
        icons[i].sprite = material != null ? material.icon : null;
        icons[i].enabled = material != null && material.icon != null;
    }

    private void Refresh()
    {
        if (!IsOpen) return;

        headerText.text = "코인 " + GameState.Coins + "     |     " + GameState.Day + "일째";

        for (int i = 0; i < sendButtons.Length; i++)
        {
            bool has = i < bag.Slots.Count;
            sendButtons[i].interactable = has;
            sendLabels[i].text = has
                ? bag.Slots[i].material.displayName + "\nx" + bag.Slots[i].count + "  " + bag.Slots[i].material.sellPrice + "코인"
                : string.Empty;
            SetIcon(sendIcons, i, has ? bag.Slots[i].material : null);
        }

        for (int i = 0; i < orderButtons.Length; i++)
        {
            MaterialData m = orderables[i];
            orderButtons[i].interactable = GameState.Coins >= m.buyPrice;
            orderLabels[i].text = m.displayName + "\n" + m.buyPrice + "코인";
            SetIcon(orderIcons, i, m);
        }

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            AcquireMethod tool = ToolOf(i);
            int cost = GameState.NextUpgradeCost(tool);
            upgradeButtons[i].interactable = cost >= 0 && !GameState.UpgradePending(tool) && GameState.Coins >= cost;
            upgradeLabels[i].text = UpgradeText(NameOf(i), tool);
        }

        summaryText.text = "보낸 재료 " + GameState.Outbox.Count + "개 : 내일 아침 " + GameState.PendingSellCoins + "코인이 와요\n(잠은 집 안의 침대에서 자요)";
        if (orderSummaryText != null)
            orderSummaryText.text = "주문한 물건 " + GameState.Orders.Count + "개 : 내일 아침 도착해요\n(잠은 집 안의 침대에서 자요)";

        collectButton.interactable = GameState.HasMail;
        collectLabel.text = GameState.HasMail
            ? "도착한 것 받기 (코인 " + GameState.ArrivedCoins + ", 물건 " + GameState.ArrivedItems.Count + "개)"
            : "아직 도착한 것이 없어요";
        if (collectInfoText != null) collectInfoText.text = CollectInfo();

        // 메뉴의 큰 버튼 글자 (제목과 한 줄 설명, 지금 상태)
        if (hubLabels != null && hubLabels.Length >= 4)
        {
            hubLabels[0].text = "판매하기\n<size=32>가방의 재료를 보내요\n(보낸 재료 " + GameState.Outbox.Count + "개)</size>";
            hubLabels[1].text = "업그레이드\n<size=32>도끼, 곡괭이, 괭이를 튼튼하게\n(내일 아침 도착)</size>";
            hubLabels[2].text = "구매하기\n<size=32>코인으로 물건을 주문해요\n(주문한 물건 " + GameState.Orders.Count + "개)</size>";
            hubLabels[3].text = GameState.HasMail
                ? "받기  (!)\n<size=32>도착했어요: 코인 " + GameState.ArrivedCoins + ", 물건 " + GameState.ArrivedItems.Count + "개</size>"
                : "받기\n<size=32>아직 도착한 것이 없어요</size>";
        }
    }
}
