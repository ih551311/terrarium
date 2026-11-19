using DynamicWeatherSystem;
using UnityEngine;

/// <summary>
/// 날씨의 모습과 소리를 에셋 "URP Dynamic Weather System"으로 보여 준다.
/// 게임이 시작되면 스스로 만들어지고, 플레이어 카메라가 생기면 날씨 시스템(WeatherManager와 모듈들)을 꾸려서
/// 오늘 날씨(Weather.Today)를 적용한다. 날씨가 바뀌면(아침이 되거나 F9) 몇 초에 걸쳐 부드럽게 바뀐다.
///
/// 쓰는 모듈: 비, 눈, 바람, 번개와 천둥, 날씨 소리, 그리고 우리 하늘에 값을 알려 주는 GameWeatherModule.
/// 쓰지 않는 모듈: 조명, 안개, 하늘. (낮과 밤은 SkyController가 맡아서 서로 싸우지 않게 했다)
/// 집 안에서는 비와 눈이 지붕을 뚫고 보이지 않게 숨기고, 날씨 소리는 먹먹하게 낮춘다.
/// 날씨 상태 파일은 Assets/Resources/Weather (메뉴: 유리병 정원 > 날씨 상태 만들기).
/// </summary>
public class WeatherEffects : MonoBehaviour
{
    /// <summary>비나 눈이 오는 정도 0~1. 음악과 새 소리를 줄일 때 GameAudio가 쓴다.</summary>
    public static float Amount => Mathf.Max(GameWeatherModule.Rain, GameWeatherModule.Snow * 0.6f);

    /// <summary>지금 바깥(섬)에 있는가. 집 안이면 false</summary>
    public static bool Outdoors { get; private set; }

    private WeatherManager manager;
    private WeatherKind? applied;
    private ParticleSystemRenderer[] particleRenderers = new ParticleSystemRenderer[0];
    private AudioLowPassFilter[] lowPass = new AudioLowPassFilter[0];
    private float muffle = 1f; // 1 바깥, 0 집 안
    private float announceAt = -1f;
    private RainGround ground;
    private bool sceneChanged; // 섬을 옮겼다: 날씨를 천천히 바꾸지 않고 바로 바꾼다 (눈 섬에서 사막 섬으로 가도 눈이 계속 내리지 않게)

    private void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { sceneChanged = true; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<WeatherEffects>() != null) return;
        GameObject go = new GameObject("WeatherEffects");
        DontDestroyOnLoad(go);
        go.AddComponent<WeatherEffects>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Outdoors = false;
    }

    private void Update()
    {
        Outdoors = Terrain.activeTerrain != null && !IndoorZone.Inside; // 집 안은 섬 씬 안의 방이다 (IndoorZone)

        // F9: 날씨를 하나씩 바꿔 본다 (맑음, 흐림, 바람, 이슬비, 비, 폭풍우, 안개, 눈, 날짜대로). 개발할 때 쓰는 키다.
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            if (Weather.Forced == null) Weather.Forced = WeatherKind.Clear;
            else if (Weather.Forced == WeatherKind.Snow) Weather.Forced = null;
            else Weather.Forced = (WeatherKind)((int)Weather.Forced.Value + 1);
            Say("날씨: " + (Weather.Forced == null ? "날짜대로" : Weather.NameOf(Weather.Forced.Value)) + " (F9)");
        }

        // 플레이어 카메라가 생기면 날씨 시스템을 꾸린다 (비와 눈이 카메라를 따라다닌다)
        if (manager == null)
        {
            if (Camera.main == null) return;
            BuildSystem();
            if (manager == null) return;
        }

        // 오늘 날씨가 바뀌었으면 적용한다. 처음은 바로, 그 뒤에는 몇 초에 걸쳐.
        WeatherKind today = Weather.Today;
        if (applied != today)
        {
            WeatherStateData state = Resources.Load<WeatherStateData>("Weather/WS_" + today);
            if (state != null)
            {
                bool instant = applied == null || sceneChanged;
                manager.SetWeather(state, instant ? 0f : 6f);
                if (sceneChanged && applied != null) ClearParticles(); // 앞 섬의 눈이나 비가 남아 떨어지지 않게
                // 날씨 알림 글자는 띄우지 않는다 (재료가 나오는 날씨는 로딩 화면의 재료 쪽지가 알려 준다)
            }
            applied = today;
        }
        sceneChanged = false;

        if (announceAt > 0f && Time.time >= announceAt && GameRoot.Instance != null && GameRoot.Instance.Player != null)
        {
            announceAt = -1f;
            Announce();
        }

        // 비 오는 날 땅의 물방울과 젖은 땅 (한 번만 만든다)
        if (ground == null)
        {
            GameObject g = new GameObject("RainGround");
            g.transform.SetParent(transform, false);
            ground = g.AddComponent<RainGround>();
        }

        UpdateIndoors();
    }

    /// <summary>집 안에서는 비와 눈 입자를 숨기고, 날씨 소리를 먹먹하게 낮춘다.</summary>
    private void UpdateIndoors()
    {
        foreach (ParticleSystemRenderer r in particleRenderers)
            if (r != null) r.enabled = Outdoors;

        muffle = Mathf.MoveTowards(muffle, Outdoors ? 1f : 0f, Time.deltaTime * 2f);
        float cutoff = Mathf.Lerp(500f, 22000f, muffle * muffle);
        foreach (AudioLowPassFilter f in lowPass)
            if (f != null) f.cutoffFrequency = cutoff;
    }

    private void ClearParticles()
    {
        foreach (ParticleSystemRenderer r in particleRenderers)
        {
            if (r == null) continue;
            ParticleSystem ps = r.GetComponent<ParticleSystem>();
            if (ps != null) ps.Clear(true);
        }
    }

    private void BuildSystem()
    {
        // 모듈은 Awake에서 한 번 쓰이므로, 꺼 둔 채로 다 만들고 마지막에 켠다
        GameObject root = new GameObject("WeatherSystem");
        root.SetActive(false);
        root.transform.SetParent(transform, false);
        manager = root.AddComponent<WeatherManager>();
        AddModule<GameWeatherModule>(root);
        // 빗줄기를 더 촘촘하고 가늘게 (기본값은 드문드문하고 긴 선이라 어색하다). 모듈이 켜질 때(Awake) 읽는 값이라 켜기 전에 맞춘다.
        RainModule rainModule = AddModule<RainModule>(root);
        SetField(rainModule, "maxEmissionRate", 1800);
        SetField(rainModule, "spawnRadius", 16f);
        SetField(rainModule, "spawnHeight", 11f);
        SnowModule snowModule = AddModule<SnowModule>(root);
        SetField(snowModule, "maxEmissionRate", 900);
        AddModule<WindModule>(root);
        AddModule<LightningModule>(root);
        AddModule<AudioModule>(root);
        root.SetActive(true);

        particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer pr in particleRenderers)
        {
            // 가까이 지나가는 빗방울이 화면을 가로지르는 굵은 막대로 보이지 않게 크기에 한계를 둔다
            pr.maxParticleSize = 0.012f;
            if (pr.renderMode == ParticleSystemRenderMode.Stretch)
            {
                pr.lengthScale = 0.8f;
                pr.velocityScale = 0.035f;
                ParticleSystem ps = pr.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.016f); // 빗방울을 가늘게
                }
            }
        }

        // 날씨 소리를 먹먹하게 만들 필터 (집 안용). 소리 재생 장치는 AudioModule이 Awake에서 만들었다.
        var filters = new System.Collections.Generic.List<AudioLowPassFilter>();
        foreach (AudioSource s in root.GetComponentsInChildren<AudioSource>(true))
        {
            AudioLowPassFilter f = s.gameObject.AddComponent<AudioLowPassFilter>();
            f.cutoffFrequency = 22000f;
            filters.Add(f);
        }
        lowPass = filters.ToArray();
    }

    private static T AddModule<T>(GameObject root) where T : WeatherModule
    {
        GameObject go = new GameObject(typeof(T).Name);
        go.transform.SetParent(root.transform, false);
        return go.AddComponent<T>();
    }

    /// <summary>모듈의 비공개 설정값(Inspector에 보이는 값)을 코드로 바꾼다.</summary>
    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
    }

    private void Announce()
    {
        switch (Weather.Today)
        {
            case WeatherKind.Cloudy: Say("오늘은 구름이 꼈어요"); break;
            case WeatherKind.Windy: Say("오늘은 바람이 불어요. 꽃 언덕에 깃털과 홀씨가 날려 왔대요"); break;
            case WeatherKind.Drizzle: Say("이슬비가 내려요. 숲에 버섯이 나요"); break;
            case WeatherKind.Rain: Say("비가 와요. 숲에 버섯이 나요"); break;
            case WeatherKind.Storm: Say("폭풍우가 몰아쳐요! 숲에 버섯이 나고, 바닷가에는 번개 맞은 유리가 있대요"); break;
            case WeatherKind.Fog: Say("안개가 짙어요. 꽃 언덕에 안개꽃이, 숲에 이슬 맺힌 거미줄이 있대요"); break;
            case WeatherKind.Snow: Say("눈이 내려요. 눈꽃이 피었을지도 몰라요"); break;
            default:
                if (GameState.WeatherYesterday == WeatherKind.Storm) Say("폭풍우가 지나갔어요. 바닷가에 바다 유리가 밀려왔을지도 몰라요");
                else if (Weather.RainbowDay) Say("비가 그치고 하늘이 갰어요. 바닷가에 무지개 조약돌이 있을지도 몰라요");
                else if (Weather.RainedYesterday) Say("어제 비가 와서 숲에 버섯이 났어요");
                break;
        }
    }

    private void Say(string text)
    {
        if (GameRoot.Instance == null || GameRoot.Instance.Player == null) return;
        Interactor who = GameRoot.Instance.Player.GetComponentInChildren<Interactor>();
        if (who != null) who.ShowMessage(text);
    }
}
