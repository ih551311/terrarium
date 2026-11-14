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

    [Header("안개 (맑은 날 기준)")]
    [SerializeField] private float fogStart = 260f, fogEnd = 1100f;
    [Tooltip("안개가 가장 짙을 때(안개 낀 날) 안개가 끝나는 거리")]
    [SerializeField] private float thickFogEnd = 140f;

    private Material skyInstance;
    private Material coastWater;                  // 해안 물 재질 복사본 (안개 낀 날 안개 색으로 물들인다)
    private Color coastDeep, coastShallow;        // 해안 물의 원래 깊은 색, 얕은 색

    private void Start()
    {
        // 해안 물(Bitgem 셰이더)은 안개를 받지 못해서, 안개가 짙은 날에는 색 자체를 안개 색 쪽으로 옮겨 먼 바다(안개를 받는 판)와 이어 준다
        GameObject coast = GameObject.Find("SeaCoast");
        if (coast != null && coast.TryGetComponent(out Renderer coastRenderer))
        {
            coastWater = coastRenderer.material;
            if (coastWater.HasProperty("Color_7D9A58EC")) coastDeep = coastWater.GetColor("Color_7D9A58EC");
            if (coastWater.HasProperty("Color_F01C36BF")) coastShallow = coastWater.GetColor("Color_F01C36BF");
        }

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
        if (coastWater != null) Destroy(coastWater);
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
        float dim = GameWeatherModule.Dim;                                          // 날씨가 해를 가린 정도 0 맑음 ~ 1 아주 어두움

        // 하늘: 낮 하늘은 아침과 노을에 따뜻한 색과 낮은 밝기, 밤 하늘은 섞이면서 밝아진다
        if (skyInstance != null)
        {
            Color neutral = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Color warmTint = new Color(0.72f, 0.45f, 0.3f, 0.5f);
            // 날씨가 하늘에 덧씌운다: 색을 곱하고(회색빛, 푸른 빛), 밝기를 줄인다. 맑은 날은 그대로다.
            Color weatherTint = GameWeatherModule.SkyTint; weatherTint.a = 1f;
            float gray = (weatherTint.r + weatherTint.g + weatherTint.b) / 3f;
            weatherTint = Color.Lerp(weatherTint, new Color(gray, gray, gray, 1f), dim * 0.7f); // 흐린 날은 하늘이 푸른 빛을 잃고 잿빛이 된다
            float weatherExposure = GameWeatherModule.SkyExposure;
            skyInstance.SetColor("_TintA", Color.Lerp(neutral, warmTint, warm) * weatherTint);
            skyInstance.SetFloat("_ExposureA", Mathf.Lerp(0.5f, 1f, height) * weatherExposure);
            skyInstance.SetColor("_TintB", neutral * weatherTint);
            skyInstance.SetFloat("_ExposureB", Mathf.Lerp(0.8f, 1f, night) * Mathf.Lerp(1f, weatherExposure, 0.7f));
            skyInstance.SetFloat("_Blend", night);

            // 흐린 날은 하늘을 잿빛 구름으로 덮는다. 낮에는 밝은 회색, 노을에는 따뜻한 빛이 돌고, 밤에는 어둡다. 폭풍우는 더 짙다.
            float cloudDay = Mathf.Max(0f, height) * (1f - night);
            float cloudBright = Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(0.35f, 0.9f, dim)) * Mathf.Lerp(0.08f, 1f, cloudDay);
            Color cloudWarm = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.72f), warm * 0.8f);
            skyInstance.SetFloat("_Overcast", Mathf.Clamp01(dim * 1.7f));
            skyInstance.SetColor("_CloudZenith", new Color(0.58f, 0.62f, 0.68f) * cloudWarm * cloudBright);
            skyInstance.SetColor("_CloudHorizon", new Color(0.78f, 0.8f, 0.82f) * cloudWarm * cloudBright);
        }

        // 해와 달빛
        if (sun != null)
        {
            float moon = night;
            float sunPower = sunIntensity * Mathf.Pow(height, 0.8f) * (1f - 0.85f * dim);
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
        float ambientDim = 1f - 0.5f * dim; // 흐린 날은 환경광도 조금 어둡다
        RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, day) * ambientDim;
        RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, day) * ambientDim;
        RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, day) * ambientDim;

        // 안개: 맑은 날은 먼 곳만 흐리고, 안개 낀 날과 비 오는 날은 가까이까지 뿌옇다 (날씨 상태의 안개 색을 낮밤 밝기에 맞춰 섞는다)
        Color clearFog = Color.Lerp(nightFog, Color.Lerp(dayFog, sunsetFog, warm), day);
        float fog = Mathf.Sqrt(GameWeatherModule.Fog);
        Color weatherFog = GameWeatherModule.FogColor * Mathf.Lerp(0.2f, 1f, day);
        RenderSettings.fogColor = Color.Lerp(clearFog, weatherFog, fog);
        RenderSettings.fogStartDistance = Mathf.Lerp(fogStart, 15f, fog);
        // 카메라가 볼 수 있는 끝(보통 1000m)보다 안쪽에서 안개가 다 차야, 먼 바다가 끝에서 뚝 끊기지 않고 안개 색으로 사라진다
        float visibleEnd = Mathf.Min(fogEnd, 900f);
        RenderSettings.fogEndDistance = Mathf.Lerp(visibleEnd, thickFogEnd, fog);

        // 해안 물: 안개 낀 날과 비 오는 날에는 안개 색으로 물든다 (안개가 짙을수록)
        if (coastWater != null)
        {
            float tintAmount = Mathf.Clamp01(fog * 0.9f);
            Color fogTone = RenderSettings.fogColor;
            if (coastWater.HasProperty("Color_7D9A58EC")) coastWater.SetColor("Color_7D9A58EC", Color.Lerp(coastDeep, fogTone, tintAmount));
            if (coastWater.HasProperty("Color_F01C36BF"))
            {
                Color shallow = Color.Lerp(coastShallow, fogTone, tintAmount * 0.8f);
                shallow.a = coastShallow.a;
                coastWater.SetColor("Color_F01C36BF", shallow);
            }
        }

        // 하늘의 지평선을 안개 색으로 맞춘다: 안개에 묻힌 먼 바다와 하늘이 같은 색으로 이어져서 지평선에 띠가 생기지 않는다
        if (skyInstance != null)
        {
            skyInstance.SetColor("_HorizonColor", RenderSettings.fogColor);
            skyInstance.SetFloat("_HorizonStrength", 1f);
            skyInstance.SetFloat("_HorizonWidth", Mathf.Lerp(0.2f, 0.32f, fog)); // 안개 낀 날은 더 높이까지 번진다
        }
    }
}
