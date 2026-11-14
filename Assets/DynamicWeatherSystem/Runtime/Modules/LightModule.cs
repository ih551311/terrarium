using UnityEngine;
using UnityEngine.Rendering;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Module that controls the main Directional Light and the scene ambient light.
    ///
    /// Setup:
    ///   - Add this component as a child of the GameObject that holds WeatherManager.
    ///   - Assign the scene's Directional Light in the Inspector.
    ///   - If left unassigned, the module will attempt to find it automatically.
    ///
    /// Ambient light:
    ///   - By default the module switches the scene ambient source to "Color" (Flat) and
    ///     drives it from each state's ambientColor.
    ///   - Disable "Control Ambient Light" if your scene uses Skybox or Gradient ambient
    ///     lighting (or baked lighting) and you don't want the module to change it.
    /// </summary>
    [AddComponentMenu("Dynamic Weather System/Modules/Light Module")]
    public class LightModule : WeatherModule
    {
        [Tooltip("The scene's Directional Light representing the sun or moon.")]
        [SerializeField] private Light directionalLight;

        [Tooltip("When enabled, the scene ambient source is set to Color (Flat) and driven by " +
                 "each state's Ambient Color. Disable it to keep your scene's own ambient settings.")]
        [SerializeField] private bool controlAmbientLight = true;

        private bool _warnedMissingLight;

        /// <summary>The Directional Light driven by this module.</summary>
        public Light DirectionalLight
        {
            get => directionalLight;
            set { directionalLight = value; _warnedMissingLight = false; }
        }

        /// <summary>
        /// Attempts to auto-find the first Directional Light in the scene
        /// when the component is added or reset from the Inspector.
        /// </summary>
        private void Reset()
        {
            directionalLight = FindDirectionalLight();
        }

        private void Awake()
        {
            if (directionalLight == null)
                directionalLight = FindDirectionalLight();
        }

        public override void Blend(WeatherStateData from, WeatherStateData to, float t)
        {
            BlendDirectionalLight(from, to, t);

            if (controlAmbientLight)
                BlendAmbientLight(from, to, t);
        }

        private void BlendDirectionalLight(WeatherStateData from, WeatherStateData to, float t)
        {
            if (directionalLight == null)
            {
                // Warn once instead of every frame during transitions.
                if (!_warnedMissingLight)
                {
                    Debug.LogWarning("[LightModule] No Directional Light assigned. " +
                        "Assign one in the component Inspector.", this);
                    _warnedMissingLight = true;
                }
                return;
            }

            directionalLight.color     = Color.Lerp(from.lightColor,     to.lightColor,     t);
            directionalLight.intensity = Mathf.Lerp(from.lightIntensity, to.lightIntensity, t);
        }

        private static void BlendAmbientLight(WeatherStateData from, WeatherStateData to, float t)
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(from.ambientColor, to.ambientColor, t);
        }

        /// <summary>Returns RenderSettings.sun if set, otherwise the first Directional Light found.</summary>
        public static Light FindDirectionalLight()
        {
            if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional
                && RenderSettings.sun.GetComponentInParent<LightningModule>() == null)
                return RenderSettings.sun;

            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                // Skip the flash light created by the Lightning Module.
                if (l.type == LightType.Directional && l.GetComponentInParent<LightningModule>() == null)
                    return l;
            }
            return null;
        }
    }
}
