using UnityEngine;

/// <summary>
/// 게임 전체에서 쓰는 글꼴을 한 곳에서 관리한다.
/// 글꼴을 바꾸려면 Assets/Resources/Fonts/ 의 파일을 바꾸고 아래 FontPath만 고치면 된다.
/// 글꼴: Terrarum Sans Bitmap (SIL OFL 1.1, 같은 폴더의 LICENSE 파일 참고)
/// </summary>
public static class GameFont
{
    private const string FontPath = "Fonts/TerrarumSansBitmap"; // 확장자(.otf)는 적지 않는다

    private static Font main;

    /// <summary>게임 기본 글꼴. 못 찾으면 null (유니티 기본 글꼴이 대신 쓰인다).</summary>
    public static Font Main
    {
        get
        {
            if (main == null)
            {
                main = Resources.Load<Font>(FontPath);
                if (main == null)
                    Debug.LogWarning("글꼴을 찾지 못했어요: Assets/Resources/" + FontPath + ".otf");
            }
            return main;
        }
    }
}
