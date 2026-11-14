using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병 속 효과 재료(눈 내리기, 반짝이 가루, 꽃잎 날리기, 비눗방울, 보슬비). 작은 유리병 모양 재료를 병에 놓으면,
/// 병 안쪽(넓은 부분)을 입자가 채운다: 눈이 내리고, 반짝이가 떠다니고, 꽃잎이 흔들리며 떨어지고, 방울이 올라가고, 빗줄기가 내린다.
/// 입자는 병의 자식이라 병을 돌려도 함께 움직이고, 선반에서 줄여도 같이 작아진다. 재료를 빼면 효과도 사라진다.
/// 입자 그림(둥근 점, 별, 고리, 꽃잎)은 코드로 만들고, 재질은 항상 들어가는 Sprites/Default를 쓴다. BottleContents가 재료를 놓을 때 Attach를 부른다.
/// </summary>
public class BottleEffect : MonoBehaviour
{
    private enum Look { Dot, Star, Ring, Petal }

    private struct Preset
    {
        public Look look;
        public Color colorA, colorB;
        public float sizeMin, sizeMax;
        public float speed;          // 아래로(음수면 위로) 움직이는 속도 (m/s)
        public float drift;          // 옆으로 흔들리는 정도 (m/s)
        public float rate;           // 초당 만드는 입자 수
        public bool fullVolume;      // true면 병 속 어디서나 생기고(반짝이), false면 위(또는 아래)에서 생긴다
        public bool twinkle;         // 크기가 깜빡인다
        public bool spin;            // 빙글 돈다
        public bool streak;          // 늘려서 빗줄기처럼
    }

    private static readonly Dictionary<string, Preset> Presets = new Dictionary<string, Preset>
    {
        { "fx_snow",    new Preset { look = Look.Dot,   colorA = new Color(1f, 1f, 1f, 0.95f),     colorB = new Color(0.85f, 0.93f, 1f, 0.9f), sizeMin = 0.007f, sizeMax = 0.013f, speed = 0.035f, drift = 0.012f, rate = 14f } },
        { "fx_glitter", new Preset { look = Look.Star,  colorA = new Color(1f, 0.88f, 0.4f, 1f),   colorB = new Color(1f, 1f, 0.95f, 1f),     sizeMin = 0.007f, sizeMax = 0.014f, speed = 0.004f, drift = 0.010f, rate = 26f, fullVolume = true, twinkle = true } },
        { "fx_petals",  new Preset { look = Look.Petal, colorA = new Color(1f, 0.72f, 0.82f, 1f),  colorB = new Color(1f, 0.85f, 0.9f, 1f),   sizeMin = 0.012f, sizeMax = 0.018f, speed = 0.028f, drift = 0.022f, rate = 7f, spin = true } },
        { "fx_bubbles", new Preset { look = Look.Ring,  colorA = new Color(0.75f, 0.92f, 1f, 0.8f), colorB = new Color(0.9f, 0.98f, 1f, 0.7f), sizeMin = 0.012f, sizeMax = 0.024f, speed = -0.03f, drift = 0.010f, rate = 5f } },
        { "fx_rain",    new Preset { look = Look.Dot,   colorA = new Color(0.7f, 0.85f, 1f, 0.85f), colorB = new Color(0.85f, 0.93f, 1f, 0.8f), sizeMin = 0.0035f, sizeMax = 0.006f, speed = 0.30f, drift = 0.0f, rate = 40f, streak = true } },
    };

    private static readonly Dictionary<Look, Material> materials = new Dictionary<Look, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        materials.Clear();
    }

    private Preset preset;
    private BottleContents bottle;
    private GameObject emitter;
    private float builtForSurface = float.NaN;

    /// <summary>효과 재료(id가 fx_로 시작)면 모델에 BottleEffect를 붙인다.</summary>
    public static void Attach(GameObject visual, string materialId)
    {
        if (visual == null || string.IsNullOrEmpty(materialId) || !Presets.TryGetValue(materialId, out Preset p)) return;
        BottleEffect fx = visual.AddComponent<BottleEffect>();
        fx.preset = p;
    }

    private void Start()
    {
        bottle = GetComponentInParent<BottleContents>();
        Build();
    }

    private void Update()
    {
        // 흙의 양을 바꾸면 흙 표면 높이가 달라지므로 다시 맞춘다
        if (bottle != null && !Mathf.Approximately(builtForSurface, bottle.SurfaceLocalY)) Build();
    }

    private void OnDestroy()
    {
        if (emitter != null) Destroy(emitter);
    }

    private void Build()
    {
        if (bottle == null) return;
        if (!bottle.TryGetInterior(out Vector3 center, out Vector3 half)) return;
        if (emitter != null) Destroy(emitter);
        builtForSurface = bottle.SurfaceLocalY;

        float top = center.y + half.y;
        float bottom = Mathf.Min(top - 0.02f, bottle.SurfaceLocalY + 0.01f); // 흙 표면 바로 위까지
        float height = Mathf.Max(0.03f, top - bottom);

        emitter = new GameObject("BottleEffect_" + name);
        emitter.transform.SetParent(bottle.transform, false);
        emitter.layer = gameObject.layer; // 병 그림 카메라에 보이도록 병과 같은 층
        bool rising = preset.speed < 0f;

        // 만드는 자리: 위(눈, 꽃잎, 비)나 아래(방울)의 얇은 판, 또는 병 속 전체(반짝이)
        Vector3 pos; Vector3 scale;
        if (preset.fullVolume) { pos = new Vector3(center.x, (top + bottom) * 0.5f, center.z); scale = new Vector3(half.x * 1.7f, height, half.z * 1.7f); }
        else if (rising) { pos = new Vector3(center.x, bottom, center.z); scale = new Vector3(half.x * 1.5f, 0.002f, half.z * 1.5f); }
        else { pos = new Vector3(center.x, top, center.z); scale = new Vector3(half.x * 1.6f, 0.002f, half.z * 1.6f); }
        emitter.transform.localPosition = pos;

        var ps = emitter.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float travel = preset.fullVolume ? 4.5f : height / Mathf.Max(0.005f, Mathf.Abs(preset.speed));
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = travel;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(preset.sizeMin, preset.sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(preset.colorA, preset.colorB);
        main.maxParticles = 160;
        main.gravityModifier = 0f;
        if (preset.spin) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = preset.rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = scale;

        // 움직임: 눈과 비는 아래로, 방울은 위로, 반짝이는 제자리에서 느리게 떠다닌다. 옆으로는 살짝 흔들린다.
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-preset.drift, preset.drift);
        vel.y = new ParticleSystem.MinMaxCurve(-preset.speed * 1.15f, -preset.speed * 0.85f);
        vel.z = new ParticleSystem.MinMaxCurve(-preset.drift, preset.drift);

        if (preset.drift > 0f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = preset.drift * 0.6f;
            noise.frequency = 0.9f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        // 나타날 때와 사라질 때 부드럽게
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        if (preset.twinkle)
        {
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.2f, 1f), new Keyframe(0.4f, 0.3f), new Keyframe(0.6f, 1f), new Keyframe(0.8f, 0.35f), new Keyframe(1f, 0f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
        if (preset.spin)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);
        }

        var renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetMaterial(preset.look);
        renderer.sortingFudge = 0f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (preset.streak)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4f;
            renderer.velocityScale = 0.15f;
        }

        ps.Play();
    }

    // ───────── 입자 그림 (코드로 만든다) ─────────

    private static Material GetMaterial(Look look)
    {
        if (materials.TryGetValue(look, out Material cached) && cached != null) return cached;
        Texture2D tex = MakeTexture(look);
        Material m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        m.color = Color.white;
        m.renderQueue = 3100; // 병 유리(3000)보다 나중에 그려서 유리 너머로도 또렷이
        materials[look] = m;
        return m;
    }

    private static Texture2D MakeTexture(Look look)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 0f;
                switch (look)
                {
                    case Look.Dot:    // 가운데가 밝고 가장자리가 부드러운 점
                        a = Mathf.Clamp01(1.25f - d * 1.25f);
                        a = a * a * (3f - 2f * a);
                        break;
                    case Look.Star:   // 네 갈래 반짝임
                        float cross = Mathf.Max(Mathf.Exp(-Mathf.Abs(dx) * 14f) * Mathf.Exp(-Mathf.Abs(dy) * 2.2f), Mathf.Exp(-Mathf.Abs(dy) * 14f) * Mathf.Exp(-Mathf.Abs(dx) * 2.2f));
                        a = Mathf.Clamp01(cross + Mathf.Exp(-d * d * 18f));
                        break;
                    case Look.Ring:   // 비눗방울: 얇은 고리와 하이라이트
                        a = Mathf.Clamp01(Mathf.Exp(-Mathf.Pow((d - 0.78f) * 7f, 2f)) * 0.9f + (d < 0.78f ? 0.07f : 0f));
                        float hx = dx + 0.35f, hy = dy - 0.38f;
                        a = Mathf.Max(a, Mathf.Exp(-(hx * hx + hy * hy) * 40f));
                        break;
                    case Look.Petal:  // 꽃잎: 한쪽이 뾰족한 타원
                        float px = dx * 1.0f, py = dy * 1.9f;
                        float e = Mathf.Sqrt(px * px + py * py) + Mathf.Max(0f, dy) * 0.2f;
                        a = Mathf.Clamp01((0.95f - e) * 9f);
                        break;
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
