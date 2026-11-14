using System.IO;
using DynamicWeatherSystem;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 게임의 날씨 상태 8가지를 Assets/Resources/Weather 에 만든다: 맑음, 흐림, 바람, 이슬비, 비, 폭풍우, 안개, 눈.
/// 에셋(URP Dynamic Weather System)에 들어 있는 날씨 상태와 소리를 바탕으로 하고, 우리 게임에 맞게 고친다.
/// 맑음(WS_Clear)이 기준이라, 하늘은 맑은 날에는 아무 것도 바뀌지 않는다. (GameWeatherModule 참고)
/// 숫자를 고쳐서 메뉴를 다시 누르면 같은 이름의 상태가 새 값으로 바뀐다.
/// 메뉴: 유리병 정원 > 날씨 상태 만들기
/// </summary>
public static class WeatherSetup
{
    private const string Folder = "Assets/Resources/Weather";
    private const string PresetFolder = "Assets/DynamicWeatherSystem/Presets/";
    private const string AudioFolder = "Assets/DynamicWeatherSystem/Audio/";

    [MenuItem("유리병 정원/날씨 상태 만들기")]
    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        AudioClip rainLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "low_rain_loop.wav");
        AudioClip windLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "wind_loop.wav");
        AudioClip stormLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "storm-wind-loop.wav");

        // 맑음: 기준. 하늘 색은 그대로(흰색)
        WeatherStateData clear = Make("Clear", "WS_Clear", s =>
        {
            s.stateName = "Clear";
            s.skyboxTint = Color.white;
            s.fogEnabled = false;
            s.windStrength = 0.05f; s.windDirection = 40f;
            s.ambientClip = null; s.ambientVolume = 0f;
        });

        // 흐림: 해가 조금 가려지고 하늘이 잿빛, 약한 바람
        Make("Cloudy", "WS_Clear", s =>
        {
            s.lightColor = new Color(0.85f, 0.88f, 0.95f); s.lightIntensity = 0.8f;
            s.ambientColor = new Color(0.2f, 0.22f, 0.28f);
            s.skyboxTint = new Color(0.72f, 0.75f, 0.8f); s.skyboxExposure = 0.85f;
            s.fogEnabled = true; s.fogColor = new Color(0.75f, 0.78f, 0.82f); s.fogDensity = 0.008f;
            s.windStrength = 0.3f; s.windDirection = 40f;
            s.ambientClip = windLoop; s.ambientVolume = 0.2f;
        });

        // 바람: 맑지만 바람이 세게 분다 (풀과 나무가 흔들리고 입자가 날린다)
        Make("Windy", "WS_Clear", s =>
        {
            s.lightIntensity = 1.15f; s.skyboxTint = new Color(0.95f, 0.97f, 1f); s.skyboxExposure = 1.25f;
            s.fogEnabled = false;
            s.windStrength = 0.85f; s.windDirection = 70f;
            s.ambientClip = windLoop; s.ambientVolume = 0.45f;
        });

        // 이슬비: 가늘고 조용한 비
        Make("Drizzle", "WS_Rain", s =>
        {
            s.lightIntensity = 0.85f; s.lightColor = new Color(0.82f, 0.86f, 0.93f);
            s.skyboxTint = new Color(0.78f, 0.82f, 0.88f); s.skyboxExposure = 0.9f;
            s.fogEnabled = true; s.fogColor = new Color(0.62f, 0.66f, 0.7f); s.fogDensity = 0.01f;
            s.rainIntensity = 0.25f; s.rainSpeed = 7f; s.rainColor = new Color(0.72f, 0.8f, 0.9f, 0.3f);
            s.windStrength = 0.1f; s.windDirection = 40f;
            s.ambientClip = rainLoop; s.ambientVolume = 0.3f;
        });

        // 비: 에셋의 비 그대로에 빗소리를 낮춘 것
        Make("Rain", "WS_Rain", s =>
        {
            s.windStrength = 0.15f; s.windDirection = 40f;
            s.ambientClip = rainLoop; s.ambientVolume = 0.5f;
        });

        // 폭풍우: 거센 비와 바람, 천둥번개
        Make("Storm", "WS_Storm", s =>
        {
            s.ambientClip = stormLoop; s.ambientVolume = 0.55f;
        });

        // 안개: 가까이까지 뿌옇다
        Make("Fog", "WS_Fog", s =>
        {
            s.windStrength = 0.05f;
            s.ambientClip = windLoop; s.ambientVolume = 0.18f;
        });

        // 눈: 포근하게 내리는 눈
        Make("Snow", "WS_Snow", s =>
        {
            s.ambientClip = windLoop; s.ambientVolume = 0.25f;
        });

        AssetDatabase.SaveAssets();
        return "날씨 상태 8가지를 만들었어요: " + Folder;
    }

    /// <summary>에셋의 날씨 상태(preset)를 복사해서 이름이 WS_(kind)인 새 상태를 만들고(이미 있으면 새 값으로 바꾸고) 고친다.</summary>
    private static WeatherStateData Make(string kind, string preset, System.Action<WeatherStateData> adjust)
    {
        WeatherStateData source = AssetDatabase.LoadAssetAtPath<WeatherStateData>(PresetFolder + preset + ".asset");
        string path = Folder + "/WS_" + kind + ".asset";
        WeatherStateData target = AssetDatabase.LoadAssetAtPath<WeatherStateData>(path);
        if (target == null)
        {
            target = ScriptableObject.CreateInstance<WeatherStateData>();
            AssetDatabase.CreateAsset(target, path);
        }
        if (source != null) EditorUtility.CopySerialized(source, target);
        target.name = "WS_" + kind;
        target.stateName = kind;
        adjust(target);
        EditorUtility.SetDirty(target);
        return target;
    }
}
