using UnityEngine;

/// <summary>
/// 줍거나 캘 때 튀는 작은 조각과 반짝임. 코드로 만든 작은 빛무리 그림(FireflyBug.GlowSprite)을 여러 개 뿌렸다가 사라지게 한다.
/// 입자 시스템과 재질 에셋이 없어도 되고, 빌드에서 빠질 걱정이 없다.
/// - Sparkle: 위로 살짝 떠오르며 반짝이다 사라진다 (재료를 주울 때)
/// - Chips: 사방으로 튀어 올랐다가 떨어진다 (나무, 돌, 풀을 칠 때)
/// </summary>
public class HitFx : MonoBehaviour
{
    private struct Bit
    {
        public Transform t;
        public SpriteRenderer r;
        public Vector3 velocity;
        public float spin, size, life;
        public Color color;
    }

    private static Material material;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        material = null;
    }

    private Bit[] bits;
    private float gravity, age, drag;

    /// <summary>재료를 얻을 때: 부드럽게 떠오르는 반짝임</summary>
    public static void Sparkle(Vector3 position, Color color, int count = 9)
    {
        Spawn(position, color, count + 5, 1.1f, 0.13f, 0.95f, -0.6f, 2.2f, true);
    }

    /// <summary>칠 때: 조각이 튀어 오른다. power가 크면 더 많이, 더 세게(마지막 타격)</summary>
    public static void Chips(Vector3 position, Color color, float power = 1f)
    {
        Spawn(position, color, Mathf.RoundToInt(5 + 6 * power), 2.2f + 1.4f * power, 0.075f, 0.7f, 9f, 0.6f, false);
    }

    private static void Spawn(Vector3 position, Color color, int count, float speed, float size, float life, float gravity, float drag, bool glow)
    {
        Sprite sprite = FireflyBug.GlowSprite;
        if (sprite == null) return;

        GameObject go = new GameObject("HitFx");
        go.transform.position = position;
        HitFx fx = go.AddComponent<HitFx>();
        fx.gravity = gravity;
        fx.drag = drag;
        fx.bits = new Bit[count];

        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) material = new Material(shader); // 한 번만 만들어 모든 효과가 같이 쓴다 (효과마다 만들면 계속 쌓인다)
        }
        Material mat = material;
        for (int i = 0; i < count; i++)
        {
            GameObject b = new GameObject("bit");
            b.transform.SetParent(go.transform, false);
            SpriteRenderer sr = b.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            Vector3 dir = Random.onUnitSphere;
            dir.y = glow ? Mathf.Abs(dir.y) * 0.6f + 0.4f : Mathf.Abs(dir.y) * 0.9f + 0.35f;
            float s = size * Random.Range(0.6f, 1.3f);
            Color c = Color.Lerp(color, Color.white, glow ? Random.Range(0.2f, 0.7f) : Random.Range(0f, 0.25f));
            fx.bits[i] = new Bit
            {
                t = b.transform,
                r = sr,
                velocity = dir * speed * Random.Range(0.4f, 1f),
                size = s,
                life = life * Random.Range(0.7f, 1.2f),
                color = c,
                spin = Random.Range(-1f, 1f)
            };
            b.transform.localScale = Vector3.one * s;
            sr.color = c;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;
        Camera cam = Camera.main;
        bool any = false;
        for (int i = 0; i < bits.Length; i++)
        {
            Bit b = bits[i];
            if (b.t == null) continue;
            float k = age / b.life;
            if (k >= 1f) { b.t.gameObject.SetActive(false); continue; }
            any = true;

            b.velocity.y -= gravity * Time.deltaTime;
            b.velocity *= 1f - Mathf.Min(1f, drag * Time.deltaTime);
            b.t.position += b.velocity * Time.deltaTime;
            if (cam != null) b.t.rotation = cam.transform.rotation;
            float twinkle = 0.75f + 0.25f * Mathf.Sin(age * 30f + i);
            b.t.localScale = Vector3.one * b.size * Mathf.Lerp(1f, 0.3f, k) * twinkle;
            Color c = b.color; c.a = 1f - k * k;
            b.r.color = c;
            bits[i] = b;
        }
        if (!any) Destroy(gameObject);
    }
}
