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

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.82f, 0.45f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.88f, 0.9f, 0.87f, 1f);

    private static readonly string[] SoilNames = { "적게", "보통", "많이" };

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
        for (int i = 0; i < soilButtons.Length; i++)
        {
            int level = i;
            soilButtons[i].onClick.AddListener(() => { SelectedSoil = level; RefreshSoil(); });
        }
    }

    /// <summary>작업대의 병 목록에 맞춰 카드와 흙의 양 버튼을 갱신한다.</summary>
    public void Refresh(BottleStation station)
    {
        AddListeners();
        RefreshSoil();

        bool anyCreatable = false;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool exists = station != null && i < station.Count;
            buttons[i].gameObject.SetActive(exists);
            if (!exists) continue;

            BottleContents bottle = station.Get(i);
            string status;
            if (!bottle.IsCreated) { status = "\n만들기"; anyCreatable = true; }
            else if (bottle.IsCompleted) status = "\n(완성) 흙 " + SoilNames[bottle.SoilLevelIndex] + (BottleStore.IsDisplayed(bottle.BottleId) ? "\n선반에 전시 중" : string.Empty);
            else status = "\n(꾸미는 중)";
            labels[i].text = bottle.DisplayName + status;

            bool creatable = !bottle.IsCreated;
            bool viewable = bottle.IsCompleted;
            buttons[i].interactable = creatable || viewable;
            backgrounds[i].color = creatable ? normalColor : (viewable ? selectedColor : disabledColor);
        }

        // 이미 만든 병에는 흙의 양을 다시 정할 수 없으므로, 만들 병이 없으면 흙의 양 버튼을 끈다
        foreach (Button b in soilButtons) b.interactable = anyCreatable;
        if (noneLeftNotice != null) noneLeftNotice.SetActive(!anyCreatable);
    }

    private void RefreshSoil()
    {
        for (int i = 0; i < soilBackgrounds.Length; i++)
            soilBackgrounds[i].color = i == SelectedSoil ? selectedColor : normalColor;
    }
}
