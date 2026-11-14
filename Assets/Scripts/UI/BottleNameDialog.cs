using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 병을 완성했을 때 뜨는 "병 이름 짓기" 상자. 이름을 쓰고 확인(또는 Enter)을 누르면 이름을 돌려준다.
/// 비워 두면 null을 돌려주고, 그러면 "종류 #번호" 이름이 쓰인다. 열려 있는 동안 Esc는 확인과 같다.
/// 패널 화면(UISetup이 만든다) 안의 오브젝트에 붙인다.
/// </summary>
public class BottleNameDialog : MonoBehaviour
{
    public const int MaxLength = 12;

    [Tooltip("켜고 끄는 상자 전체 (창을 덮는 어두운 바탕 포함)")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_Text placeholderLabel;
    [SerializeField] private Button confirmButton;

    private Action<string> onDone;

    /// <summary>이름 짓는 상자가 열려 있는가. 열려 있으면 병 패널은 Esc를 상자에 맡긴다.</summary>
    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        input.characterLimit = MaxLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        confirmButton.onClick.AddListener(Confirm);
        input.onSubmit.AddListener(_ => Confirm());
        root.SetActive(false);
    }

    private void OnDisable()
    {
        IsOpen = false;
        onDone = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Confirm();
    }

    /// <summary>상자를 연다. 이름을 정하면 onDone(이름)이 불린다. (비웠으면 null)</summary>
    public void Show(string defaultName, Action<string> done)
    {
        onDone = done;
        IsOpen = true;
        root.SetActive(true);
        input.text = string.Empty;
        if (placeholderLabel != null) placeholderLabel.text = "비워 두면 \"" + defaultName + "\"";
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(input.gameObject);
        input.ActivateInputField();
    }

    /// <summary>지금 쓴 이름으로 마친다. (확인 버튼, Enter, Esc, 패널을 닫을 때)</summary>
    public void Confirm()
    {
        if (!IsOpen) return;
        string text = input.text != null ? input.text.Trim() : string.Empty;
        Action<string> done = onDone;
        onDone = null;
        IsOpen = false;
        root.SetActive(false);
        done?.Invoke(string.IsNullOrEmpty(text) ? null : text);
    }
}
