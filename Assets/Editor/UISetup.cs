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

        // ── 병 꾸미기 패널 (먼저 만들어 가방보다 아래에 그려지게 한다)
        GameObject bottleRoot = Rect(ui.transform, "BottlePanel");
        Stretch(bottleRoot);
        Image dim = bottleRoot.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject bWindow = Rect(bottleRoot.transform, "Window");
        Center(bWindow, 1300f, 960f);
        Image bBg = bWindow.AddComponent<Image>(); bBg.color = new Color(0.96f, 0.94f, 0.89f, 1f);
        Label(bWindow.transform, "Title", "병 꾸미기", 64, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -25f), new Vector2(600f, 90f), TextAlignmentOptions.TopLeft);
        TextMeshProUGUI message = Label(bWindow.transform, "Message", string.Empty, 44, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 70f), TextAlignmentOptions.Center);
        message.color = new Color(0.72f, 0.3f, 0.2f, 1f);
        Button closeButton = MakeButton(bWindow.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(220f, 80f));

        GameObject view = Rect(bWindow.transform, "BottleView");
        Anchor(view, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(620f, 620f));
        RawImage raw = view.AddComponent<RawImage>(); raw.color = Color.white;
        raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        DragRotate drag = view.AddComponent<DragRotate>();

        // 꾸밀 때만 보이는 것들은 EditTools 안에 모은다 (완성한 병을 열면 통째로 숨긴다)
        GameObject edit = Rect(bWindow.transform, "EditTools");
        Stretch(edit);

        Label(edit.transform, "Hint", "클릭: 놓기   |   재료 위 휠: 돌리기   |   그 밖의 휠: 확대/축소   |   Q/R: 크기   |   우클릭: 빼기   |   끌기: 병 돌리기",
            28, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(1240f, 50f), TextAlignmentOptions.Center);

        // 완성한 병을 열었을 때 보이는 글자 (평소엔 꺼 둔다)
        GameObject completedLabel = Label(bWindow.transform, "CompletedLabel", "완성한 병이에요\n끌어서 돌리고, 휠로 확대/축소할 수 있어요", 48,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1000f, 160f), TextAlignmentOptions.Center).gameObject;
        completedLabel.SetActive(false);

        // 재료 칸: 가방 칸을 가로로 늘어놓는다
        GameObject strip = Rect(edit.transform, "MaterialStrip");
        Anchor(strip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1220f, 130f));
        HorizontalLayoutGroup hlg = strip.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        Button[] slotButtons = new Button[SlotCount];
        TMP_Text[] slotLabels = new TMP_Text[SlotCount];
        Image[] slotBackgrounds = new Image[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slot = Rect(strip.transform, "Slot" + i);
            Image bg = slot.AddComponent<Image>(); bg.color = new Color(0.88f, 0.9f, 0.87f, 1f);
            Button b = slot.AddComponent<Button>(); b.targetGraphic = bg;
            GameObject t = Rect(slot.transform, "Text");
            Stretch(t);
            TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.fontSize = 28; tmp.color = TextColor; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; tmp.text = string.Empty;
            slotButtons[i] = b; slotLabels[i] = tmp; slotBackgrounds[i] = bg;
        }

        MaterialStrip materialStrip = strip.AddComponent<MaterialStrip>();
        SetRef(materialStrip, "bag", bag);
        SetArray(materialStrip, "buttons", slotButtons);
        SetArray(materialStrip, "labels", slotLabels);
        SetArray(materialStrip, "backgrounds", slotBackgrounds);

        BottleDecorator decorator = view.AddComponent<BottleDecorator>();
        SetRef(decorator, "bag", bag); SetRef(decorator, "strip", materialStrip); SetRef(decorator, "messageText", message);

        // 바닥재 고르기 (병 그림 오른쪽)
        GameObject groundBox = Rect(edit.transform, "GroundSelector");
        Anchor(groundBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -170f), new Vector2(290f, 360f));
        Label(groundBox.transform, "Title", "바닥재", 48, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(290f, 70f), TextAlignmentOptions.Center);
        Button soilButton = MakeButton(groundBox.transform, "SoilButton", "흙", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(290f, 100f));
        Button sandButton = MakeButton(groundBox.transform, "SandButton", "모래 x0", new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(290f, 100f));
        GroundSelector groundSelector = groundBox.AddComponent<GroundSelector>();
        SetRef(groundSelector, "bag", bag); SetRef(groundSelector, "decorator", decorator);
        SetRef(groundSelector, "sand", BottleSetup.EnsureSandAsset());
        SetRef(groundSelector, "soilButton", soilButton); SetRef(groundSelector, "sandButton", sandButton);
        SetRef(groundSelector, "soilBackground", soilButton.GetComponent<Image>());
        SetRef(groundSelector, "sandBackground", sandButton.GetComponent<Image>());
        SetRef(groundSelector, "sandLabel", sandButton.GetComponentInChildren<TextMeshProUGUI>());

        // 병 고르기 (병 그림 왼쪽, 완성 버튼 아래). 완성한 병도 보러 갈 수 있게 꾸미기 도구 밖에 둔다.
        GameObject chooseBox = Rect(bWindow.transform, "BottleChooser");
        Anchor(chooseBox, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -340f), new Vector2(290f, 340f));
        Label(chooseBox.transform, "Title", "병 고르기", 48, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(290f, 70f), TextAlignmentOptions.Center);
        const int ChoiceCount = 3;
        Button[] choiceButtons = new Button[ChoiceCount];
        TMP_Text[] choiceLabels = new TMP_Text[ChoiceCount];
        Image[] choiceBackgrounds = new Image[ChoiceCount];
        for (int i = 0; i < ChoiceCount; i++)
        {
            Button b = MakeButton(chooseBox.transform, "Choice" + i, "병", new Vector2(0.5f, 1f), new Vector2(0f, -85f - i * 95f), new Vector2(290f, 85f));
            b.GetComponentInChildren<TextMeshProUGUI>().fontSize = 38;
            choiceButtons[i] = b;
            choiceLabels[i] = b.GetComponentInChildren<TextMeshProUGUI>();
            choiceBackgrounds[i] = b.GetComponent<Image>();
        }
        BottleChooser chooser = chooseBox.AddComponent<BottleChooser>();
        SetArray(chooser, "buttons", choiceButtons);
        SetArray(chooser, "labels", choiceLabels);
        SetArray(chooser, "backgrounds", choiceBackgrounds);

        // 완성 버튼 (병 그림 왼쪽)
        Button completeButton = MakeButton(edit.transform, "CompleteButton", "완성", new Vector2(0f, 1f), new Vector2(50f, -170f), new Vector2(290f, 140f));
        completeButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 40;
        completeButton.GetComponent<Image>().color = new Color(0.98f, 0.82f, 0.45f, 1f);
        CompleteBottleButton complete = completeButton.gameObject.AddComponent<CompleteBottleButton>();

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
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slot = Rect(grid.transform, "Slot" + i);
            Image bg = slot.AddComponent<Image>(); bg.color = new Color(0.82f, 0.88f, 0.82f, 1f); bg.raycastTarget = false;
            GameObject t = Rect(slot.transform, "Text");
            Stretch(t);
            TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.fontSize = 38; tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false; tmp.text = string.Empty;
            slotTexts[i] = tmp;
        }

        // ── 제어 스크립트 (Canvas 오브젝트에 붙는다)
        BottlePanel bp = ui.AddComponent<BottlePanel>();
        SetRef(bp, "root", bottleRoot); SetRef(bp, "closeButton", closeButton);
        SetRef(bp, "dragRotate", drag); SetRef(bp, "decorator", decorator);
        SetRef(bp, "groundSelector", groundSelector); SetRef(bp, "completeButton", complete); SetRef(bp, "chooser", chooser);
        SetRef(bp, "editTools", edit); SetRef(bp, "completedLabel", completedLabel);
        SetRef(complete, "button", completeButton);
        SetRef(complete, "label", completeButton.GetComponentInChildren<TextMeshProUGUI>());
        SetRef(complete, "panel", bp); SetRef(complete, "decorator", decorator);
        InventoryPanel ip = ui.AddComponent<InventoryPanel>();
        SetRef(ip, "bag", bag); SetRef(ip, "root", invRoot);
        SetArray(ip, "slotTexts", slotTexts);

        // GameRoot가 새 병 패널을 가리키도록 다시 연결한다
        SetRef(root.GetComponent<GameRoot>(), "bottlePanel", bp);

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
