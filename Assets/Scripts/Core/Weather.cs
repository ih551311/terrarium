/// <summary>날씨의 종류. 아래 순서대로 Resources/Weather 에 같은 이름의 날씨 상태(WS_...)가 있다.</summary>
public enum WeatherKind
{
    Clear,   // 맑음
    Cloudy,  // 흐림
    Windy,   // 바람
    Drizzle, // 이슬비
    Rain,    // 비
    Storm,   // 폭풍우 (천둥번개)
    Fog,     // 안개
    Snow     // 눈
}

/// <summary>
/// 날씨. 아침마다 무작위로 뽑는다. (첫날은 맑다) 같은 날씨가 사흘 연달아 나오지는 않는다.
/// 오늘과 어제의 날씨는 GameState가 기억하고 저장하므로, 껐다 켜도 그날의 날씨가 그대로다.
/// 비가 오는 날(이슬비, 비, 폭풍우)과 그다음 날은 숲에 버섯이 난다. (MaterialCondition.AfterRain 재료가 나타난다)
/// 날씨의 모습(비, 눈, 바람, 번개와 소리)은 에셋 "URP Dynamic Weather System"이 그리고, WeatherEffects가 연결한다.
/// </summary>
public static class Weather
{
    /// <summary>개발할 때 날씨를 고정해 보는 값(F9). null이면 정해진 오늘 날씨.</summary>
    public static WeatherKind? Forced;

    // 날씨마다 나올 가능성 (합계 100)
    private static readonly int[] Weights = { 30, 14, 8, 11, 15, 6, 9, 7 };

    /// <summary>새 날씨를 무작위로 뽑는다. yesterday, today가 같은 날씨였으면 그 날씨는 다시 뽑지 않는다(사흘 연속 방지).</summary>
    public static WeatherKind Roll(WeatherKind yesterday, WeatherKind today)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int pick = UnityEngine.Random.Range(0, 100), sum = 0;
            WeatherKind kind = WeatherKind.Clear;
            for (int i = 0; i < Weights.Length; i++)
            {
                sum += Weights[i];
                if (pick < sum) { kind = (WeatherKind)i; break; }
            }
            if (yesterday == today && kind == today) continue;
            return kind;
        }
        return WeatherKind.Clear;
    }

    public static WeatherKind Today => Forced ?? GameState.WeatherToday;

    /// <summary>비가 오는 날씨인가 (이슬비, 비, 폭풍우)</summary>
    public static bool IsWet(WeatherKind kind)
    {
        return kind == WeatherKind.Drizzle || kind == WeatherKind.Rain || kind == WeatherKind.Storm;
    }

    public static bool RainToday => IsWet(Today);

    /// <summary>어제 비가 왔는가 (오늘 맑아도 땅이 촉촉하다)</summary>
    public static bool RainedYesterday => !Forced.HasValue && IsWet(GameState.WeatherYesterday);

    /// <summary>숲에 버섯이 나는 날: 비가 오는 날과 그다음 날</summary>
    public static bool MushroomDay => RainToday || RainedYesterday;

    public static string NameOf(WeatherKind kind)
    {
        switch (kind)
        {
            case WeatherKind.Clear: return "맑음";
            case WeatherKind.Cloudy: return "흐림";
            case WeatherKind.Windy: return "바람이 불어요";
            case WeatherKind.Drizzle: return "이슬비";
            case WeatherKind.Rain: return "비";
            case WeatherKind.Storm: return "폭풍우";
            case WeatherKind.Fog: return "안개";
            default: return "눈";
        }
    }
}
