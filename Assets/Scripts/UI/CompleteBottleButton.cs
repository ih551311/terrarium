using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "완성" 버튼. 한 번 누르면 바로 완성된다(이름을 짓는 상자가 뜬다).
/// 완성하면 병이 닫히고(뚜껑) 더는 고칠 수 없다. 재료는 몇 개든 상관없다.
/// 패널 화면(UISetup이 만든다) 안에 있는 오브젝트에 붙인다.
/// </summary>
public class CompleteBottleButton : MonoBehaviour
{
    private const string NormalText = "완성";

    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private BottlePanel panel;
    [SerializeField] private BottleDecorator decorator;
    [SerializeField] private float confirmSeconds = 3f;

    private BottleContents contents;
    private float confirmUntil;
    private bool listenerAdded;

    private void Awake()
    {
        AddListener();
    }

    private void OnEnable()
    {
        AddListener();
        ResetLabel();
    }

    private void AddListener()
    {
        if (listenerAdded) return;
        listenerAdded = true;
        button.onClick.AddListener(OnClicked);
    }

    /// <summary>다룰 병을 받는다. 닫을 때는 null.</summary>
    public void SetTarget(BottleContents bottleContents)
    {
        contents = bottleContents;
        ResetLabel();
    }

    private void Update()
    {
        // 3초 안에 다시 누르지 않으면 원래 글자로 돌아간다.
        if (confirmUntil > 0f && Time.time > confirmUntil) ResetLabel();
    }

    private void OnClicked()
    {
        if (contents == null || contents.IsCompleted) return;

        contents.Complete();
        GameAudio.Play(GameAudio.Sfx.Chime);
        ResetLabel();
        panel.OnBottleCompleted(); // 완성한 병은 병 인벤토리로 들어가고, 병 만들기 화면으로 돌아간다
    }

    private void ResetLabel()
    {
        confirmUntil = 0f;
        if (label != null) label.text = NormalText;
    }
}
