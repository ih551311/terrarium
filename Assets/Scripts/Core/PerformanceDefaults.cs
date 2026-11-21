using UnityEngine;

/// <summary>
/// 게임이 켜질 때 화면 설정을 맞춘다.
///  - 수직 동기화 켜기: 꺼져 있으면 그래픽 카드가 쉬지 않고 최대 속도로 그려서 열이 나고 팬이 시끄러워진다.
///  - 그래픽 품질: 설정 화면에서 고른 값(PlayerPrefs quality_level)을 적용한다. 처음에는 가장 높은 품질.
/// 프로젝트의 품질 설정(QualitySettings)이 0이어도 여기서 켠다.
/// </summary>
public static class PerformanceDefaults
{
    public const string QualityKey = "quality_level";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        int last = QualitySettings.names.Length - 1;
        QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, last), 0, last), true);
        QualitySettings.vSyncCount = 1; // 모니터 주사율에 맞춘다 (60Hz면 60프레임)
        Application.targetFrameRate = -1; // 수직 동기화가 켜져 있으면 이 값은 쓰이지 않는다
    }

    /// <summary>품질을 한 단계 바꾼다 (맨 위에서 다시 맨 아래로). 바뀐 단계를 돌려준다.</summary>
    public static int CycleQuality()
    {
        int count = QualitySettings.names.Length;
        int next = (QualitySettings.GetQualityLevel() + 1) % count;
        QualitySettings.SetQualityLevel(next, true);
        QualitySettings.vSyncCount = 1;
        PlayerPrefs.SetInt(QualityKey, next);
        return next;
    }

    /// <summary>화면에 보일 이름 (가장 높은 단계는 "높음", 나머지는 "보통")</summary>
    public static string QualityLabel() => QualitySettings.GetQualityLevel() >= QualitySettings.names.Length - 1 ? "높음" : "보통";
}