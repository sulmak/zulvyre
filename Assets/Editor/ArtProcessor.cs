using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zulvyre.EditorTools
{
    public static class ArtProcessor
    {
        static readonly string[] Characters = { "rowan", "ilsa", "corvin", "rival", "elder", "smith" };

        [MenuItem("Zulvyre/Process Character Art")]
        public static void ProcessArt()
        {
            string sourceDir = Path.Combine(Application.dataPath, "Art", "Source");
            string outDir = Path.Combine(Application.dataPath, "StreamingAssets", "Art");
            Directory.CreateDirectory(outDir);

            var previews = new Texture2D[Characters.Length];
            for (int i = 0; i < Characters.Length; i++)
            {
                string name = Characters[i];
                string sourcePath = Path.Combine(sourceDir, name + ".png");
                if (!File.Exists(sourcePath))
                {
                    Debug.LogError("[ArtProcessor] Missing source artwork: " + sourcePath);
                    continue;
                }

                var source = LoadTexture(sourcePath);
                if (source == null)
                {
                    Debug.LogError("[ArtProcessor] Could not decode " + sourcePath);
                    continue;
                }

                var keyed = ChromaKeyAndTrim(source);
                if (keyed == null)
                {
                    Debug.LogError("[ArtProcessor] No opaque pixels found in " + name);
                    continue;
                }

                string outputPath = Path.Combine(outDir, name + ".png");
                File.WriteAllBytes(outputPath, keyed.EncodeToPNG());
                previews[i] = keyed;
                Debug.Log($"[ArtProcessor] {name}: {source.width}x{source.height} -> {keyed.width}x{keyed.height}");

                Object.DestroyImmediate(source);
            }

            BuildContactSheet(previews);
            AssetDatabase.Refresh();
        }

        static Texture2D LoadTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Object.DestroyImmediate(texture);
                return null;
            }
            return texture;
        }

        static Texture2D ChromaKeyAndTrim(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            var pixels = source.GetPixels32();
            int minX = width, minY = height, maxX = -1, maxY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color32 p = pixels[index];
                    float r = p.r / 255f;
                    float g = p.g / 255f;
                    float b = p.b / 255f;
                    float minRedBlue = Mathf.Min(r, b);
                    float magenta = minRedBlue - g;
                    bool magentaHue = minRedBlue > 0.5f && Mathf.Abs(r - b) < 0.3f && magenta > 0.08f;

                    byte alpha = 255;
                    if (magentaHue)
                    {
                        float amount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.28f, magenta));
                        alpha = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - amount) * 255f), 0, 255);
                        float despill = Mathf.Clamp01((magenta - 0.04f) / 0.24f) * 0.7f;
                        float neutral = Mathf.Max(0f, g - 0.03f);
                        r = Mathf.Lerp(r, neutral, despill);
                        b = Mathf.Lerp(b, neutral, despill);
                    }

                    if (alpha < 10)
                    {
                        pixels[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    pixels[index] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(r * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(g * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(b * 255f), 0, 255),
                        alpha);

                    if (alpha > 32)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY) return null;

            const int padding = 8;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);

            int outWidth = maxX - minX + 1;
            int outHeight = maxY - minY + 1;
            var result = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false);
            var trimmed = new Color32[outWidth * outHeight];
            for (int y = 0; y < outHeight; y++)
            {
                for (int x = 0; x < outWidth; x++)
                {
                    trimmed[y * outWidth + x] = pixels[(minY + y) * width + (minX + x)];
                }
            }
            result.SetPixels32(trimmed);
            result.Apply(false, false);
            return result;
        }

        static void BuildContactSheet(Texture2D[] previews)
        {
            const int cellWidth = 300;
            const int sheetHeight = 860;
            const int targetHeight = 760;
            int sheetWidth = cellWidth * previews.Length;
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false);
            var pixels = new Color32[sheetWidth * sheetHeight];

            for (int y = 0; y < sheetHeight; y++)
            {
                for (int x = 0; x < sheetWidth; x++)
                {
                    bool light = ((x / 24) + (y / 24)) % 2 == 0;
                    byte value = (byte)(light ? 64 : 48);
                    pixels[y * sheetWidth + x] = new Color32(value, value, value, 255);
                }
            }

            for (int i = 0; i < previews.Length; i++)
            {
                var art = previews[i];
                if (art == null) continue;

                float scale = Mathf.Min((cellWidth - 24f) / art.width, targetHeight / (float)art.height);
                int drawWidth = Mathf.RoundToInt(art.width * scale);
                int drawHeight = Mathf.RoundToInt(art.height * scale);
                int originX = i * cellWidth + (cellWidth - drawWidth) / 2;
                int originY = (sheetHeight - drawHeight) / 2;

                for (int y = 0; y < drawHeight; y++)
                {
                    for (int x = 0; x < drawWidth; x++)
                    {
                        float u = (x + 0.5f) / drawWidth;
                        float v = (y + 0.5f) / drawHeight;
                        Color src = art.GetPixelBilinear(u, v);
                        int px = originX + x;
                        int py = originY + y;
                        if (px < 0 || px >= sheetWidth || py < 0 || py >= sheetHeight) continue;
                        int index = py * sheetWidth + px;
                        Color dst = pixels[index];
                        float a = src.a;
                        pixels[index] = new Color(
                            src.r * a + dst.r * (1f - a),
                            src.g * a + dst.g * (1f - a),
                            src.b * a + dst.b * (1f - a),
                            1f);
                    }
                }
            }

            sheet.SetPixels32(pixels);
            sheet.Apply(false, false);
            string path = Path.Combine(Application.dataPath, "Art", "_contactsheet.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }
    }
}
