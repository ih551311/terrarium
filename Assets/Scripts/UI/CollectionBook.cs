using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 정원 도감. 집 책상 위의 책(DeskLetter)을 바라보고 E를 누르면 열린다.
/// 재료를 한 번이라도 얻으면(Bag.TryAdd -> GameState.Discover) 도감에 그림과 이름이 채워지고,
/// 아직 못 얻은 것은 그림자와 "???"로 보인다. 못 얻은 것을 누르면 어디서 얻는지 알려 준다.
/// 구역(숲, 꽃 언덕, 바닷가, 눈 섬, 사막 섬, 어디서나, 우체통 가게)마다 칸이 있고,
/// 한 구역을 다 채우면 선물(코인)을 한 번 받을 수 있다. 시간 제한이나 불이익은 없다.
/// 새 재료를 처음 얻으면 화면 아래에 "도감에 담겼어요"가 잠깐 뜬다.
/// 화면은 Assets/Resources/UI/CollectionBook.prefab (유니티에서 열어 모양을 직접 고칠 수 있다)을 처음 열 때 꺼내 와 씬을 넘어 남겨 둔다.
/// 재료 칸(Cell_*)만 열 때마다 코드가 채운다.
/// </summary>
public class CollectionBook : MonoBehaviour
{
    private struct Tab
    {
        public string key, label;
        public MaterialZone[] zones;
        public int gift;
        public Tab(string key, string label, int gift, params MaterialZone[] zones) { this.key = key; this.label = label; this.gift = gift; this.zones = zones; }
    }

    // 칸 순서. 선물 코인은 구역을 다 채웠을 때 한 번 받는다.
    private static readonly Tab[] Tabs =
    {
        new Tab("forest",  "숲",         40, MaterialZone.Forest),
        new Tab("hill",    "꽃 언덕",    40, MaterialZone.FlowerHill),
        new Tab("beach",   "바닷가",     40, MaterialZone.Beach),
        new Tab("snow",    "눈 섬",      40, MaterialZone.Snow),
        new Tab("desert",  "사막 섬",    40, MaterialZone.Desert),
        new Tab("any",     "어디서나",   30, MaterialZone.Anywhere),
        new Tab("shop",    "우체통 가게", 80, MaterialZone.Shop),
    };

    public const int Columns = 8;
    public static int TabCount => Tabs.Length;
    public static string TabLabel(int i) => Tabs[i].label;

    private static CollectionBook instance;
    private static readonly HashSet<string> recent = new HashSet<string>(); // 이번에 새로 담겨서 아직 도감을 안 열어 본 것

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleCount, detailName, detailBody, giftLabel;
    [SerializeField] private Button giftButton, closeButton;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scroll;
    private TMP_Text[] tabLabels;
    private int tabIndex;
    private MaterialData selected;
    private bool isOpen;
    private int openedFrame = -1;
    private readonly List<GameObject> cells = new List<GameObject>();

    public static bool IsOpen => instance != null && instance.isOpen;
    /// <summary>도감이 마지막으로 닫힌 프레임 (같은 프레임의 Esc를 일시정지 메뉴가 또 받지 않게)</summary>
    public static int ClosedFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        recent.Clear();
        ClosedFrame = -1;
    }

    // 새 재료를 처음 얻으면 알려 준다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        GameState.Discovered -= OnDiscovered;
        GameState.Discovered += OnDiscovered;
    }

    private static void OnDiscovered(MaterialData m)
    {
        if (m == null) return;
        recent.Add(m.id);
        Interactor interactor = Object.FindFirstObjectByType<Interactor>();
        if (interactor != null) interactor.ShowMessage("도감에 담겼어요: " + m.displayName + "  (" + GameState.DiscoveredCount + "/" + TotalCount() + ")");
    }

    private static int TotalCount()
    {
        MaterialDatabase db = MaterialDatabase.Instance;
        int n = 0;
        if (db != null && db.materials != null) foreach (MaterialData m in db.materials) if (m != null && !string.IsNullOrEmpty(m.id)) n++;
        return n;
    }

    public static void Show()
    {
        if (ClosedFrame == Time.frameCount) return; // 닫는 E 키로 같은 프레임에 다시 열리지 않게
        if (instance == null) LoadPrefab();
        if (instance != null) instance.Open();
    }

    // ───────── 화면 꺼내기 ─────────

    private static void LoadPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("UI/CollectionBook");
        if (prefab == null) { Debug.LogError("UI/CollectionBook 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
        GameObject go = Object.Instantiate(prefab);
        go.name = "CollectionBook";
        Object.DontDestroyOnLoad(go);
        instance = go.GetComponent<CollectionBook>();
    }

    private void Awake()
    {
        tabLabels = new TMP_Text[tabButtons.Length];
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabLabels[i] = tabButtons[i].GetComponentInChildren<TMP_Text>();
            tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }
        giftButton.onClick.AddListener(Claim);
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    // ───────── 열고 닫기 ─────────

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        openedFrame = Time.frameCount;
        root.SetActive(true);
        selected = null;
        UIFocus.Open();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.8f);
        // 새로 담긴 것이 있는 칸을 먼저 보여 준다
        tabIndex = FirstTabWithNew();
        Refresh();
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        ClosedFrame = Time.frameCount;
        recent.Clear();
        root.SetActive(false);
        UIFocus.Close();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.5f);
    }

    private void Update()
    {
        if (!isOpen || Time.frameCount == openedFrame) return; // 여는 데 쓴 E 키로 바로 닫히지 않게
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) Close();
    }

    // ───────── 내용 ─────────

    private int FirstTabWithNew()
    {
        for (int i = 0; i < Tabs.Length; i++)
            foreach (MaterialData m in MaterialsOf(i)) if (recent.Contains(m.id)) return i;
        return Mathf.Clamp(tabIndex, 0, Tabs.Length - 1);
    }

    private static List<MaterialData> MaterialsOf(int tab)
    {
        var list = new List<MaterialData>();
        MaterialDatabase db = MaterialDatabase.Instance;
        if (db == null || db.materials == null) return list;
        foreach (MaterialData m in db.materials)
        {
            if (m == null || string.IsNullOrEmpty(m.id)) continue;
            foreach (MaterialZone z in Tabs[tab].zones) if (m.zone == z) { list.Add(m); break; }
        }
        // 식물, 덮개, 바닥재, 장식 순서로, 이름순
        list.Sort((a, b) =>
        {
            int ka = KindOrder(a.kind), kb = KindOrder(b.kind);
            return ka != kb ? ka.CompareTo(kb) : string.CompareOrdinal(a.displayName, b.displayName);
        });
        return list;
    }

    private static int KindOrder(MaterialKind k)
    {
        switch (k) { case MaterialKind.Plant: return 0; case MaterialKind.Cover: return 1; case MaterialKind.Ground: return 2; default: return 3; }
    }

    private void SelectTab(int index)
    {
        tabIndex = index;
        selected = null;
        Refresh();
        scroll.verticalNormalizedPosition = 1f;
        GameAudio.Play(GameAudio.Sfx.Click, 0.4f);
    }

    private void Refresh()
    {
        titleCount.text = GameState.DiscoveredCount + " / " + TotalCount();

        Color on = new Color(0.98f, 0.82f, 0.45f, 1f), off = UIKit.ButtonColor;
        for (int i = 0; i < Tabs.Length; i++)
        {
            List<MaterialData> list = MaterialsOf(i);
            int have = 0, news = 0;
            foreach (MaterialData m in list) { if (GameState.IsDiscovered(m.id)) have++; if (recent.Contains(m.id)) news++; }
            bool complete = list.Count > 0 && have == list.Count;
            tabLabels[i].text = Tabs[i].label + "  " + have + "/" + list.Count + (complete ? "  ★" : (news > 0 ? "  새로" : string.Empty));
            tabButtons[i].GetComponent<Image>().color = i == tabIndex ? on : off;
        }

        BuildCells();
        RefreshDetail();
        RefreshGift();
    }

    private void BuildCells()
    {
        foreach (GameObject c in cells) if (c != null) Destroy(c);
        cells.Clear();

        foreach (MaterialData m in MaterialsOf(tabIndex))
        {
            MaterialData material = m;
            bool have = GameState.IsDiscovered(m.id);
            bool isNew = recent.Contains(m.id);

            RectTransform cell = UIKit.Rect(content, "Cell_" + m.id);
            Image bg = cell.gameObject.AddComponent<Image>();
            bg.color = material == selected ? new Color(0.98f, 0.82f, 0.45f, 1f) : (have ? new Color(0.99f, 0.97f, 0.92f, 1f) : new Color(0.84f, 0.80f, 0.72f, 1f));
            Button button = cell.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => { selected = material; GameAudio.Play(GameAudio.Sfx.Click, 0.4f); BuildCellsKeepScroll(); RefreshDetail(); });

            RectTransform iconRect = UIKit.Rect(cell, "Icon");
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.sizeDelta = new Vector2(88f, 88f);
            iconRect.anchoredPosition = new Vector2(0f, -8f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.sprite = m.icon;
            icon.preserveAspect = true;
            icon.enabled = m.icon != null;
            icon.color = have ? Color.white : new Color(0.20f, 0.18f, 0.18f, 0.55f); // 못 얻은 것은 그림자

            TMP_Text name = UIKit.Label(cell, have ? m.displayName : "???", 21f, new Vector2(0f, -52f), new Vector2(120f, 40f), TextAlignmentOptions.Center, have ? UIKit.Ink : new Color(0.45f, 0.40f, 0.36f, 1f));
            name.enableAutoSizing = true; name.fontSizeMin = 14f; name.fontSizeMax = 21f;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            if (isNew)
            {
                RectTransform dot = UIKit.Rect(cell, "New");
                dot.anchorMin = dot.anchorMax = new Vector2(1f, 1f);
                dot.pivot = new Vector2(1f, 1f);
                dot.sizeDelta = new Vector2(30f, 30f);
                dot.anchoredPosition = new Vector2(-4f, -4f);
                Image d = dot.gameObject.AddComponent<Image>();
                d.color = new Color(0.95f, 0.45f, 0.28f, 1f);
                d.raycastTarget = false;
                UIKit.Label(dot, "새", 18f, Vector2.zero, new Vector2(30f, 30f), TextAlignmentOptions.Center, Color.white);
            }
            cells.Add(cell.gameObject);
        }
    }

    private void BuildCellsKeepScroll()
    {
        float pos = scroll.verticalNormalizedPosition;
        BuildCells();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = pos;
    }

    private void RefreshDetail()
    {
        if (selected == null)
        {
            detailName.text = "그림을 눌러 보세요";
            detailBody.text = "얻은 재료는 그림이 채워지고, 아직 못 얻은 재료는 어디서 얻는지 알려 줘요.";
            return;
        }
        bool have = GameState.IsDiscovered(selected.id);
        // 값은 이름 옆에 작고 옅게 쓴다. 우체통에서 사는 재료는 사는 값과 파는 값을 둘 다 쓴다.
        string price = string.Empty;
        if (have)
        {
            bool buyable = selected.buyPrice > 0;
            string text = buyable ? "사면 " + selected.buyPrice + "코인, 팔면 " + selected.sellPrice + "코인" : selected.sellPrice + "코인";
            price = "   <size=55%><color=#8a7a66>" + text + "</color></size>";
        }
        detailName.text = have ? selected.displayName + (selected.isRare ? "  (희귀)" : string.Empty) + price : "???";
        detailBody.text = (have ? string.Empty : "아직 얻지 못했어요.\n") + LoadingScreen.TipOf(selected);
    }

    private void RefreshGift()
    {
        Tab tab = Tabs[tabIndex];
        List<MaterialData> list = MaterialsOf(tabIndex);
        int have = 0;
        foreach (MaterialData m in list) if (GameState.IsDiscovered(m.id)) have++;
        bool complete = list.Count > 0 && have == list.Count;
        bool claimed = GameState.IsDexClaimed(tab.key);

        giftButton.gameObject.SetActive(complete && !claimed);
        UIKit.SetButtonText(giftButton, "선물 받기  +" + tab.gift + "코인");
        giftLabel.text = claimed ? tab.label + " 도감 완성! 선물을 받았어요"
                       : complete ? string.Empty
                       : tab.label + " 도감을 다 채우면 선물이 있어요";
    }

    private void Claim()
    {
        Tab tab = Tabs[tabIndex];
        if (GameState.TryClaimDex(tab.key, tab.gift))
        {
            GameAudio.Play(GameAudio.Sfx.Coin, 1f);
            GameAudio.Play(GameAudio.Sfx.Chime, 0.6f);
            RefreshGift();
            Refresh();
        }
    }
}
