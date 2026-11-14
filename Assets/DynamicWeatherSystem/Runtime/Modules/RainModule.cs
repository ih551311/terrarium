using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Module that manages rain using a particle system.
    ///
    /// Quick setup:
    ///   1. Add this component as a child of the GameObject that holds WeatherManager.
    ///   2. Assign a followTarget (typically Camera.main — auto-detected if left empty).
    ///   3. In your WeatherStateData assets, set rainIntensity > 0 for rainy states.
    ///
    /// A ParticleSystem is created and configured automatically if none is assigned.
    /// For a custom look, assign a pre-configured ParticleSystem in the Inspector.
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Modules/Rain Module")]
    public class RainModule : WeatherModule
    {
        [Header("References")]
        [Tooltip("Particle system used to render rain. " +
                 "If left empty, one is created and configured automatically at runtime.")]
        [SerializeField] private ParticleSystem rainParticles;

        [Header("Emission Area")]
        [Tooltip("Maximum number of particles per second at 100% intensity.")]
        [SerializeField, Min(10)] private int maxEmissionRate = 400;

        [Tooltip("Height above the follow target from which particles spawn, in world units.")]
        [SerializeField, Min(2f)] private float spawnHeight = 15f;

        [Tooltip("Radius of the square emission area around the follow target.")]
        [SerializeField, Min(5f)] private float spawnRadius = 25f;

        [Header("Camera Follow")]
        [Tooltip("The module emits rain centred on this transform. " +
                 "Automatically set to Camera.main if left empty.")]
        [SerializeField] private Transform followTarget;

        [Header("Look (optional)")]
        [Tooltip("Optional material for the rain drops. If empty, a transparent URP particle " +
                 "material is created at runtime.")]
        [SerializeField] private Material rainMaterial;

        [Header("Wind")]
        [Tooltip("Horizontal speed (m/s) added to the drops at Wind Strength = 1. " +
                 "Only has an effect when a weather state has Wind Strength above 0.")]
        [SerializeField, Min(0f)] private float maxWindSpeed = 6f;

        // --- Cached ParticleSystem sub-modules ---
        // These are proxy structs: assigning values updates the underlying ParticleSystem.
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;
        private ParticleSystem.VelocityOverLifetimeModule _velocityOverLifetime;

        private Material _runtimeMaterial;
        private bool _isReady;
        private Vector3 _windOffset;

        // --- Lifecycle ---

        private void Awake()
        {
            if (rainParticles == null)
                rainParticles = GetComponentInChildren<ParticleSystem>(includeInactive: true);

            if (rainParticles == null)
                rainParticles = CreateParticleSystemObject();

            ConfigureParticleSystem();
            CacheModules();
            _isReady = true;

            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;

            if (followTarget == null)
                Debug.LogWarning("[RainModule] No follow target found. " +
                    "Assign one in the Inspector so rain follows the player camera.", this);
        }

        private void Reset()
        {
            // Auto-detect an existing child ParticleSystem when the component is added
            rainParticles = GetComponentInChildren<ParticleSystem>(includeInactive: true);
        }

        private void LateUpdate()
        {
            if (followTarget == null || !_isReady) return;

            // Move the emitter so it is always centred above the follow target.
            // With World-space simulation, already-emitted particles are unaffected.
            // When wind is blowing, spawn upwind so drops still land around the target.
            var pos  = followTarget.position + _windOffset;
            pos.y   += spawnHeight;
            transform.position = pos;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        // --- WeatherModule ---

        public override void Blend(WeatherStateData from, WeatherStateData to, float t)
        {
            if (!_isReady) return;

            float intensity = Mathf.Lerp(from.rainIntensity, to.rainIntensity, t);
            Color color     = Color.Lerp(from.rainColor,     to.rainColor,     t);
            float speed     = Mathf.Lerp(from.rainSpeed,     to.rainSpeed,     t);
            float windStr   = Mathf.Lerp(from.windStrength,  to.windStrength,  t);
            float windDir   = Mathf.LerpAngle(from.windDirection, to.windDirection, t);

            ApplyRain(intensity, color, speed,
                WeatherParticleUtility.WindVector(windDir, windStr * maxWindSpeed));
        }

        // --- Internal Logic ---

        private void ApplyRain(float intensity, Color color, float speed, Vector3 wind)
        {
            if (intensity <= 0.001f)
            {
                // Fade to zero — existing particles finish their lifecycle naturally
                _emission.rateOverTime = 0;
                return;
            }

            // Fall speed (downward in world space)
            float safeSpeed = Mathf.Max(0.5f, speed);
            _velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(wind.x);
            _velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-safeSpeed);
            _velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(wind.z);
            _windOffset = -wind * (spawnHeight / safeSpeed) * 0.5f;

            // Lifetime: time to travel spawnHeight with a small variation
            _main.startLifetime = new ParticleSystem.MinMaxCurve(
                spawnHeight / safeSpeed,
                spawnHeight / safeSpeed * 1.3f);

            // Color and opacity
            _main.startColor = color;

            // Emission rate proportional to intensity
            _emission.rateOverTime = Mathf.RoundToInt(intensity * maxEmissionRate);

            if (!rainParticles.isPlaying)
                rainParticles.Play();
        }

        private void CacheModules()
        {
            _emission             = rainParticles.emission;
            _main                 = rainParticles.main;
            _velocityOverLifetime = rainParticles.velocityOverLifetime;
        }

        // --- Particle System Configuration ---

        private ParticleSystem CreateParticleSystemObject()
        {
            var go = new GameObject("RainParticles");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go.AddComponent<ParticleSystem>();
        }

        private void ConfigureParticleSystem()
        {
            // --- Main ---
            var main = rainParticles.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            main.startColor      = new Color(0.7f, 0.8f, 0.9f, 0.4f);
            main.gravityModifier = 0f;   // velocity is driven by VelocityOverLifetime
            main.maxParticles    = 3000;
            main.startLifetime   = spawnHeight / 8f;

            // --- Emission: disabled until a state enables it ---
            var emission = rainParticles.emission;
            emission.rateOverTime = 0;

            // --- Shape: flat box centred on the module ---
            var shape = rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(spawnRadius * 2f, 0.5f, spawnRadius * 2f);

            // --- VelocityOverLifetime: constant vertical fall ---
            var vel     = rainParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = 0f;
            vel.y       = new ParticleSystem.MinMaxCurve(-8f);
            vel.z       = 0f;

            // --- Renderer: stretched billboard for rain streak effect ---
            var rend           = rainParticles.GetComponent<ParticleSystemRenderer>();
            rend.renderMode    = ParticleSystemRenderMode.Stretch;
            rend.velocityScale = 0.08f;
            rend.lengthScale   = 1.5f;

            AssignMaterial(rend);
        }

        /// <summary>
        /// Assigns the rain material: the user-provided one if set, otherwise a runtime
        /// alpha-blended URP particle material. If URP is not available, the
        /// ParticleSystem's default material is kept.
        /// </summary>
        private void AssignMaterial(ParticleSystemRenderer rend)
        {
            if (rainMaterial != null)
            {
                rend.sharedMaterial = rainMaterial;
                return;
            }

            _runtimeMaterial = WeatherParticleUtility.CreateTransparentMaterial(rend, "Rain_Runtime");

            if (_runtimeMaterial == null)
            {
                Debug.LogWarning("[RainModule] URP particle shader not found. " +
                    "The default material will be used. Make sure URP is installed.", this);
                return;
            }

            rend.sharedMaterial = _runtimeMaterial;
        }
    }
}
