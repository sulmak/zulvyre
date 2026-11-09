using UnityEngine;

namespace Zulvyre
{
    /// <summary>
    /// The atmospheric side of the world: one procedural sky dome plus the per-region
    /// mood (fog, ambient, sun) that tints it. Moods are blended at runtime so walking
    /// from Alderwick into Nocturne actually changes the light.
    /// </summary>
    public static class WorldStage
    {
        public struct Mood
        {
            public Color SkyTint;
            public Color Horizon;
            public Color SunColor;
            public float SunIntensity;
            public float ShadowStrength;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public float FogStart;
            public float FogEnd;
        }

        public static readonly Mood Greywood = new Mood
        {
            SkyTint = new Color(0.98f, 0.92f, 0.86f),
            Horizon = new Color(0.50f, 0.44f, 0.43f),
            SunColor = new Color(1f, 0.88f, 0.72f),
            SunIntensity = 1.42f,
            ShadowStrength = 0.62f,
            AmbientSky = new Color(0.40f, 0.47f, 0.58f),
            AmbientEquator = new Color(0.30f, 0.32f, 0.33f),
            AmbientGround = new Color(0.17f, 0.16f, 0.13f),
            FogStart = 60f,
            FogEnd = 270f
        };

        public static readonly Mood Alderwick = new Mood
        {
            SkyTint = new Color(1f, 0.94f, 0.84f),
            Horizon = new Color(0.58f, 0.49f, 0.42f),
            SunColor = new Color(1f, 0.9f, 0.7f),
            SunIntensity = 1.55f,
            ShadowStrength = 0.66f,
            AmbientSky = new Color(0.46f, 0.5f, 0.58f),
            AmbientEquator = new Color(0.34f, 0.33f, 0.3f),
            AmbientGround = new Color(0.20f, 0.17f, 0.12f),
            FogStart = 62f,
            FogEnd = 250f
        };

        public static readonly Mood Durnmar = new Mood
        {
            SkyTint = new Color(0.96f, 0.8f, 0.64f),
            Horizon = new Color(0.47f, 0.35f, 0.28f),
            SunColor = new Color(1f, 0.74f, 0.5f),
            SunIntensity = 1.22f,
            ShadowStrength = 0.55f,
            AmbientSky = new Color(0.4f, 0.36f, 0.36f),
            AmbientEquator = new Color(0.3f, 0.25f, 0.2f),
            AmbientGround = new Color(0.16f, 0.12f, 0.09f),
            FogStart = 48f,
            FogEnd = 215f
        };

        public static readonly Mood Nocturne = new Mood
        {
            SkyTint = new Color(0.6f, 0.62f, 0.92f),
            Horizon = new Color(0.23f, 0.21f, 0.32f),
            SunColor = new Color(0.66f, 0.72f, 1f),
            SunIntensity = 0.62f,
            ShadowStrength = 0.7f,
            AmbientSky = new Color(0.24f, 0.25f, 0.42f),
            AmbientEquator = new Color(0.19f, 0.18f, 0.26f),
            AmbientGround = new Color(0.1f, 0.09f, 0.14f),
            FogStart = 38f,
            FogEnd = 190f
        };

        public static readonly Mood Crypt = new Mood
        {
            SkyTint = new Color(0.34f, 0.35f, 0.4f),
            Horizon = new Color(0.07f, 0.075f, 0.09f),
            SunColor = new Color(0.7f, 0.66f, 0.6f),
            SunIntensity = 0.24f,
            ShadowStrength = 0.5f,
            AmbientSky = new Color(0.16f, 0.16f, 0.2f),
            AmbientEquator = new Color(0.12f, 0.12f, 0.15f),
            AmbientGround = new Color(0.07f, 0.07f, 0.09f),
            FogStart = 9f,
            FogEnd = 72f
        };

        public static Mood Lerp(Mood a, Mood b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Mood
            {
                SkyTint = Color.Lerp(a.SkyTint, b.SkyTint, t),
                Horizon = Color.Lerp(a.Horizon, b.Horizon, t),
                SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
                SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
                ShadowStrength = Mathf.Lerp(a.ShadowStrength, b.ShadowStrength, t),
                AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t),
                AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t),
                AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t),
                FogStart = Mathf.Lerp(a.FogStart, b.FogStart, t),
                FogEnd = Mathf.Lerp(a.FogEnd, b.FogEnd, t)
            };
        }

        public static void Apply(Mood mood, Camera cam, Light sun, Material sky)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = mood.Horizon;
            RenderSettings.fogStartDistance = mood.FogStart;
            RenderSettings.fogEndDistance = mood.FogEnd;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = mood.AmbientSky;
            RenderSettings.ambientEquatorColor = mood.AmbientEquator;
            RenderSettings.ambientGroundColor = mood.AmbientGround;

            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = mood.Horizon;
            }

            if (sun != null)
            {
                sun.color = mood.SunColor;
                sun.intensity = mood.SunIntensity;
                sun.shadowStrength = mood.ShadowStrength;
            }

            if (sky != null) sky.color = mood.SkyTint;
        }

        /// <summary>Inside-out sphere that follows the camera. Unlit, so fog never eats the horizon.</summary>
        public static GameObject BuildSky(Transform parent, float yaw, out Material skyMaterial)
        {
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Sky Dome";
            dome.transform.SetParent(parent, false);
            dome.transform.localScale = new Vector3(SkyRadius, SkyRadius, -SkyRadius);
            dome.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Object.Destroy(dome.GetComponent<Collider>());

            var renderer = dome.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            Shader skyShader = Resources.Load<Shader>("ZulvyreSky");
            if (skyShader == null) skyShader = Shader.Find("Unlit/Texture");
            if (skyShader == null) skyShader = Shader.Find("Hidden/InternalErrorShader");
            var material = new Material(skyShader);
            material.name = "sky-dome";
            material.mainTexture = SkyTexture();
            material.color = Greywood.SkyTint;
            renderer.material = material;

            skyMaterial = material;
            return dome;
        }

        public const float SkyRadius = 600f;

        static Texture2D CachedSky;

        static Texture2D SkyTexture()
        {
            if (CachedSky != null) return CachedSky;

            const int size = 512;
            var pixels = new Color32[size * size];

            Color zenith = new Color(0.47f, 0.58f, 0.78f);
            Color mid = new Color(0.70f, 0.72f, 0.78f);
            Color horizon = new Color(0.96f, 0.85f, 0.71f);
            Color under = new Color(0.40f, 0.38f, 0.38f);
            Color cloudLight = new Color(0.99f, 0.96f, 0.92f);
            Color cloudDark = new Color(0.52f, 0.5f, 0.54f);
            Color sunCore = new Color(1.6f, 1.35f, 1.05f);

            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                float latitude = (v - 0.5f) * 2f;

                Color sky;
                if (latitude < 0f)
                {
                    sky = Color.Lerp(horizon, under, Mathf.Clamp01(-latitude * 2.4f));
                }
                else
                {
                    float up = Mathf.Pow(latitude, 0.62f);
                    sky = Color.Lerp(horizon, mid, Mathf.Clamp01(up * 1.9f));
                    sky = Color.Lerp(sky, zenith, Mathf.Clamp01((up - 0.35f) * 1.7f));
                }

                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    Color c = sky;

                    if (latitude > 0.02f)
                    {
                        float band = Mathf.Clamp01(1f - Mathf.Abs(latitude - 0.34f) * 2.6f);
                        float shape = SurfaceAssets.Fbm(u * 1.6f, v * 3.2f, 6, 4, 21);
                        float wisp = SurfaceAssets.Fbm(u * 6f, v * 9f, 14, 2, 41);
                        float coverage = Mathf.Clamp01((shape * 0.7f + wisp * 0.3f - 0.42f) * 2.6f) * band;
                        c = Color.Lerp(c, Color.Lerp(cloudDark, cloudLight, shape * 0.8f + 0.2f), coverage * 0.85f);
                    }

                    float du = u - 0.5f;
                    float dv = v - 0.735f;
                    float d = Mathf.Sqrt(du * du * 0.55f + dv * dv);
                    float glow = Mathf.Exp(-d * 6.5f) + Mathf.Exp(-d * 2.1f) * 0.22f;
                    c = Color.Lerp(c, sunCore, Mathf.Clamp01(glow * 0.85f));

                    if (latitude > 0.25f)
                    {
                        float star = SurfaceAssets.Fbm(u, v, 190, 1, 77);
                        if (star > 0.955f)
                        {
                            float twinkle = (star - 0.955f) / 0.045f * Mathf.Clamp01((latitude - 0.25f) * 2.2f);
                            c = Color.Lerp(c, Color.white, Mathf.Clamp01(twinkle));
                        }
                    }

                    pixels[y * size + x] = To32(c);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "sky-gradient";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(true, false);

            CachedSky = texture;
            return texture;
        }

        static Color32 To32(Color c)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255),
                255);
        }
    }
}
