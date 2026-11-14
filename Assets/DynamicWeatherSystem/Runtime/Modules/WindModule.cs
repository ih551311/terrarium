using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Module that drives a Unity WindZone from each state's wind settings.
    /// The WindZone moves terrain trees, SpeedTree vegetation and any particle system
    /// that uses the "External Forces" module.
    ///
    /// Setup:
    ///   - Add this component as a child of the GameObject that holds WeatherManager.
    ///   - Assign a WindZone, or leave it empty and one is created automatically.
    ///
    /// Rain and Snow modules read the same wind values directly from the weather state,
    /// so they tilt and drift even without this module.
    ///
    /// Gameplay code can read <see cref="WindVector"/> to push cloth, flags, boats, etc.
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Modules/Wind Module")]
    public class WindModule : WeatherModule
    {
        [Tooltip("WindZone driven by this module. If empty, a directional WindZone is created at runtime.")]
        [SerializeField] private WindZone windZone;

        [Header("Mapping (Wind Strength 0 → 1)")]
        [Tooltip("WindZone Main value at Wind Strength = 0 (calm). A small value keeps trees subtly alive.")]
        [SerializeField, Min(0f)] private float calmWindMain = 0.15f;

        [Tooltip("WindZone Main value at Wind Strength = 1 (storm).")]
        [SerializeField, Min(0f)] private float stormWindMain = 2.5f;

        [Tooltip("WindZone Turbulence value at Wind Strength = 1.")]
        [SerializeField, Min(0f)] private float stormTurbulence = 1.5f;

        [Tooltip("Gust (pulse) magnitude at Wind Strength = 1.")]
        [SerializeField, Min(0f)] private float stormPulseMagnitude = 1f;

        [Tooltip("Gust (pulse) frequency at Wind Strength = 1.")]
        [SerializeField, Min(0f)] private float stormPulseFrequency = 0.5f;

        private float _strength;
        private float _direction;

        /// <summary>Current normalized wind strength [0, 1].</summary>
        public float Strength => _strength;

        /// <summary>Current wind direction in degrees (0 = +Z, 90 = +X).</summary>
        public float Direction => _direction;

        /// <summary>Current horizontal wind as a world-space vector scaled by strength [0, 1].</summary>
        public Vector3 WindVector => WeatherParticleUtility.WindVector(_direction, _strength);

        /// <summary>The WindZone driven by this module.</summary>
        public WindZone WindZone => windZone;

        private void Reset()
        {
            windZone = GetComponentInChildren<WindZone>();
        }

        private void Awake()
        {
            if (windZone == null)
                windZone = GetComponentInChildren<WindZone>();

            if (windZone == null)
            {
                var go = new GameObject("WindZone");
                go.transform.SetParent(transform, false);
                windZone = go.AddComponent<WindZone>();
            }

            windZone.mode = WindZoneMode.Directional;
        }

        public override void Blend(WeatherStateData from, WeatherStateData to, float t)
        {
            _strength  = Mathf.Lerp(from.windStrength, to.windStrength, t);
            _direction = Mathf.Repeat(Mathf.LerpAngle(from.windDirection, to.windDirection, t), 360f);

            if (windZone == null) return;

            windZone.transform.rotation = Quaternion.Euler(0f, _direction, 0f);
            windZone.windMain           = Mathf.Lerp(calmWindMain, stormWindMain, _strength);
            windZone.windTurbulence     = stormTurbulence     * _strength;
            windZone.windPulseMagnitude = stormPulseMagnitude * _strength;
            windZone.windPulseFrequency = stormPulseFrequency * _strength;
        }
    }
}
