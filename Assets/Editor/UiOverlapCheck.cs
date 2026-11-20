using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 지금 화면에 보이는 UI에서 글자가 서로 겹치거나, 스크롤 창이 다른 글자와 버튼을 가리거나, 글자가 칸 밖으로 넘치는 곳을 찾는다.
/// Play 중에 화면을 열어 둔 채로 부른다. 메뉴: 유리병 정원 > UI 겹침 검사 (결과는 콘솔에도 찍힌다).
/// 검사 1) 같은 화면(Canvas) 안의 글자끼리 겹침  2) 스크롤 창(RectMask2D, Mask)과 그 안이 아닌 글자, 버튼의 겹침
///      3) 한 줄로 쓰는 글자가 자기 칸보다 넓게 삐져나옴  4) 글자가 화면 밖으로 나감
/// </summary>
public static class UiOverlapCheck
{
    [MenuItem("유리병 정원/UI 겹침 검사")]
    public static string Menu()
    {
        string r = Scan("현재 화면");
        Debug.Log(r);
        return r;
    }

    private static Rect WorldRect(RectTransform rt)
    {
        var c = new Vector3[4];
        rt.GetWorldCorners(c);
        return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
    }

    /// <summary>글자가 실제로 차지한 영역 (칸이 아니라 글자 자체)</summary>
    private static bool TextRect(TMP_Text t, out Rect rect)
    {
        rect = default;
        if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.05f) return false;
        t.ForceMeshUpdate();
        Bounds b = t.textBounds;
        if (b.size.x < 0.5f || b.size.y < 0.5f) return false;
        Vector3 a = t.transform.TransformPoint(b.min), c = t.transform.TransformPoint(b.max);
        rect = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        return true;
    }

    /// <summary>스크롤 창(RectMask2D, Mask) 안에 있으면 보이는 부분만 남긴다. 아예 안 보이면 false.</summary>
    private static bool ClipToMasks(Transform t, ref Rect r)
    {
        foreach (RectMask2D m in t.GetComponentsInParent<RectMask2D>())
        {
            Rect mr = WorldRect(m.rectTransform);
            float x0 = Mathf.Max(r.xMin, mr.xMin), y0 = Mathf.Max(r.yMin, mr.yMin), x1 = Mathf.Min(r.xMax, mr.xMax), y1 = Mathf.Min(r.yMax, mr.yMax);
            if (x1 - x0 < 1f || y1 - y0 < 1f) return false;
            r = Rect.MinMaxRect(x0, y0, x1, y1);
        }
        foreach (Mask m in t.GetComponentsInParent<Mask>())
        {
            Rect mr = WorldRect((RectTransform)m.transform);
            float x0 = Mathf.Max(r.xMin, mr.xMin), y0 = Mathf.Max(r.yMin, mr.yMin), x1 = Mathf.Min(r.xMax, mr.xMax), y1 = Mathf.Min(r.yMax, mr.yMax);
            if (x1 - x0 < 1f || y1 - y0 < 1f) return false;
            r = Rect.MinMaxRect(x0, y0, x1, y1);
        }
        return true;
    }

    private static long DrawOrder(Graphic g)
    {
        Canvas root = g.canvas != null ? g.canvas.rootCanvas : null;
        return (root != null ? root.sortingOrder : 0) * 1000000L + g.canvasRenderer.absoluteDepth;
    }

    /// <summary>이 글자 위에 거의 불투명한 그림(판, 배경)이 덮여 있어서 안 보이는가</summary>
    private static bool Occluded(TMP_Text t, Rect r, List<Image> opaque)
    {
        long order = DrawOrder(t);
        foreach (Image img in opaque)
        {
            if (DrawOrder(img) <= order) continue;
            if (t.transform.IsChildOf(img.transform)) continue;
            Rect ir = WorldRect(img.rectTransform);
            float a = OverlapArea(r, ir);
            if (a >= r.width * r.height * 0.9f) return true;
        }
        return false;
    }

    /// <summary>겹친 자리가, 아래쪽 글자 위에 그려진 불투명한 판에 완전히 덮여 있는가 (그러면 실제로는 안 보인다)</summary>
    private static bool CoveredAbove(TMP_Text a, TMP_Text b, Rect ra, Rect rb, List<Image> opaque)
    {
        TMP_Text lower = DrawOrder(a) <= DrawOrder(b) ? a : b;
        Rect o = Rect.MinMaxRect(Mathf.Max(ra.xMin, rb.xMin), Mathf.Max(ra.yMin, rb.yMin), Mathf.Min(ra.xMax, rb.xMax), Mathf.Min(ra.yMax, rb.yMax));
        float need = o.width * o.height * 0.9f;
        long order = DrawOrder(lower);
        foreach (Image img in opaque)
        {
            if (DrawOrder(img) <= order || lower.transform.IsChildOf(img.transform)) continue;
            if (OverlapArea(o, WorldRect(img.rectTransform)) >= need) return true;
        }
        return false;
    }

    private static float OverlapArea(Rect a, Rect b)
    {
        float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
        float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        return w > 0f && h > 0f ? w * h : 0f;
    }

    private static string Path(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (Transform p = t.parent; p != null && sb.Length < 70; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    private static string Short(TMP_Text t)
    {
        string s = t.text.Replace("\n", " ");
        if (s.Length > 18) s = s.Substring(0, 18) + "...";
        return "'" + s + "' (" + t.transform.parent.name + "/" + t.name + ")";
    }

    public static string Scan(string label)
    {
        var report = new StringBuilder();
        int issues = 0;

        // 거의 불투명한 판과 배경 (이 위에 덮이면 아래 글자는 안 보인다)
        var opaque = new List<Image>();
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (img.enabled && img.color.a * img.canvasRenderer.GetInheritedAlpha() >= 0.85f && img.GetComponent<TMP_Text>() == null) opaque.Add(img);

        // 보이는 글자
        var texts = new List<TMP_Text>();
        var rects = new List<Rect>();
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!TextRect(t, out Rect r)) continue;
            Canvas canvas = t.canvas;
            if (canvas == null || !canvas.isActiveAndEnabled) continue;
            CanvasGroup[] groups = t.GetComponentsInParent<CanvasGroup>();
            bool hidden = false;
            foreach (CanvasGroup g in groups) if (g.alpha < 0.05f) { hidden = true; break; }
            if (hidden) continue;
            if (!ClipToMasks(t.transform, ref r)) continue;
            if (Occluded(t, r, opaque)) continue;
            texts.Add(t); rects.Add(r);
        }

        // 1) 글자끼리 겹침
        for (int i = 0; i < texts.Count; i++)
        {
            for (int j = i + 1; j < texts.Count; j++)
            {
                if (texts[i].canvas.rootCanvas != texts[j].canvas.rootCanvas) continue;
                if (texts[i].transform.IsChildOf(texts[j].transform) || texts[j].transform.IsChildOf(texts[i].transform)) continue;
                float area = OverlapArea(rects[i], rects[j]);
                if (area > 0f && CoveredAbove(texts[i], texts[j], rects[i], rects[j], opaque)) continue;
                float smaller = Mathf.Min(rects[i].width * rects[i].height, rects[j].width * rects[j].height);
                if (area > 12f && area > smaller * 0.12f)
                {
                    issues++;
                    report.Append("  [글자 겹침] " + Short(texts[i]) + "  x  " + Short(texts[j]) + "  (" + Mathf.RoundToInt(area / smaller * 100f) + "%)\n");
                }
            }
        }

        // 2) 스크롤 창이 다른 글자와 버튼을 가린다
        var masks = new List<RectTransform>();
        foreach (RectMask2D m in Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) masks.Add(m.rectTransform);
        foreach (Mask m in Object.FindObjectsByType<Mask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) masks.Add((RectTransform)m.transform);
        foreach (RectTransform mask in masks)
        {
            Rect mr = WorldRect(mask);
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i].transform.IsChildOf(mask) || texts[i].canvas.rootCanvas != mask.GetComponentInParent<Canvas>().rootCanvas) continue;
                if (mask.IsChildOf(texts[i].transform)) continue;
                if (OverlapArea(mr, rects[i]) > 12f)
                {
                    issues++;
                    report.Append("  [스크롤 창이 글자를 가림] 창 " + mask.name + " 가 " + Short(texts[i]) + " 와 겹침\n");
                }
            }
            foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (b.transform.IsChildOf(mask) || mask.IsChildOf(b.transform)) continue;
                if (b.GetComponentInParent<Canvas>().rootCanvas != mask.GetComponentInParent<Canvas>().rootCanvas) continue;
                Rect br = WorldRect((RectTransform)b.transform);
                if (br.width < 1f || br.height < 1f) continue;
                if (OverlapArea(mr, br) > 12f)
                {
                    issues++;
                    report.Append("  [스크롤 창이 버튼을 가림] 창 " + mask.name + " 가 버튼 " + b.name + " 와 겹침\n");
                }
            }
        }

        // 3) 한 줄 글자가 칸보다 넓음, 4) 화면 밖
        foreach (TMP_Text t in texts)
        {
            RectTransform rt = t.rectTransform;
            if (t.textWrappingMode == TextWrappingModes.NoWrap && !t.enableAutoSizing && t.overflowMode == TextOverflowModes.Overflow && t.preferredWidth > rt.rect.width + 4f)
            {
                issues++;
                report.Append("  [글자가 칸보다 넓음] " + Short(t) + "  글자 " + Mathf.RoundToInt(t.preferredWidth) + " > 칸 " + Mathf.RoundToInt(rt.rect.width) + "\n");
            }
        }
        for (int i = 0; i < texts.Count; i++)
        {
            Canvas root = texts[i].canvas.rootCanvas;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            Rect screen = new Rect(0, 0, Screen.width, Screen.height);
            Rect r = rects[i];
            if (r.xMin < -2f || r.yMin < -2f || r.xMax > screen.width + 2f || r.yMax > screen.height + 2f)
            {
                issues++;
                report.Append("  [글자가 화면 밖] " + Short(texts[i]) + "\n");
            }
        }

        return "[" + label + "] 글자 " + texts.Count + "개 검사, 문제 " + issues + "개\n" + report;
    }
}
