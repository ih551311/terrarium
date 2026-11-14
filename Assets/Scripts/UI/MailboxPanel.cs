using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 우체통 패널. 한 화면에서 세 가지를 한다.
/// - 보내기: 가방의 재료를 눌러 하나씩 넣는다. 다음 날 아침 코인이 온다.
/// - 주문하기: 코인을 내고 물건을 고른다. 다음 날 아침 도착한다.
/// - 받기: 도착한 코인과 물건을 한꺼번에 받는다.
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

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private float messageUntil;

    private void Awake()
    {
        root.SetActive(false);
        closeButton.onClick.AddListener(Close);
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
        if (keyboard != null && !InventoryPanel.IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();

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

        string summary = "보낸 재료 " + GameState.Outbox.Count + "개 → 내일 아침 " + GameState.PendingSellCoins + "코인";
        summary += "\n주문한 물건 " + GameState.Orders.Count + "개 → 내일 아침 도착";
        summary += "\n\n잠은 집 안의 침대에서 자요";
        summaryText.text = summary;

        collectButton.interactable = GameState.HasMail;
        collectLabel.text = GameState.HasMail
            ? "도착한 것 받기 (코인 " + GameState.ArrivedCoins + ", 물건 " + GameState.ArrivedItems.Count + "개)"
            : "아직 도착한 것이 없어요";
    }
}
