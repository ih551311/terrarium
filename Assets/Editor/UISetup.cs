using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 가방 패널과 병 꾸미기 패널(UI)을 GameRoot 프리팹 안에 만들고 연결한다.
/// 메뉴: 유리병 정원 > UI 만들기. 여러 번 눌러도 같은 결과가 나온다(UI를 지우고 다시 만든다).
/// 프리팹을 고치므로, 열려 있는 모든 씬의 GameRoot에 바로 반영된다.
/// 병과 병 카메라는 여기서 다루지 않는다(집 안 씬의 병이 맡는다).
/// </summary>
public static class UISetup
{
    public const string BottleLayerName = "BottleView";
    private const string PrefabPath = "Assets/Prefabs/GameRoot.prefab";
    private const string FontAssetPath = "Assets/Resources/Fonts/Cafe24DongdongLight SDF.asset";
    private const string RenderTexturePath = "Assets/Data/Placeholder/BottleView.renderTexture";
    private const int SlotCount = 10;

    private static readonly Color TextColor = new Color(0.2f, 0.25f, 0.2f, 1f);

    private static TMP_FontAsset font;

    [MenuItem("유리병 정원/UI 만들기")]
    public static string Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) return "폰트 에셋이 없어요: " + FontAssetPath;
        SetDefaultTmpFont();

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            string result = BuildInside(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string BuildInside(GameObject root)
    {
        Transform player = root.transform.Find("Player");
        if (player == null) return "프리팹 안에 Player가 없어요.";
        Bag bag = player.GetComponent<Bag>();

        EnsureEventSystem(root);

        // UI 전체를 지우고 다시 만든다
        Transform old = root.transform.Find("UI");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject ui = new GameObject("UI", typeof(RectTransform));
        ui.transform.SetParent(root.transform, false);
        ui.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = ui.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = ui.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        ui.AddComponent<GraphicRaycaster>();

        // ── HUD: 바라보는 물건 안내와 알림 (맨 먼저 만들어 모든 패널 아래에 그려지게 한다)
        GameObject hudRoot = Rect(ui.transform, "Hud");
        Stretch(hudRoot);
        TextMeshProUGUI promptText, messageText;
        GameObject promptBar = MakeBar(hudRoot.transform, "PromptBar", new Vector2(0f, -160f), 54, Color.white, out promptText);
        GameObject messageBar = MakeBar(hudRoot.transform, "MessageBar", new Vector2(0f, -270f), 46, new Color(1f, 0.88f, 0.6f), out messageText);
        Hud hud = hudRoot.AddComponent<Hud>();
        SetRef(hud, "promptBar", promptBar); SetRef(hud, "promptText", promptText);
        SetRef(hud, "messageBar", messageBar); SetRef(hud, "messageText", messageText);
        SetRef(player.GetComponent<Interactor>(), "hud", hud);

        // 작은 지도(미니맵): 화면 오른쪽 위. 내 주변만 보여 주고 N 키로 켜고 끈다.
        Texture2D mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapBake.ImagePath);
        GameObject mini = Rect(hudRoot.transform, "MiniMap");
        Anchor(mini, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(324f, 324f));
        Image miniFrame = mini.AddComponent<Image>(); miniFrame.color = new Color(0.05f, 0.04f, 0.03f, 0.85f); miniFrame.raycastTarget = false;
        GameObject miniView = Rect(mini.transform, "View");
        Anchor(miniView, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
        RawImage miniImage = miniView.AddComponent<RawImage>(); miniImage.texture = mapTexture; miniImage.raycastTarget = false;
        GameObject miniHut = Rect(miniView.transform, "HutMarker");
        Anchor(miniHut, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
        Image miniHutImg = miniHut.AddComponent<Image>(); miniHutImg.color = new Color(0.55f, 0.33f, 0.2f, 1f); miniHutImg.raycastTarget = false;
        Label(miniHut.transform, "Text", "집", 20, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f), TextAlignmentOptions.Center).color = Color.white;
        GameObject miniPlayer = Rect(miniView.transform, "PlayerMarker");
        Anchor(miniPlayer, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
        Image miniPlayerImg = miniPlayer.AddComponent<Image>(); miniPlayerImg.color = new Color(0.9f, 0.22f, 0.22f, 1f); miniPlayerImg.raycastTarget = false;
        GameObject miniNose = Rect(miniPlayer.transform, "Nose");
        Anchor(miniNose, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(7f, 13f));
        Image miniNoseImg = miniNose.AddComponent<Image>(); miniNoseImg.color = Color.white; miniNoseImg.raycastTarget = false;
        Label(mini.transform, "North", "북", 26, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(60f, 34f), TextAlignmentOptions.Center).color = Color.white;

        MiniMap miniMap = hudRoot.AddComponent<MiniMap>();
        SetRef(miniMap, "root", mini); SetRef(miniMap, "image", miniImage);
        SetRef(miniMap, "area", miniView.GetComponent<RectTransform>());
        SetRef(miniMap, "playerMarker", miniPlayer.GetComponent<RectTransform>());
        SetRef(miniMap, "hutMarker", miniHut.GetComponent<RectTransform>());
        SerializedObject miniSo = new SerializedObject(miniMap);
        miniSo.FindProperty("mapCenter").vector2Value = MapBake.Center;
        miniSo.FindProperty("mapHalfExtent").floatValue = MapBake.HalfExtent;
        miniSo.ApplyModifiedPropertiesWithoutUndo();

        // 코인 표시: 항상 떠 있다. 오른쪽 위, 작은 지도의 왼쪽 옆 (지도가 꺼져도 같은 자리)
        GameObject coinBox = Rect(hudRoot.transform, "CoinHud");
        Anchor(coinBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-368f, -30f), new Vector2(240f, 64f));
        Image coinBg = coinBox.AddComponent<Image>(); coinBg.color = new Color(0.05f, 0.04f, 0.03f, 0.85f); coinBg.raycastTarget = false;
        GameObject coinIcon = Rect(coinBox.transform, "CoinIcon");
        Anchor(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // 동그란 모양
        Image coinIconImg = coinIcon.AddComponent<Image>(); coinIconImg.sprite = circle; coinIconImg.color = new Color(0.98f, 0.8f, 0.25f, 1f); coinIconImg.raycastTarget = false;
        GameObject coinShine = Rect(coinIcon.transform, "Shine");
        Anchor(coinShine, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
        Image coinShineImg = coinShine.AddComponent<Image>(); coinShineImg.sprite = circle; coinShineImg.color = new Color(1f, 0.93f, 0.55f, 1f); coinShineImg.raycastTarget = false;
        TextMeshProUGUI coinText = Label(coinBox.transform, "Amount", "0", 38, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(160f, 56f), TextAlignmentOptions.MidlineRight);
        coinText.color = Color.white;
        CoinHud coinHud = coinBox.AddComponent<CoinHud>();
        SetRef(coinHud, "amountText", coinText); SetRef(coinHud, "pulseTarget", coinBox.GetComponent<RectTransform>());

        // ── 병 꾸미기 패널 (먼저 만들어 가방보다 아래에 그려지게 한다)
        GameObject bottleRoot = Rect(ui.transform, "BottlePanel");
        Stretch(bottleRoot);
        Image dim = bottleRoot.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject bWindow = Rect(bottleRoot.transform, "Window");
        Center(bWindow, 1500f, 1000f);
        Image bBg = bWindow.AddComponent<Image>(); bBg.color = new Color(0.96f, 0.94f, 0.89f, 1f);
        Label(bWindow.transform, "Title", "병 꾸미기", 48, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(500f, 70f), TextAlignmentOptions.TopLeft);
        TextMeshProUGUI message = Label(bWindow.transform, "Message", string.Empty, 38, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(800f, 60f), TextAlignmentOptions.Center);
        message.color = new Color(0.72f, 0.3f, 0.2f, 1f);
        Button closeButton = MakeSmallButton(bWindow.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(150f, 60f), 32);

        // 병 그림: 꾸미는 곳이라 크게 둔다. 버튼들은 작게 해서 옆으로 비킨다.
        GameObject view = Rect(bWindow.transform, "BottleView");
        Anchor(view, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(740f, 740f));
        RawImage raw = view.AddComponent<RawImage>(); raw.color = Color.white;
        raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        DragRotate drag = view.AddComponent<DragRotate>();

        // 꾸밀 때만 보이는 것들은 EditTools 안에 모은다 (완성한 병을 열면 통째로 숨긴다)
        GameObject edit = Rect(bWindow.transform, "EditTools");
        Stretch(edit);

        Label(edit.transform, "Hint", "클릭: 놓기  |  재료 위 휠: 돌리기  |  그 밖의 휠: 확대/축소  |  Q/R: 크기  |  우클릭: 빼기  |  끌기: 병 방향 바꾸기 (T: 원위치)",
            24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(1440f, 40f), TextAlignmentOptions.Center);

        // 완성한 병을 열었을 때 보이는 글자 (평소엔 꺼 둔다)
        GameObject completedLabel = Label(bWindow.transform, "CompletedLabel", "완성한 병이에요\n끌어서 돌리고, 휠로 확대/축소할 수 있어요", 40,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1000f, 120f), TextAlignmentOptions.Center).gameObject;
        completedLabel.SetActive(false);

        // 완성한 뒤에 나오는 "새 병 만들기" 버튼 (병 만들기 화면으로 간다)
        Button newBottleButton = MakeSmallButton(bWindow.transform, "NewBottleButton", "새 병 만들기", new Vector2(0f, 1f), new Vector2(30f, -110f), new Vector2(230f, 80f), 34);
        newBottleButton.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
        newBottleButton.gameObject.SetActive(false);

        // 완성한 병을 선반에 전시하거나 내리는 버튼 (새 병 만들기 아래)
        Button displayButton = MakeSmallButton(bWindow.transform, "DisplayButton", "선반에 전시하기", new Vector2(0f, 1f), new Vector2(30f, -200f), new Vector2(230f, 80f), 30);
        displayButton.gameObject.SetActive(false);

        // 완성한 병을 비우는 버튼 (전시 버튼 아래, 두 번 눌러야 비워진다)
        Button emptyButton = MakeSmallButton(bWindow.transform, "EmptyButton", "병 비우기", new Vector2(0f, 1f), new Vector2(30f, -290f), new Vector2(230f, 90f), 26);
        emptyButton.GetComponent<Image>().color = new Color(0.9f, 0.78f, 0.74f, 1f);
        emptyButton.gameObject.SetActive(false);

        // 재료 칸: 가방 칸을 가로로 늘어놓는다
        GameObject strip = Rect(edit.transform, "MaterialStrip");
        Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1440f, 100f));
        HorizontalLayoutGroup hlg = strip.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        Button[] slotButtons = new Button[SlotCount];
        TMP_Text[] slotLabels = new TMP_Text[SlotCount];
        Image[] slotBackgrounds = new Image[SlotCount];
        Image[] stripIcons = new Image[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slot = Rect(strip.transform, "Slot" + i);
            Image bg = slot.AddComponent<Image>(); bg.color = new Color(0.88f, 0.9f, 0.87f, 1f);
            Button b = slot.AddComponent<Button>(); b.targetGraphic = bg;
            GameObject t = Rect(slot.transform, "Text");
            Stretch(t);
            TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.fontSize = 24; tmp.color = TextColor; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; tmp.text = string.Empty;
            // 위에 재료 그림, 아래에 이름과 개수 (재료 칸은 낮아서 글자를 작게)
            tmp.fontSize = 19;
            SetBottomText(tmp.rectTransform, 46f);
            stripIcons[i] = MakeSlotIcon(slot.transform, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(52f, 52f));
            slotButtons[i] = b; slotLabels[i] = tmp; slotBackgrounds[i] = bg;
        }

        MaterialStrip materialStrip = strip.AddComponent<MaterialStrip>();
        SetRef(materialStrip, "bag", bag);
        SetArray(materialStrip, "buttons", slotButtons);
        SetArray(materialStrip, "labels", slotLabels);
        SetArray(materialStrip, "backgrounds", slotBackgrounds);
        SetArray(materialStrip, "icons", stripIcons);

        BottleDecorator decorator = view.AddComponent<BottleDecorator>();
        SetRef(decorator, "bag", bag); SetRef(decorator, "strip", materialStrip); SetRef(decorator, "messageText", message);

        // 바닥재 고르기 (병 그림 오른쪽, 작게)
        GameObject groundBox = Rect(edit.transform, "GroundSelector");
        Anchor(groundBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -110f), new Vector2(200f, 210f));
        Label(groundBox.transform, "Title", "바닥재", 34, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(200f, 50f), TextAlignmentOptions.Center);
        Button soilButton = MakeSmallButton(groundBox.transform, "SoilButton", "흙", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(200f, 70f), 30);
        Button sandButton = MakeSmallButton(groundBox.transform, "SandButton", "모래 x0", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(200f, 70f), 30);
        GroundSelector groundSelector = groundBox.AddComponent<GroundSelector>();
        SetRef(groundSelector, "bag", bag); SetRef(groundSelector, "decorator", decorator);
        SetRef(groundSelector, "sand", BottleSetup.EnsureSandAsset());
        SetRef(groundSelector, "soilButton", soilButton); SetRef(groundSelector, "sandButton", sandButton);
        SetRef(groundSelector, "soilBackground", soilButton.GetComponent<Image>());
        SetRef(groundSelector, "sandBackground", sandButton.GetComponent<Image>());
        SetRef(groundSelector, "sandLabel", sandButton.GetComponentInChildren<TextMeshProUGUI>());

        // 병 만들기 화면: 꾸미는 중인 병이 없을 때 먼저 나온다. 흙의 양을 고르고 병 카드를 누르면 병이 만들어진다.
        // 병 종류와 흙의 양은 이때 정해지고 그 뒤로는 바꿀 수 없다. (창 전체를 덮어서, 닫기 버튼 말고는 아래 화면이 보이지 않는다)
        GameObject chooseBox = Rect(bWindow.transform, "BottleChooser");
        Stretch(chooseBox);
        Image chooseBg = chooseBox.AddComponent<Image>(); chooseBg.color = new Color(0.93f, 0.95f, 0.9f, 1f);
        Label(chooseBox.transform, "Title", "병을 만들어요", 56, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1000f, 80f), TextAlignmentOptions.Center);
        Label(chooseBox.transform, "Subtitle", "병 종류와 흙의 양은 만들 때 정하고, 그 뒤로는 바꿀 수 없어요", 30, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1300f, 50f), TextAlignmentOptions.Center);

        Label(chooseBox.transform, "SoilLabel", "흙의 양", 32, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-290f, -230f), new Vector2(160f, 56f), TextAlignmentOptions.Center);
        string[] soilNames = { "적게", "보통", "많이" };
        Button[] soilButtons = new Button[3];
        Image[] soilBackgrounds = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Button b = MakeSmallButton(chooseBox.transform, "Soil" + i, soilNames[i], new Vector2(0.5f, 1f), new Vector2(-90f + i * 180f, -230f), new Vector2(160f, 56f), 28);
            soilButtons[i] = b; soilBackgrounds[i] = b.GetComponent<Image>();
        }

        const int ChoiceCount = 4;
        Button[] choiceButtons = new Button[ChoiceCount];
        TMP_Text[] choiceLabels = new TMP_Text[ChoiceCount];
        Image[] choiceBackgrounds = new Image[ChoiceCount];
        for (int i = 0; i < ChoiceCount; i++)
        {
            // 카드: 위에 병 예시 그림, 아래에 이름과 상태
            Button b = MakeSmallButton(chooseBox.transform, "Choice" + i, "병", new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 290f, -110f), new Vector2(270f, 400f), 30);
            choiceButtons[i] = b;
            choiceLabels[i] = b.GetComponentInChildren<TextMeshProUGUI>();
            choiceBackgrounds[i] = b.GetComponent<Image>();

            RectTransform labelRect = choiceLabels[i].rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f); labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 8f); labelRect.sizeDelta = new Vector2(0f, 120f);

            GameObject icon = Rect(b.transform, "Icon");
            Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(240f, 240f));
            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = BottleIconMaker.Load(i);
            iconImage.color = iconImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.raycastTarget = false;
        }
        GameObject noneLeft = Label(chooseBox.transform, "NoneLeft", "만들 수 있는 병이 없어요 (병은 나중에 살 수 있어요)", 34, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1300f, 60f), TextAlignmentOptions.Center).gameObject;
        noneLeft.SetActive(false);
        closeButton.transform.SetAsLastSibling(); // 닫기 버튼이 병 만들기 화면 위에도 보이게 한다

        BottleChooser chooser = chooseBox.AddComponent<BottleChooser>();
        SetArray(chooser, "buttons", choiceButtons);
        SetArray(chooser, "labels", choiceLabels);
        SetArray(chooser, "backgrounds", choiceBackgrounds);
        SetArray(chooser, "soilButtons", soilButtons);
        SetArray(chooser, "soilBackgrounds", soilBackgrounds);
        SetRef(chooser, "noneLeftNotice", noneLeft);

        // 완성 버튼 (병 그림 왼쪽, 작게)
        Button completeButton = MakeSmallButton(edit.transform, "CompleteButton", "완성", new Vector2(0f, 1f), new Vector2(30f, -110f), new Vector2(200f, 80f), 36);
        completeButton.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
        CompleteBottleButton complete = completeButton.gameObject.AddComponent<CompleteBottleButton>();

        // ── 섬 지도 패널 (M 키). 지도 그림 위에 내 위치와 집을 표시하고, 집으로 돌아가는 버튼이 있다.
        GameObject mapRoot = Rect(ui.transform, "MapPanel");
        Stretch(mapRoot);
        Image mapDim = mapRoot.AddComponent<Image>(); mapDim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject mWindow = Rect(mapRoot.transform, "Window");
        Center(mWindow, 1560f, 940f);
        Image mBg = mWindow.AddComponent<Image>(); mBg.color = new Color(0.96f, 0.94f, 0.89f, 1f);
        Label(mWindow.transform, "Title", "섬 지도", 64, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -25f), new Vector2(600f, 90f), TextAlignmentOptions.TopLeft);
        Button mapCloseButton = MakeButton(mWindow.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 80f));

        GameObject mapImageGo = Rect(mWindow.transform, "Map");
        Anchor(mapImageGo, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 40f), new Vector2(800f, 800f));
        RawImage mapImage = mapImageGo.AddComponent<RawImage>();
        mapImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapBake.ImagePath);
        mapImage.raycastTarget = true; // 클릭하면 그 자리로 이동한다 (MapClick)
        MapClick mapClick = mapImageGo.AddComponent<MapClick>();

        // 집 표시: 갈색 네모와 "집" 글자
        GameObject hutMarker = Rect(mapImageGo.transform, "HutMarker");
        Anchor(hutMarker, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
        Image hutImg = hutMarker.AddComponent<Image>(); hutImg.color = new Color(0.55f, 0.33f, 0.2f, 1f); hutImg.raycastTarget = false;
        TextMeshProUGUI hutText = Label(hutMarker.transform, "Text", "집", 34, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f), TextAlignmentOptions.Center);
        hutText.color = Color.white;

        // 내 위치: 빨간 네모와 바라보는 방향을 알려 주는 작은 흰 막대 (위쪽이 북쪽)
        GameObject playerMarker = Rect(mapImageGo.transform, "PlayerMarker");
        Anchor(playerMarker, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        Image playerImg = playerMarker.AddComponent<Image>(); playerImg.color = new Color(0.9f, 0.22f, 0.22f, 1f); playerImg.raycastTarget = false;
        GameObject nose = Rect(playerMarker.transform, "Nose");
        Anchor(nose, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(12f, 22f));
        Image noseImg = nose.AddComponent<Image>(); noseImg.color = Color.white; noseImg.raycastTarget = false;

        // 오른쪽: 집으로 돌아가기 버튼과 설명
        Button homeButton = MakeButton(mWindow.transform, "HomeButton", "집으로 돌아가기", new Vector2(1f, 1f), new Vector2(-60f, -230f), new Vector2(560f, 130f));
        homeButton.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
        homeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 54;
        Label(mWindow.transform, "Legend", "지도를 누르면 그 자리로 이동해요\n\n빨간 표시: 지금 내 위치\n(흰 막대가 바라보는 방향)\n갈색 \"집\": 오두막 앞\n위쪽이 북쪽이에요\n\nM 또는 Esc: 닫기  |  N: 작은 지도 켜기/끄기",
            38, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -400f), new Vector2(560f, 400f), TextAlignmentOptions.TopLeft);
        TextMeshProUGUI mapMessage = Label(mWindow.transform, "Message", string.Empty, 40, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60f, 60f), new Vector2(560f, 140f), TextAlignmentOptions.BottomLeft);
        mapMessage.color = new Color(0.72f, 0.3f, 0.2f, 1f);

        // ── 우체통 패널: 보내기(가방 재료 → 다음 날 코인), 주문하기(코인 → 다음 날 물건), 도착한 것 받기
        GameObject mailRoot = Rect(ui.transform, "MailboxPanel");
        Stretch(mailRoot);
        Image mailDim = mailRoot.AddComponent<Image>(); mailDim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject mailWindow = Rect(mailRoot.transform, "Window");
        Center(mailWindow, 1560f, 940f);
        Image mailBg = mailWindow.AddComponent<Image>(); mailBg.color = new Color(0.96f, 0.94f, 0.89f, 1f);
        Label(mailWindow.transform, "Title", "우체통", 64, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -25f), new Vector2(400f, 90f), TextAlignmentOptions.TopLeft);
        TextMeshProUGUI mailHeader = Label(mailWindow.transform, "Header", "코인 0", 50, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(760f, 80f), TextAlignmentOptions.Center);
        Button mailCloseButton = MakeButton(mailWindow.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 80f));

        // 왼쪽: 보내기 (가방 칸마다 하나씩, 누르면 1개 넣기)
        Label(mailWindow.transform, "SendTitle", "보내기  (내일 아침 코인이 와요)", 40, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -125f), new Vector2(700f, 60f), TextAlignmentOptions.TopLeft);
        GameObject sendGrid = Rect(mailWindow.transform, "SendGrid");
        Anchor(sendGrid, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -195f), new Vector2(700f, 280f));
        GridLayoutGroup sendLayout = sendGrid.AddComponent<GridLayoutGroup>();
        sendLayout.cellSize = new Vector2(132f, 130f); sendLayout.spacing = new Vector2(8f, 10f);
        sendLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; sendLayout.constraintCount = 5;
        Button[] sendButtons = new Button[SlotCount];
        TMP_Text[] sendLabels = new TMP_Text[SlotCount];
        Image[] sendIcons = new Image[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            Button b = MakeButton(sendGrid.transform, "Send" + i, string.Empty, new Vector2(0f, 1f), Vector2.zero, new Vector2(132f, 130f));
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 20;
            SetBottomText(t.rectTransform, 56f);
            sendIcons[i] = MakeSlotIcon(b.transform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(66f, 66f));
            sendButtons[i] = b; sendLabels[i] = t;
        }

        // 보낸 것과 주문한 것 요약
        TextMeshProUGUI mailSummary = Label(mailWindow.transform, "Summary", string.Empty, 34, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -500f), new Vector2(700f, 200f), TextAlignmentOptions.TopLeft);

        // 오른쪽: 주문하기 (코인을 내고 사는 물건. 가격이 있는 재료를 모두 보여 준다)
        // 도구 강화 (도끼, 곡괭이): 코인을 내고 주문하면 다음 날 도착하고, 받으면 도구 단계가 오른다
        Label(mailWindow.transform, "UpgradeTitle", "도구 강화  (내일 아침 도착해요)", 40, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(810f, -115f), new Vector2(700f, 50f), TextAlignmentOptions.TopLeft);
        Button[] upgradeButtons = new Button[2];
        TMP_Text[] upgradeLabels = new TMP_Text[2];
        for (int i = 0; i < 2; i++)
        {
            Button b = MakeButton(mailWindow.transform, "Upgrade" + i, string.Empty, new Vector2(0f, 1f), new Vector2(810f + i * 355f, -170f), new Vector2(345f, 100f));
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 22;
            b.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
            upgradeButtons[i] = b; upgradeLabels[i] = t;
        }

        Label(mailWindow.transform, "OrderTitle", "주문하기  (내일 아침 도착해요)", 40, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(810f, -285f), new Vector2(700f, 50f), TextAlignmentOptions.TopLeft);
        GameObject orderGrid = Rect(mailWindow.transform, "OrderGrid");
        Anchor(orderGrid, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(810f, -340f), new Vector2(700f, 350f));
        GridLayoutGroup orderLayout = orderGrid.AddComponent<GridLayoutGroup>();
        orderLayout.cellSize = new Vector2(167f, 80f); orderLayout.spacing = new Vector2(10f, 8f);
        orderLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; orderLayout.constraintCount = 4; // 16개가 4줄에 들어간다

        var orderableList = new System.Collections.Generic.List<MaterialData>();
        foreach (string guid in AssetDatabase.FindAssets("t:MaterialData"))
        {
            MaterialData md = AssetDatabase.LoadAssetAtPath<MaterialData>(AssetDatabase.GUIDToAssetPath(guid));
            if (md != null && md.buyPrice > 0) orderableList.Add(md);
        }
        orderableList.Sort((a, b) => a.buyPrice != b.buyPrice ? a.buyPrice.CompareTo(b.buyPrice) : string.CompareOrdinal(a.id, b.id));
        Button[] orderButtons = new Button[orderableList.Count];
        TMP_Text[] orderLabels = new TMP_Text[orderableList.Count];
        Image[] orderIcons = new Image[orderableList.Count];
        for (int i = 0; i < orderableList.Count; i++)
        {
            Button b = MakeButton(orderGrid.transform, "Order" + i, string.Empty, new Vector2(0f, 1f), Vector2.zero, new Vector2(226f, 80f));
            TextMeshProUGUI t = b.GetComponentInChildren<TextMeshProUGUI>();
            t.fontSize = 22;
            // 왼쪽에 재료 그림, 오른쪽에 이름과 가격
            RectTransform tr = t.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(60f, 0f); tr.offsetMax = Vector2.zero;
            orderIcons[i] = MakeSlotIcon(b.transform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(54f, 54f));
            orderButtons[i] = b; orderLabels[i] = t;
        }

        // 아래: 도착한 것 받기와 알림
        Button collectButton = MakeButton(mailWindow.transform, "CollectButton", "도착한 것 받기", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1000f, 110f));
        collectButton.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
        TextMeshProUGUI collectLabel = collectButton.GetComponentInChildren<TextMeshProUGUI>();
        collectLabel.fontSize = 44;
        TextMeshProUGUI mailMessage = Label(mailWindow.transform, "Message", string.Empty, 38, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1400f, 70f), TextAlignmentOptions.Center);
        mailMessage.color = new Color(0.72f, 0.3f, 0.2f, 1f);

        // ── 가방 패널
        GameObject invRoot = Rect(ui.transform, "InventoryPanel");
        Stretch(invRoot);
        Image invDim = invRoot.AddComponent<Image>(); invDim.color = new Color(0f, 0f, 0f, 0.35f);

        GameObject iWindow = Rect(invRoot.transform, "Window");
        Center(iWindow, 1000f, 620f);
        Image iBg = iWindow.AddComponent<Image>(); iBg.color = new Color(0.93f, 0.95f, 0.9f, 1f);
        Label(iWindow.transform, "Title", "가방", 64, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(500f, 90f), TextAlignmentOptions.TopLeft);
        Label(iWindow.transform, "Hint", "Tab: 닫기", 40, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -45f), new Vector2(400f, 60f), TextAlignmentOptions.TopRight);

        GameObject grid = Rect(iWindow.transform, "Slots");
        Anchor(grid, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(920f, 400f));
        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(168f, 180f); glg.spacing = new Vector2(10f, 20f);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; glg.constraintCount = 5;
        glg.childAlignment = TextAnchor.UpperCenter;

        TMP_Text[] slotTexts = new TMP_Text[SlotCount];
        Image[] slotIcons = new Image[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slot = Rect(grid.transform, "Slot" + i);
            Image bg = slot.AddComponent<Image>(); bg.color = new Color(0.82f, 0.88f, 0.82f, 1f); bg.raycastTarget = false;
            GameObject t = Rect(slot.transform, "Text");
            Stretch(t);
            TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.fontSize = 28; tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false; tmp.text = string.Empty;
            SetBottomText(tmp.rectTransform, 70f);
            slotIcons[i] = MakeSlotIcon(slot.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(100f, 100f));
            slotTexts[i] = tmp;
        }

        // ── 제어 스크립트 (Canvas 오브젝트에 붙는다)
        BottlePanel bp = ui.AddComponent<BottlePanel>();
        SetRef(bp, "root", bottleRoot); SetRef(bp, "closeButton", closeButton);
        SetRef(bp, "dragRotate", drag); SetRef(bp, "decorator", decorator);
        SetRef(bp, "groundSelector", groundSelector); SetRef(bp, "completeButton", complete); SetRef(bp, "chooser", chooser);
        SetRef(bp, "bottleView", view); SetRef(bp, "newBottleButton", newBottleButton);
        SetRef(bp, "emptyButton", emptyButton); SetRef(bp, "emptyLabel", emptyButton.GetComponentInChildren<TextMeshProUGUI>());
        SetRef(bp, "displayButton", displayButton); SetRef(bp, "displayLabel", displayButton.GetComponentInChildren<TextMeshProUGUI>());
        SetRef(bp, "editTools", edit); SetRef(bp, "completedLabel", completedLabel);
        SetRef(complete, "button", completeButton);
        SetRef(complete, "label", completeButton.GetComponentInChildren<TextMeshProUGUI>());
        SetRef(complete, "panel", bp); SetRef(complete, "decorator", decorator);
        InventoryPanel ip = ui.AddComponent<InventoryPanel>();
        SetRef(ip, "bag", bag); SetRef(ip, "root", invRoot);
        SetArray(ip, "slotTexts", slotTexts);
        SetArray(ip, "slotIcons", slotIcons);

        MapPanel mp = ui.AddComponent<MapPanel>();
        SetRef(mp, "root", mapRoot); SetRef(mp, "closeButton", mapCloseButton); SetRef(mp, "homeButton", homeButton);
        SetRef(mp, "mapRect", mapImageGo.GetComponent<RectTransform>());
        SetRef(mp, "playerMarker", playerMarker.GetComponent<RectTransform>());
        SetRef(mp, "hutMarker", hutMarker.GetComponent<RectTransform>());
        SetRef(mp, "messageText", mapMessage);
        SetRef(mapClick, "panel", mp);
        SerializedObject mapSo = new SerializedObject(mp);
        mapSo.FindProperty("mapCenter").vector2Value = MapBake.Center;
        mapSo.FindProperty("mapHalfExtent").floatValue = MapBake.HalfExtent;
        mapSo.ApplyModifiedPropertiesWithoutUndo();

        MailboxPanel mailPanel = ui.AddComponent<MailboxPanel>();
        SetRef(mailPanel, "bag", bag); SetRef(mailPanel, "root", mailRoot); SetRef(mailPanel, "closeButton", mailCloseButton);
        SetRef(mailPanel, "headerText", mailHeader); SetRef(mailPanel, "summaryText", mailSummary); SetRef(mailPanel, "messageText", mailMessage);
        SetArray(mailPanel, "sendButtons", sendButtons); SetArray(mailPanel, "sendLabels", sendLabels); SetArray(mailPanel, "sendIcons", sendIcons);
        SetArray(mailPanel, "orderIcons", orderIcons);
        SetArray(mailPanel, "orderables", orderableList.ToArray());
        SetArray(mailPanel, "orderButtons", orderButtons); SetArray(mailPanel, "orderLabels", orderLabels);
        SetArray(mailPanel, "upgradeButtons", upgradeButtons); SetArray(mailPanel, "upgradeLabels", upgradeLabels);
        SetRef(mailPanel, "collectButton", collectButton); SetRef(mailPanel, "collectLabel", collectLabel);

        // GameRoot가 새 병 패널과 우체통 패널을 가리키도록 다시 연결한다
        SetRef(root.GetComponent<GameRoot>(), "bottlePanel", bp);
        SetRef(root.GetComponent<GameRoot>(), "mailboxPanel", mailPanel);

        return "UI 만들기 완료";
    }

    // ───────── 도우미 ─────────

    private static GameObject Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Center(GameObject go, float w, float h)
    {
        Anchor(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
    }

    /// <summary>앵커와 피벗을 같은 점으로 맞추고, 그 점에서 떨어진 위치와 크기를 정한다.</summary>
    private static void Anchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, float size,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject go = Rect(parent, name);
        Anchor(go, anchor, pivot, pos, box);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = font; t.text = text; t.fontSize = size; t.alignment = align;
        t.color = TextColor; t.raycastTarget = false;
        return t;
    }

    /// <summary>글자 크기에 맞춰 늘어나는 어두운 반투명 바탕 위에 글자를 얹은 안내 막대를 만든다.</summary>
    private static GameObject MakeBar(Transform parent, string name, Vector2 pos, float fontSize, Color textColor, out TextMeshProUGUI text)
    {
        GameObject bar = Rect(parent, name);
        Anchor(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(100f, 100f));
        Image bg = bar.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.03f, 0.84f); // 밝은 모래와 하늘 위에서도 읽히게 짙게
        bg.raycastTarget = false;

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 16, 16);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = bar.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject t = Rect(bar.transform, "Text");
        text = t.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.color = textColor;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = "[E] 안내 글자";
        return bar;
    }

    private static Button MakeButton(Transform parent, string name, string text, Vector2 corner, Vector2 pos, Vector2 size)
    {
        GameObject go = Rect(parent, name);
        Anchor(go, corner, corner, pos, size);
        Image img = go.AddComponent<Image>(); img.color = new Color(0.66f, 0.8f, 0.72f, 1f);
        Button b = go.AddComponent<Button>(); b.targetGraphic = img;
        GameObject t = Rect(go.transform, "Text");
        Stretch(t);
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = font; tmp.text = text; tmp.fontSize = 44; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.15f, 0.2f, 0.15f, 1f); tmp.raycastTarget = false;
        return b;
    }

    /// <summary>칸 위쪽(또는 왼쪽)에 놓는 재료 그림 자리를 만든다. 그림이 없으면 숨겨진다.</summary>
    private static Image MakeSlotIcon(Transform slot, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject go = Rect(slot, "Icon");
        Anchor(go, anchor, anchor, pos, size);
        Image img = go.AddComponent<Image>();
        img.preserveAspect = true; img.raycastTarget = false; img.enabled = false;
        return img;
    }

    /// <summary>칸의 글자를 아래쪽 띠(높이 height)로 옮긴다. (위쪽은 재료 그림 자리)</summary>
    private static void SetBottomText(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 2f); rt.sizeDelta = new Vector2(0f, height);
    }

    /// <summary>글자 크기를 정해서 만드는 버튼 (꾸미는 화면처럼 좁은 곳에서 작게 쓴다)</summary>
    private static Button MakeSmallButton(Transform parent, string name, string text, Vector2 corner, Vector2 pos, Vector2 size, float fontSize)
    {
        Button b = MakeButton(parent, name, text, corner, pos, size);
        b.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
        return b;
    }

    public static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty arr = so.FindProperty(field);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < 32; i++) // 8번부터가 사용자 레이어
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        throw new System.Exception("빈 레이어가 없어요.");
    }

    private static void SetDefaultTmpFont()
    {
        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null) return;
        SerializedObject so = new SerializedObject(settings);
        SerializedProperty p = so.FindProperty("m_defaultFontAsset");
        if (p != null) { p.objectReferenceValue = font; so.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void EnsureEventSystem(GameObject root)
    {
        EventSystem es = root.GetComponentInChildren<EventSystem>(true);
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(root.transform, false);
            es = go.GetComponent<EventSystem>();
        }
        // 새 Input System용 입력 모듈로 바꾼다.
        StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
