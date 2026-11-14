using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Module that produces lightning flashes and delayed thunder during stormy states.
    ///
    /// Setup:
    ///   - Add this component as a child of the GameObject that holds WeatherManager.
    ///   - Set lightningFrequency > 0 (strikes per minute) in the states that should have lightning.
    ///
    /// The flash uses its own Directional Light (created automatically) so it never fights
    /// with the Light Module. If no thunder clips are assigned, a procedural thunder sound
    /// is generated at startup, so the module works with zero extra assets.
    ///
    /// Use <see cref="Strike"/> to trigger a strike from gameplay code or a cutscene.
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Modules/Lightning Module")]
    public class LightningModule : WeatherModule
    {
        [Header("Flash")]
        [Tooltip("Color of the lightning flash light.")]
        [SerializeField] private Color flashColor = new Color(0.82f, 0.88f, 1f);

        [Tooltip("Peak intensity of the flash light.")]
        [SerializeField, Range(0f, 10f)] private float flashIntensity = 3f;

        [Tooltip("Total duration of one strike's flicker, in seconds.")]
        [SerializeField, Range(0.1f, 1.5f)] private float flashDuration = 0.45f;

        [Tooltip("Optional light used for the flash. If empty, a Directional Light is created " +
                 "with the same rotation as the scene's sun.")]
        [SerializeField] private Light flashLight;

        [Tooltip("Minimum time between two strikes, in seconds, regardless of frequency.")]
        [SerializeField, Min(0.2f)] private float minInterval = 2.5f;

        [Header("Thunder")]
        [Tooltip("Thunder clips. One is chosen at random per strike. " +
                 "Leave empty to use a procedurally generated thunder sound.")]
        [SerializeField] private AudioClip[] thunderClips = new AudioClip[0];

        [Tooltip("Generate a procedural thunder sound when no clips are assigned.")]
        [SerializeField] private bool generateThunderIfEmpty = true;

        [Tooltip("Thunder volume.")]
        [SerializeField, Range(0f, 1f)] private float thunderVolume = 0.8f;

        [Tooltip("Random delay range (seconds) between the flash and the thunder. " +
                 "Longer delays feel like more distant strikes.")]
        [SerializeField] private Vector2 thunderDelay = new Vector2(0.3f, 2.5f);

        // Flicker envelope: pairs of (time 0..1, brightness 0..1).
        private static readonly Vector2[] FlickerPattern =
        {
            new Vector2(0.00f, 0.0f), new Vector2(0.04f, 1.0f), new Vector2(0.12f, 0.15f),
            new Vector2(0.20f, 0.75f), new Vector2(0.30f, 0.10f), new Vector2(0.38f, 0.45f),
            new Vector2(1.00f, 0.0f)
        };

        private float _frequency;          // strikes per minute, blended
        private float _nextStrikeTime = -1f;
        private float _flashStart = -1f;
        private float _flashScale = 1f;
        private float _thunderTime = -1f;
        private float _thunderVolumeScale = 1f;

        private AudioSource _thunderSource;
        private AudioClip   _proceduralThunder;
        private bool        _createdFlashLight;

        /// <summary>Raised at the moment of each lightning flash.</summary>
        public event System.Action Struck;

        /// <summary>Current lightning frequency in strikes per minute.</summary>
        public float Frequency => _frequency;

        // --- Lifecycle ---

        private void Awake()
        {
            if (flashLight == null)
            {
                var go = new GameObject("LightningFlash");
                go.transform.SetParent(transform, false);

                var sun = LightModule.FindDirectionalLight();
                go.transform.rotation = sun != null
                    ? sun.transform.rotation
                    : Quaternion.Euler(55f, -30f, 0f);

                flashLight = go.AddComponent<Light>();
                flashLight.type    = LightType.Directional;
                flashLight.shadows = LightShadows.None;
                _createdFlashLight = true;
            }

            flashLight.color     = flashColor;
            flashLight.intensity = 0f;
            flashLight.enabled   = false;

            var audioGo = new GameObject("ThunderAudio");
            audioGo.transform.SetParent(transform, false);
            _thunderSource              = audioGo.AddComponent<AudioSource>();
            _thunderSource.playOnAwake  = false;
            _thunderSource.loop         = false;
            _thunderSource.spatialBlend = 0f;

            if ((thunderClips == null || thunderClips.Length == 0) && generateThunderIfEmpty)
                _proceduralThunder = ProceduralThunder.Create();
        }

        private void OnDisable()
        {
            if (flashLight != null)
            {
                flashLight.intensity = 0f;
                flashLight.enabled   = false;
            }
            _flashStart  = -1f;
            _thunderTime = -1f;
        }

        private void OnDestroy()
        {
            if (_proceduralThunder != null) Destroy(_proceduralThunder);
            if (_createdFlashLight && flashLight != null) Destroy(flashLight.gameObject);
        }

        private void Update()
        {
            float now = Time.time;

            // Automatic strikes
            if (_frequency > 0.01f)
            {
                if (_nextStrikeTime < 0f)
                    ScheduleNextStrike(now);
                else if (now >= _nextStrikeTime)
                {
                    Strike();
                    ScheduleNextStrike(now);
                }
            }
            else
            {
                _nextStrikeTime = -1f;
            }

            UpdateFlash(now);

            if (_thunderTime >= 0f && now >= _thunderTime)
            {
                PlayThunder();
                _thunderTime = -1f;
            }
        }

        // --- WeatherModule ---

        public override void Blend(WeatherStateData from, WeatherStateData to, float t)
        {
            _frequency = Mathf.Lerp(from.lightningFrequency, to.lightningFrequency, t);
        }

        // --- Public API ---

        /// <summary>
        /// Triggers a lightning strike immediately (flash + delayed thunder),
        /// regardless of the current weather state.
        /// </summary>
        /// <param name="strength">Brightness/loudness multiplier, 0..1.</param>
        public void Strike(float strength = 1f)
        {
            strength = Mathf.Clamp01(strength);

            _flashStart = Time.time;
            _flashScale = Random.Range(0.6f, 1f) * strength;

            float delay = Random.Range(Mathf.Min(thunderDelay.x, thunderDelay.y),
                                       Mathf.Max(thunderDelay.x, thunderDelay.y));
            _thunderTime = Time.time + Mathf.Max(0f, delay);
            // Distant strikes (longer delay) sound quieter.
            _thunderVolumeScale = Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(thunderDelay.x, thunderDelay.y, delay)) * strength;

            Struck?.Invoke();
        }

        // --- Internal Logic ---

        private void ScheduleNextStrike(float now)
        {
            // Exponential distribution gives natural, irregular spacing.
            float mean = 60f / Mathf.Max(0.01f, _frequency);
            float wait = -Mathf.Log(1f - Random.value * 0.999f) * mean;
            _nextStrikeTime = now + Mathf.Max(minInterval, wait);
        }

        private void UpdateFlash(float now)
        {
            if (flashLight == null) return;

            if (_flashStart < 0f)
            {
                if (flashLight.enabled)
                {
                    flashLight.intensity = 0f;
                    flashLight.enabled   = false;
                }
                return;
            }

            float u = (now - _flashStart) / Mathf.Max(0.01f, flashDuration);

            if (u >= 1f)
            {
                _flashStart = -1f;
                flashLight.intensity = 0f;
                flashLight.enabled   = false;
                return;
            }

            flashLight.enabled   = true;
            flashLight.color     = flashColor;
            flashLight.intensity = EvaluateFlicker(u) * flashIntensity * _flashScale;
        }

        private static float EvaluateFlicker(float u)
        {
            for (int i = 1; i < FlickerPattern.Length; i++)
            {
                var a = FlickerPattern[i - 1];
                var b = FlickerPattern[i];
                if (u <= b.x)
                    return Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, u));
            }
            return 0f;
        }

        private void PlayThunder()
        {
            AudioClip clip = null;

            if (thunderClips != null && thunderClips.Length > 0)
                clip = thunderClips[Random.Range(0, thunderClips.Length)];

            if (clip == null)
                clip = _proceduralThunder;

            if (clip == null || _thunderSource == null) return;

            _thunderSource.pitch = Random.Range(0.85f, 1.1f);
            _thunderSource.PlayOneShot(clip, thunderVolume * _thunderVolumeScale);
        }

        /// <summary>Generates a rumbling thunder clip from filtered noise.</summary>
        private static class ProceduralThunder
        {
            public static AudioClip Create()
            {
                const int   sampleRate = 22050;
                const float length     = 4.5f;
                int count = Mathf.CeilToInt(sampleRate * length);
                var data  = new float[count];
                var rng   = new System.Random(1234);

                float brown = 0f, low = 0f, peak = 0f;

                // A few random "rolls" that swell and fade over the rumble.
                const int rolls = 4;
                var rollTime = new float[rolls];
                var rollAmp  = new float[rolls];
                for (int r = 0; r < rolls; r++)
                {
                    rollTime[r] = 0.3f + (float)rng.NextDouble() * 2.5f;
                    rollAmp[r]  = 0.4f + (float)rng.NextDouble() * 0.6f;
                }

                for (int i = 0; i < count; i++)
                {
                    float time  = i / (float)sampleRate;
                    float white = (float)(rng.NextDouble() * 2.0 - 1.0);

                    // Brown noise (leaky integrator) for the deep rumble
                    brown = (brown + white * 0.02f) * 0.998f;
                    // Extra low-pass smoothing
                    low += (brown - low) * 0.08f;

                    // Initial crack: short bright noise burst
                    float crack = time < 0.18f ? white * (1f - time / 0.18f) * 0.35f : 0f;

                    // Rumble envelope: quick attack, long decay, plus random rolls
                    float env = Mathf.Clamp01(time / 0.08f) * Mathf.Exp(-time * 0.9f);
                    for (int r = 0; r < rolls; r++)
                    {
                        float d = (time - rollTime[r]) / 0.45f;
                        env += rollAmp[r] * Mathf.Exp(-d * d) * Mathf.Exp(-time * 0.5f);
                    }

                    // Fade out the tail to avoid a click
                    float tail = Mathf.Clamp01((length - time) / 0.6f);

                    float s = (low * 9f * env + crack) * tail;
                    data[i] = s;
                    peak = Mathf.Max(peak, Mathf.Abs(s));
                }

                // Normalize to ~0.9 peak
                float gain = peak > 0f ? 0.9f / peak : 1f;
                for (int i = 0; i < count; i++) data[i] *= gain;

                var clip = AudioClip.Create("DWS_Thunder_Procedural", count, 1, sampleRate, false);
                clip.SetData(data, 0);
                return clip;
            }
        }
    }
}
