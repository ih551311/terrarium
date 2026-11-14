using UnityEngine;
using UnityEngine.Rendering;

namespace DynamicWeatherSystem
{
    /// <summary>
    /// Shared helpers for particle-based weather modules (rain, snow).
    /// Internal to the package: not part of the public API.
    /// </summary>
    internal static class WeatherParticleUtility
    {
        private const string UrpParticleUnlit = "Universal Render Pipeline/Particles/Unlit";

        private static readonly int SurfaceId  = Shader.PropertyToID("_Surface");
        private static readonly int BlendId    = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId   = Shader.PropertyToID("_ZWrite");
        private static readonly int BaseMapId  = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId  = Shader.PropertyToID("_MainTex");

        /// <summary>
        /// Creates a runtime alpha-blended particle material.
        /// Order of preference:
        ///   1. URP "Particles/Unlit" shader (found via Shader.Find).
        ///   2. A copy of the renderer's current material (the pipeline default particle
        ///      material, which is always included in builds).
        /// Returns null if neither is available.
        /// </summary>
        public static Material CreateTransparentMaterial(ParticleSystemRenderer rend, string name)
        {
            Material mat = null;
            var shader = Shader.Find(UrpParticleUnlit);

            if (shader != null && shader.isSupported)
                mat = new Material(shader);
            else if (rend != null && rend.sharedMaterial != null)
                mat = new Material(rend.sharedMaterial);

            if (mat == null) return null;

            mat.name = name;
            MakeAlphaBlended(mat);
            return mat;
        }

        /// <summary>
        /// Configures a URP particle material for standard alpha blending.
        /// Setting _Surface alone is not enough at runtime: the blend factors are normally
        /// written by the material inspector, so they are set explicitly here.
        /// </summary>
        public static void MakeAlphaBlended(Material mat)
        {
            SetIfPresent(mat, SurfaceId, 1f);   // Transparent
            SetIfPresent(mat, BlendId,   0f);   // Alpha
            SetIfPresent(mat, SrcBlendId, (float)BlendMode.SrcAlpha);
            SetIfPresent(mat, DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(mat, SrcBlendAlphaId, (float)BlendMode.One);
            SetIfPresent(mat, DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            SetIfPresent(mat, ZWriteId, 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>Assigns a texture to _BaseMap (URP) and _MainTex (legacy) when present.</summary>
        public static void SetTexture(Material mat, Texture tex)
        {
            if (mat.HasProperty(BaseMapId)) mat.SetTexture(BaseMapId, tex);
            if (mat.HasProperty(MainTexId)) mat.SetTexture(MainTexId, tex);
        }

        /// <summary>
        /// Generates a small soft round texture (white, radial alpha falloff).
        /// Used for snowflakes so the package needs no texture asset.
        /// </summary>
        public static Texture2D CreateSoftDotTexture(int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name       = "DWS_SoftDot_Runtime",
                wrapMode   = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float half = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);   // smoothstep falloff
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>
        /// Converts a wind direction (degrees, 0 = +Z / north, clockwise) and a strength
        /// into a horizontal world-space vector.
        /// </summary>
        public static Vector3 WindVector(float directionDegrees, float strength)
        {
            float rad = directionDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * strength;
        }

        private static void SetIfPresent(Material mat, int id, float value)
        {
            if (mat.HasProperty(id)) mat.SetFloat(id, value);
        }
    }
}
