using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "내 병" 화면. 완성해서 병 인벤토리에 들어간 병들을 칸으로 보여 주고, 칸을 누르면 선반에 전시하거나 내린다.
/// 한 화면에 칸이 정해진 수만큼 있어서, 병이 많으면 쪽(이전/다음)을 넘긴다.
/// 패널 화면(UISetup이 만든다) 안의 오브젝트에 붙인다.
/// </summary>
public class BottleInventoryView : MonoBehaviour
{
    [SerializeField] private Button[] cells;
    [SerializeField] private Image[] icons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Image[] backgrounds;
    [Tooltip("병 종류(카드 순서)별 병 그림")]
    [SerializeField] private Sprite[] typeIcons;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text infoLabel;

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color displayedColor = new Color(0.98f, 0.82f, 0.45f, 1f);

    /// <summary>"뒤로" 버튼을 눌렀다.</summary>
    public event Action Back;

    private BottleStation station;
    private int page;
    private string notice = string.Empty;
    private string[] cellIds;
    private bool listenersAdded;

    private void Awake()
    {
        AddListeners();
    }

    private void AddListeners()
    {
        if (listenersAdded) return;
        listenersAdded = true;
        cellIds = new string[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            int index = i;
            cells[i].onClick.AddListener(() => OnCell(index));
        }
        previousButton.onClick.AddListener(() => { page--; notice = string.Empty; Redraw(); });
        nextButton.onClick.AddListener(() => { page++; notice = string.Empty; Redraw(); });
        backButton.onClick.AddListener(() => Back?.Invoke());
    }

    public void Open(BottleStation bottleStation)
    {
        AddListeners();
        station = bottleStation;
        page = 0;
        notice = string.Empty;
        Redraw();
    }

    private void OnCell(int index)
    {
        string id = cellIds[index];
        if (string.IsNullOrEmpty(id)) return;

        if (BottleStore.IsDisplayed(id))
        {
            BottleStore.Undisplay(id);
            notice = "선반에서 내렸어요";
        }
        else if (BottleStore.TryDisplay(id))
        {
            notice = "선반에 전시했어요";
        }
        else
        {
            notice = "선반이 가득 찼어요 (병을 더 만들면 칸이 늘어요)";
        }
        Redraw();
    }

    private void Redraw()
    {
        var ids = BottleStore.ArchivedIds();
        int perPage = cells.Length;
        int pageCount = Mathf.Max(1, (ids.Count + perPage - 1) / perPage);
        page = Mathf.Clamp(page, 0, pageCount - 1);

        for (int i = 0; i < cells.Length; i++)
        {
            int n = page * perPage + i;
            bool has = n < ids.Count;
            cells[i].gameObject.SetActive(has);
            cellIds[i] = has ? ids[n] : null;
            if (!has) continue;

            string id = ids[n];
            BottleRecord record = BottleStore.Get(id);
            int type = station != null ? station.IndexOf(record.typeId) : -1;
            string typeName = type >= 0 ? station.Get(type).DisplayName : "병";
            string name = BottleStore.NameOf(id, typeName);
            bool shown = BottleStore.IsDisplayed(id);
            labels[i].enableAutoSizing = true; // 긴 이름은 글자가 작아진다
            labels[i].fontSizeMin = 14f;
            labels[i].fontSizeMax = 26f;
            // 이름을 지어 줬으면 둘째 줄 앞에 병 종류를 붙인다. (지어 주지 않았으면 이름에 이미 종류가 들어 있다)
            // 전시 중인 병은 칸 색깔로 알 수 있어서 글자로는 쓰지 않는다
            string kind = string.IsNullOrEmpty(record.name) ? string.Empty : typeName;
            string hint = shown ? string.Empty : "눌러서 전시하기";
            string second = kind.Length > 0 && hint.Length > 0 ? kind + ", " + hint : kind + hint;
            labels[i].text = second.Length > 0 ? name + "\n" + second : name;
            backgrounds[i].color = shown ? displayedColor : normalColor;

            Sprite sprite = type >= 0 && typeIcons != null && type < typeIcons.Length ? typeIcons[type] : null;
            icons[i].sprite = sprite;
            icons[i].color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        previousButton.gameObject.SetActive(pageCount > 1);
        nextButton.gameObject.SetActive(pageCount > 1);
        previousButton.interactable = page > 0;
        nextButton.interactable = page < pageCount - 1;

        int onShelf = 0;
        foreach (string id in ids) if (BottleStore.IsDisplayed(id)) onShelf++;
        string head = ids.Count == 0
            ? "아직 완성한 병이 없어요"
            : "내 병 " + ids.Count + "개  |  전시 " + onShelf + "/" + BottleStore.DisplayCapacity + "칸" + (pageCount > 1 ? "  |  " + (page + 1) + "/" + pageCount + "쪽" : string.Empty);
        infoLabel.text = string.IsNullOrEmpty(notice) ? head : head + "\n" + notice;
    }
}
