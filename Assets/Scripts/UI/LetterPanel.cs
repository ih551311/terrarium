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
        "새로 온 정원사에게\n\n" +
        "이 집은 이제 네 거야. 나는 여기서 몇 해 동안 유리병 정원을 만들다가 떠나. 짐은 거의 챙겼는데 작업대랑 선반은 두고 갈게.\n\n" +
        "섬을 돌아다니면서 풀이나 꽃, 조개, 나뭇가지 같은 걸 모아 봐. 풀이랑 꽃은 괭이로, 나무는 도끼로, 돌은 곡괭이로 캐면 돼. 도구는 마우스 왼쪽, 줍는 건 E야.\n\n" +
        "모은 걸 작업대 유리병에 넣어서 꾸미고, 다 되면 이름 붙여서 선반에 올려 둬. 남는 재료는 문 앞 우체통에 넣으면 다음 날 아침에 코인으로 와. 그걸로 병도 사고 도구도 좋은 걸로 바꿀 수 있어.\n\n" +
        "비 오는 날이나 그다음 날엔 숲에 버섯이 나. 선착장에서 배를 타면 다른 섬에도 갈 수 있는데, 처음엔 돈이 좀 들 거야.\n\n" +
        "해가 지면 침대에서 자. 급할 거 없어.\n\n" +
        "                                                                     먼저 살던 정원사가\n\n" +
        "추신. 첫 병은 하나 남겨 뒀어.\nTab 가방   M 지도   P 음악   Esc 메뉴";

    private void Awake()
    {
        // 씬마다 들어 있는 GameRoot 복사본의 편지 화면은 곧 지워지므로, 이미 있으면 등록하지 않는다
        // (등록했다가 지워지면 책상 위 편지가 열리지 않는다)
        if (Instance != null && Instance != this) return;
        Instance = this;
        root.SetActive(false);
        bodyLabel.text = Letter;
        closeButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        if (Instance != this) return;
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
