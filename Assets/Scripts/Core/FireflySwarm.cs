using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 섬의 밤에 날아다니는 반딧불이. 밤이 되면(맑거나 흐리거나 안개 낀 날) 집 둘레, 숲, 꽃 언덕 위로 노란 초록빛이 하나둘 떠올라
/// 천천히 떠다닌다. 가까이 가서 바라보고 E를 누르면 잡아서 재료 "반딧불이"를 얻는다. (병 속에서도 밤에 반짝인다)
/// 잡은 자리는 그날 밤 비어 있고, 다음 날 아침이 지나면 다시 나타난다. 비, 폭풍우, 눈이 오는 날에는 나오지 않는다.
/// 지형(Terrain)이 있는 섬 씬에서만 게임이 켜질 때 코드로 만들어진다. 에디터에서 따로 설정할 것은 없다.
/// </summary>
public class FireflySwarm : MonoBehaviour
{
    private const int Count = 36;
    private const string MaterialId = "firefly";

    private static Sprite glowSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Spawn();
    }

    private static void Spawn()
    {
        if (Terrain.activeTerrain == null) return;           // 집 안에는 반딧불이가 없다
        if (FindFirstObjectByType<FireflySwarm>() != null) return;
        new GameObject("FireflySwarm").AddComponent<FireflySwarm>();
    }

    private void Start()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;
        MaterialData material = MaterialDatabase.Instance != null ? MaterialDatabase.Instance.Find(MaterialId) : null;

        // 자리는 늘 같다 (씨앗을 고정해서 껐다 켜도 같은 자리에 나온다): 집 둘레, 숲, 꽃 언덕
        System.Random rng = new System.Random(20261116);
        Vector2[] centers = { new Vector2(0f, 14f), new Vector2(-10f, 75f), new Vector2(90f, 85f) };
        float[] minRadius = { 10f, 0f, 0f };
        float[] maxRadius = { 40f, 55f, 40f };

        int made = 0, tries = 0;
        while (made < Count && tries++ < 2000)
        {
            int zone = made % 3;
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(minRadius[zone], maxRadius[zone], Mathf.Sqrt((float)rng.NextDouble()));
            float x = centers[zone].x + Mathf.Cos(ang) * rad, z = centers[zone].y + Mathf.Sin(ang) * rad;
            float ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            if (ground < 1.5f) continue; // 바다와 바닷가는 피한다

            GameObject go = new GameObject("Firefly" + made);
            go.transform.SetParent(transform, false);
            go.AddComponent<FireflyBug>().Init(new Vector3(x, ground + 1.3f, z), made, material, (float)rng.NextDouble() * 20f, GetGlowSprite());
            made++;
        }
    }

    private static Sprite GetGlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d2 = dx * dx + dy * dy;
                float a = Mathf.Clamp01(0.95f * Mathf.Exp(-d2 * 45f) + 0.38f * Mathf.Exp(-d2 * 7f)); // 밝은 점과 부드러운 빛무리
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, d2 > 1f ? 0f : a));
            }
        }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return glowSprite;
    }
}

/// <summary>반딧불이 한 마리: 자리(anchor) 둘레를 천천히 떠다닌다. 바라보고 E로 잡는다.</summary>
public class FireflyBug : MonoBehaviour, IInteractable
{
    private Vector3 anchor;
    private string id;
    private MaterialData material;
    private float phase;
    private SpriteRenderer glow;
    private SphereCollider catchArea;
    private float visible;      // 0 안 보임 ~ 1 환하게 보임
    private bool collected;     // 오늘 밤 이미 잡았는가
    private Transform cam;

    public void Init(Vector3 anchorPoint, int index, MaterialData mat, float phaseOffset, Sprite sprite)
    {
        anchor = anchorPoint;
        id = "ff_" + index;
        material = mat;
        phase = phaseOffset;
        collected = GameState.IsTaken(id);

        glow = gameObject.AddComponent<SpriteRenderer>();
        glow.sprite = sprite;
        glow.color = new Color(0.85f, 1f, 0.35f, 0f);
        transform.position = anchor;
        transform.localScale = Vector3.one * 0.55f;

        catchArea = gameObject.AddComponent<SphereCollider>();
        catchArea.isTrigger = true; // 걸리지 않고, 바라보는 광선만 맞는다
        catchArea.radius = 0.7f;     // 보이는 것보다 넉넉하게 (움직여서 잡기 어렵지 않게)
        catchArea.enabled = false;
    }

    private void OnEnable()
    {
        GameState.DayStarted += Refill;
    }

    private void OnDisable()
    {
        GameState.DayStarted -= Refill;
    }

    private void Refill()
    {
        collected = false;
    }

    public string Prompt => (visible > 0.5f && !collected && material != null) ? "[E] 반딧불이 잡기" : null;

    public void Interact(Interactor who)
    {
        if (visible <= 0.5f || collected || material == null) return;
        if (who.Bag.TryAdd(material))
        {
            who.ShowMessage("반딧불이 +1");
            GameState.MarkTaken(id);
            collected = true;
            GameAudio.Play(GameAudio.Sfx.Pickup, 0.7f);
        }
        else
        {
            who.ShowMessage("가방이 가득 찼어요");
        }
    }

    private void Update()
    {
        // 밤에만, 날씨가 궂지 않을 때만 나온다
        WeatherKind weather = Weather.Today;
        bool goodNight = weather != WeatherKind.Rain && weather != WeatherKind.Drizzle && weather != WeatherKind.Storm && weather != WeatherKind.Snow;
        float target = goodNight && !collected ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.74f, 0.84f, GameState.TimeOfDay)) : 0f;
        visible = Mathf.MoveTowards(visible, target, Time.deltaTime * 0.5f);

        bool show = visible > 0.01f;
        glow.enabled = show;
        catchArea.enabled = visible > 0.5f;
        if (!show) return;

        // 천천히 떠다닌다 (자리 둘레를 느리게 돈다)
        float t = Time.time * 0.35f + phase;
        transform.position = anchor + new Vector3(Mathf.Sin(t * 1.0f) * 1.4f, Mathf.Sin(t * 1.7f + 1f) * 0.45f, Mathf.Cos(t * 0.8f + 2f) * 1.4f);

        // 천천히 깜빡인다
        float blink = 0.55f + 0.45f * Mathf.Sin(Time.time * 1.8f + phase * 3f);
        Color c = glow.color;
        c.a = visible * blink;
        glow.color = c;

        // 빛무리가 늘 카메라를 보게 한다
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam != null) transform.rotation = cam.rotation;
    }
}
