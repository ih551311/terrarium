using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코드로 화면(UI)을 만들 때 쓰는 작은 도우미. 에디터 설정 스크립트(UISetup) 없이 게임이 켜질 때 만드는 패널
/// (일시정지 메뉴, 편지 답장 화면)이 쓴다. 종이 같은 크림색 바탕에 짙은 갈색 글씨, 모래색 버튼으로 통일한다.
/// </summary>
public static class UIKit
{
    public static readonly Color Paper = new Color(0.97f, 0.94f, 0.87f, 0.98f);
    public static readonly Color Ink = new Color(0.27f, 0.20f, 0.16f, 1f);
    public static readonly Color ButtonColor = new Color(0.84f, 0.70f, 0.50f, 1f);
    public static readonly Color ButtonDisabled = new Color(0.78f, 0.75f, 0.70f, 1f);
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);

    private static TMP_FontAsset font;

    /// <summary>게임 글꼴 (카페24 동동). 못 찾으면 TMP 기본 글꼴.</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts/Cafe24DongdongLight SDF");
            return font;
        }
    }

    /// <summary>화면 전체를 덮는 Canvas를 만든다. 씬을 옮겨도 남는다.</summary>
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        UnityEngine.Object.DontDestroyOnLoad(go);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>화면 전체를 덮는 어두운 막. (뒤의 것을 클릭하지 못하게 막는다)</summary>
    public static RectTransform Backdrop(Transform parent)
    {
        RectTransform rect = Rect(parent, "Backdrop");
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Dim;
        return rect;
    }

    /// <summary>화면 가운데에 놓이는 종이 색 판.</summary>
    public static RectTransform Panel(Transform parent, string name, Vector2 size)
    {
        RectTransform rect = Rect(parent, name);
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Paper;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.40f, 0.28f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);
        return rect;
    }

    /// <summary>글자. pos는 부모 가운데에서의 위치, 부모 가운데가 (0,0)이다.</summary>
    public static TMP_Text Label(Transform parent, string text, float fontSize, Vector2 pos, Vector2 size,
                                 TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
    {
        RectTransform rect = Rect(parent, "Label");
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) label.font = Font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = align;
        label.color = color ?? Ink;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    /// <summary>버튼 (모래색 바탕, 가운데 글자).</summary>
    public static Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, Action onClick, float fontSize = 30f)
    {
        RectTransform rect = Rect(parent, "Button");
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
        colors.pressedColor = new Color(0.78f, 0.62f, 0.42f, 1f);
        colors.disabledColor = ButtonDisabled;
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(() => onClick());
        Label(rect, text, fontSize, Vector2.zero, size - new Vector2(16f, 8f));
        return button;
    }

    /// <summary>버튼 안의 글자를 바꾼다.</summary>
    public static void SetButtonText(Button button, string text)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }

    /// <summary>가로 슬라이더 (0~1).</summary>
    public static Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size, float value, Action<float> onChanged)
    {
        RectTransform rect = Rect(parent, "Slider");
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        RectTransform bg = Rect(rect, "Background");
        bg.anchorMin = new Vector2(0f, 0.5f); bg.anchorMax = new Vector2(1f, 0.5f);
        bg.sizeDelta = new Vector2(0f, 14f);
        bg.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.68f, 0.58f, 1f);

        RectTransform fillArea = Rect(rect, "Fill Area");
        fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(0f, -7f); fillArea.offsetMax = new Vector2(0f, 7f);
        RectTransform fill = Rect(fillArea, "Fill");
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
        fill.sizeDelta = new Vector2(10f, 0f);
        fill.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.72f, 0.45f, 1f);

        RectTransform handleArea = Rect(rect, "Handle Slide Area");
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(14f, 0f); handleArea.offsetMax = new Vector2(-14f, 0f);
        RectTransform handle = Rect(handleArea, "Handle");
        handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(28f, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(0.55f, 0.40f, 0.28f, 1f);

        Slider slider = rect.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.value = value;
        if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
        return slider;
    }

    /// <summary>가운데 기준(앵커 0.5, 0.5) RectTransform 하나를 만든다.</summary>
    public static RectTransform Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
}
