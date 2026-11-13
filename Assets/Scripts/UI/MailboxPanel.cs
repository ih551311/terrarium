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

    [Header("주문하기: 살 수 있는 물건마다 하나씩")]
    [SerializeField] private MaterialData[] orderables;
    [SerializeField] private Button[] orderButtons;
    [SerializeField] private TMP_Text[] orderLabels;

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
        Say(material.displayName + "을(를) 넣었어요. 내일 아침 " + material.sellPrice + "코인이 와요");
    }

    private void Order(int index)
    {
        MaterialData material = orderables[index];
        if (GameState.TryOrder(material))
            Say(material.displayName + "을(를) 주문했어요. 내일 아침 도착해요");
        else
            Say("코인이 모자라요 (" + material.buyPrice + "코인 필요)");
    }

    private void Collect()
    {
        if (!GameState.HasMail) return;
        GameState.Collect(bag, out int coins, out int items, out int left);

        string text = string.Empty;
        if (coins > 0) text += "코인 " + coins + "개 ";
        if (items > 0) text += "물건 " + items + "개 ";
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

    private void Refresh()
    {
        if (!IsOpen) return;

        headerText.text = "코인 " + GameState.Coins + "     |     " + GameState.Day + "일째";

        for (int i = 0; i < sendButtons.Length; i++)
        {
            bool has = i < bag.Slots.Count;
            sendButtons[i].interactable = has;
            sendLabels[i].text = has
                ? bag.Slots[i].material.displayName + "\nx" + bag.Slots[i].count + "\n" + bag.Slots[i].material.sellPrice + "코인"
                : string.Empty;
        }

        for (int i = 0; i < orderButtons.Length; i++)
        {
            MaterialData m = orderables[i];
            orderButtons[i].interactable = GameState.Coins >= m.buyPrice;
            orderLabels[i].text = m.displayName + "\n" + m.buyPrice + "코인";
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
