using UnityEngine;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Defines all visual and audio parameters that characterize a single weather state.
    /// Create via: Assets > Create > Dynamic Weather System > Weather State
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeatherState_New",
        menuName = "Dynamic Weather System/Weather State",
        order = 0)]
    public class WeatherStateData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Descriptive name for this state. Used in logs and the Manager inspector.")]
        public string stateName = "New State";

        [Header("Fog")]
        [Tooltip("Enables or disables global scene fog when this state is active.")]
        public bool fogEnabled = false;

        [Tooltip("Fog color.")]
        public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("Fog density in Exponential Squared mode. Values above 0.05 are very dense.")]
        [Range(0f, 0.1f)]
        public float fogDensity = 0.02f;

        [Header("Directional Light")]
        [Tooltip("Color of the main directional light (sun or moon).")]
        public Color lightColor = Color.white;

        [Tooltip("Intensity of the main directional light.")]
        [Range(0f, 8f)]
        public float lightIntensity = 1f;

        [Header("Ambient Light")]
        [Tooltip("Scene ambient light color.")]
        public Color ambientColor = new Color(0.2f, 0.2f, 0.25f, 1f);

        [Header("Sky")]
        [Tooltip("Multiplicative tint applied to the skybox material. " +
                 "White = no change. Darken for storms, shift cool for rain.")]
        public Color skyboxTint = Color.white;

        [Tooltip("Skybox exposure (overall brightness). " +
                 "1 = normal. >1 = brighter. <1 = darker.")]
        [Range(0f, 8f)]
        public float skyboxExposure = 1f;

        [Header("Audio")]
        [Tooltip("Looping ambient audio clip for this state. " +
                 "Can be rain, wind, forest ambience, etc. " +
                 "Leave empty for silence.")]
        public AudioClip ambientClip;

        [Tooltip("Target volume for the ambient clip in this state.")]
        [Range(0f, 1f)]
        public float ambientVolume = 0.5f;

        [Header("Rain")]
        [Tooltip("Rain intensity. 0 = no rain, 1 = maximum rain. " +
                 "Transitions between states interpolate this value continuously.")]
        [Range(0f, 1f)]
        public float rainIntensity = 0f;

        [Tooltip("Color and opacity of the rain drops.")]
        public Color rainColor = new Color(0.7f, 0.8f, 0.9f, 0.4f);

        [Tooltip("Downward fall speed of rain drops in metres per second.")]
        [Range(2f, 20f)]
        public float rainSpeed = 8f;

        // ---------------------------------------------------------------
        // Added in 1.1.0. All defaults are "off", so states created with
        // 1.0.x keep looking exactly the same after updating.
        // ---------------------------------------------------------------

        [Header("Snow (requires Snow Module)")]
        [Tooltip("Snow intensity. 0 = no snow, 1 = heavy snowfall.")]
        [Range(0f, 1f)]
        public float snowIntensity = 0f;

        [Tooltip("Color and opacity of the snowflakes.")]
        public Color snowColor = new Color(1f, 1f, 1f, 0.85f);

        [Tooltip("Downward fall speed of snowflakes in metres per second.")]
        [Range(0.3f, 5f)]
        public float snowSpeed = 1.2f;

        [Header("Wind (requires Wind Module for trees; also tilts rain and snow)")]
        [Tooltip("Normalized wind strength. 0 = calm, 1 = strong storm wind.")]
        [Range(0f, 1f)]
        public float windStrength = 0f;

        [Tooltip("Direction the wind blows towards, in degrees. 0 = +Z (forward), 90 = +X (right).")]
        [Range(0f, 360f)]
        public float windDirection = 45f;

        [Header("Lightning (requires Lightning Module)")]
        [Tooltip("Average number of lightning strikes per minute. 0 = no lightning.")]
        [Range(0f, 30f)]
        public float lightningFrequency = 0f;

        /// <summary>
        /// Writes the interpolation between <paramref name="from"/> and <paramref name="to"/>
        /// at <paramref name="t"/> into <paramref name="target"/>.
        /// Used internally to snapshot the visual state when a transition is interrupted.
        /// </summary>
        public static void LerpInto(WeatherStateData target, WeatherStateData from, WeatherStateData to, float t)
        {
            target.lightColor         = Color.Lerp(from.lightColor,       to.lightColor,       t);
            target.lightIntensity     = Mathf.Lerp(from.lightIntensity,   to.lightIntensity,   t);
            target.ambientColor       = Color.Lerp(from.ambientColor,     to.ambientColor,     t);
            target.fogColor           = Color.Lerp(from.fogColor,         to.fogColor,         t);
            target.fogDensity         = Mathf.Lerp(from.fogDensity,       to.fogDensity,       t);
            // Keep fog active if either state had it enabled, preventing a pop on interruption.
            target.fogEnabled         = from.fogEnabled || to.fogEnabled;
            target.rainIntensity      = Mathf.Lerp(from.rainIntensity,    to.rainIntensity,    t);
            target.rainColor          = Color.Lerp(from.rainColor,        to.rainColor,        t);
            target.rainSpeed          = Mathf.Lerp(from.rainSpeed,        to.rainSpeed,        t);
            target.skyboxTint         = Color.Lerp(from.skyboxTint,       to.skyboxTint,       t);
            target.skyboxExposure     = Mathf.Lerp(from.skyboxExposure,   to.skyboxExposure,   t);
            // Use the dominant clip based on transition progress.
            target.ambientClip        = t < 0.5f ? from.ambientClip : to.ambientClip;
            target.ambientVolume      = Mathf.Lerp(from.ambientVolume,    to.ambientVolume,    t);
            target.snowIntensity      = Mathf.Lerp(from.snowIntensity,    to.snowIntensity,    t);
            target.snowColor          = Color.Lerp(from.snowColor,        to.snowColor,        t);
            target.snowSpeed          = Mathf.Lerp(from.snowSpeed,        to.snowSpeed,        t);
            target.windStrength       = Mathf.Lerp(from.windStrength,     to.windStrength,     t);
            target.windDirection      = Mathf.Repeat(Mathf.LerpAngle(from.windDirection, to.windDirection, t), 360f);
            target.lightningFrequency = Mathf.Lerp(from.lightningFrequency, to.lightningFrequency, t);
        }
    }
}
