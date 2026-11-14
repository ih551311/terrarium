using UnityEngine;

/// <summary>
/// 발소리. 걷는 거리에 맞춰 발걸음마다 소리를 내고, 서 있는 바닥에 맞는 소리를 고른다.
///  - 집 안: 나무 바닥
///  - 섬: 땅(Terrain)의 어느 재질 위에 서 있는지 읽어서 풀밭, 모래, 흙 중에서 고른다.
///  - 비 오는 날 풀밭과 흙에서는 가끔 젖은 땅의 철벅 소리가 섞인다.
///  - 걷기는 한 걸음이 짧고 달리면 길다. 점프하고 내려앉을 때는 한 번 더 크게 난다.
///  - 소리 크기는 바닥마다 달라서(풀밭 소리는 아주 작게 녹음되어 있다), 시작할 때 모든 소리의 크기를 맞춰 둔다.
/// GameAudio가 만든다. 소리 모음(FootstepSet)이 없으면 쓸 수 없다(GameAudio가 코드로 만든 발소리를 쓴다).
/// </summary>
public class FootstepPlayer : MonoBehaviour
{
    private enum Surface { Grass, Sand, Soil, Wood }

    private const float WalkStride = 1.1f, RunStride = 1.9f; // 한 걸음의 거리(미터)

    private AudioClip[] grass, sand, soil, wood, wet;
    private AudioSource source;
    private CharacterController controller;
    private float travelled;
    private bool wasGrounded = true;
    private int lastGrass = -1, lastSand = -1, lastSoil = -1, lastWood = -1, lastWet = -1;

    public bool Load()
    {
        FootstepSet set = Resources.Load<FootstepSet>("FootstepSet");
        if (set == null || set.grass.Length == 0) return false;

        // 바닥마다 소리 크기를 맞춘다: 목표 최대 크기(0~1)가 바닥마다 다르다
        grass = Normalize(set.grass, 0.55f);
        sand = Normalize(set.sand, 0.55f);
        soil = Normalize(set.soil, 0.6f);
        wood = Normalize(set.wood, 0.65f);
        wet = Normalize(set.wet, 0.5f);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        return true;
    }

    /// <summary>매 프레임 부른다.</summary>
    public void Tick(Transform player)
    {
        if (player == null) return;
        if (controller == null) controller = player.GetComponent<CharacterController>();
        if (controller == null || UIFocus.IsOpen) return;

        bool grounded = controller.isGrounded;
        Vector3 velocity = controller.velocity; velocity.y = 0f;
        float speed = velocity.magnitude;

        // 점프하고 내려앉을 때
        if (grounded && !wasGrounded) Step(player, 1f, 1.2f);
        wasGrounded = grounded;

        if (!grounded || speed < 0.5f) { travelled = Mathf.Min(travelled, WalkStride * 0.4f); return; }

        float stride = Mathf.Lerp(WalkStride, RunStride, Mathf.InverseLerp(3.5f, 7f, speed));
        travelled += speed * Time.deltaTime;
        if (travelled < stride) return;
        travelled -= stride;
        // 달릴수록 크고 힘차게
        Step(player, Mathf.Lerp(0.6f, 0.95f, Mathf.InverseLerp(3.5f, 7f, speed)), 1f);
    }

    private void Step(Transform player, float volume, float pitchScale)
    {
        Surface surface = SurfaceAt(player.position);
        AudioClip[] list;
        ref int last = ref lastGrass;
        switch (surface)
        {
            case Surface.Wood: list = wood; last = ref lastWood; break;
            case Surface.Sand: list = sand; last = ref lastSand; break;
            case Surface.Soil: list = soil; last = ref lastSoil; break;
            default: list = grass; last = ref lastGrass; break;
        }

        // 비 오는 날 풀밭과 흙에서는 젖은 땅 소리가 섞인다 (비가 셀수록 자주)
        if ((surface == Surface.Grass || surface == Surface.Soil) && wet.Length > 0 && Random.value < WeatherEffects.Amount * 0.55f)
        {
            list = wet; last = ref lastWet;
        }
        if (list == null || list.Length == 0) return;

        // 같은 소리가 연달아 나지 않게
        int index = Random.Range(0, list.Length);
        if (list.Length > 1 && index == last) index = (index + 1 + Random.Range(0, list.Length - 1)) % list.Length;
        last = index;

        source.pitch = pitchScale * Random.Range(0.95f, 1.06f);
        source.PlayOneShot(list[index], volume);
    }

    /// <summary>서 있는 자리의 바닥 종류</summary>
    private Surface SurfaceAt(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return Surface.Wood; // 집 안

        TerrainData data = terrain.terrainData;
        Vector3 local = position - terrain.transform.position;
        int x = Mathf.Clamp(Mathf.RoundToInt(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
        float[,,] weights = data.GetAlphamaps(x, z, 1, 1);

        int best = 0;
        float bestWeight = -1f;
        for (int i = 0; i < weights.GetLength(2); i++)
            if (weights[0, 0, i] > bestWeight) { bestWeight = weights[0, 0, i]; best = i; }

        TerrainLayer[] layers = data.terrainLayers;
        string layerName = best < layers.Length && layers[best] != null ? layers[best].name.ToLowerInvariant() : string.Empty;
        if (layerName.Contains("snow") || layerName.Contains("ice")) return Surface.Soil; // 눈과 얼음 위: 부드러운 땅 소리 (눈 발소리 팩이 없어서)
        if (layerName.Contains("sand") || layerName.Contains("beach")) return Surface.Sand;
        if (layerName.Contains("soil") || layerName.Contains("dirt") || layerName.Contains("path")) return Surface.Soil;
        return Surface.Grass;
    }

    /// <summary>소리 복사본을 만들면서, 가장 큰 소리가 target이 되게 키운다(너무 큰 증폭은 막는다).</summary>
    private static AudioClip[] Normalize(AudioClip[] sources, float target)
    {
        var result = new AudioClip[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            AudioClip clip = sources[i];
            if (clip == null) continue;
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            float peak = 0.0001f;
            foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            float gain = Mathf.Min(target / peak, 14f);
            for (int k = 0; k < data.Length; k++) data[k] *= gain;
            AudioClip copy = AudioClip.Create(clip.name + "_n", clip.samples, clip.channels, clip.frequency, false);
            copy.SetData(data, 0);
            result[i] = copy;
        }
        return result;
    }
}
