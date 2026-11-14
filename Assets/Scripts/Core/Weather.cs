/// <summary>
/// 날씨. 날마다 정해져 있고(날짜로 계산하므로 저장할 것이 없다), 비가 오는 날은 대략 세 날에 하루다.
/// 비가 온 날과 그다음 날은 숲에 버섯이 난다. (MaterialCondition.AfterRain 재료가 나타난다)
/// 첫날은 항상 맑다.
/// </summary>
public static class Weather
{
    /// <summary>개발할 때 날씨를 고정해 보는 값. null이면 날짜대로. (true 비, false 맑음)</summary>
    public static bool? Forced;

    /// <summary>이 날(1부터)에 비가 오는가</summary>
    public static bool IsRain(int day)
    {
        if (day <= 1) return false;
        var random = new System.Random(day * 7919 + 17);
        return random.NextDouble() < 0.33;
    }

    public static bool RainToday => Forced ?? IsRain(GameState.Day);

    /// <summary>어제 비가 왔는가 (오늘 맑아도 땅이 촉촉하다)</summary>
    public static bool RainedYesterday => Forced.HasValue ? false : IsRain(GameState.Day - 1);

    /// <summary>숲에 버섯이 나는 날: 비가 오는 날과 그다음 날</summary>
    public static bool MushroomDay => RainToday || RainedYesterday;
}
