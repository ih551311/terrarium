using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 크레딧 화면. 메인 화면과 일시정지 메뉴의 "크레딧" 버튼으로 연다. Esc나 닫기 버튼으로 닫는다.
/// 화면은 Assets/Resources/UI/CreditsPanel.prefab (유니티에서 열어 모양을 직접 고칠 수 있다)을 처음 열 때 꺼내 와 씬을 넘어 남겨 둔다.
/// 글자는 프리팹 안의 Content/Label 에서 고친다. 아래 Text는 프리팹을 새로 만들 때(UIBake) 쓰는 처음 글자다.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    public const string Text =
        "<size=150%>Mini Terra</size>\n미니 테라리움 꾸미기\n\n" +
        "<b>만든 사람</b>\n" +
        "기획, 제작: CheeseBugi\n" +
        "햄부기 모델: 제작자가 직접 만듦\n\n" +
        "<b>3D 에셋 (Unity Asset Store)</b>\n" +
        "Cozy Snowy Environment - Low Poly (HobiSoLoved)\n" +
        "Low Poly Nature Pack (BD Studios)\n" +
        "Low Poly Tree Mega Pack - 1000 Trees (MysticForge)\n" +
        "Medieval Town Exteriors (Lylek Games)\n" +
        "Poly Desert (Runemark Studio)\n" +
        "AllSky Free - 10 Sky / Skybox Set (rpgwhitelock)\n" +
        "URP Dynamic Weather System (CreaTuMundo)\n\n" +
        "<b>음악과 소리</b>\n" +
        "Dreamcore Music (juanjo_sound / Michael Barker)\n" +
        "Footsteps Sounds - Volume 02 (The Sound Guild)\n" +
        "효과음 일부: EarShot (Leon Lin, MIT License)\n\n" +
        "<b>글꼴</b>\n" +
        "Cafe24 동동 (Cafe24)\n\n" +
        "<b>만든 도구</b>\n" +
        "Unity 6\n\n" +
        "플레이해 주셔서 고마워요.";

    private static CreditsPanel instance;
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;
    /// <summary>크레딧이 마지막으로 닫힌 프레임 (같은 프레임의 Esc를 다른 메뉴가 또 받지 않게)</summary>
    public static int ClosedFrame { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /// <summary>크레딧 화면을 연다.</summary>
    public static void Show()
    {
        if (instance == null) LoadPrefab();
        if (instance != null) instance.Open();
    }

    /// <summary>
    /// template 버튼을 복사해서 "크레딧" 버튼을 만든다. 메인 화면에서는 화면 왼쪽 아래 구석에 작게 둔다.
    /// (이미 있는 버튼 줄에 끼우면 줄 간격 계산이 어긋날 수 있어서 따로 둔다)
    /// </summary>
    public static Button AddCornerButton(Button template, Transform parent)
    {
        Button button = Object.Instantiate(template, parent);
        button.name = "CreditsButton";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Show);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40f, 36f);
        rect.sizeDelta = new Vector2(200f, 56f);
        UIKit.SetButtonText(button, "크레딧");
        return button;
    }

    /// <summary>template 버튼을 복사해서 template 바로 아래에 "크레딧" 버튼을 둔다 (일시정지 메뉴용).</summary>
    public static Button AddButtonBelow(Button template, float gap)
    {
        Button button = Object.Instantiate(template, template.transform.parent);
        button.name = "CreditsButton";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Show);
        RectTransform rect = (RectTransform)button.transform;
        RectTransform source = (RectTransform)template.transform;
        rect.anchoredPosition = source.anchoredPosition + new Vector2(0f, -(source.sizeDelta.y + gap));
        UIKit.SetButtonText(button, "크레딧");

        // 새 버튼이 판 밖으로 나가지 않게 판을 아래로 늘린다 (가운데가 아래로 내려가는 만큼 판 위치를 맞춘다)
        RectTransform panel = source.parent as RectTransform;
        if (panel != null)
        {
            float grow = source.sizeDelta.y + gap + 24f;
            panel.sizeDelta += new Vector2(0f, grow);
            panel.anchoredPosition += new Vector2(0f, -grow * 0.5f);
            foreach (RectTransform child in panel) if (child != null) child.anchoredPosition += new Vector2(0f, grow * 0.5f);
        }
        return button;
    }

    private static void LoadPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("UI/CreditsPanel");
        if (prefab == null) { Debug.LogError("UI/CreditsPanel 프리팹이 없어요. 메뉴 '유리병 정원 > 화면 캔버스 다시 만들기'를 눌러 주세요."); return; }
        GameObject go = Object.Instantiate(prefab);
        go.name = "CreditsPanel";
        Object.DontDestroyOnLoad(go);
        instance = go.GetComponent<CreditsPanel>();
    }

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        root.SetActive(true);
        UIFocus.Open();
        GameAudio.Play(GameAudio.Sfx.Paper, 0.6f);
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        ClosedFrame = Time.frameCount;
        root.SetActive(false);
        UIFocus.Close();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (isOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            // 같은 프레임에 일시정지 메뉴가 닫히거나 다시 열리지 않게 한다 (Close가 LastCloseFrame을 남긴다)
        }
    }
}
