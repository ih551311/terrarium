using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 크레딧 화면. 메인 화면과 일시정지 메뉴의 "크레딧" 버튼으로 연다. Esc나 닫기 버튼으로 닫는다.
/// 화면은 처음 열 때 코드로 만들고 씬을 넘어 남겨 둔다. 글자는 아래 Text에서 고친다.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    private const string Text =
        "<size=150%>Mini Terra</size>\n미니 테라리움 꾸미기\n\n" +
        "<b>만든 사람</b>\n" +
        "기획, 제작: CheeseBugi\n" +
        "미쿠 모델: 제작자가 직접 만듦\n\n" +
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
    private GameObject root;
    // (ClosedFrame은 아래 IsOpen 옆에 있다)
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
        if (instance == null) Build();
        instance.Open();
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

    private static void Build()
    {
        Canvas canvas = UIKit.CreateCanvas("CreditsPanel", 800);
        instance = canvas.gameObject.AddComponent<CreditsPanel>();
        instance.root = new GameObject("Root", typeof(RectTransform));
        instance.root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = (RectTransform)instance.root.transform;
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        UIKit.Backdrop(rootRect).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform panel = UIKit.Panel(rootRect, "Panel", new Vector2(1100f, 880f));

        // 스크롤 영역
        RectTransform view = UIKit.Rect(panel, "View");
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.sizeDelta = new Vector2(1000f, 700f);
        view.anchoredPosition = new Vector2(0f, 50f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect(view, "Content");
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        TMP_Text label = UIKit.Label(content, Text, 34f, Vector2.zero, new Vector2(960f, 100f), TextAlignmentOptions.Top);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f); labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(20f, 0f); labelRect.offsetMax = new Vector2(-20f, 0f);
        label.richText = true;
        label.lineSpacing = 12f;
        ContentSizeFitter fit = labelRect.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ContentSizeFitter fitContent = content.gameObject.AddComponent<ContentSizeFitter>();
        fitContent.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = view; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        UIKit.MakeButton(panel, "닫기", new Vector2(0f, -370f), new Vector2(300f, 70f), () => instance.Close(), 34f);
        instance.root.SetActive(false);
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
