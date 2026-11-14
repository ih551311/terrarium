using DynamicWeatherSystem;
using UnityEngine;

/// <summary>
/// 에셋(Dynamic Weather System)의 날씨 모듈 하나. 날씨 상태가 바뀌는 동안 섞인 값을 읽어서,
/// 우리 하늘(SkyController)이 쓸 수 있게 정적 값으로 내놓는다.
/// 에셋의 조명, 안개, 하늘 모듈은 쓰지 않는다. 낮과 밤을 우리 SkyController가 맡고 있어서 서로 싸우기 때문이다.
/// 그 대신 이 모듈이 "얼마나 어두운가, 하늘이 얼마나 흐린가, 안개가 얼마나 짙은가"만 알려 주고, 하늘이 낮밤 위에 덧씌운다.
/// 맑은 날(WS_Clear)의 값이 기준이라 맑은 날은 아무 것도 바뀌지 않는다.
/// </summary>
public class GameWeatherModule : WeatherModule
{
    // 기준(맑은 날)의 값. 날씨 상태의 값을 이 값에 견준다.
    private const float ClearLight = 1.3f;
    private const float ClearSky = 1.3f;

    /// <summary>해가 얼마나 가려졌는가 0(맑음)~1(아주 어두움)</summary>
    public static float Dim { get; private set; }

    /// <summary>하늘 밝기 배율 (맑음 1, 폭풍우는 아주 작다)</summary>
    public static float SkyExposure { get; private set; } = 1f;

    /// <summary>하늘에 곱하는 색 (맑음은 흰색)</summary>
    public static Color SkyTint { get; private set; } = Color.white;

    /// <summary>안개의 짙은 정도 0~1</summary>
    public static float Fog { get; private set; }

    public static Color FogColor { get; private set; } = new Color(0.6f, 0.62f, 0.66f);

    /// <summary>비의 세기 0~1 (이슬비는 작고 폭풍우는 크다)</summary>
    public static float Rain { get; private set; }

    /// <summary>눈의 세기 0~1</summary>
    public static float Snow { get; private set; }

    /// <summary>바람의 세기 0~1</summary>
    public static float Wind { get; private set; }

    public override void Blend(WeatherStateData from, WeatherStateData to, float t)
    {
        float light = Mathf.Lerp(from.lightIntensity, to.lightIntensity, t);
        Dim = Mathf.Clamp01(1f - light / ClearLight);
        SkyExposure = Mathf.Clamp(Mathf.Lerp(from.skyboxExposure, to.skyboxExposure, t) / ClearSky, 0.15f, 1.2f);
        SkyTint = Color.Lerp(from.skyboxTint, to.skyboxTint, t);

        float f0 = from.fogEnabled ? from.fogDensity : 0f, f1 = to.fogEnabled ? to.fogDensity : 0f;
        Fog = Mathf.Clamp01(Mathf.Lerp(f0, f1, t) / 0.04f);
        FogColor = Color.Lerp(from.fogColor, to.fogColor, t);

        Rain = Mathf.Lerp(from.rainIntensity, to.rainIntensity, t);
        Snow = Mathf.Lerp(from.snowIntensity, to.snowIntensity, t);
        Wind = Mathf.Lerp(from.windStrength, to.windStrength, t);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Dim = 0f; SkyExposure = 1f; SkyTint = Color.white; Fog = 0f; Rain = 0f; Snow = 0f; Wind = 0f;
    }
}
