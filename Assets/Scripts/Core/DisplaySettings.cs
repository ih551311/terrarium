using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 모드와 해상도. 메인 화면 설정과 일시정지 메뉴가 같이 쓴다.
///  - 전체 화면: 테두리 없는 전체 화면(모니터 해상도 그대로). 이때 해상도는 모니터에 맞춰지므로 고를 수 없다.
///  - 창 모드: 모니터가 지원하는 해상도 중에서 고른다. 창 모드로 바꿀 때는 모니터보다 작은 가장 큰 해상도로 연다.
/// 고른 값은 유니티가 알아서 기억한다.
/// </summary>
public static class DisplaySettings
{
    private const int MinWidth = 1024, MinHeight = 576;
    private static List<Vector2Int> cache;

    public static bool FullScreen => Screen.fullScreen;

    /// <summary>창 모드에서 고를 수 있는 해상도 (작은 것부터)</summary>
    public static List<Vector2Int> Resolutions()
    {
        if (cache != null) return cache;
        var set = new HashSet<Vector2Int>();
        Vector2Int monitor = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        foreach (Resolution r in Screen.resolutions)
        {
            if (r.width < MinWidth || r.height < MinHeight) continue;
            if (r.width > monitor.x || r.height > monitor.y) continue;
            set.Add(new Vector2Int(r.width, r.height));
        }
        if (set.Count == 0) set.Add(new Vector2Int(1280, 720));
        cache = new List<Vector2Int>(set);
        cache.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return cache;
    }

    /// <summary>창 모드에서 지금 해상도와 가장 가까운 목록 번호</summary>
    private static int NearestIndex()
    {
        List<Vector2Int> list = Resolutions();
        int best = 0; long bestDist = long.MaxValue;
        for (int i = 0; i < list.Count; i++)
        {
            long dx = list[i].x - Screen.width, dy = list[i].y - Screen.height;
            long d = dx * dx + dy * dy;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    public static void ToggleFullScreen()
    {
        if (Screen.fullScreen)
        {
            // 창 모드로: 모니터 높이의 90% 안에 드는 가장 큰 해상도
            List<Vector2Int> list = Resolutions();
            Vector2Int pick = list[0];
            int limit = Mathf.RoundToInt(Screen.currentResolution.height * 0.9f);
            foreach (Vector2Int r in list) if (r.y <= limit) pick = r;
            Screen.SetResolution(pick.x, pick.y, FullScreenMode.Windowed);
        }
        else
        {
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
    }

    /// <summary>창 모드에서 해상도를 한 단계 키운다 (맨 위에서 맨 아래로 돌아간다).</summary>
    public static void CycleResolution()
    {
        if (Screen.fullScreen) return;
        List<Vector2Int> list = Resolutions();
        Vector2Int next = list[(NearestIndex() + 1) % list.Count];
        Screen.SetResolution(next.x, next.y, FullScreenMode.Windowed);
    }

    public static string ScreenLabel() => Screen.fullScreen ? "전체 화면" : "창 모드";

    public static string ResolutionLabel() => Screen.fullScreen ? "화면에 맞춤" : Screen.width + " x " + Screen.height;
}