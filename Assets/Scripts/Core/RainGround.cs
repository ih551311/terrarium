using UnityEngine;

/// <summary>
/// 비 오는 날 땅의 모습: 플레이어 둘레 땅과 바다에 빗방울이 튀는 작은 물결 고리와, 비가 오래 오면 젖어서 살짝 윤이 나는 땅.
/// 심하지 않게, 가까이(플레이어 둘레 15m 안)에서만 드문드문 튀고, 땅이 젖는 것도 은은하다.
/// WeatherEffects가 만들어 준다. 비의 세기는 GameWeatherModule.Rain을 따른다.
/// </summary>
public class RainGround : MonoBehaviour
{
    [Tooltip("비가 가장 셀 때 1초에 튀는 물방울 수 (플레이어 둘레 한정)")]
    [SerializeField] private float maxSplashesPerSecond = 110f;

    [Tooltip("물방울이 튀는 범위(미터). 카메라 바로 앞은 비운다")]
    [SerializeField] private float radius = 12f;

    [Tooltip("땅이 가장 젖었을 때의 윤기(0이면 윤기 없음). 은은하게 둔다")]
    [SerializeField, Range(0f, 1f)] private float wetSmoothness = 0.42f;

    private ParticleSystem splash;
    private Material splashMaterial;
    private MaterialPropertyBlock block;
    private float wet;
    private float splashCarry;

    private void Awake()
    {
        splash = CreateSplash();
        block = new MaterialPropertyBlock();
    }

    private void OnDestroy()
    {
        if (splashMaterial != null) Destroy(splashMaterial);
        ApplyWet(0f);
    }

    private void Update()
    {
        float rain = GameWeatherModule.Rain;
        Transform player = GameRoot.Instance != null ? GameRoot.Instance.Player : null;
        Terrain terrain = Terrain.activeTerrain;

        // 빗방울이 땅에 튄다
        if (WeatherEffects.Outdoors && terrain != null && player != null && rain > 0.03f)
        {
            splashCarry += rain * maxSplashesPerSecond * Time.deltaTime;
            int count = Mathf.FloorToInt(splashCarry);
            splashCarry -= count;
            for (int i = 0; i < count; i++) EmitSplash(terrain, player.position);
        }

        // 땅이 젖는다: 비가 오면 천천히 젖고(약 15초), 그친 뒤에는 더 천천히 마른다(약 50초)
        bool raining = rain > 0.05f && WeatherEffects.Outdoors;
        float before = wet;
        wet = Mathf.MoveTowards(wet, raining ? Mathf.Clamp01(rain * 1.6f) : 0f, Time.deltaTime * (raining ? 0.07f : 0.02f));
        if (!Mathf.Approximately(before, wet)) ApplyWet(wet);
    }

    private void EmitSplash(Terrain terrain, Vector3 center)
    {
        float angle = Random.value * Mathf.PI * 2f;
        float distance = 1.8f + Mathf.Sqrt(Random.value) * (radius - 1.8f);
        float x = center.x + Mathf.Cos(angle) * distance;
        float z = center.z + Mathf.Sin(angle) * distance;
        float ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        float y = Mathf.Max(ground, 0f) + 0.03f; // 바다 위에서는 바다 높이(0)에서 물결이 퍼진다

        // 땅에 닿은 빗방울이 작은 물방울 두세 개로 톡 튀어 오른다 (옆에서 봐도 보이는 모습)
        int drops = Random.Range(2, 4);
        for (int i = 0; i < drops; i++)
        {
            Vector2 side = Random.insideUnitCircle * 0.7f;
            var emit = new ParticleSystem.EmitParams
            {
                position = new Vector3(x, y, z),
                velocity = new Vector3(side.x, Random.Range(1.1f, 2.0f), side.y),
                startSize = Random.Range(0.05f, 0.09f),
                startLifetime = Random.Range(0.22f, 0.34f),
                startColor = new Color(0.92f, 0.96f, 1f, Random.Range(0.45f, 0.75f))
            };
            splash.Emit(emit, 1);
        }
    }

    /// <summary>땅(Terrain)을 젖은 듯 살짝 윤 나게 한다. wet 0이면 원래대로.</summary>
    private void ApplyWet(float amount)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null || block == null) return;

        if (amount <= 0.001f)
        {
            terrain.SetSplatMaterialPropertyBlock(null);
            return;
        }
        float smooth = wetSmoothness * amount;
        for (int i = 0; i < 4; i++) block.SetFloat("_Smoothness" + i, smooth);
        terrain.SetSplatMaterialPropertyBlock(block);
    }

    /// <summary>물방울이 튀는 모습: 땅에서 톡 튀어 올랐다가 중력에 떨어지는 아주 작은 물방울들 (가늘게 늘어난 점)</summary>
    private ParticleSystem CreateSplash()
    {
        GameObject go = new GameObject("RainSplash");
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;       // 속도는 Emit할 때 하나씩 준다
        main.startLifetime = 0.3f;
        main.startSize = 0.07f;
        main.gravityModifier = 1.6f;
        main.maxParticles = 600;

        var emission = ps.emission; emission.enabled = false; // 코드로 하나씩 낸다 (Emit)
        var shape = ps.shape; shape.enabled = false;

        // 떨어지면서 사라진다
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // 움직이는 방향으로 가늘게 늘어난다
        renderer.lengthScale = 1.6f;
        renderer.velocityScale = 0.06f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            splashMaterial = new Material(shader);
            renderer.material = splashMaterial;
        }

        ps.Play();
        return ps;
    }
}