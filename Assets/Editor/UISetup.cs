using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 가방 패널과 병 꾸미기 패널(UI)을 씬에 만들고 연결한다.
/// 메뉴: 유리병 정원 > UI 만들기. 여러 번 눌러도 같은 결과가 나온다(UI를 지우고 다시 만든다).
/// 병과 병 카메라는 여기서 다루지 않는다(집 안 씬의 BottleObject가 맡는다).
/// </summary>
public static class UISetup
{
    public const string BottleLayerName = "BottleView";
    private const string FontAssetPath = "Assets/Resources/Fonts/Cafe24DongdongLight SDF.asset";

    private static TMP_FontAsset font;

    [MenuItem("유리병 정원/UI 만들기")]
    public static string Build()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null) return "폰트 에셋이 없어요: " + FontAssetPath;

        SetDefaultTmpFont();
        EnsureEventSystem();

        GameObject player = GameObject.Find("Player");
        if (player == null) return "Player를 찾을 수 없어요.";
        Bag bag = player.GetComponent<Bag>();

        // UI 전체를 지우고 다시 만든다
        GameObject old = GameObject.Find("UI");
        if (old != null) Object.DestroyImmediate(old);

        GameObject ui = new GameObject("UI", typeof(RectTransform));
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
        Center(bWindow, 1200f, 860f);
        Image bBg = bWindow.AddComponent<Image>(); bBg.color = new Color(0.96f, 0.94f, 0.89f, 1f);
        Label(bWindow.transform, "Title", "병 꾸미기", 64, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(600f, 90f), TextAlignmentOptions.TopLeft);

        GameObject view = Rect(bWindow.transform, "BottleView");
        Anchor(view, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 640f));
        RawImage raw = view.AddComponent<RawImage>(); raw.color = Color.white;
        raw.texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Data/Placeholder/BottleView.renderTexture");
        DragRotate drag = view.AddComponent<DragRotate>();

        Label(bWindow.transform, "Hint", "병을 끌어서 돌려 보세요", 44, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(900f, 70f), TextAlignmentOptions.Center);
        Button closeButton = MakeButton(bWindow.transform, "CloseButton", "닫기", new Vector2(1f, 1f), new Vector2(-40f, -35f), new Vector2(220f, 80f));

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

        int slotCount = 10;
        TMP_Text[] slotTexts = new TMP_Text[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = Rect(grid.transform, "Slot" + i);
            Image bg = slot.AddComponent<Image>(); bg.color = new Color(0.82f, 0.88f, 0.82f, 1f); bg.raycastTarget = false;
            GameObject t = Rect(slot.transform, "Text");
            Stretch(t);
            TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.fontSize = 38; tmp.color = new Color(0.2f, 0.25f, 0.2f, 1f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false; tmp.text = string.Empty;
            slotTexts[i] = tmp;
        }

        // ── 제어 스크립트 (Canvas 오브젝트에 붙는다)
        BottlePanel bp = ui.AddComponent<BottlePanel>();
        SetRef(bp, "root", bottleRoot); SetRef(bp, "closeButton", closeButton); SetRef(bp, "dragRotate", drag);
        InventoryPanel ip = ui.AddComponent<InventoryPanel>();
        SetRef(ip, "bag", bag); SetRef(ip, "root", invRoot);
        SerializedObject so = new SerializedObject(ip);
        SerializedProperty arr = so.FindProperty("slotTexts");
        arr.arraySize = slotCount;
        for (int i = 0; i < slotCount; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = slotTexts[i];
        so.ApplyModifiedPropertiesWithoutUndo();

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
        t.color = new Color(0.2f, 0.25f, 0.2f, 1f); t.raycastTarget = false;
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
        SerializedProperty p = so.FindProperty(field);
        p.objectReferenceValue = value;
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

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        // 새 Input System용 입력 모듈로 바꾼다.
        StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
