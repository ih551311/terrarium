using UnityEngine;

/// <summary>
/// 게임이 켜질 때 화면 갱신 속도를 모니터에 맞춘다 (수직 동기화 켜기).
/// 꺼져 있으면 그래픽 카드가 쉬지 않고 최대 속도로 그려서 열이 나고 팬이 시끄러워진다.
/// 프로젝트의 품질 설정(QualitySettings)이 0이어도 여기서 켠다.
/// </summary>
public static class PerformanceDefaults
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 1; // 모니터 주사율에 맞춘다 (60Hz면 60프레임)
        Application.targetFrameRate = -1; // 수직 동기화가 켜져 있으면 이 값은 쓰이지 않는다
    }
}
