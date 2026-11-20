using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게임의 소리 전부. 소리 파일이 없어서 모두 코드로 만든다. (진짜 소리 파일을 구하면 이 스크립트의 clip만 바꿔 끼우면 된다)
/// - 배경 음악: 낮에는 밝고 느린 펜타토닉 선율, 밤에는 더 느리고 낮은 선율. 하루 시간(GameState.TimeOfDay)에 따라 서로 섞인다.
/// - 주변 소리: 파도(바닷가에 가까울수록 크게), 바람, 낮의 새 소리, 밤의 풀벌레, 빗소리, 집 안의 조용한 방 소리
/// - 효과음: 줍기, 병 안에 놓기, 빼기, 완성 종소리, 코인, 종이, 버튼 누르기, 발소리
/// - P 키: 음악 켜기/끄기
/// 게임이 시작될 때 스스로 만들어지고 씬이 바뀌어도 남는다. 효과음은 GameAudio.Play(GameAudio.Sfx.Pickup)처럼 부른다.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public enum Sfx { Pickup, Place, Remove, Chime, Coin, Paper, Click, DoorOpen, DoorClose }

    private const int Rate = 22050;

    private static GameAudio instance;
    private static bool musicOn = true;

    /// <summary>배경 음악이 켜져 있는가 (P 키나 일시정지 메뉴로 바꾼다)</summary>
    public static bool MusicOn
    {
        get { return musicOn; }
        set { musicOn = value; PlayerPrefs.SetInt("music_on", value ? 1 : 0); } // 다음에 켜도 그대로
    }

    /// <summary>효과음을 한 번 낸다. 소리 시스템이 아직 없으면 아무 일도 하지 않는다.</summary>
    public static void Play(Sfx sfx, float volume = 1f)
    {
        if (instance == null) return;
        instance.PlaySfx(sfx, volume);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudio>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        musicOn = PlayerPrefs.GetInt("music_on", 1) == 1;
    }

    // ───────── 소리 재료 ─────────

    private readonly Dictionary<Sfx, AudioClip> sfxClips = new Dictionary<Sfx, AudioClip>();
    private AudioClip[] steps, sandSteps, birds;
    private AudioSource sfxSource, waves, wind, rainLoop, crickets, roomTone, musicDay, musicNight;

    private MusicPlayer music;
    private FootstepPlayer footsteps;
    private CharacterController controller;
    private float stepTimer, birdTimer;
    private float musicLevel;

    private void Awake()
    {
        sfxSource = NewSource("Sfx", null, 1f, false);
        sfxClips[Sfx.Pickup] = Glide("Pickup", 520f, 900f, 0.10f, 18f, 0.5f);
        sfxClips[Sfx.Place] = Thump("Place", 210f, 0.09f, 34f, 0.35f);
        sfxClips[Sfx.Remove] = Glide("Remove", 420f, 250f, 0.10f, 22f, 0.4f);
        sfxClips[Sfx.Chime] = Bells("Chime", new[] { 1046.5f, 1318.5f, 1568f, 2093f }, 0.16f, 1.6f);
        sfxClips[Sfx.Coin] = Bells("Coin", new[] { 1975.5f, 2637f }, 0.09f, 0.55f);
        sfxClips[Sfx.Paper] = FilteredNoise("Paper", 0.28f, 0.45f, 6f, 3, 0.6f);
        sfxClips[Sfx.Click] = Glide("Click", 1400f, 1100f, 0.035f, 70f, 0.25f);
        sfxClips[Sfx.DoorOpen] = Creak("DoorOpen", 95f, 150f, 0.7f, false);
        sfxClips[Sfx.DoorClose] = Creak("DoorClose", 130f, 80f, 0.7f, true);

        // 소리 파일이 Resources/Sfx에 있으면 코드로 만든 소리 대신 쓴다 (EarShot 소리 모음에서 뽑아 둔 것. 없으면 위의 코드 소리)
        foreach (Sfx key in new[] { Sfx.Chime, Sfx.Coin, Sfx.Click, Sfx.Pickup, Sfx.Place, Sfx.Paper, Sfx.Remove, Sfx.DoorOpen, Sfx.DoorClose })
        {
            AudioClip file = Resources.Load<AudioClip>("Sfx/" + key);
            if (file != null) sfxClips[key] = file;
        }

        steps = new[] { Footstep("Step1", 0.16f, 0.10f, 32f, 1), Footstep("Step2", 0.13f, 0.11f, 30f, 2), Footstep("Step3", 0.19f, 0.095f, 34f, 3) };
        sandSteps = new[] { Footstep("Sand1", 0.38f, 0.13f, 24f, 4), Footstep("Sand2", 0.32f, 0.14f, 22f, 5), Footstep("Sand3", 0.42f, 0.12f, 26f, 6) };
        birds = new[] { Chirp("Bird1", 3200f, 4300f, 3), Chirp("Bird2", 2600f, 3900f, 2), Chirp("Bird3", 3600f, 2900f, 4) };

        waves = NewSource("Waves", Loop("Waves", WavesData(), Rate), 0f, true);
        wind = NewSource("Wind", Loop("Wind", WindData(), Rate), 0f, true);
        rainLoop = NewSource("Rain", Loop("Rain", RainData(), Rate), 0f, true);
        // 바람은 진짜 녹음(Resources/Sfx/WindLoop)이 있으면 그것으로 바꾼다. (빗소리는 날씨 에셋이 따로 낸다)
        AudioClip windFile = Resources.Load<AudioClip>("Sfx/WindLoop");
        if (windFile != null) { wind.clip = windFile; wind.Play(); }
        crickets = NewSource("Crickets", Loop("Crickets", CricketData(), Rate), 0f, true);
        roomTone = NewSource("Room", Loop("Room", RoomData(), Rate), 0f, true);

        // 발소리: 사신 소리 모음(Footsteps Sounds)이 있으면 바닥에 맞는 진짜 소리를, 없으면 코드로 만든 소리를 쓴다
        footsteps = gameObject.AddComponent<FootstepPlayer>();
        if (!footsteps.Load()) { Destroy(footsteps); footsteps = null; }

        // 배경 음악: 사신 곡 모음(Dreamcore Music)이 있으면 그것을, 없으면 코드로 만든 음악을 쓴다
        music = gameObject.AddComponent<MusicPlayer>();
        if (!music.Load())
        {
            Destroy(music);
            music = null;
            musicDay = NewSource("MusicDay", Music("MusicDay", day: true), 0f, true);
            musicNight = NewSource("MusicNight", Music("MusicNight", day: false), 0f, true);
        }
    }

    private AudioSource NewSource(string name, AudioClip clip, float volume, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.clip = clip; s.volume = volume; s.loop = loop; s.spatialBlend = 0f; s.playOnAwake = false;
        if (loop && clip != null) s.Play();
        return s;
    }

    private void PlaySfx(Sfx sfx, float volume)
    {
        if (sfxClips.TryGetValue(sfx, out AudioClip clip)) sfxSource.PlayOneShot(clip, volume);
    }

    // ───────── 매 프레임 ─────────

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
        {
            MusicOn = !musicOn; // 켜고 끄는 것은 소리로 알 수 있어서 글자는 띄우지 않는다
        }

        // 패널이 열려 있을 때 마우스를 누르면 "톡" 소리
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && UIFocus.IsOpen) PlaySfx(Sfx.Click, 0.5f);

        bool outdoors = WeatherEffects.Outdoors;
        float t = GameState.TimeOfDay;
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.86f, t));
        float rain = WeatherEffects.Amount;
        Transform player = GameRoot.Instance != null ? GameRoot.Instance.Player : null;

        // 음악: 낮과 밤을 섞는다. 비 오는 날은 조금 작게.
        float musicTarget = musicOn ? 0.32f * (1f - 0.35f * rain) : 0f;
        musicLevel = Mathf.MoveTowards(musicLevel, musicTarget, Time.deltaTime * 0.3f);
        if (music != null)
        {
            music.Tick(musicLevel, night, Weather.Today);
        }
        else
        {
            musicDay.volume = musicLevel * (1f - night);
            musicNight.volume = musicLevel * night;
        }

        // 파도: 섬 가장자리(바닷가)에 가까울수록 크게. 집 안에서는 안 들린다.
        float shore = 0f;
        if (outdoors && player != null)
        {
            float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(0f, 30f));
            shore = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 140f, d));
        }
        Fade(waves, outdoors ? Mathf.Lerp(0.05f, 0.26f, shore) : 0.015f);
        Fade(wind, outdoors ? 0.17f + 0.12f * rain : 0.03f);
        Fade(rainLoop, 0f); // 빗소리와 폭풍 소리는 날씨 에셋(AudioModule)이 낸다
        Fade(crickets, outdoors ? 0.07f * night * (1f - rain) : 0f);
        Fade(roomTone, outdoors ? 0f : 0.10f);

        // 낮의 새 소리 (바깥, 맑은 날)
        birdTimer -= Time.deltaTime;
        if (birdTimer <= 0f)
        {
            birdTimer = Random.Range(4f, 11f);
            if (outdoors && t < 0.7f && rain < 0.2f)
            {
                sfxSource.panStereo = Random.Range(-0.7f, 0.7f);
                sfxSource.PlayOneShot(birds[Random.Range(0, birds.Length)], Random.Range(0.12f, 0.24f));
                sfxSource.panStereo = 0f;
            }
        }

        if (footsteps != null) footsteps.Tick(player);
        else UpdateFootsteps(player);
    }

    private static void Fade(AudioSource s, float target)
    {
        s.volume = Mathf.MoveTowards(s.volume, target, Time.deltaTime * 0.6f);
    }

    private void UpdateFootsteps(Transform player)
    {
        if (player == null) return;
        if (controller == null) controller = player.GetComponent<CharacterController>();
        if (controller == null || UIFocus.IsOpen) return;

        Vector3 v = controller.velocity; v.y = 0f;
        float speed = v.magnitude;
        if (!controller.isGrounded || speed < 0.6f) { stepTimer = 0.2f; return; }

        stepTimer -= Time.deltaTime * Mathf.Clamp(speed, 1f, 8f) / 2.2f;
        if (stepTimer > 0f) return;
        stepTimer = 1f;
        bool sand = WeatherEffects.Outdoors && Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(0f, 30f)) > 108f; // 바닷가는 모래
        AudioClip[] set = sand ? sandSteps : steps;
        sfxSource.PlayOneShot(set[Random.Range(0, set.Length)], sand ? 0.3f : 0.26f);
    }

    // ───────── 소리 만들기 ─────────

    private static AudioClip Make(string name, float[] data, int rate = Rate)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>높이가 변하는 짧은 사인 소리 (줍기 "뽁", 빼기, 버튼)</summary>
    private static AudioClip Glide(string name, float f0, float f1, float seconds, float decay, float gain)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float f = Mathf.Lerp(f0, f1, u);
            phase += 2f * Mathf.PI * f / Rate;
            float env = Mathf.Min(1f, i / 80f) * Mathf.Exp(-u * seconds * decay);
            d[i] = Mathf.Sin(phase) * env * gain;
        }
        return Make(name, d);
    }

    /// <summary>낮은 톡 소리 (놓기, 발소리)</summary>
    private static AudioClip Thump(string name, float freq, float seconds, float decay, float gain)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        var rnd = new System.Random(name.GetHashCode());
        float low = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Exp(-t * decay);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            low += (noise - low) * 0.18f; // 부드럽게
            d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f + low * 0.7f) * env * gain;
        }
        return Make(name, d);
    }

    /// <summary>
    /// 발소리: 짧은 잡음을 걸러서 "사박" 하고 부드럽게 난다. 북소리 같은 낮은 울림은 거의 없다.
    /// brightness가 클수록 바삭하고(모래), 작을수록 폭신하다(풀밭). 낮은 소리는 발이 닿는 아주 작은 둔탁함만 더한다.
    /// </summary>
    private static AudioClip Footstep(string name, float brightness, float seconds, float decay, int seed)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        var rnd = new System.Random(seed * 977 + 13);
        float lp1 = 0f, lp2 = 0f, hp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp1 += (x - lp1) * brightness;     // 높은 소리를 덜어 내고
            lp2 += (lp1 - lp2) * brightness;
            hp += (lp2 - hp) * 0.015f;         // 아주 낮은 울림도 덜어 낸다 (북소리가 되지 않게)
            float body = lp2 - hp;
            float attack = Mathf.Min(1f, i / 120f);               // 딱 하는 끝이 없게 살짝 부드럽게 시작
            float env = attack * Mathf.Exp(-t * decay);
            float thud = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 60f) * 0.12f; // 발 닿는 둔탁함은 아주 작게
            d[i] = (body * 4.5f + thud) * env;
        }
        return Make(name, d);
    }
    /// <summary>
    /// 나무 문 삐걱 소리: 낮은 톱니 소리의 높이를 천천히 바꾸고 거친 잡음을 섞는다. 닫는 쪽(thud)은 끝에 "쿵" 하고 부딪힌다.
    /// </summary>
    private static AudioClip Creak(string name, float f0, float f1, float seconds, bool thud)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        var rnd = new System.Random(f0 > f1 ? 41 : 43);
        float phase = 0f, lp = 0f, wobble = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate, k = i / (float)n;
            // 천천히 높이가 변하고, 삐걱거리며 흔들린다
            wobble += ((float)rnd.NextDouble() - 0.5f) * 0.02f;
            wobble *= 0.995f;
            float f = Mathf.Lerp(f0, f1, k) * (1f + wobble * 2f + 0.03f * Mathf.Sin(t * 37f));
            phase += f / Rate;
            float saw = (phase % 1f) * 2f - 1f;
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.08f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.05f)); // 부드럽게 시작하고 끝난다
            float tone = saw * 0.22f + lp * 0.5f;
            d[i] = tone * env * 0.5f;
        }
        if (thud)
        {
            int start = (int)(n * 0.9f);
            for (int i = start; i < n; i++)
            {
                float t = (i - start) / (float)Rate;
                d[i] += Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 28f) * 0.55f;
            }
        }
        return Make(name, d);
    }

    /// <summary>종소리: 음마다 길게 울리며 겹친다</summary>
    private static AudioClip Bells(string name, float[] notes, float step, float tail)
    {
        int n = (int)(Rate * (step * notes.Length + tail));
        var d = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            int start = (int)(Rate * step * k);
            for (int i = 0; start + i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, i / 60f) * Mathf.Exp(-t * 3.2f);
                if (env < 0.001f) break;
                float s = Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * notes[k] * 2.01f * t) + 0.12f * Mathf.Sin(2f * Mathf.PI * notes[k] * 3.1f * t);
                d[start + i] += s * env * 0.22f;
            }
        }
        return Make(name, d);
    }

    /// <summary>걸러진 잡음 한 번 (종이, 바람 같은 짧은 소리). poles: 거르는 횟수, softness 클수록 낮은 소리</summary>
    private static AudioClip FilteredNoise(string name, float seconds, float gain, float decay, int poles, float coef)
    {
        int n = (int)(Rate * seconds);
        var d = new float[n];
        var rnd = new System.Random(name.GetHashCode());
        var state = new float[poles];
        for (int i = 0; i < n; i++)
        {
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            for (int p = 0; p < poles; p++) { state[p] += (x - state[p]) * coef; x = state[p]; }
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, i / 300f) * Mathf.Exp(-t * decay);
            d[i] = x * env * gain * 2.5f;
        }
        return Make(name, d);
    }

    /// <summary>새 소리: 짧은 사인 높이 변화를 몇 번 이어 붙인 "찌르륵"</summary>
    private static AudioClip Chirp(string name, float f0, float f1, int notes)
    {
        float noteLen = 0.07f, gap = 0.05f;
        int n = (int)(Rate * (notes * (noteLen + gap) + 0.1f));
        var d = new float[n];
        for (int k = 0; k < notes; k++)
        {
            int start = (int)(Rate * k * (noteLen + gap));
            int len = (int)(Rate * noteLen);
            float phase = 0f;
            for (int i = 0; i < len && start + i < n; i++)
            {
                float u = i / (float)len;
                float f = Mathf.Lerp(f0, f1, u) * (1f + 0.04f * k) * (1f + 0.025f * Mathf.Sin(u * 38f)); // 목소리가 살짝 떨리게
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Sin(Mathf.PI * u); env *= env;                                        // 부드럽게 시작하고 끝난다
                float tone = Mathf.Sin(phase) + 0.18f * Mathf.Sin(phase * 2f) + 0.05f * Mathf.Sin(phase * 3f);
                d[start + i] = tone * env * 0.32f;
            }
        }
        return Make(name, d);
    }

    /// <summary>끝과 처음이 이어지도록 맞춘 반복 소리를 만든다. (data는 fade만큼 더 길게 만든 것)</summary>
    private static AudioClip Loop(string name, float[] data, int rate)
    {
        const int fade = LoopFade;
        int n = data.Length - fade;
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (i < fade) { float u = i / (float)fade; d[i] = data[i] * u + data[n + i] * (1f - u); }
            else d[i] = data[i];
        }
        return Make(name, d, rate);
    }

    private const int LoopFade = 4000;

    /// <summary>파도: 낮은 잡음이 7초마다 밀려왔다 빠진다</summary>
    private static float[] WavesData()
    {
        int n = Rate * 14 + LoopFade;
        var d = new float[n];
        var rnd = new System.Random(11);
        float a = 0f, b = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            a += (x - a) * 0.05f; b += (a - b) * 0.12f;
            float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 7f);
            float swell2 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4.7f + 1.3f);
            d[i] = (b * 3.2f) * (0.25f + 0.75f * swell * swell) * (0.7f + 0.3f * swell2);
        }
        return d;
    }

    /// <summary>바람: 아주 낮고 부드러운 잡음이 천천히 세졌다 약해진다</summary>
    private static float[] WindData()
    {
        int n = Rate * 12 + LoopFade;
        var d = new float[n];
        var rnd = new System.Random(23);
        float a = 0f, b = 0f, c = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            a += (x - a) * 0.03f; b += (a - b) * 0.03f; c += (b - c) * 0.1f;
            float gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / 6f + 0.7f);
            d[i] = c * 14f * gust;
        }
        return d;
    }

    /// <summary>빗소리: 높은 잡음의 고른 쏴아 소리</summary>
    private static float[] RainData()
    {
        int n = Rate * 5 + LoopFade;
        var d = new float[n];
        var rnd = new System.Random(31);
        float lp = 0f, hp = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp += (x - lp) * 0.5f;       // 아주 높은 쉿 소리는 덜어 내고
            hp += (lp - hp) * 0.04f;     // 낮은 울림도 덜어 낸다
            float drop = (float)rnd.NextDouble() < 0.0025f ? (float)rnd.NextDouble() * 0.8f : 0f; // 가끔 방울
            d[i] = (lp - hp) * 0.7f + drop * 0.5f * (lp - hp);
        }
        return d;
    }

    /// <summary>
    /// 밤의 풀벌레: 예전에는 높은 사인음이 쉬지 않고 "삐삐삐" 하고 켜졌다 꺼져서 기계음처럼 거슬렸다.
    /// 지금은 귀뚜라미 두 마리가 번갈아 "찌르르" 하고 짧게 울고(세 번 끊어 우는 한 묶음), 한참 쉬었다가 다시 운다.
    /// 소리는 부드럽게 시작하고 끝나며, 높이가 조금씩 흔들리고 숨 같은 잡음이 약간 섞여서 사인음처럼 들리지 않는다.
    /// </summary>
    private static float[] CricketData()
    {
        int n = Rate * 10 + LoopFade;
        var d = new float[n];
        var rnd = new System.Random(61);
        float[] pitch = { 3650f, 4150f };
        float[] nextAt = { 0.4f, 2.6f }; // 두 마리가 서로 다른 때에 운다
        for (int k = 0; k < 2; k++)
        {
            float t0 = nextAt[k];
            while (t0 < 10f)
            {
                int pulses = 3;
                for (int p = 0; p < pulses; p++)
                {
                    float start = t0 + p * 0.085f;
                    int i0 = (int)(start * Rate), len = (int)(0.05f * Rate);
                    float f = pitch[k] * (1f + 0.012f * (float)(rnd.NextDouble() - 0.5));
                    float phase = 0f;
                    for (int i = 0; i < len && i0 + i < n; i++)
                    {
                        float u = i / (float)len;
                        float env = Mathf.Sin(Mathf.PI * u); env *= env;        // 부드럽게 커졌다 작아진다
                        phase += 2f * Mathf.PI * (f * (1f + 0.02f * u)) / Rate;
                        float hiss = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.18f;
                        d[i0 + i] += (Mathf.Sin(phase) + 0.25f * Mathf.Sin(phase * 2.02f) + hiss) * env * 0.22f;
                    }
                }
                t0 += 0.9f + (float)rnd.NextDouble() * 1.8f; // 한 묶음 우는 사이 한참 쉰다
            }
        }
        return d;
    }
    /// <summary>집 안의 조용한 방 소리: 아주 낮은 웅웅 소리와 가끔 장작 타는 톡톡</summary>
    private static float[] RoomData()
    {
        int n = Rate * 6 + LoopFade;
        var d = new float[n];
        var rnd = new System.Random(47);
        float a = 0f, crackle = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float x = (float)(rnd.NextDouble() * 2.0 - 1.0);
            a += (x - a) * 0.02f;
            if (rnd.NextDouble() < 0.00045) crackle = (float)rnd.NextDouble() * 0.9f;
            crackle *= 0.993f;
            d[i] = a * 3f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / 6f)) + x * crackle * 0.35f;
        }
        return d;
    }

    // ───────── 배경 음악 ─────────

    private static readonly int[] DayScale = { 0, 2, 4, 7, 9 };      // 도 레 미 솔 라 (밝은 펜타토닉)
    private static readonly int[] NightScale = { 0, 3, 5, 7, 10 };   // 라 도 레 미 솔 (짧은 음계의 쓸쓸하고 포근한 느낌)

    /// <summary>
    /// 32박 짜리 반복 음악. 낮은 쪽에는 4마디 화음(느린 사인 화음)이 깔리고, 위에는 펜타토닉 선율이 가끔 쉬면서 흐른다.
    /// 소리마다 부드럽게 시작해 천천히 사라지고, 메아리를 살짝 얹는다. 정해진 씨앗으로 만들어서 늘 같은 곡이다.
    /// </summary>
    private static AudioClip Music(string name, bool day)
    {
        float bpm = day ? 74f : 54f;
        float beat = 60f / bpm;
        int beats = 32;
        int total = (int)(Rate * beat * beats);
        var d = new float[total + Rate * 3]; // 끝의 여운이 처음으로 돌아 이어지게 여유를 둔다
        var rnd = new System.Random(day ? 5 : 9);
        int[] scale = day ? DayScale : NightScale;
        float rootHz = day ? 261.63f : 220f; // 도 / 라

        // 화음: 으뜸음, 5도, 3도 (4마디 x 8박). 낮은 옥타브
        int[][] chords = day
            ? new[] { new[] { 0, 7, 16 }, new[] { 9, 16, 24 }, new[] { 5, 12, 21 }, new[] { 7, 14, 23 } }   // I vi IV V
            : new[] { new[] { 0, 7, 15 }, new[] { 8, 15, 20 }, new[] { 5, 12, 20 }, new[] { 7, 14, 22 } };  // i VI iv v
        for (int c = 0; c < 4; c++)
        {
            int start = (int)(Rate * beat * 8 * c);
            int len = (int)(Rate * beat * 9);
            foreach (int semi in chords[c])
            {
                float hz = rootHz * 0.5f * Mathf.Pow(2f, semi / 12f);
                for (int i = 0; i < len && start + i < d.Length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Min(1f, i / (Rate * 1.2f)) * Mathf.Min(1f, (len - i) / (Rate * 1.5f));
                    d[start + i] += Mathf.Sin(2f * Mathf.PI * hz * t) * env * 0.045f;
                }
            }
        }

        // 선율: 음계 위를 한두 칸씩 걷는다
        int degree = 2;
        for (int b = 0; b < beats; )
        {
            if (rnd.NextDouble() < (day ? 0.22 : 0.3)) { b++; continue; } // 쉼표
            int step = rnd.Next(-2, 3);
            degree = Mathf.Clamp(degree + step, 0, scale.Length * 2 - 1);
            int octave = degree / scale.Length;
            int semi = scale[degree % scale.Length] + 12 * octave;
            float hz = rootHz * 2f * Mathf.Pow(2f, semi / 12f);
            int lengthBeats = rnd.NextDouble() < 0.4 ? 2 : 1;
            int start = (int)(Rate * beat * b);
            int len = (int)(Rate * beat * (lengthBeats + 1.2f));
            for (int i = 0; i < len && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, i / 700f) * Mathf.Exp(-t * (day ? 2.6f : 2.0f));
                if (env < 0.002f) break;
                float s = Mathf.Sin(2f * Mathf.PI * hz * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * hz * 2f * t) + 0.08f * Mathf.Sin(2f * Mathf.PI * hz * 3f * t);
                d[start + i] += s * env * 0.16f;
            }
            b += lengthBeats;
        }

        // 메아리 (0.375박 뒤에 작게 두 번)
        int delay = (int)(Rate * beat * 0.75f);
        for (int i = delay; i < d.Length; i++) d[i] += d[i - delay] * 0.28f;

        // 끝의 여운을 처음에 겹쳐서 이어 붙인다
        var loop = new float[total];
        for (int i = 0; i < total; i++) loop[i] = d[i] + (i + total < d.Length ? d[i + total] : 0f);

        // 너무 크지 않게 정리
        float peak = 0.0001f;
        foreach (float v in loop) peak = Mathf.Max(peak, Mathf.Abs(v));
        for (int i = 0; i < total; i++) loop[i] = loop[i] / peak * 0.8f;
        return Make(name, loop);
    }
}
