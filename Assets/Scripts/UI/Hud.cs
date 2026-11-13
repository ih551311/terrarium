using TMPro;
using UnityEngine;

/// <summary>
/// 화면 가운데 아래에 보이는 안내 글자: 바라보는 물건의 안내("[E] 조약돌 줍기")와 잠깐 뜨는 알림("가방이 가득 찼어요").
/// 밝은 모래나 하늘 위에서도 읽히도록 어두운 반투명 바탕과 검은 외곽선을 쓴다.
/// UI(Canvas) 맨 아래에 두어서 가방, 병 꾸미기, 지도 패널이 열리면 그 아래로 가려진다.
/// 모양(바탕, 위치)은 UISetup이 만든다.
/// </summary>
public class Hud : MonoBehaviour
{
    [Header("바라보는 물건 안내")]
    [SerializeField] private GameObject promptBar;
    [SerializeField] private TMP_Text promptText;

    [Header("알림")]
    [SerializeField] private GameObject messageBar;
    [SerializeField] private TMP_Text messageText;

    private string currentPrompt;
    private float messageUntil;

    private void Awake()
    {
        // 외곽선은 글자 재질을 따로 만들어 쓰므로 실행할 때 준다.
        ApplyOutline(promptText);
        ApplyOutline(messageText);
        promptBar.SetActive(false);
        messageBar.SetActive(false);
    }

    private static void ApplyOutline(TMP_Text text)
    {
        text.outlineWidth = 0.22f;
        text.outlineColor = new Color32(30, 20, 10, 255);
    }

    /// <summary>안내 글자를 보여 준다. null이나 빈 글자면 숨긴다.</summary>
    public void SetPrompt(string text)
    {
        if (text == currentPrompt) return;
        currentPrompt = text;

        bool show = !string.IsNullOrEmpty(text);
        promptBar.SetActive(show);
        if (show) promptText.text = text;
    }

    /// <summary>알림을 잠깐 보여 준다.</summary>
    public void ShowMessage(string text, float seconds)
    {
        messageText.text = text;
        messageBar.SetActive(true);
        messageUntil = Time.time + seconds;
    }

    private void Update()
    {
        if (messageBar.activeSelf && (Time.time > messageUntil || UIFocus.IsOpen))
            messageBar.SetActive(false);
    }
}
