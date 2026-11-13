using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 병 꾸미기 패널의 "병 고르기": 작업대에 올릴 수 있는 병(둥근 병, 긴 병, 넓은 병)을 버튼으로 고른다.
/// 완성한 병은 이름 옆에 (완성)이 붙는다. 패널 화면(UISetup이 만든다) 안의 오브젝트에 붙인다.
/// </summary>
public class BottleChooser : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private Image[] backgrounds;

    [SerializeField] private Color normalColor = new Color(0.82f, 0.88f, 0.82f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.82f, 0.45f, 1f);

    /// <summary>버튼을 눌렀을 때, 눌린 병의 번호와 함께 알린다.</summary>
    public event Action<int> Chosen;

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
    }

    /// <summary>작업대의 병 목록과 지금 고른 병을 화면에 맞춘다.</summary>
    public void Refresh(BottleStation station)
    {
        AddListeners();
        for (int i = 0; i < buttons.Length; i++)
        {
            bool exists = station != null && i < station.Count;
            buttons[i].gameObject.SetActive(exists);
            if (!exists) continue;

            BottleContents bottle = station.Get(i);
            labels[i].text = bottle.DisplayName + (bottle.IsCompleted ? " (완성)" : string.Empty);
            backgrounds[i].color = i == station.SelectedIndex ? selectedColor : normalColor;
        }
    }
}
