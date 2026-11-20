using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 밤이 되면 병 속에서 은은하게 빛나는 재료(초, 별 장식, 초승달, 달맞이꽃, 반딧불이, 캠프파이어, 요정 문).
/// 밤이 깊어질수록 재료가 따뜻하게 빛나고, 병 하나에 작은 불빛이 하나 생겨 근처를 살짝 밝힌다.
/// BottleContents가 재료를 병에 놓을 때 Attach로 붙인다. 낮에는 아무 효과가 없다.
/// </summary>
public class NightGlow : MonoBehaviour
{
    private struct Glow
    {
        public Color color;
        public float strength; // 재료 자체가 빛나는 정도
        public Glow(float r, float g, float b, float strength) { color = new Color(r, g, b); this.strength = strength; }
    }

    private static readonly Dictionary<string, Glow> Glowing = new Dictionary<string, Glow>
    {
        { "candle",           new Glow(1.00f, 0.72f, 0.30f, 1.0f) },
        { "star_ornament",    new Glow(1.00f, 0.90f, 0.40f, 0.9f) },
        { "crescent_moon",    new Glow(1.00f, 0.93f, 0.55f, 0.9f) },
        { "evening_primrose", new Glow(1.00f, 0.90f, 0.40f, 0.5f) },
        { "firefly",          new Glow(0.80f, 1.00f, 0.25f, 0.7f) },
        { "campfire",         new Glow(1.00f, 0.50f, 0.18f, 1.0f) },
        { "fairy_door",       new Glow(1.00f, 0.82f, 0.50f, 0.6f) },
        { "snow_flower",      new Glow(0.70f, 0.90f, 1.00f, 0.4f) },
        { "lantern",          new Glow(1.00f, 0.78f, 0.38f, 1.0f) },
        { "blue_mushroom",    new Glow(0.45f, 0.85f, 1.00f, 0.7f) },
        { "lightning_glass",  new Glow(0.95f, 0.95f, 0.60f, 0.5f) },
        { "aurora_fish",      new Glow(0.55f, 1.00f, 0.80f, 0.9f) },
        { "lighthouse",       new Glow(1.00f, 0.92f, 0.55f, 0.8f) },
        { "street_lamp",      new Glow(1.00f, 0.88f, 0.48f, 0.9f) },
        { "house",            new Glow(1.00f, 0.80f, 0.45f, 0.5f) },
    };

    private Material[] materials;
    private Light glowLight;
    private Glow glow;
    private float phase;
    private bool isFirefly;
    private SpriteRenderer halo;      // 반딧불이 꽁무니의 빛무리 (모델이 아주 작아서 빛무리가 있어야 낮에도 보인다)
    private BottleContents bottleOwner;

    /// <summary>이 재료가 밤에 빛나는 재료면 모델에 NightGlow를 붙인다.</summary>
    public static void Attach(GameObject visual, string materialId)
    {
        if (visual == null || string.IsNullOrEmpty(materialId) || !Glowing.TryGetValue(materialId, out Glow g)) return;
        NightGlow ng = visual.AddComponent<NightGlow>();
        ng.glow = g;
        ng.isFirefly = materialId == "firefly";
        ng.phase = Random.value * 10f;
        ng.bottleOwner = visual.GetComponentInParent<BottleContents>();

        if (ng.isFirefly)
        {
            // 모델이 손톱만 해서 병 속에서 거의 안 보이므로 조금 키우고, 꽁무니에 빛무리를 둔다 (낮에는 은은하게, 밤에는 환하게)
            visual.transform.localScale *= 2.2f;
            GameObject haloObject = new GameObject("FireflyHalo");
            haloObject.transform.SetParent(visual.transform, false);
            haloObject.transform.localPosition = new Vector3(0f, 0.005f, 0f); // 구슬 한가운데에서 퍼져 나온다
            haloObject.transform.localScale = Vector3.one * 0.04f;
            haloObject.layer = visual.layer;
            ng.halo = haloObject.AddComponent<SpriteRenderer>();
            ng.halo.sprite = FireflyBug.GlowSprite;
            ng.halo.color = new Color(0.85f, 1f, 0.4f, 0.4f);
            ng.halo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // 병 하나에 불빛은 하나만 둔다 (불빛이 너무 많아지지 않게)
        BottleContents bottle = visual.GetComponentInParent<BottleContents>();
        bool lightTaken = false;
        if (bottle != null)
            foreach (NightGlow other in bottle.GetComponentsInChildren<NightGlow>(true))
                if (other != ng && other.glowLight != null) { lightTaken = true; break; }
        if (!lightTaken)
        {
            GameObject lightObject = new GameObject("NightGlowLight");
            lightObject.transform.SetParent(visual.transform, false);
            lightObject.transform.position = visual.transform.position + Vector3.up * 0.03f;
            ng.glowLight = lightObject.AddComponent<Light>();
            ng.glowLight.type = LightType.Point;
            ng.glowLight.color = g.color;
            ng.glowLight.range = 0.45f;
            ng.glowLight.intensity = 0f;
            ng.glowLight.shadows = LightShadows.None;
            ng.glowLight.enabled = false;
            lightObject.layer = visual.layer;
        }
    }

    private void Start()
    {
        // 재료마다 따로 빛나도록 재질을 복사해서 쓴다
        var list = new List<Material>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SpriteRenderer) continue;
            foreach (Material m in r.materials)
            {
                m.EnableKeyword("_EMISSION");
                list.Add(m);
            }
        }
        materials = list.ToArray();
    }

    private void OnDestroy()
    {
        if (materials == null) return;
        foreach (Material m in materials) if (m != null) Destroy(m);
    }

    private void Update()
    {
        if (materials == null) return;
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 0.86f, GameState.TimeOfDay));
        float t = Time.time + phase;
        // 촛불처럼 살짝 일렁이고, 반딧불이는 천천히 깜빡인다
        float pulse = isFirefly ? 0.55f + 0.45f * Mathf.Sin(t * 1.6f) : 0.88f + 0.12f * Mathf.Sin(t * 5.3f) * Mathf.Sin(t * 1.7f);
        float level = isFirefly ? Mathf.Max(night, 0.2f) : night; // 반딧불이는 낮에도 꽁무니가 희미하게 빛난다
        Color emission = glow.color * (glow.strength * level * pulse);
        if (halo != null)
        {
            Color hc = halo.color; hc.a = Mathf.Lerp(0.45f, 0.7f, night) * (0.6f + 0.4f * pulse);
            halo.color = hc;
            Camera viewer = bottleOwner != null && bottleOwner.ViewCamera != null && bottleOwner.ViewCamera.enabled ? bottleOwner.ViewCamera : Camera.main;
            if (viewer != null) halo.transform.rotation = viewer.transform.rotation; // 늘 보는 쪽을 향한다
        }
        foreach (Material m in materials) if (m != null) m.SetColor("_EmissionColor", emission);

        if (glowLight != null)
        {
            glowLight.enabled = night > 0.02f;
            glowLight.intensity = night * pulse * 0.5f;
        }
    }
}
