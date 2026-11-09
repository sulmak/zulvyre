using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zulvyre
{
    public static class ImageAssets
    {
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        static Texture2D white;
        static Texture2D dot;
        static Texture2D shadow;
        static Texture2D ring;
        static Texture2D rune;
        static Texture2D vignette;
        static Sprite dotSprite;
        static Sprite shadowSprite;
        static Sprite ringSprite;
        static Sprite runeSprite;

        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    white.SetPixel(0, 0, Color.white);
                    white.Apply();
                }
                return white;
            }
        }

        public static Texture2D Dot
        {
            get
            {
                if (dot == null)
                {
                    const int size = 128;
                    dot = NewTexture(size, size, "Soft Dot");
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                            float a = Mathf.Pow(1f - r, 2.6f) + Mathf.Pow(1f - r, 8f) * 0.6f;
                            a = Mathf.Clamp01(a);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                        }
                    }
                    dot.SetPixels32(pixels);
                    dot.Apply();
                }
                return dot;
            }
        }

        public static Texture2D ShadowTexture
        {
            get
            {
                if (shadow == null)
                {
                    const int w = 256;
                    const int h = 128;
                    shadow = NewTexture(w, h, "Ground Shadow");
                    var pixels = new Color32[w * h];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float dx = ((x + 0.5f) / w * 2f - 1f) / 0.94f;
                            float dy = ((y + 0.5f) / h * 2f - 1f) / 0.72f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
                            pixels[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 210));
                        }
                    }
                    shadow.SetPixels32(pixels);
                    shadow.Apply();
                }
                return shadow;
            }
        }

        public static Texture2D RingTexture
        {
            get
            {
                if (ring == null)
                {
                    const int size = 256;
                    ring = NewTexture(size, size, "Rune Ring");
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.05f);
                            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.16f) * 0.22f;
                            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.73f) / 0.012f) * 0.45f;
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                        }
                    }
                    ring.SetPixels32(pixels);
                    ring.Apply();
                }
                return ring;
            }
        }

        public static Texture2D RuneTexture
        {
            get
            {
                if (rune == null)
                {
                    const int size = 256;
                    rune = NewTexture(size, size, "Duel Sigil");
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.03f) * 0.8f;
                            a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.66f) / 0.012f) * 0.55f;

                            float angle = Mathf.Atan2(dy, dx);
                            float ticks = (angle + Mathf.PI) / (Mathf.PI * 2f) * 12f;
                            float tickDistance = Mathf.Abs(ticks - Mathf.Round(ticks));
                            if (tickDistance < 0.07f && r > 0.7f && r < 0.8f)
                            {
                                a += 0.7f;
                            }

                            float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                            if (Mathf.Abs(diamond - 0.28f) < 0.012f) a += 0.5f;

                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
                        }
                    }
                    rune.SetPixels32(pixels);
                    rune.Apply();
                }
                return rune;
            }
        }

        public static Texture2D Vignette
        {
            get
            {
                if (vignette == null)
                {
                    const int size = 512;
                    vignette = NewTexture(size, size, "Vignette");
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float dx = (x + 0.5f) / size * 2f - 1f;
                            float dy = (y + 0.5f) / size * 2f - 1f;
                            float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.98f, r)) * 0.94f;
                            pixels[y * size + x] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(a) * 255));
                        }
                    }
                    vignette.SetPixels32(pixels);
                    vignette.Apply();
                }
                return vignette;
            }
        }

        public static Sprite DotSprite { get { if (dotSprite == null) dotSprite = MakeSprite(Dot, new Vector2(0.5f, 0.5f), 128f, "fx-dot"); return dotSprite; } }
        public static Sprite ShadowSprite { get { if (shadowSprite == null) shadowSprite = MakeSprite(ShadowTexture, new Vector2(0.5f, 0.5f), 256f, "fx-shadow"); return shadowSprite; } }
        public static Sprite RingSprite { get { if (ringSprite == null) ringSprite = MakeSprite(RingTexture, new Vector2(0.5f, 0.5f), 256f, "fx-ring"); return ringSprite; } }
        public static Sprite RuneSprite { get { if (runeSprite == null) runeSprite = MakeSprite(RuneTexture, new Vector2(0.5f, 0.5f), 256f, "fx-rune"); return runeSprite; } }

        public static Texture2D CharacterTexture(string name)
        {
            string key = "char:" + name;
            if (Textures.TryGetValue(key, out var cached)) return cached;
            var texture = LoadPng(Path.Combine(Application.streamingAssetsPath, "Art", name + ".png"), name);
            Textures[key] = texture;
            return texture;
        }

        public static Texture2D BackgroundTexture(string name)
        {
            string key = "bg:" + name;
            if (Textures.TryGetValue(key, out var cached)) return cached;
            var texture = LoadPng(Path.Combine(Application.streamingAssetsPath, "Bg", name + ".png"), name);
            Textures[key] = texture;
            return texture;
        }

        public static Sprite CharacterSprite(string name, float worldHeight)
        {
            string key = "char-sprite:" + name + ":" + worldHeight.ToString("0.00");
            if (Sprites.TryGetValue(key, out var cached)) return cached;

            var texture = CharacterTexture(name);
            float pixelsPerUnit = texture.height / Mathf.Max(0.1f, worldHeight);
            var sprite = MakeSprite(texture, new Vector2(0.5f, 0.018f), pixelsPerUnit, key);
            Sprites[key] = sprite;
            return sprite;
        }

        public static Sprite BackgroundSprite(string name)
        {
            string key = "bg-sprite:" + name;
            if (Sprites.TryGetValue(key, out var cached)) return cached;

            var texture = BackgroundTexture(name);
            var sprite = MakeSprite(texture, new Vector2(0.5f, 0.5f), 100f, key);
            Sprites[key] = sprite;
            return sprite;
        }

        public static Sprite MakeSprite(Texture2D texture, Vector2 pivot, float pixelsPerUnit, string name)
        {
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                pivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        static Texture2D NewTexture(int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        static Texture2D LoadPng(string path, string name)
        {
            if (File.Exists(path))
            {
                var texture = NewTexture(2, 2, name);
                if (texture.LoadImage(File.ReadAllBytes(path)))
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;
                    return texture;
                }
                Object.Destroy(texture);
            }

            Debug.LogWarning("[Zulvyre] Missing artwork: " + path);
            return MakePlaceholder(name);
        }

        static Texture2D MakePlaceholder(string name)
        {
            const int w = 256;
            const int h = 512;
            var texture = NewTexture(w, h, "placeholder-" + name);
            var pixels = new Color32[w * h];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 cloak = new Color32(72, 86, 66, 255);
            Color32 trim = new Color32(150, 130, 88, 255);
            Color32 skin = new Color32(204, 174, 140, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = clear;
                    float nx = (x + 0.5f) / w;
                    float ny = (y + 0.5f) / h;
                    float headDx = nx - 0.5f;
                    float headDy = ny - 0.82f;
                    bool head = headDx * headDx / 0.0028f + headDy * headDy / 0.006f < 1f;
                    bool body = ny < 0.76f && ny > 0.1f && Mathf.Abs(nx - 0.5f) < Mathf.Lerp(0.05f, 0.2f, (0.76f - ny) / 0.66f);
                    if (head) pixels[y * w + x] = skin;
                    else if (body) pixels[y * w + x] = ny < 0.18f ? trim : cloak;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
