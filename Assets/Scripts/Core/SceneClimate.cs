using UnityEngine;

/// <summary>
/// 이 씬(섬)의 날씨를 늘 같게 붙잡아 둔다. 눈 섬은 언제나 눈이 내린다.
/// 씬이 열려 있는 동안 Weather.Forced를 정해 두고, 씬이 닫히면 되돌린다. (하늘, 안개, 눈, 음악이 모두 그 날씨를 따라간다)
/// </summary>
public class SceneClimate : MonoBehaviour
{
    [SerializeField] private WeatherKind weather = WeatherKind.Snow;

    private WeatherKind? previous;

    private void OnEnable()
    {
        previous = Weather.Forced;
        Weather.Forced = weather;
    }

    private void OnDisable()
    {
        // 다른 씬이 이미 자기 날씨를 정해 놓았으면 건드리지 않는다
        if (Weather.Forced == weather) Weather.Forced = previous;
    }
}
