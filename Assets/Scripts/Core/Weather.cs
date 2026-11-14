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
/// 날씨. 날마다 정해져 있고(날짜로 계산하므로 저장할 것이 없다), 첫날은 항상 맑다.
/// 비가 오는 날(이슬비, 비, 폭풍우)과 그다음 날은 숲에 버섯이 난다. (MaterialCondition.AfterRain 재료가 나타난다)
/// 날씨의 모습(비, 눈, 바람, 번개와 소리)은 에셋 "URP Dynamic Weather System"이 그리고, WeatherEffects가 연결한다.
/// </summary>
public static class Weather
{
    /// <summary>개발할 때 날씨를 고정해 보는 값(F9). null이면 날짜대로.</summary>
    public static WeatherKind? Forced;

    // 날씨마다 나올 가능성 (합계 100)
    private static readonly int[] Weights = { 34, 14, 8, 10, 14, 5, 9, 6 };

    /// <summary>이 날(1부터)의 날씨</summary>
    public static WeatherKind KindOf(int day)
    {
        if (day <= 1) return WeatherKind.Clear;
        var random = new System.Random(day * 7919 + 17);
        int pick = random.Next(100), sum = 0;
        for (int i = 0; i < Weights.Length; i++)
        {
            sum += Weights[i];
            if (pick < sum) return (WeatherKind)i;
        }
        return WeatherKind.Clear;
    }

    public static WeatherKind Today => Forced ?? KindOf(GameState.Day);

    /// <summary>비가 오는 날씨인가 (이슬비, 비, 폭풍우)</summary>
    public static bool IsWet(WeatherKind kind)
    {
        return kind == WeatherKind.Drizzle || kind == WeatherKind.Rain || kind == WeatherKind.Storm;
    }

    public static bool RainToday => IsWet(Today);

    /// <summary>어제 비가 왔는가 (오늘 맑아도 땅이 촉촉하다)</summary>
    public static bool RainedYesterday => !Forced.HasValue && IsWet(KindOf(GameState.Day - 1));

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
