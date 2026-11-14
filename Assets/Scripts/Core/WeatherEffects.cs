using UnityEngine;

/// <summary>
/// 비가 오는 날의 모습. 섬에서 플레이어 위에 빗방울을 내리고, 하늘을 어둡게 하는 정도(Amount)를 부드럽게 바꾼다.
/// 집 안에서는 빗방울을 보이지 않는다. (소리는 GameAudio가 낮게 들려 준다)
/// 아침이 되면 오늘 날씨를 알려 준다. 게임이 시작될 때 스스로 만들어지고 씬이 바뀌어도 남는다.
/// </summary>
public class WeatherEffects : MonoBehaviour
{
    /// <summary>비의 세기 0(맑음)~1(비). 하늘, 안개, 빗소리가 쓴다. 천천히 변한다.</summary>
    public static float Amount { get; private set; }

    /// <summary>지금 바깥(섬)에 있는가. 집 안이면 false</summary>
    public static bool Outdoors { get; private set; }

    private ParticleSystem rain;
    private Transform follow;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<WeatherEffects>() != null) return;
        GameObject go = new GameObject("WeatherEffects");
        DontDestroyOnLoad(go);
        go.AddComponent<WeatherEffects>();
    }

    private void Awake()
    {
        Amount = 0f;
        rain = CreateRain();
    }

    private void OnEnable() { GameState.DayStarted += OnDayStarted; }
    private void OnDisable() { GameState.DayStarted -= OnDayStarted; }

    private void OnDayStarted() { Announce(); }

    private void Update()
    {
        Outdoors = Terrain.activeTerrain != null;

        float target = Weather.RainToday ? 1f : 0f;
        Amount = Mathf.MoveTowards(Amount, target, Time.deltaTime * 0.2f); // 5초에 걸쳐 변한다

        if (follow == null && GameRoot.Instance != null) follow = GameRoot.Instance.Player;
        if (follow != null) rain.transform.position = follow.position + Vector3.up * 12f;

        var emission = rain.emission;
        emission.rateOverTime = Outdoors ? Amount * 1400f : 0f;
        if (Amount > 0.01f && Outdoors && !rain.isPlaying) rain.Play();

        // 게임을 시작하고 잠시 뒤에 오늘 날씨를 한 번 알려 준다
        if (!announced && follow != null && Time.timeSinceLevelLoad > 3f)
        {
            announced = true;
            Announce();
        }
    }

    private void Announce()
    {
        if (GameRoot.Instance == null) return;
        Interactor who = GameRoot.Instance.Player != null ? GameRoot.Instance.Player.GetComponentInChildren<Interactor>() : null;
        if (who == null) return;
        if (Weather.RainToday) who.ShowMessage("오늘은 비가 와요. 숲에 버섯이 나요");
        else if (Weather.RainedYesterday) who.ShowMessage("어제 비가 와서 숲에 버섯이 났어요");
    }

    /// <summary>빗방울: 플레이어 위 12m의 넓은 상자에서 아래로 떨어지는 가늘고 긴 점들</summary>
    private ParticleSystem CreateRain()
    {
        GameObject go = new GameObject("Rain");
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 상자의 앞(z)이 아래를 향한다

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1.3f;
        main.startSpeed = 14f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
        main.maxParticles = 3000;
        main.startColor = new Color(0.78f, 0.84f, 0.95f, 0.5f);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 26f, 0.2f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 4f;
        renderer.velocityScale = 0.015f;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) renderer.material = new Material(shader);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        return ps;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Amount = 0f;
        Outdoors = false;
    }
}
