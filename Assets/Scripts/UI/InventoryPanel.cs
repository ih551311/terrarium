using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 가방 패널. Tab으로 어디서든 열고 닫는다. (병 꾸미기 패널 위에서도 열린다)
/// 패널 화면(root)은 UISetup이 만들어 주고, 이 스크립트는 Canvas 오브젝트(UI)에 붙는다.
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private Bag bag;
    [Tooltip("켜고 끄는 패널 화면 전체")]
    [SerializeField] private GameObject root;
    [Tooltip("가방 칸마다 하나씩, 순서대로")]
    [SerializeField] private TMP_Text[] slotTexts;
    [Tooltip("가방 칸마다 하나씩. 재료 그림을 보여 준다")]
    [SerializeField] private UnityEngine.UI.Image[] slotIcons;

    [Header("가방 / 병 탭")]
    [SerializeField] private Button bagTab;
    [SerializeField] private Button bottleTab;
    [SerializeField] private Image bagTabBackground;
    [SerializeField] private Image bottleTabBackground;
    [Tooltip("가방 칸들을 담은 오브젝트")]
    [SerializeField] private GameObject bagGroup;
    [Tooltip("병 칸들을 담은 오브젝트")]
    [SerializeField] private GameObject bottleGroup;
    [SerializeField] private TMP_Text bottleInfo;
    [SerializeField] private Image[] bottleIcons;
    [SerializeField] private TMP_Text[] bottleTexts;
    [Tooltip("병 칸마다 하나씩. 누르면 그 병을 자세히 본다")]
    [SerializeField] private Button[] bottleButtons;
    [SerializeField] private Button bottlePrev;
    [SerializeField] private Button bottleNext;
    [Tooltip("병 종류(둥근, 긴, 넓은, 누운)별 병 그림")]
    [SerializeField] private Sprite[] typeIcons;

    private static readonly string[] TypeNames = { "둥근 병", "긴 병", "넓은 병", "누운 병" };
    private bool showBottles;
    private int bottlePage;

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        root.SetActive(false);
        bagTab.onClick.AddListener(() => SetTab(false));
        bottleTab.onClick.AddListener(() => SetTab(true));
        bottlePrev.onClick.AddListener(() => { bottlePage--; Refresh(); });
        bottleNext.onClick.AddListener(() => { bottlePage++; Refresh(); });
        if (bottleButtons != null)
            for (int i = 0; i < bottleButtons.Length; i++)
            {
                int cell = i;
                bottleButtons[i].onClick.AddListener(() => OpenBottleDetail(cell));
            }
    }

    /// <summary>병 칸을 눌렀다. 그 병을 자세히 본다. (집 안에서만 볼 수 있어서, 밖이면 안내만 한다)</summary>
    private void OpenBottleDetail(int cell)
    {
        var ids = BottleStore.ArchivedIds();
        int n = bottlePage * bottleTexts.Length + cell;
        if (n < 0 || n >= ids.Count) return;

        BottleDetailPanel panel = GameRoot.Instance != null ? GameRoot.Instance.BottleDetailPanel : null;
        string problem = panel != null ? panel.Open(ids[n]) : "병을 자세히 볼 수 없어요";
        if (problem != null) bottleInfo.text = problem;
    }

    private void SetTab(bool bottles)
    {
        showBottles = bottles;
        bottlePage = 0;
        Refresh();
    }

    private void OnEnable()
    {
        if (bag != null) bag.Changed += Refresh;
        BottleStore.Changed += Refresh;
    }

    private void OnDisable()
    {
        if (bag != null) bag.Changed -= Refresh;
        BottleStore.Changed -= Refresh;
        if (IsOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (BottleDetailPanel.IsOpen) return; // 병 자세히 보기가 위에 열려 있으면 그쪽이 먼저 닫히게 양보한다

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (IsOpen) Close(); else Open();
        }
        else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void Open()
    {
        IsOpen = true;
        root.SetActive(true);
        UIFocus.Open();
        Refresh();
    }

    private void Close()
    {
        IsOpen = false;
        root.SetActive(false);
        UIFocus.Close();
    }

    private void SetIcon(int i, MaterialData material)
    {
        if (slotIcons == null || i >= slotIcons.Length || slotIcons[i] == null) return;
        slotIcons[i].sprite = material != null ? material.icon : null;
        slotIcons[i].enabled = material != null && material.icon != null;
    }

    private void RefreshBottles()
    {
        var ids = BottleStore.ArchivedIds();
        int perPage = bottleTexts.Length;
        int pageCount = Mathf.Max(1, (ids.Count + perPage - 1) / perPage);
        bottlePage = Mathf.Clamp(bottlePage, 0, pageCount - 1);

        int onShelf = 0;
        foreach (string id in ids) if (BottleStore.IsDisplayed(id)) onShelf++;

        for (int i = 0; i < perPage; i++)
        {
            int n = bottlePage * perPage + i;
            bool has = n < ids.Count;
            bottleTexts[i].transform.parent.gameObject.SetActive(has);
            if (!has) continue;

            string id = ids[n];
            string typeId = BottleStore.Get(id).typeId;
            int type = 0;
            if (typeId != null && typeId.Length > 6 && int.TryParse(typeId.Substring(6), out int t)) type = t;
            bool shown = BottleStore.IsDisplayed(id);
            string typeName = type >= 0 && type < TypeNames.Length ? TypeNames[type] : "병";
            // 지어 준 이름이 있으면 이름을, 없으면 "종류 #번호"를 보여 준다. 둘째 줄: 전시 중이면 그 안내, 아니면 병 종류.
            bottleTexts[i].enableAutoSizing = true;
            bottleTexts[i].fontSizeMin = 12f;
            bottleTexts[i].fontSizeMax = 20f;
            bool named = !string.IsNullOrEmpty(BottleStore.Get(id).name); // 이름이 없으면 첫 줄이 이미 종류를 말하므로 종류를 또 쓰지 않는다
            bottleTexts[i].text = BottleStore.NameOf(id, typeName) + "\n" + (shown ? "선반에 전시 중" : named ? typeName : string.Empty);
            Sprite sprite = typeIcons != null && type >= 0 && type < typeIcons.Length ? typeIcons[type] : null;
            bottleIcons[i].sprite = sprite;
            bottleIcons[i].enabled = sprite != null;
        }

        bottlePrev.gameObject.SetActive(pageCount > 1);
        bottleNext.gameObject.SetActive(pageCount > 1);
        bottlePrev.interactable = bottlePage > 0;
        bottleNext.interactable = bottlePage < pageCount - 1;
        bottleInfo.text = ids.Count == 0
            ? "아직 완성한 병이 없어요"
            : "완성한 병 " + ids.Count + "개  |  전시 " + onShelf + "/" + BottleStore.DisplayCapacity + "칸" + (pageCount > 1 ? "  |  " + (bottlePage + 1) + "/" + pageCount + "쪽" : string.Empty);
    }

    private void Refresh()
    {
        if (!IsOpen) return;
        Color on = new Color(0.98f, 0.82f, 0.45f, 1f), off = new Color(0.82f, 0.88f, 0.82f, 1f);
        bagTabBackground.color = showBottles ? off : on;
        bottleTabBackground.color = showBottles ? on : off;
        bagGroup.SetActive(!showBottles);
        bottleGroup.SetActive(showBottles);
        if (showBottles) { RefreshBottles(); return; }

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (bag != null && i < bag.Slots.Count)
            {
                Bag.Slot slot = bag.Slots[i];
                slotTexts[i].text = slot.material.displayName + "\nx" + slot.count;
                SetIcon(i, slot.material);
            }
            else
            {
                slotTexts[i].text = string.Empty;
                SetIcon(i, null);
            }
        }
    }
}
