using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 병 만들기 화면: 만들 병의 종류(둥근 병, 긴 병, 넓은 병, 누운 병)와 흙의 양(적게, 보통, 많이)을 정한다.
/// 흙의 양을 고른 뒤 병 카드를 누르면 그 병이 만들어지고, 병 종류와 흙의 양은 그 뒤로 바꿀 수 없다.
/// 이미 만든 병(꾸미는 중이거나 완성한 병)은 카드에 상태가 적혀 있고, 완성한 병은 눌러서 볼 수 있다.
/// (지금은 개발용으로 병을 바로 주지만, 나중에는 병을 사야 만들 수 있게 한다)
/// 패널 화면(UISetup이 만든다) 안의 오브젝트에 붙인다.
/// </summary>
public class BottleChooser : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Image[] backgrounds;

    [Header("흙의 양 (적게, 보통, 많이)")]
    [SerializeField] private Button[] soilButtons;
    [SerializeField] private Image[] soilBackgrounds;
    [Tooltip("만들 수 있는 병이 하나도 없을 때 보이는 안내 글자")]
    [SerializeField] private GameObject noneLeftNotice;

    [Header("병 인벤토리")]
    [Tooltip("완성한 병이 병 인벤토리로 들어갔다는 등의 안내 글자")]
    [SerializeField] private TMP_Text noticeLabel;
    [Tooltip("\"내 병\" 화면을 여는 버튼")]
    [SerializeField] private Button myBottlesButton;
    [SerializeField] private TMP_Text myBottlesLabel;

    /// <summary>"내 병" 버튼을 눌렀다.</summary>
    public event Action OpenInventory;

    [Header("병 가격")]
    [Tooltip("켜면 병을 만들 때 코인을 낸다. (시작할 때 받은 공짜 병은 먼저 쓴다)")]
    [SerializeField] private bool requirePurchase = true;
    [Tooltip("병 카드 순서(둥근, 긴, 넓은, 누운)대로의 가격(코인). 임시 값.")]
    [SerializeField] private int[] prices = { 20, 20, 30, 40 };

    public bool RequirePurchase => requirePurchase;
    public int PriceOf(int index) => prices != null && index < prices.Length ? prices[index] : 0;

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.82f, 0.45f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.88f, 0.9f, 0.87f, 1f);

    /// <summary>병 카드를 눌렀을 때, 눌린 병의 번호와 함께 알린다.</summary>
    public event Action<int> Chosen;

    /// <summary>지금 고른 흙의 양 (0 적게, 1 보통, 2 많이)</summary>
    public int SelectedSoil { get; private set; } = 1;

    private bool listenersAdded;

    private void Awake()
    {
        AddListeners();
    }

    private void AddListeners()
    {
        if (listenersAdded) return;
        listenersAdded = true;
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // 반복 변수를 버튼마다 따로 잡아 둔다
            buttons[i].onClick.AddListener(() => Chosen?.Invoke(index));
        }
        if (myBottlesButton != null) myBottlesButton.onClick.AddListener(() => OpenInventory?.Invoke());
        for (int i = 0; i < soilButtons.Length; i++)
        {
            int level = i;
            soilButtons[i].onClick.AddListener(() => { SelectedSoil = level; RefreshSoil(); });
        }
    }

    /// <summary>
    /// 작업대의 병 목록에 맞춰 카드와 흙의 양 버튼을 갱신한다.
    /// 병은 몇 개든 만들 수 있다(완성한 병은 병 인벤토리로 들어가고 카드는 다시 비어 있다).
    /// notice가 있으면 위쪽에 안내 글자로 보여 준다. (예: "병 인벤토리에 넣었어요")
    /// </summary>
    public void Refresh(BottleStation station, string notice = null)
    {
        AddListeners();
        RefreshSoil();

        for (int i = 0; i < buttons.Length; i++)
        {
            bool exists = station != null && i < station.Count;
            buttons[i].gameObject.SetActive(exists);
            if (!exists) continue;

            BottleContents bottle = station.Get(i);
            string cost = GameState.FreeBottles > 0 ? " (공짜 병 " + GameState.FreeBottles + "개)" : requirePurchase ? " (" + PriceOf(i) + "코인)" : string.Empty;
            labels[i].text = bottle.DisplayName + "\n만들기" + cost;
            buttons[i].interactable = true;
            backgrounds[i].color = normalColor;
        }

        foreach (Button b in soilButtons) b.interactable = true;
        if (noneLeftNotice != null) noneLeftNotice.SetActive(false);

        if (noticeLabel != null)
        {
            noticeLabel.text = notice ?? string.Empty;
            noticeLabel.gameObject.SetActive(!string.IsNullOrEmpty(notice));
        }

        int count = BottleStore.CompletedCount;
        if (myBottlesLabel != null) myBottlesLabel.text = "내 병 (" + count + "개)";
    }

    private void RefreshSoil()
    {
        for (int i = 0; i < soilBackgrounds.Length; i++)
            soilBackgrounds[i].color = i == SelectedSoil ? selectedColor : normalColor;
    }
}
