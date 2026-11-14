using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널의 바닥재 고르기: 흙(항상 쓸 수 있다), 모래와 눈(가방의 모래나 눈 1개를 쓴다).
/// 모래나 눈에서 다른 바닥재로 바꾸면 쓰던 것이 가방으로 돌아온다. 가방이 가득 차 있으면 되돌릴 수 없다.
/// 패널 화면(UISetup이 만든다) 안에 있는 오브젝트에 붙인다.
/// </summary>
public class GroundSelector : MonoBehaviour
{
    [SerializeField] private Bag bag;
    [SerializeField] private BottleDecorator decorator;
    [Tooltip("바닥재로 쓰는 모래 재료")]
    [SerializeField] private MaterialData sand;
    [SerializeField] private Button soilButton;
    [SerializeField] private Button sandButton;
    [SerializeField] private Image soilBackground;
    [SerializeField] private Image sandBackground;
    [SerializeField] private TMP_Text sandLabel;
    [Tooltip("바닥재로 쓰는 눈 재료 (눈 섬에서 줍는다)")]
    [SerializeField] private MaterialData snow;
    [SerializeField] private Button snowButton;
    [SerializeField] private Image snowBackground;
    [SerializeField] private TMP_Text snowLabel;

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.82f, 0.45f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.88f, 0.9f, 0.87f, 1f);

    private BottleContents contents;
    private bool listenersAdded;

    private void Awake()
    {
        AddListeners();
    }

    private void OnEnable()
    {
        AddListeners();
        if (bag != null) bag.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (bag != null) bag.Changed -= Refresh;
    }

    private void AddListeners()
    {
        if (listenersAdded) return;
        listenersAdded = true;
        soilButton.onClick.AddListener(() => Choose(null));
        sandButton.onClick.AddListener(() => Choose(sand));
        if (snowButton != null) snowButton.onClick.AddListener(() => Choose(snow));
    }

    /// <summary>다룰 병을 받는다. 닫을 때는 null.</summary>
    public void SetTarget(BottleContents bottleContents)
    {
        contents = bottleContents;
        Refresh();
    }

    /// <summary>바닥재를 고른다. null이면 흙.</summary>
    private void Choose(MaterialData ground)
    {
        if (contents == null || contents.IsCompleted || contents.Ground == ground) return;
        MaterialData current = contents.Ground;

        // 새 바닥재(모래)를 가방에서 꺼낸다.
        if (ground != null && !bag.TryRemove(ground))
        {
            decorator.ShowMessage("가방에 " + ground.displayName + "이(가) 없어요");
            return;
        }

        // 쓰던 바닥재(모래)는 가방으로 돌려준다. 자리가 없으면 바꾸지 않는다.
        if (current != null && !bag.TryAdd(current))
        {
            if (ground != null) bag.TryAdd(ground); // 꺼낸 것을 되돌린다
            decorator.ShowMessage("가방이 가득 찼어요");
            return;
        }

        contents.SetGround(ground);
        Refresh();
    }

    private void Refresh()
    {
        if (contents == null || bag == null) return;

        bool sandSelected = contents.Ground == sand && sand != null;
        int sandCount = sand != null ? bag.CountOf(sand) : 0;

        bool snowSelected = contents.Ground == snow && snow != null;
        int snowCount = snow != null ? bag.CountOf(snow) : 0;

        soilBackground.color = (sandSelected || snowSelected) ? normalColor : selectedColor;
        sandBackground.color = sandSelected ? selectedColor : (sandCount > 0 ? normalColor : disabledColor);
        sandButton.interactable = sandSelected || sandCount > 0;
        sandLabel.text = "모래 x" + sandCount;
        if (snowButton != null)
        {
            snowBackground.color = snowSelected ? selectedColor : (snowCount > 0 ? normalColor : disabledColor);
            snowButton.interactable = snowSelected || snowCount > 0;
            snowLabel.text = "눈 x" + snowCount;
        }
    }
}
