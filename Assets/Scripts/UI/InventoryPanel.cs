using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

    /// <summary>다른 패널이 Esc 처리를 양보할 때 쓴다.</summary>
    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (bag != null) bag.Changed += Refresh;
    }

    private void OnDisable()
    {
        if (bag != null) bag.Changed -= Refresh;
        if (IsOpen) Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

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

    private void Refresh()
    {
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
