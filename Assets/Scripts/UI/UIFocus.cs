using UnityEngine;

/// <summary>
/// 화면 위에 패널(가방, 병 꾸미기 등)이 열려 있는지 알려 주는 공통 규칙.
/// 패널이 하나라도 열려 있으면 커서를 풀고, 걷기와 줍기는 멈춘다.
/// 패널을 열 때 Open(), 닫을 때 Close()를 한 번씩 부른다.
/// </summary>
public static class UIFocus
{
    private static int openCount;

    /// <summary>패널이 하나라도 열려 있으면 true</summary>
    public static bool IsOpen => openCount > 0;

    /// <summary>패널이 마지막으로 닫힌 프레임 (같은 프레임에 Esc로 패널이 닫혔다면 일시정지 메뉴를 열지 않으려고 쓴다)</summary>
    public static int LastCloseFrame { get; private set; } = -1;

    public static void Open()
    {
        openCount++;
        if (openCount == 1)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public static void Close()
    {
        if (openCount == 0) return;
        openCount--;
        LastCloseFrame = Time.frameCount;
        if (openCount == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Play를 껐다 켤 때 값이 남아 있지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        openCount = 0;
    }
}
