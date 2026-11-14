using UnityEngine;

/// <summary>
/// 섬의 하늘, 해(달), 환경광, 안개를 하루의 시간(GameState.TimeOfDay)에 맞춰 바꾼다. 섬 씬에 하나 둔다.
///
/// 하루는 약 10분이다: 아침 → 낮 → 노을 → 밤. 밤이 되면 시간이 멈추고, 침대에서 자야 아침이 온다. (기획서 6.4)
///   시간 0.00~0.12  아침 : 낮 하늘에 따뜻한 색과 낮은 밝기에서 시작해 점점 밝아진다
///   시간 0.12~0.55  낮
///   시간 0.55~0.72  노을 : 낮 하늘이 따뜻한 주황빛으로 어두워진다
///   시간 0.72~0.86  밤으로 : 낮 하늘이 밤 하늘로 섞인다
///   시간 0.86~1.00  밤   : 달빛, 시간은 여기서 멈춘다
/// 하늘은 낮과 밤 두 장(AllSky 팩의 Cartoon Base)을 섞어서 쓴다. 값은 아래 Inspector에서 고칠 수 있다.
/// </summary>
public class SkyController : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("해와 달빛을 맡는 방향 조명")]
    [SerializeField] private Light sun;
    [Tooltip("낮 하늘과 밤 하늘을 섞는 하늘 재질 (유리병 정원/Sky Blend)")]
    [SerializeField] private Material skyMaterial;

    [Header("해와 달")]
    [SerializeField] private float sunIntensity = 2f;
    [SerializeField] private float moonIntensity = 0.6f;
    [SerializeField] private Color sunColor = new Color(1f, 0.97f, 0.9f);
    [SerializeField] private Color sunsetColor = new Color(1f, 0.62f, 0.38f);
    [SerializeField] private Color moonColor = new Color(0.55f, 0.65f, 1f);

    [Header("환경광 (하늘, 지평선, 땅)")]
    [SerializeField] private Color daySky = new Color(0.55f, 0.7f, 0.95f);
    [SerializeField] private Color dayEquator = new Color(0.62f, 0.65f, 0.62f);
    [SerializeField] private Color dayGround = new Color(0.35f, 0.32f, 0.26f);
    [SerializeField] private Color nightSky = new Color(0.16f, 0.2f, 0.38f);
    [SerializeField] private Color nightEquator = new Color(0.13f, 0.16f, 0.27f);
    [SerializeField] private Color nightGround = new Color(0.04f, 0.04f, 0.07f);

    [Header("안개")]
    [SerializeField] private Color dayFog = new Color(0.78f, 0.87f, 0.92f);
    [SerializeField] private Color sunsetFog = new Color(0.92f, 0.7f, 0.55f);
    [SerializeField] private Color nightFog = new Color(0.06f, 0.08f, 0.17f);

    [Header("비 오는 날")]
    [Tooltip("비가 올 때 해의 밝기가 줄어드는 정도 (1이면 완전히 꺼진다)")]
    [SerializeField] private float rainSunDim = 0.6f;
    [SerializeField] private Color rainFog = new Color(0.52f, 0.57f, 0.62f);
    [SerializeField] private float fogStart = 260f, fogEnd = 1100f;
    [Tooltip("비가 올 때 안개가 끝나는 거리 (가까워져서 먼 곳이 뿌옇다)")]
    [SerializeField] private float rainFogEnd = 380f;

    private Material skyInstance;

    private void Start()
    {
        // 재질 파일 자체를 바꾸지 않도록 복사본을 쓴다
        skyInstance = new Material(skyMaterial);
        RenderSettings.skybox = skyInstance;
        RenderSettings.ambientMode = (UnityEngine.Rendering.AmbientMode)1; // 그라디언트(하늘, 지평선, 땅 세 색) 환경광
        RenderSettings.fog = true;
        Apply(GameState.TimeOfDay);
    }

    private void OnDestroy()
    {
        if (skyInstance != null) Destroy(skyInstance);
    }

    private void Update()
    {
        Apply(GameState.TimeOfDay);
    }

    /// <summary>시간(0~1)에 맞춰 하늘, 해, 환경광, 안개를 정한다.</summary>
    public void Apply(float t)
    {
        // 해의 높이(0 밤 ~ 1 한낮)와 따뜻한 정도(0 없음 ~ 1 노을색)
        float height, warm;
        if (t < 0.12f) { float u = t / 0.12f; height = Mathf.Lerp(0.45f, 1f, u); warm = 1f - u; }
        else if (t < 0.55f) { height = 1f; warm = 0f; }
        else if (t < 0.72f) { float u = (t - 0.55f) / 0.17f; height = Mathf.Lerp(1f, 0.35f, u); warm = u; }
        else if (t < 0.88f) { float u = (t - 0.72f) / 0.16f; height = Mathf.Lerp(0.35f, 0f, u); warm = 1f - u; }
        else { height = 0f; warm = 0f; }
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.86f, t)); // 0 낮 하늘 ~ 1 밤 하늘 (Pickup.NightStart 0.78은 이 한가운데쯤)
        float rain = WeatherEffects.Amount;                                         // 0 맑음 ~ 1 비

        // 하늘: 낮 하늘은 아침과 노을에 따뜻한 색과 낮은 밝기, 밤 하늘은 섞이면서 밝아진다
        if (skyInstance != null)
        {
            Color neutral = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Color warmTint = new Color(0.72f, 0.45f, 0.3f, 0.5f);
            Color grayTint = new Color(0.42f, 0.44f, 0.47f, 0.5f);
            skyInstance.SetColor("_TintA", Color.Lerp(Color.Lerp(neutral, warmTint, warm), grayTint, rain)); // 비 오는 날은 회색빛
            skyInstance.SetFloat("_ExposureA", Mathf.Lerp(0.5f, 1f, height) * (1f - 0.35f * rain));
            skyInstance.SetColor("_TintB", Color.Lerp(neutral, grayTint, rain * 0.8f));
            skyInstance.SetFloat("_ExposureB", Mathf.Lerp(0.8f, 1f, night) * (1f - 0.4f * rain));
            skyInstance.SetFloat("_Blend", night);
        }

        // 해와 달빛
        if (sun != null)
        {
            float moon = night;
            float sunPower = sunIntensity * Mathf.Pow(height, 0.8f) * (1f - rainSunDim * rain);
            float moonPower = moonIntensity * moon;
            Color daylight = Color.Lerp(sunColor, sunsetColor, warm);
            sun.color = Color.Lerp(daylight, moonColor, moon);
            sun.intensity = Mathf.Max(Mathf.Lerp(sunPower, 0f, moon), moonPower);
            float elevation = Mathf.Lerp(8f, 62f, height);
            elevation = Mathf.Lerp(elevation, 48f, moon); // 달은 높이 떠 있다
            sun.transform.rotation = Quaternion.Euler(elevation, 330f, 0f);
        }

        // 환경광과 안개: 낮의 밝기(해의 높이)와 밤을 섞는다
        float day = Mathf.Max(0f, height) * (1f - night);
        RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, day);
        RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, day);
        RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, day);
        Color clearFog = Color.Lerp(nightFog, Color.Lerp(dayFog, sunsetFog, warm), day);
        RenderSettings.fogColor = Color.Lerp(clearFog, rainFog * Mathf.Lerp(0.25f, 1f, day), rain);
        RenderSettings.fogStartDistance = Mathf.Lerp(fogStart, 40f, rain);
        RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd, rainFogEnd, rain);
    }
}
