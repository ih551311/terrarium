using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Module that renders snowfall using a particle system.
    ///
    /// Quick setup:
    ///   1. Add this component as a child of the GameObject that holds WeatherManager.
    ///   2. A followTarget is auto-detected (Camera.main) if left empty.
    ///   3. In your WeatherStateData assets, set snowIntensity > 0 for snowy states.
    ///
    /// A ParticleSystem and a soft snowflake material are created automatically at runtime,
    /// so no textures or extra assets are required. Assign your own material for a custom look.
    /// Snowflakes drift with the state's wind settings.
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Modules/Snow Module")]
    public class SnowModule : WeatherModule
    {
        [Header("References")]
        [Tooltip("Particle system used to render snow. " +
                 "If left empty, one is created and configured automatically at runtime.")]
        [SerializeField] private ParticleSystem snowParticles;

        [Tooltip("Optional material for the snowflakes. If empty, a soft round flake " +
                 "material is generated at runtime.")]
        [SerializeField] private Material snowMaterial;

        [Header("Emission Area")]
        [Tooltip("Maximum number of particles per second at 100% intensity.")]
        [SerializeField, Min(10)] private int maxEmissionRate = 700;

        [Tooltip("Height above the follow target from which flakes spawn, in world units.")]
        [SerializeField, Min(2f)] private float spawnHeight = 12f;

        [Tooltip("Radius of the square emission area around the follow target.")]
        [SerializeField, Min(5f)] private float spawnRadius = 22f;

        [Header("Flakes")]
        [Tooltip("Minimum and maximum snowflake size in world units.")]
        [SerializeField] private Vector2 flakeSize = new Vector2(0.08f, 0.2f);

        [Tooltip("How much flakes flutter sideways while falling.")]
        [SerializeField, Range(0f, 2f)] private float flutter = 0.6f;

        [Header("Wind")]
        [Tooltip("Horizontal drift speed (m/s) at Wind Strength = 1.")]
        [SerializeField, Min(0f)] private float maxWindSpeed = 4f;

        [Header("Camera Follow")]
        [Tooltip("The module emits snow centred on this transform. " +
                 "Automatically set to Camera.main if left empty.")]
        [SerializeField] private Transform followTarget;

        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.MainModule _main;
        private ParticleSystem.VelocityOverLifetimeModule _velocity;

        private Material  _runtimeMaterial;
        private Texture2D _runtimeTexture;
        private Vector3   _windOffset;
        private bool      _isReady;

        /// <summary>Transform the snow follows (usually the main camera).</summary>
        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        // --- Lifecycle ---

        private void Awake()
        {
            if (snowParticles == null)
                snowParticles = GetComponentInChildren<ParticleSystem>(includeInactive: true);

            if (snowParticles == null)
                snowParticles = CreateParticleSystemObject();

            ConfigureParticleSystem();

            _emission = snowParticles.emission;
            _main     = snowParticles.main;
            _velocity = snowParticles.velocityOverLifetime;
            _isReady  = true;

            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (!_isReady) return;

            if (followTarget == null && Camera.main != null)
                followTarget = Camera.main.transform;

            if (followTarget == null) return;

            var pos = followTarget.position + _windOffset;
            pos.y  += spawnHeight;
            transform.position = pos;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
            if (_runtimeTexture  != null) Destroy(_runtimeTexture);
        }

        // --- WeatherModule ---

        public override void Blend(WeatherStateData from, WeatherStateData to, float t)
        {
            if (!_isReady) return;

            float intensity = Mathf.Lerp(from.snowIntensity, to.snowIntensity, t);

            if (intensity <= 0.001f)
            {
                // Existing flakes finish falling naturally.
                _emission.rateOverTime = 0;
                return;
            }

            float speed   = Mathf.Max(0.1f, Mathf.Lerp(from.snowSpeed, to.snowSpeed, t));
            float windStr = Mathf.Lerp(from.windStrength, to.windStrength, t);
            float windDir = Mathf.LerpAngle(from.windDirection, to.windDirection, t);
            var   wind    = WeatherParticleUtility.WindVector(windDir, windStr * maxWindSpeed);

            float lifetime = spawnHeight / speed;

            _velocity.x = new ParticleSystem.MinMaxCurve(wind.x);
            _velocity.y = new ParticleSystem.MinMaxCurve(-speed);
            _velocity.z = new ParticleSystem.MinMaxCurve(wind.z);
            _windOffset = -wind * lifetime * 0.5f;

            _main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime, lifetime * 1.25f);
            _main.startColor    = Color.Lerp(from.snowColor, to.snowColor, t);
            _emission.rateOverTime = Mathf.RoundToInt(intensity * maxEmissionRate);

            if (!snowParticles.isPlaying)
                snowParticles.Play();
        }

        // --- Particle System Configuration ---

        private ParticleSystem CreateParticleSystemObject()
        {
            var go = new GameObject("SnowParticles");
            go.transform.SetParent(transform, false);
            return go.AddComponent<ParticleSystem>();
        }

        private void ConfigureParticleSystem()
        {
            // A ParticleSystem added at runtime may start playing immediately.
            snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = snowParticles.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.001f, flakeSize.x), Mathf.Max(flakeSize.x, flakeSize.y));
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor      = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = 0f;
            main.maxParticles    = 6000;
            main.startLifetime   = spawnHeight / 1.2f;

            var emission = snowParticles.emission;
            emission.rateOverTime = 0;

            var shape = snowParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(spawnRadius * 2f, 0.5f, spawnRadius * 2f);

            var vel     = snowParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = 0f;
            vel.y       = new ParticleSystem.MinMaxCurve(-1.2f);
            vel.z       = 0f;

            // Gentle turbulence makes flakes flutter instead of falling in straight lines.
            var noise = snowParticles.noise;
            noise.enabled     = flutter > 0f;
            noise.strength    = flutter;
            noise.frequency   = 0.35f;
            noise.scrollSpeed = 0.2f;
            noise.damping     = true;
            noise.quality     = ParticleSystemNoiseQuality.Low;

            var rend        = snowParticles.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            AssignMaterial(rend);
        }

        private void AssignMaterial(ParticleSystemRenderer rend)
        {
            if (snowMaterial != null)
            {
                rend.sharedMaterial = snowMaterial;
                return;
            }

            _runtimeMaterial = WeatherParticleUtility.CreateTransparentMaterial(rend, "Snow_Runtime");

            if (_runtimeMaterial == null)
            {
                Debug.LogWarning("[SnowModule] URP particle shader not found. " +
                    "The default particle material will be used.", this);
                return;
            }

            _runtimeTexture = WeatherParticleUtility.CreateSoftDotTexture();
            WeatherParticleUtility.SetTexture(_runtimeMaterial, _runtimeTexture);
            rend.sharedMaterial = _runtimeMaterial;
        }
    }
}
