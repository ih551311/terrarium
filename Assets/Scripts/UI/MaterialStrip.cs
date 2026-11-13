using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널 아래의 재료 칸. 가방의 칸을 그대로 보여 주고, 하나를 골라 두면 병 안에 놓을 수 있다.
/// (보관 상자는 아직 없어서 가방만 보여 준다.)
/// 패널 화면(UISetup이 만든다) 안에 있는 오브젝트에 붙인다.
/// </summary>
public class MaterialStrip : MonoBehaviour
{
    [SerializeField] private Bag bag;
    [Tooltip("칸마다 하나씩, 순서대로 (가방 칸 수와 같게)")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Image[] backgrounds;
    [Tooltip("칸마다 하나씩. 재료 그림을 보여 준다 (없으면 글자만)")]
    [SerializeField] private Image[] icons;

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.82f, 0.45f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.88f, 0.9f, 0.87f, 1f);

    /// <summary>지금 골라 둔 재료. 없으면 null.</summary>
    public MaterialData Selected { get; private set; }

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
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // 반복 변수를 버튼마다 따로 잡아 둔다
            buttons[i].onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    private void OnSlotClicked(int index)
    {
        if (bag == null || index >= bag.Slots.Count) return;
        Selected = bag.Slots[index].material;
        Refresh();
    }

    private void Refresh()
    {
        // 고른 재료가 가방에서 다 떨어졌으면 선택을 푼다.
        if (Selected != null && !Contains(Selected)) Selected = null;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool filled = bag != null && i < bag.Slots.Count;
            buttons[i].interactable = filled;

            if (filled)
            {
                Bag.Slot slot = bag.Slots[i];
                labels[i].text = slot.material.displayName + "\nx" + slot.count;
                backgrounds[i].color = slot.material == Selected ? selectedColor : normalColor;
                ShowIcon(i, slot.material);
            }
            else
            {
                labels[i].text = string.Empty;
                backgrounds[i].color = emptyColor;
                ShowIcon(i, null);
            }
        }
    }

    private void ShowIcon(int i, MaterialData material)
    {
        if (icons == null || i >= icons.Length || icons[i] == null) return;
        icons[i].sprite = material != null ? material.icon : null;
        icons[i].enabled = material != null && material.icon != null;
    }

    private bool Contains(MaterialData material)
    {
        foreach (Bag.Slot slot in bag.Slots)
            if (slot.material == material) return true;
        return false;
    }
}
