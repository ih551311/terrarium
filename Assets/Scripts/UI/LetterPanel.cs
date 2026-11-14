using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 편지 화면. 처음 시작하면 잠시 뒤에 저절로 열려서, 이 섬에서 하는 일을 편지로 알려 준다.
/// 읽고 나면 집 책상 위의 편지(DeskLetter)를 바라보고 E를 눌러 다시 볼 수 있다.
/// 닫기 버튼, E, Esc, 스페이스로 닫는다. 닫으면 읽은 것으로 적는다.
/// 패널 화면은 UISetup이 만들고, 이 스크립트는 Canvas 오브젝트(UI)에 붙는다.
/// </summary>
public class LetterPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text bodyLabel;
    [SerializeField] private Button closeButton;

    [Tooltip("처음 시작하고 편지가 저절로 열리기까지 기다리는 시간(초)")]
    [SerializeField] private float autoOpenDelay = 2.5f;

    public static LetterPanel Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    private const string Letter =
        "안녕, 새 정원사야.\n\n" +
        "이 섬에 와 줘서 고마워. 여기는 파도 소리와 바람 소리뿐이라 조용히 쉬어 가기 좋은 곳이야. 서두를 건 하나도 없어.\n\n" +
        "섬을 천천히 거닐며 풀과 꽃, 조개, 나뭇가지를 모아 봐. 풀과 꽃은 괭이로, 나무는 도끼로, 돌은 곡괭이로 캘 수 있어. (도구는 왼쪽 클릭, 줍기는 E)\n\n" +
        "모은 재료는 집 작업대에서 유리병 속에 마음대로 놓아 꾸며 봐. 다 꾸미면 이름을 지어 주고 선반에 전시해.\n\n" +
        "재료는 문 옆 우체통에 보내면 다음 날 아침 코인이 와. 코인으로 새 병을 만들고 도구를 튼튼하게 할 수 있어.\n\n" +
        "비가 온 날에는 숲에 버섯이 나니까 우산을 챙겨(그냥 맞아도 괜찮아). 졸리면 침대에서 자고, 내일 또 천천히 해.\n\n" +
        "                                                                 — 먼저 다녀간 정원사가\n\n" +
        "추신. 첫 병은 공짜야.   Tab 가방 · M 지도 · P 음악 켜기/끄기";

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
        bodyLabel.text = Letter;
        closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (!GameState.LetterRead) StartCoroutine(AutoOpen());
    }

    private IEnumerator AutoOpen()
    {
        yield return new WaitForSeconds(autoOpenDelay);
        // 다른 화면이 열려 있으면 닫힐 때까지 기다린다
        while (UIFocus.IsOpen) yield return null;
        if (!GameState.LetterRead) Show();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsOpen) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            Close();
    }

    public void Show()
    {
        if (IsOpen) return;
        IsOpen = true;
        root.SetActive(true);
        UIFocus.Open();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.8f);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        root.SetActive(false);
        UIFocus.Close();
        GameState.MarkLetterRead();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.5f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsOpen = false;
    }
}
