using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 코드로 그리던 화면을 실제 캔버스 오브젝트로 만들어 둔다. (UIKit의 도우미로 만들어 에디터에서 보이고 고칠 수 있게 저장한다)
/// - 메인 화면: MainMenu 씬 안의 캔버스 (BuildMainMenuCanvas, "메인 화면 만들기"가 부른다). 그림 파일은 Assets/UI/MainMenu/*.png
/// - 일시정지 메뉴, 로딩 화면, 편지 답장 화면, 선착장 창: Assets/Resources/UI/*.prefab (게임이 켜질 때 하나 꺼내 쓴다)
/// 각 화면의 스크립트(MainMenu, PauseMenu, LoadingScreen, RequestPanel, DockPanel)는 여기서 연결해 둔 버튼과 글자를 가져다 쓴다.
/// 메뉴: 유리병 정원 > 화면 캔버스 다시 만들기 (프리팹 네 개)
/// </summary>
public static class UIBake
{
    private const string PrefabFolder = "Assets/Resources/UI";
    private const string ImageFolder = "Assets/UI/MainMenu";

    private static readonly Color Cream = new Color(0.96f, 0.92f, 0.82f, 1f);
    private static readonly Color Mist = new Color(0.72f, 0.80f, 0.78f, 1f);
    private static readonly Color Brown = new Color(0.55f, 0.40f, 0.28f, 1f);

    // ───────── 프리팹 네 개 ─────────

    [MenuItem("유리병 정원/화면 캔버스 다시 만들기")]
    public static string BakeOverlays()
    {
        if (EditorApplication.isPlaying) return "Play 중에는 만들 수 없어요";
        Directory.CreateDirectory(PrefabFolder);

        // 임시 빈 씬에서 만들고 프리팹으로 저장한 뒤 버린다 (열려 있는 씬을 건드리지 않는다)
        Scene previous = SceneManager.GetActiveScene();
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);
        try
        {
            SavePrefab(BuildPause(), "PauseMenu");
            SavePrefab(BuildLoading(), "LoadingScreen");
            SavePrefab(BuildRequest(), "RequestPanel");
            SavePrefab(BuildDock(), "DockPanel");
            SavePrefab(BuildCredits(), "CreditsPanel");
            SavePrefab(BuildBook(), "CollectionBook");
            SavePrefab(BuildSleep(), "SleepSequence");
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(temp, true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "화면 프리팹 일곱 개를 만들었어요: " + PrefabFolder;
    }

    /// <summary>일시정지 메뉴 프리팹만 새로 만든다.</summary>
    [MenuItem("유리병 정원/일시정지 메뉴 프리팹만 만들기")]
    public static string BakePauseOnly()
    {
        if (EditorApplication.isPlaying) return "Play 중에는 만들 수 없어요";
        Directory.CreateDirectory(PrefabFolder);
        Scene previous = SceneManager.GetActiveScene();
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);
        try { SavePrefab(BuildPause(), "PauseMenu"); }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(temp, true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "일시정지 메뉴 프리팹을 만들었어요: " + PrefabFolder;
    }

    /// <summary>크레딧과 정원 도감 프리팹만 새로 만든다 (다른 화면 프리팹에 손으로 고친 것이 있으면 건드리지 않는다).</summary>
    [MenuItem("유리병 정원/크레딧, 도감 화면 프리팹만 만들기")]
    public static string BakeCreditsAndBook()
    {
        if (EditorApplication.isPlaying) return "Play 중에는 만들 수 없어요";
        Directory.CreateDirectory(PrefabFolder);
        Scene previous = SceneManager.GetActiveScene();
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);
        try
        {
            SavePrefab(BuildCredits(), "CreditsPanel");
            SavePrefab(BuildBook(), "CollectionBook");
            SavePrefab(BuildSleep(), "SleepSequence");
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(temp, true);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "크레딧, 정원 도감, 잠자기 화면 프리팹을 만들었어요: " + PrefabFolder;
    }

    private static void SavePrefab(GameObject root, string name)
    {
        UIRound.Apply(root); // 네모난 바탕을 둥근 모서리로
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    private static GameObject NewHolder(string name, string canvasName, int order, out RectTransform root, out Component component, System.Type scriptType)
    {
        GameObject go = new GameObject(name);
        component = go.AddComponent(scriptType);
        Canvas canvas = UIKit.CreateCanvas(canvasName, order);
        canvas.transform.SetParent(go.transform, false);
        root = Full(canvas.transform, "Root");
        return go;
    }

    private static RectTransform Full(Transform parent, string name)
    {
        RectTransform r = UIKit.Rect(parent, name);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    private static void Put(object target, string field, object value)
    {
        FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) throw new System.Exception(target.GetType().Name + "에 " + field + " 칸이 없어요");
        f.SetValue(target, value);
    }

    private static Button Btn(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize = 30f)
    {
        return UIKit.MakeButton(parent, text, pos, size, null, fontSize);
    }

    // ───────── 일시정지 메뉴 ─────────

    private static GameObject BuildPause()
    {
        GameObject go = NewHolder("PauseMenu", "PauseMenuCanvas", 400, out RectTransform root, out Component comp, typeof(PauseMenu));
        UIKit.Backdrop(root);
        RectTransform panel = UIKit.Panel(root, "Panel", new Vector2(640f, 840f)); // 크레딧 버튼이 밑에 붙으며 판이 112 늘어나도 화면(1080) 안에 들어오는 높이

        UIKit.Label(panel, "잠시 쉬어가요", 48f, new Vector2(0f, 355f), new Vector2(560f, 70f));
        UIKit.Label(panel, "소리 크기", 28f, new Vector2(0f, 285f), new Vector2(460f, 40f));
        Slider volume = UIKit.MakeSlider(panel, new Vector2(0f, 245f), new Vector2(420f, 40f), 1f, null);
        UIKit.Label(panel, "마우스 감도", 28f, new Vector2(0f, 185f), new Vector2(460f, 40f));
        Slider look = UIKit.MakeSlider(panel, new Vector2(0f, 145f), new Vector2(420f, 40f), 0.5f, null);
        look.name = "LookSlider";

        Button music = Btn(panel, "음악 끄기", new Vector2(0f, 70f), new Vector2(460f, 64f));
        Button screen = Btn(panel, "창 모드", new Vector2(0f, 0f), new Vector2(460f, 64f)); screen.name = "ScreenButton";
        Button resolution = Btn(panel, "1920 x 1080", new Vector2(0f, -70f), new Vector2(460f, 64f)); resolution.name = "ResolutionButton";
        Button quality = Btn(panel, "높음", new Vector2(0f, -140f), new Vector2(460f, 64f)); quality.name = "QualityButton";
        Button menu = Btn(panel, "메인 화면으로", new Vector2(0f, -230f), new Vector2(460f, 72f));
        Button quit = Btn(panel, "게임 끝내기", new Vector2(0f, -315f), new Vector2(460f, 72f));

        Put(comp, "root", root.gameObject);
        Put(comp, "volumeSlider", volume);
        Put(comp, "lookSlider", look);
        Put(comp, "musicButton", music);
        Put(comp, "screenButton", screen);
        Put(comp, "resolutionButton", resolution);
        Put(comp, "qualityButton", quality);
        Put(comp, "menuButton", menu);
        Put(comp, "quitButton", quit);
        return go;
    }
    // ───────── 로딩 화면 ─────────

    private static GameObject BuildLoading()
    {
        GameObject go = NewHolder("LoadingScreen", "LoadingCanvas", 900, out RectTransform root, out Component comp, typeof(LoadingScreen));
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f; group.blocksRaycasts = false;
        root.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.22f, 0.25f, 1f); // 병 그림 배경과 같은 짙은 청회색

        RectTransform card = UIKit.Panel(root, "Card", new Vector2(1040f, 400f));
        RectTransform ir = UIKit.Rect(card, "Icon");
        ir.anchoredPosition = new Vector2(-310f, 0f); ir.sizeDelta = new Vector2(260f, 260f);
        Image icon = ir.gameObject.AddComponent<Image>();
        icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = false;

        UIKit.Label(card, "재료 메모", 32f, new Vector2(150f, 125f), new Vector2(620f, 44f), TextAlignmentOptions.Left, Brown);
        TMP_Text title = UIKit.Label(card, "재료 이름", 64f, new Vector2(150f, 65f), new Vector2(620f, 80f), TextAlignmentOptions.Left);
        TMP_Text tip = UIKit.Label(card, "어디서 어떻게 얻는지 쪽지가 여기에 나와요.", 40f, new Vector2(150f, -75f), new Vector2(620f, 190f), TextAlignmentOptions.TopLeft);
        TMP_Text destination = UIKit.Label(root, "섬으로 나가는 중...", 40f, new Vector2(0f, -300f), new Vector2(1000f, 56f), TextAlignmentOptions.Center, new Color(0.93f, 0.90f, 0.82f, 1f));

        Put(comp, "group", group);
        Put(comp, "icon", icon);
        Put(comp, "title", title);
        Put(comp, "tip", tip);
        Put(comp, "destination", destination);
        return go;
    }

    // ───────── 편지 답장 화면 ─────────

    private static GameObject BuildRequest()
    {
        GameObject go = NewHolder("RequestPanel", "RequestCanvas", 350, out RectTransform root, out Component comp, typeof(RequestPanel));
        UIKit.Backdrop(root);
        RectTransform panel = UIKit.Panel(root, "Panel", new Vector2(980f, 905f));

        // 위쪽 글자는 위에서, 아래 버튼은 아래에서 잰 자리에 둔다. 판의 높이는 RequestPanel이 병 줄 수에 맞춰 바꾼다.
        TMP_Text from = UIKit.Label(panel, "여행자 언니의 편지", 42f, new Vector2(0f, -60f), new Vector2(880f, 60f));
        TMP_Text body = UIKit.Label(panel, "편지 내용이 여기에 나와요.", 34f, new Vector2(0f, -178f), new Vector2(840f, 160f), TextAlignmentOptions.TopLeft);
        TMP_Text condition = UIKit.Label(panel, "조건  ", 30f, new Vector2(0f, -285f), new Vector2(860f, 50f), TextAlignmentOptions.Center, new Color(0.45f, 0.30f, 0.12f));
        TopAnchor(from); TopAnchor(body); TopAnchor(condition);

        var rows = new Button[RequestPanel.MaxRows];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = Btn(panel, "병 보내기", new Vector2(0f, -355f - i * 80f), new Vector2(820f, 70f), 28f);
            TopAnchor(rows[i]);
        }

        TMP_Text note = UIKit.Label(panel, "", 26f, new Vector2(0f, -345f), new Vector2(880f, 70f), TextAlignmentOptions.Center);
        TopAnchor(note);
        Button mailbox = Btn(panel, "우체통 열기", new Vector2(-220f, 48f), new Vector2(380f, 64f), 28f);
        Button close = Btn(panel, "닫기", new Vector2(220f, 48f), new Vector2(380f, 64f), 28f);
        BottomAnchor(mailbox); BottomAnchor(close);

        Put(comp, "root", root.gameObject);
        Put(comp, "panelRect", panel);
        Put(comp, "fromLabel", from);
        Put(comp, "bodyLabel", body);
        Put(comp, "conditionLabel", condition);
        Put(comp, "noteLabel", note);
        Put(comp, "rowButtons", rows);
        Put(comp, "mailboxButton", mailbox);
        Put(comp, "closeButton", close);
        return go;
    }

    /// <summary>글자나 버튼을 부모의 위쪽 또는 아래쪽 가운데 기준으로 잰다</summary>
    private static void TopAnchor(Component c) { Reanchor((RectTransform)c.transform, 1f); }
    private static void BottomAnchor(Component c) { Reanchor((RectTransform)c.transform, 0f); }

    private static void Reanchor(RectTransform r, float anchorY)
    {
        // 자리 값은 호출하는 쪽이 이미 그 기준(위에서 또는 아래에서 잰 거리)으로 적었으므로 기준만 옮긴다
        r.anchorMin = r.anchorMax = new Vector2(r.anchorMin.x, anchorY);
    }
    // ───────── 선착장 창 ─────────

    private static GameObject BuildDock()
    {
        GameObject go = NewHolder("DockPanel", "DockPanelCanvas", 380, out RectTransform root, out Component comp, typeof(DockPanel));
        UIKit.Backdrop(root);
        int count = DockPanel.DestinationCount;
        RectTransform panel = UIKit.Panel(root, "Panel", new Vector2(800f, 250f + count * 150f));
        float top = panel.sizeDelta.y * 0.5f;

        UIKit.Label(panel, "어느 섬으로 갈까요?", 46f, new Vector2(0f, top - 60f), new Vector2(680f, 70f));
        var names = new TMP_Text[count]; var blurbs = new TMP_Text[count]; var buttons = new Button[count];
        for (int i = 0; i < count; i++)
        {
            float y = top - 170f - i * 150f;
            names[i] = UIKit.Label(panel, DockPanel.NameOf(i), 40f, new Vector2(-150f, y + 22f), new Vector2(380f, 50f), TextAlignmentOptions.Left);
            blurbs[i] = UIKit.Label(panel, DockPanel.BlurbOf(i), 26f, new Vector2(-150f, y - 32f), new Vector2(380f, 70f), TextAlignmentOptions.TopLeft);
            buttons[i] = Btn(panel, "가기", new Vector2(240f, y), new Vector2(240f, 84f), 30f);
        }
        TMP_Text status = UIKit.Label(panel, "", 28f, new Vector2(0f, -top + 120f), new Vector2(680f, 40f));
        Button close = Btn(panel, "닫기", new Vector2(0f, -top + 52f), new Vector2(240f, 64f), 30f);

        Put(comp, "root", root.gameObject);
        Put(comp, "status", status);
        Put(comp, "names", names);
        Put(comp, "blurbs", blurbs);
        Put(comp, "buttons", buttons);
        Put(comp, "closeButton", close);
        return go;
    }

    // ───────── 잠자기 연출 (까만 막, 글자) ─────────

    private static GameObject BuildSleep()
    {
        GameObject go = NewHolder("SleepSequence", "SleepSequenceCanvas", 900, out RectTransform root, out Component comp, typeof(SleepSequence));
        RectTransform black = Full(root, "Curtain");
        black.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.07f, 1f);
        CanvasGroup curtain = black.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 0f; curtain.blocksRaycasts = false;
        TMP_Text dream = UIKit.Label(black, "", 64f, Vector2.zero, new Vector2(900f, 140f), TextAlignmentOptions.Center, new Color(0.86f, 0.88f, 0.96f, 1f));
        dream.name = "DreamText";

        RectTransform titleRoot = UIKit.Rect(root, "Title");
        titleRoot.anchorMin = titleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        titleRoot.sizeDelta = new Vector2(1200f, 300f);
        CanvasGroup titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f; titleGroup.blocksRaycasts = false;
        Color warm = new Color(1f, 0.96f, 0.86f, 1f);
        TMP_Text title = UIKit.Label(titleRoot, "", 96f, new Vector2(0f, 40f), new Vector2(1200f, 150f), TextAlignmentOptions.Center, warm);
        title.name = "TitleText";
        TMP_Text sub = UIKit.Label(titleRoot, "", 44f, new Vector2(0f, -60f), new Vector2(1200f, 90f), TextAlignmentOptions.Center, warm);
        sub.name = "SubText";

        Put(comp, "curtain", curtain);
        Put(comp, "titleGroup", titleGroup);
        Put(comp, "dreamText", dream);
        Put(comp, "titleText", title);
        Put(comp, "subText", sub);
        return go;
    }
    // ───────── 크레딧 ─────────

    private static GameObject BuildCredits()
    {
        GameObject go = NewHolder("CreditsPanel", "CreditsPanelCanvas", 800, out RectTransform root, out Component comp, typeof(CreditsPanel));
        UIKit.Backdrop(root).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform panel = UIKit.Panel(root, "Panel", new Vector2(1100f, 880f));

        RectTransform view = UIKit.Rect(panel, "View");
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.sizeDelta = new Vector2(1000f, 700f);
        view.anchoredPosition = new Vector2(0f, 50f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect(view, "Content");
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        TMP_Text label = UIKit.Label(content, CreditsPanel.Text, 34f, Vector2.zero, new Vector2(960f, 100f), TextAlignmentOptions.Top);
        label.name = "Label";
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f); labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.offsetMin = new Vector2(20f, 0f); labelRect.offsetMax = new Vector2(-20f, 0f);
        label.richText = true;
        label.lineSpacing = 12f;
        labelRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = view; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        Button close = Btn(panel, "닫기", new Vector2(0f, -370f), new Vector2(300f, 70f), 34f);
        close.name = "CloseButton";
        Put(comp, "root", root.gameObject);
        Put(comp, "closeButton", close);
        return go;
    }

    // ───────── 정원 도감 (재료 칸 Cell_*은 열 때 코드가 채운다) ─────────

    private static GameObject BuildBook()
    {
        GameObject go = NewHolder("CollectionBook", "CollectionBookCanvas", 810, out RectTransform root, out Component comp, typeof(CollectionBook));
        UIKit.Backdrop(root).GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        RectTransform panel = UIKit.Panel(root, "Panel", new Vector2(1640f, 940f));

        UIKit.Label(panel, "정원 도감", 58f, new Vector2(-250f, 408f), new Vector2(700f, 80f), TextAlignmentOptions.Center).name = "Title";
        TMP_Text titleCount = UIKit.Label(panel, "", 38f, new Vector2(100f, 408f), new Vector2(400f, 60f), TextAlignmentOptions.Left);
        titleCount.name = "TitleCount";
        Button close = Btn(panel, "닫기", new Vector2(690f, 408f), new Vector2(180f, 64f), 32f);
        close.name = "CloseButton";

        var tabs = new Button[CollectionBook.TabCount];
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i] = Btn(panel, CollectionBook.TabLabel(i), new Vector2(-640f, 290f - i * 78f), new Vector2(290f, 66f), 30f);
            tabs[i].name = "Tab" + i;
        }
        Button gift = Btn(panel, "선물 받기", new Vector2(-640f, -330f), new Vector2(290f, 74f), 28f);
        gift.name = "GiftButton";
        TMP_Text giftLabel = UIKit.Label(panel, "", 26f, new Vector2(-640f, -405f), new Vector2(300f, 60f), TextAlignmentOptions.Center);
        giftLabel.name = "GiftLabel";

        RectTransform view = UIKit.Rect(panel, "View");
        view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
        view.sizeDelta = new Vector2(1090f, 600f);
        view.anchoredPosition = new Vector2(140f, 62f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect(view, "Content");
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(124f, 140f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CollectionBook.Columns;
        grid.padding = new RectOffset(6, 6, 6, 6);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = view; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        RectTransform detail = UIKit.Rect(panel, "Detail");
        detail.anchorMin = detail.anchorMax = new Vector2(0.5f, 0.5f);
        detail.sizeDelta = new Vector2(1090f, 200f);
        detail.anchoredPosition = new Vector2(140f, -355f);
        detail.gameObject.AddComponent<Image>().color = new Color(0.93f, 0.89f, 0.80f, 1f);
        TMP_Text detailName = UIKit.Label(detail, "", 38f, new Vector2(0f, 62f), new Vector2(1040f, 56f), TextAlignmentOptions.Left);
        detailName.name = "DetailName";
        TMP_Text detailBody = UIKit.Label(detail, "", 28f, new Vector2(0f, -28f), new Vector2(1040f, 120f), TextAlignmentOptions.TopLeft);
        detailBody.name = "DetailBody";

        Put(comp, "root", root.gameObject);
        Put(comp, "titleCount", titleCount);
        Put(comp, "detailName", detailName);
        Put(comp, "detailBody", detailBody);
        Put(comp, "giftLabel", giftLabel);
        Put(comp, "giftButton", gift);
        Put(comp, "closeButton", close);
        Put(comp, "tabButtons", tabs);
        Put(comp, "content", content);
        Put(comp, "scroll", scroll);
        return go;
    }

    // ───────── 메인 화면 (씬 안의 캔버스) ─────────

    private static readonly string[,] MoveKeys =
    {
        { "W A S D", "걷기" }, { "Shift", "달리기" }, { "Space", "점프" }, { "마우스", "둘러보기" },
        { "E", "줍기, 열기, 말 걸기" }, { "좌클릭", "나무, 돌, 풀 캐기" }, { "Tab", "가방 열기" },
        { "M", "지도 (눌러서 이동)" }, { "N", "작은 지도 켜기/끄기" }, { "P", "음악 켜기/끄기" }, { "Esc", "메뉴, 닫기" },
    };

    private static readonly string[,] BottleKeys =
    {
        { "클릭", "고른 재료 놓기" }, { "우클릭", "놓은 재료 빼기" }, { "휠", "재료 돌리기" },
        { "Q / R", "재료 작게 / 크게" }, { "끌기", "병 돌려 보기" },
    };

    private const string HowToPlay =
        "휠, Q, R은 병 속 재료에 마우스를 올리고 써요.\n\n" +
        "섬에서 재료를 모아 집 작업대(E)에서 병을 꾸며요. 완성한 병은 선반에 올려 둘 수 있어요.\n\n" +
        "우체통에 재료를 보내면 다음 날 코인이 돼요. 코인으로 장식을 사고, 도구를 강화하고, 선착장에서 다른 섬을 열어요.\n\n" +
        "침대에서 자면 다음 날이 되고 저장돼요. 책상 위 책(E)은 정원 도감이에요.";

    /// <summary>지금 열려 있는 씬에 메인 화면 캔버스를 만들고 MainMenu 스크립트에 연결한다.</summary>
    public static void BuildMainMenuCanvas(MainMenu menu)
    {
        Directory.CreateDirectory(ImageFolder);
        Sprite gradient = SaveSprite(MakeGradient(), "backdrop_gradient");
        Sprite glow = SaveSprite(MakeGlow(), "glow");
        Sprite glass = SaveSprite(MakeGlass(), "jar_glass");
        Sprite soil = SaveSprite(MakeSoil(), "jar_soil");

        Canvas canvas = UIKit.CreateCanvas("MainMenuCanvas", 100);
        RectTransform root = Full(canvas.transform, "Root");

        // 바탕
        RectTransform back = Full(root, "Backdrop");
        Image backImage = back.gameObject.AddComponent<Image>();
        backImage.sprite = gradient; backImage.raycastTarget = false;

        // 반딧불이
        RectTransform flyRoot = Full(root, "Fireflies");
        var flies = new Image[26];
        var rnd = new System.Random(7);
        for (int i = 0; i < flies.Length; i++)
        {
            RectTransform r = UIKit.Rect(flyRoot, "Firefly" + i);
            float size = 12f + (float)rnd.NextDouble() * 22f;
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = new Vector2((float)(rnd.NextDouble() * 1800 - 900), (float)(rnd.NextDouble() * 920 - 460));
            Image img = r.gameObject.AddComponent<Image>();
            img.sprite = glow; img.raycastTarget = false; img.color = new Color(1f, 0.93f, 0.62f, 0.6f);
            flies[i] = img;
        }

        // 병과 그림자
        RectTransform shadow = UIKit.Rect(root, "JarShadow");
        shadow.anchoredPosition = new Vector2(430f, -10f - 322f);
        shadow.sizeDelta = new Vector2(560f, 70f);
        Image shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.sprite = glow; shadowImage.color = new Color(0f, 0f, 0f, 0.42f); shadowImage.raycastTarget = false;

        RectTransform jar = UIKit.Rect(root, "Jar");
        jar.anchoredPosition = new Vector2(430f, -10f);
        jar.sizeDelta = new Vector2(512f, 640f);
        FullImage(jar, "Soil", soil);

        float[] xs = { 132f, 256f, 380f };
        var plants = new Image[3];
        for (int i = 0; i < plants.Length; i++)
        {
            RectTransform r = UIKit.Rect(jar, "Plant" + i);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(190f, 190f);
            r.anchoredPosition = new Vector2(xs[i] - 256f, 112f - 320f);
            Image img = r.gameObject.AddComponent<Image>();
            img.preserveAspect = true; img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f); // 그림은 켜질 때 정해진다 (에디터에서는 비어 있다)
            plants[i] = img;
        }
        FullImage(jar, "Glass", glass);

        // 왼쪽: 제목과 버튼
        const float x = -480f;
        UIKit.Label(root, MainMenu.GameTitle, 118f, new Vector2(x, 290f), new Vector2(820f, 160f), TextAlignmentOptions.Center, Cream).name = "Title";
        UIKit.Label(root, "작은 병 안에 섬의 풀과 꽃을 담아요", 34f, new Vector2(x, 190f), new Vector2(820f, 56f), TextAlignmentOptions.Center, Mist).name = "Subtitle";
        Button start = Btn(root, "시작하기", new Vector2(x, 60f), new Vector2(440f, 88f), 40f); start.name = "StartButton";
        Button help = Btn(root, "도움말", new Vector2(x, -50f), new Vector2(440f, 88f), 40f); help.name = "HelpButton";
        Button settings = Btn(root, "설정", new Vector2(x, -160f), new Vector2(440f, 88f), 40f); settings.name = "SettingsButton";
        TMP_Text note = UIKit.Label(root, "저장된 정원이 있어요 (1일째)", 27f, new Vector2(x, -255f), new Vector2(820f, 44f), TextAlignmentOptions.Center, Mist);
        note.name = "SaveNote";

        // 도움말 판
        var closers = new List<Button>();
        GameObject helpPanel = NewPanel(root, "HelpPanel", "도움말", new Vector2(1260f, 960f), out RectTransform hp, closers);
        UIKit.Label(hp, "움직이기와 줍기", 38f, new Vector2(-265f, 345f), new Vector2(560f, 50f), TextAlignmentOptions.Left, Brown);
        UIKit.Label(hp, "병 꾸미기", 38f, new Vector2(310f, 345f), new Vector2(560f, 50f), TextAlignmentOptions.Left, Brown);
        for (int i = 0; i < MoveKeys.GetLength(0); i++) KeyRow(hp, -460f, -185f, 280f - i * 58f, MoveKeys[i, 0], MoveKeys[i, 1]);
        for (int i = 0; i < BottleKeys.GetLength(0); i++) KeyRow(hp, 115f, 390f, 280f - i * 58f, BottleKeys[i, 0], BottleKeys[i, 1]);
        UIKit.Label(hp, HowToPlay, 25f, new Vector2(310f, -185f), new Vector2(560f, 340f), TextAlignmentOptions.TopLeft).name = "HowToPlay";

        // 설정 판
        GameObject settingsPanel = NewPanel(root, "SettingsPanel", "설정", new Vector2(820f, 760f), out RectTransform sp, closers);
        UIKit.Label(sp, "소리 크기", 32f, new Vector2(-230f, 190f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        TMP_Text volumeValue = UIKit.Label(sp, "100%", 28f, new Vector2(345f, 190f), new Vector2(100f, 50f), TextAlignmentOptions.Right, Brown);
        Slider volumeSlider = UIKit.MakeSlider(sp, new Vector2(100f, 190f), new Vector2(320f, 40f), 1f, null);
        UIKit.Label(sp, "마우스 감도", 32f, new Vector2(-230f, 70f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        TMP_Text lookValue = UIKit.Label(sp, "100%", 28f, new Vector2(345f, 70f), new Vector2(100f, 50f), TextAlignmentOptions.Right, Brown);
        Slider lookSlider = UIKit.MakeSlider(sp, new Vector2(100f, 70f), new Vector2(320f, 40f), Mathf.InverseLerp(MainMenu.LookMin, MainMenu.LookMax, 1f), null);
        UIKit.Label(sp, "배경 음악", 32f, new Vector2(-230f, -50f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        Button music = Btn(sp, "켜짐", new Vector2(130f, -50f), new Vector2(260f, 64f));
        UIKit.Label(sp, "화면", 32f, new Vector2(-230f, -170f), new Vector2(280f, 50f), TextAlignmentOptions.Left);
        Button screen = Btn(sp, "창 모드", new Vector2(130f, -170f), new Vector2(260f, 64f));

        // 처음에 걷히는 까만 막 (에디터에서 화면을 가리지 않도록 투명하게 두고, 켜질 때 MainMenu가 덮는다)
        RectTransform black = Full(root, "Curtain");
        Image blackImage = black.gameObject.AddComponent<Image>();
        blackImage.color = new Color(0.07f, 0.09f, 0.11f, 1f); blackImage.raycastTarget = false;
        CanvasGroup curtain = black.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 0f; curtain.blocksRaycasts = false;

        helpPanel.SetActive(false);
        settingsPanel.SetActive(false);
        UIRound.Apply(canvas.gameObject);

        Put(menu, "startButton", start);
        Put(menu, "helpButton", help);
        Put(menu, "settingsButton", settings);
        Put(menu, "noteLabel", note);
        Put(menu, "helpPanel", helpPanel);
        Put(menu, "settingsPanel", settingsPanel);
        Put(menu, "closeButtons", closers.ToArray());
        Put(menu, "volumeSlider", volumeSlider);
        Put(menu, "volumeValue", volumeValue);
        Put(menu, "lookSlider", lookSlider);
        Put(menu, "lookValue", lookValue);
        Put(menu, "musicButton", music);
        Put(menu, "screenButton", screen);
        Put(menu, "curtain", curtain);
        Put(menu, "jar", jar);
        Put(menu, "jarShadow", shadow);
        Put(menu, "plantSlots", plants);
        Put(menu, "fireflyImages", flies);
    }

    private static GameObject NewPanel(RectTransform root, string name, string title, Vector2 size, out RectTransform panel, List<Button> closers)
    {
        RectTransform holder = Full(root, name);
        RectTransform back = UIKit.Backdrop(holder); // 어두운 막을 누르면 닫힌다
        Button backButton = back.gameObject.AddComponent<Button>();
        backButton.transition = Selectable.Transition.None;
        backButton.targetGraphic = back.GetComponent<Image>();
        closers.Add(backButton);

        panel = UIKit.Panel(holder, "Panel", size);
        UIKit.Label(panel, title, 60f, new Vector2(0f, size.y / 2f - 75f), new Vector2(size.x - 100f, 84f)).name = "Title";
        Button close = Btn(panel, "닫기", new Vector2(0f, -size.y / 2f + 65f), new Vector2(300f, 70f));
        close.name = "CloseButton";
        closers.Add(close);
        return holder.gameObject;
    }

    private static void KeyRow(RectTransform panel, float keyX, float descX, float y, string key, string desc)
    {
        UIKit.Label(panel, key, 30f, new Vector2(keyX, y), new Vector2(170f, 52f), TextAlignmentOptions.Left, Brown).name = "Key";
        UIKit.Label(panel, desc, 27f, new Vector2(descX, y), new Vector2(360f, 52f), TextAlignmentOptions.Left).name = "Desc";
    }

    private static void FullImage(RectTransform parent, string name, Sprite sprite)
    {
        RectTransform r = Full(parent, name);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite; img.raycastTarget = false;
    }

    // ───────── 그림 만들기 (PNG로 저장해 스프라이트로 쓴다) ─────────

    private static Sprite SaveSprite(Texture2D tex, string name)
    {
        string path = ImageFolder + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D MakeGradient()
    {
        var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false);
        Color top = new Color(0.09f, 0.14f, 0.20f), bottom = new Color(0.25f, 0.32f, 0.31f);
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / 255f)));
        tex.Apply();
        return tex;
    }

    // 병 모양 거리: 음수면 병 안쪽 (몸통과 목을 부드럽게 이은 모양, y는 위쪽이 큼)
    private static float JarDist(Vector2 p)
    {
        float body = Box(p, new Vector2(256f, 285f), new Vector2(205f, 245f), 80f);
        float neck = Box(p, new Vector2(256f, 545f), new Vector2(112f, 60f), 22f);
        return SmoothMin(body, neck, 36f);
    }

    private static float Box(Vector2 p, Vector2 center, Vector2 half, float radius)
    {
        float qx = Mathf.Abs(p.x - center.x) - half.x + radius;
        float qy = Mathf.Abs(p.y - center.y) - half.y + radius;
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return Mathf.Min(Mathf.Max(qx, qy), 0f) + outside - radius;
    }

    private static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    private static Texture2D MakeGlass()
    {
        const int W = 512, H = 640;
        var pixels = new Color32[W * H];
        Color glass = new Color(0.86f, 0.95f, 1f);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = JarDist(p);

                float a = 0f;
                if (d < 0f)
                {
                    float inside = Mathf.Clamp01(0.5f - d);
                    a = (0.09f + 0.20f * Mathf.Clamp01(1f + d / 34f)) * inside;
                }
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d) / 3f) * 0.55f;
                a = 1f - (1f - a) * (1f - rim);

                float h1 = Box(p, new Vector2(92f, 300f), new Vector2(8f, 120f), 8f);
                float h2 = Box(p, new Vector2(92f, 458f), new Vector2(8f, 8f), 8f);
                float h3 = Box(p, new Vector2(425f, 255f), new Vector2(4f, 70f), 4f);
                a = Mathf.Max(a, 0.34f * Mathf.Clamp01(0.5f - h1));
                a = Mathf.Max(a, 0.34f * Mathf.Clamp01(0.5f - h2));
                a = Mathf.Max(a, 0.17f * Mathf.Clamp01(0.5f - h3));

                // 코르크 뚜껑
                float ld = Box(p, new Vector2(256f, 612f), new Vector2(142f, 26f), 11f);
                float cover = Mathf.Clamp01(0.5f - ld);
                Color lid = y < 597 ? new Color(0.46f, 0.32f, 0.21f) : (y > 628 ? new Color(0.72f, 0.54f, 0.37f) : new Color(0.62f, 0.45f, 0.30f));

                float outA = cover + a * (1f - cover);
                Color rgb = outA > 0f ? (lid * cover + glass * (a * (1f - cover))) / outA : glass;
                pixels[y * W + x] = new Color(rgb.r, rgb.g, rgb.b, outA);
            }
        }
        return Finish(W, H, pixels);
    }

    private static Texture2D MakeSoil()
    {
        const int W = 512, H = 640;
        var pixels = new Color32[W * H];
        for (int y = 0; y < 260; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = JarDist(p);
                float top = 118f + 7f * Mathf.Sin(x * 0.035f) + 4f * Mathf.Sin(x * 0.09f + 1f);
                float cover = Mathf.Clamp01(top - y + 0.5f) * Mathf.Clamp01(-d - 6f + 0.5f);
                if (cover <= 0f) continue;

                float grain = (Hash(x / 3, y / 3) - 0.5f) * 0.06f;
                float depth = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(y / top));
                Color c = new Color(0.34f, 0.25f, 0.19f) * depth;
                if (top - y < 7f) c = Color.Lerp(c, new Color(0.47f, 0.36f, 0.27f), 0.8f);
                c += new Color(grain, grain, grain);
                pixels[y * W + x] = new Color(c.r, c.g, c.b, 0.96f * cover);
            }
        }
        return Finish(W, H, pixels);
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static Texture2D Finish(int w, int h, Color32[] pixels)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // 가운데가 밝고 가장자리가 부드럽게 사라지는 동그라미 (반딧불이, 그림자)
    private static Texture2D MakeGlow()
    {
        const int S = 64;
        var pixels = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float r = new Vector2(x + 0.5f - S / 2f, y + 0.5f - S / 2f).magnitude / (S / 2f);
                float a = Mathf.Clamp01(1f - r);
                a *= a;
                pixels[y * S + x] = new Color(1f, 1f, 1f, a);
            }
        }
        return Finish(S, S, pixels);
    }
}
