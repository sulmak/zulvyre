using UnityEngine;

namespace Zulvyre
{
    public static partial class SurfaceAssets
    {
        // ------------------------------------------------------------------ painters

        static void PaintGrass(Color32[] alb, float[] h, int size, Color32 baseColor, Color32 dark, Color32 light, int seed)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float patch = Fbm(u, v, 3, 3, seed);
                    float fine = Fbm(u, v, 24, 3, seed + 5);

                    Color c = Color.Lerp(dark, baseColor, Mathf.Clamp01(patch * 1.5f));
                    c = Color.Lerp(c, light, Mathf.Clamp01((fine - 0.55f) * 1.4f));

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = fine * 0.5f + patch * 0.5f;
                }
            }

            var rng = new System.Random(seed);

            // Blades: short strokes that catch the sun and break up the flat base.
            for (int b = 0; b < size * 3; b++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                float angle = Mathf.PI * 0.5f + (float)(rng.NextDouble() - 0.5) * 0.9f;
                float len = 5f + (float)rng.NextDouble() * 9f;
                Color32 tint = Lerp32(light, baseColor, (float)rng.NextDouble() * 0.7f);
                Stroke(alb, h, x, y, x + Mathf.Cos(angle) * len, y + Mathf.Sin(angle) * len, 1.6f, tint, 0.35f, size);
            }

            for (int f = 0; f < size / 12; f++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                Color32 petal = f % 3 == 0 ? new Color32(212, 176, 74, 255)
                    : f % 3 == 1 ? new Color32(196, 96, 92, 255)
                    : new Color32(226, 222, 200, 255);
                Dot(alb, h, x, y, 2.2f, petal, 0.5f, size);
            }
        }

        static void PaintDirt(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 light = new Color32(146, 120, 88, 255);
            Color32 baseColor = new Color32(110, 88, 62, 255);
            Color32 dark = new Color32(74, 58, 42, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float broad = Fbm(u, v, 4, 4, seed);
                    float grit = Fbm(u, v, 40, 2, seed + 3);

                    Color c = Color.Lerp(dark, baseColor, Mathf.Clamp01(broad * 1.4f));
                    c = Color.Lerp(c, light, Mathf.Clamp01((grit - 0.5f) * 0.9f));

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = broad * 0.4f + grit * 0.6f;
                }
            }

            var rng = new System.Random(seed);
            for (int p = 0; p < size / 3; p++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                float r = 1.6f + (float)rng.NextDouble() * 4.2f;
                Dot(alb, h, x, y, r, Lerp32(light, dark, (float)rng.NextDouble()), 0.75f, size);
            }
        }

        static void PaintStone(Color32[] alb, float[] h, int size, Color32 baseColor, int seed)
        {
            Color32 mortar = new Color32(
                (byte)(baseColor.r * 0.42f), (byte)(baseColor.g * 0.42f), (byte)(baseColor.b * 0.44f), 255);

            for (int i = 0; i < alb.Length; i++) { alb[i] = mortar; h[i] = 0f; }

            var rng = new System.Random(seed);
            int rows = size / 32;
            float rowHeight = size / (float)rows;

            for (int r = 0; r < rows; r++)
            {
                float y0 = r * rowHeight + 2f;
                float x = -((r % 2) * rowHeight * 0.5f + (float)rng.NextDouble() * 8f);

                while (x < size)
                {
                    float w = 40f + (float)rng.NextDouble() * 46f;
                    float bx = Mathf.Max(0f, x + 2f);
                    float by = y0 + 2f;
                    float bw = Mathf.Min(size - bx, w - 4f);
                    float bh = rowHeight - 4f;

                    Color32 block = Lerp32(baseColor, new Color32(210, 205, 192, 255), (float)rng.NextDouble() * 0.35f);
                    block = Lerp32(block, new Color32(52, 54, 58, 255), (float)rng.NextDouble() * 0.3f);

                    FillRect(alb, h, bx, by, bw, bh, block, 0.85f, size);

                    for (int i = 0; i < 26; i++)
                    {
                        Dot(alb, h, bx + (float)rng.NextDouble() * bw, by + (float)rng.NextDouble() * bh,
                            1f + (float)rng.NextDouble() * 2.4f,
                            Lerp32(block, mortar, 0.45f + (float)rng.NextDouble() * 0.4f), 0.5f, size);
                    }

                    if (rng.NextDouble() < 0.4)
                    {
                        float px = rng.NextDouble() < 0.5 ? bx : bx + bw;
                        Dot(alb, h, px, by + (float)rng.NextDouble() * bh,
                            2f + (float)rng.NextDouble() * 3f, mortar, 0.2f, size);
                    }

                    x += w;
                }
            }

            Color32 moss = new Color32(66, 84, 46, 255);
            for (int m = 0; m < size / 6; m++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                for (int i = 0; i < 8; i++)
                {
                    Dot(alb, h, x + (float)(rng.NextDouble() - 0.5) * 10f, y + (float)(rng.NextDouble() - 0.5) * 10f,
                        2f + (float)rng.NextDouble() * 4f, Lerp32(moss, baseColor, (float)rng.NextDouble() * 0.4f), 0.55f, size);
                }
            }
        }

        static void PaintWood(Color32[] alb, float[] h, int size, Color32 baseColor, int seed)
        {
            var rng = new System.Random(seed);
            Color32 light = Lerp32(baseColor, new Color32(255, 236, 200, 255), 0.32f);
            Color32 dark = Lerp32(baseColor, new Color32(0, 0, 0, 255), 0.55f);

            const int planks = 4;
            float plankHeight = size / (float)planks;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plank = Mathf.Min(planks - 1, (int)(y / plankHeight));
                    float local = (y % plankHeight) / plankHeight;

                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    float warp = Fbm(u, v * (6 + plank * 2f), 6, 3, seed + plank * 13);
                    float grain = Mathf.Pow(Mathf.Sin((u * 34f + warp * 6f) * Mathf.PI) * 0.5f + 0.5f, 2.2f);

                    Color c = Color.Lerp(baseColor, light, grain * 0.55f);
                    c = Color.Lerp(c, dark, (1f - grain) * 0.18f);

                    float seam = Mathf.Min(local, 1f - local);
                    if (seam < 0.035f) c = Color.Lerp(dark, c, seam / 0.035f * 0.7f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = grain * 0.35f - (1f - Mathf.Clamp01(seam * 12f)) * 0.4f;
                }
            }

            for (int k = 0; k < planks; k++)
            {
                float kx = (float)rng.NextDouble() * size;
                float ky = k * plankHeight + plankHeight * (0.3f + (float)rng.NextDouble() * 0.4f);
                for (int r = 0; r < 6; r++)
                {
                    Ring(alb, h, kx, ky, 3f + r * 1.9f, Lerp32(dark, baseColor, r / 8f), 0.4f, size);
                }
                Dot(alb, h, kx, ky, 3.2f, dark, 0.5f, size);
            }
        }

        static void PaintThatch(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 dark = new Color32(74, 56, 32, 255);
            Color32 baseColor = new Color32(128, 100, 56, 255);
            Color32 light = new Color32(186, 156, 96, 255);

            for (int i = 0; i < alb.Length; i++) { alb[i] = dark; h[i] = 0f; }

            var rng = new System.Random(seed);
            for (int layer = 0; layer < 5; layer++)
            {
                float yTop = size * 0.16f * layer;
                for (int s = 0; s < size * 6; s++)
                {
                    float x = (float)rng.NextDouble() * size;
                    float y = yTop + (float)rng.NextDouble() * (size * 0.3f);
                    float len = 12f + (float)rng.NextDouble() * 26f;
                    float sway = (float)(rng.NextDouble() - 0.5) * 6f;
                    Color32 straw = Lerp32(baseColor, light, (float)rng.NextDouble());
                    if (rng.NextDouble() < 0.18) straw = Lerp32(straw, dark, 0.6f);
                    Stroke(alb, h, x, y, x + sway, y + len, 1.5f + (float)rng.NextDouble(), straw, 0.5f, size);
                }

                for (int x = 0; x < size; x++)
                {
                    float wave = yTop + Mathf.Sin(x * 0.06f + layer) * 3f;
                    for (int d = 0; d < 3; d++)
                    {
                        int i = Index(x, (int)(wave + d), size);
                        alb[i] = Lerp32(alb[i], dark, 0.5f);
                        h[i] = Mathf.Min(h[i], 0.1f);
                    }
                }
            }
        }

        static void PaintBark(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 dark = new Color32(42, 32, 24, 255);
            Color32 baseColor = new Color32(84, 64, 44, 255);
            Color32 light = new Color32(126, 104, 74, 255);

            var rng = new System.Random(seed);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float ridge = Mathf.Pow(Fbm(u * 5f, v * 1.4f, 10, 4, seed), 0.7f);
                    float fine = Fbm(u, v, 48, 2, seed + 7);

                    Color c = Color.Lerp(dark, baseColor, Mathf.Clamp01((ridge - 0.25f) * 2.4f));
                    c = Color.Lerp(c, light, Mathf.Clamp01((ridge - 0.7f) * 1.6f) * 0.7f);
                    c = Color.Lerp(c, dark, (1f - fine) * 0.15f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = ridge;
                }
            }

            for (int m = 0; m < size / 10; m++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                for (int i = 0; i < 5; i++)
                {
                    Dot(alb, h, x + (float)(rng.NextDouble() - 0.5) * 8f, y + (float)(rng.NextDouble() - 0.5) * 14f,
                        2f + (float)rng.NextDouble() * 4f, new Color32(64, 78, 46, 255), 0.4f, size);
                }
            }
        }

        static void PaintLeaf(Color32[] alb, float[] h, int size, Color32 dark, Color32 light, int seed)
        {
            for (int i = 0; i < alb.Length; i++) { alb[i] = dark; h[i] = 0.1f; }

            var rng = new System.Random(seed);
            for (int c = 0; c < size * 2; c++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                Color32 tint = Lerp32(dark, light, (float)rng.NextDouble());
                if (rng.NextDouble() < 0.25) tint = Lerp32(tint, new Color32(28, 40, 24, 255), 0.5f);
                Dot(alb, h, x, y, 3f + (float)rng.NextDouble() * 7f, tint, 0.6f + (float)rng.NextDouble() * 0.4f, size);
            }

            for (int c = 0; c < size; c++)
            {
                Dot(alb, h, (float)rng.NextDouble() * size, (float)rng.NextDouble() * size,
                    1.6f + (float)rng.NextDouble() * 2.4f,
                    Lerp32(light, new Color32(206, 214, 150, 255), 0.35f), 0.9f, size);
            }
        }

        static void PaintMetal(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 baseColor = new Color32(96, 100, 108, 255);
            Color32 dark = new Color32(44, 46, 52, 255);
            Color32 light = new Color32(168, 174, 182, 255);
            Color32 rust = new Color32(112, 62, 34, 255);

            var rng = new System.Random(seed);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float brushed = Fbm(u * 30f, v * 1.2f, 60, 2, seed);
                    float blotch = Fbm(u, v, 6, 3, seed + 11);

                    Color c = Color.Lerp(baseColor, light, Mathf.Clamp01((brushed - 0.45f) * 1.6f));
                    c = Color.Lerp(c, dark, Mathf.Clamp01((0.4f - blotch) * 1.5f));
                    float rustMask = Mathf.Clamp01((blotch - 0.62f) * 3.2f);
                    c = Color.Lerp(c, rust, rustMask * 0.8f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = brushed * 0.3f + (1f - rustMask) * 0.4f;
                }
            }

            for (int r = 0; r < 10; r++)
            {
                Dot(alb, h, (float)rng.NextDouble() * size, (float)rng.NextDouble() * size,
                    2f + (float)rng.NextDouble() * 3f, dark, 0.25f, size);
            }
        }

        static void PaintCloth(Color32[] alb, float[] h, int size, int seed)
        {
            var rng = new System.Random(seed);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float weave = ((x + y) & 1) == 0 ? 0.62f : 0.38f;
                    float thread = (Mathf.Sin(x * Mathf.PI * 0.5f) * 0.5f + 0.5f) * (Mathf.Sin(y * Mathf.PI * 0.5f) * 0.5f + 0.5f);
                    float stain = Fbm(u, v, 5, 3, seed);

                    float shade = 0.72f + weave * 0.16f + thread * 0.12f;
                    shade *= Mathf.Lerp(0.82f, 1.06f, stain);

                    int i = y * size + x;
                    byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(shade * 255f), 0, 255);
                    alb[i] = new Color32(value, value, value, 255);
                    h[i] = weave * 0.5f + thread * 0.5f;
                }
            }

            for (int s = 0; s < 14; s++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                float len = 10f + (float)rng.NextDouble() * 40f;
                Stroke(alb, h, x, y, x + (float)(rng.NextDouble() - 0.5) * len, y + len, 2.5f,
                    new Color32(150, 150, 150, 255), 0.25f, size);
            }
        }

        static void PaintBone(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 baseColor = new Color32(214, 204, 176, 255);
            Color32 dark = new Color32(122, 112, 92, 255);

            var rng = new System.Random(seed);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float mottle = Fbm(u, v, 8, 3, seed);
                    float pore = Fbm(u, v, 60, 2, seed + 3);

                    Color c = Color.Lerp(baseColor, dark, Mathf.Clamp01((0.55f - mottle) * 1.1f));
                    c = Color.Lerp(c, dark, Mathf.Clamp01(0.5f - pore) * 0.35f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = pore * 0.5f + mottle * 0.5f;
                }
            }

            for (int c = 0; c < 12; c++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                Stroke(alb, h, x, y, x + (float)(rng.NextDouble() - 0.5) * 40f, y + (float)(rng.NextDouble() - 0.5) * 40f,
                    1f, dark, 0.15f, size);
            }
        }

        static void PaintFur(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 dark = new Color32(92, 82, 68, 255);
            Color32 baseColor = new Color32(150, 134, 108, 255);
            Color32 light = new Color32(212, 194, 160, 255);

            for (int i = 0; i < alb.Length; i++) { alb[i] = dark; h[i] = 0f; }

            var rng = new System.Random(seed);
            for (int s = 0; s < size * 8; s++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                float len = 5f + (float)rng.NextDouble() * 11f;
                float sway = (float)(rng.NextDouble() - 0.5) * 4f;
                Color32 tint = Lerp32(baseColor, light, (float)rng.NextDouble() * (float)rng.NextDouble());
                Stroke(alb, h, x, y, x + sway, y + len, 1.4f, tint, 0.55f, size);
            }
        }

        static void PaintSkin(Color32[] alb, float[] h, int size, int seed)
        {
            // Near-neutral luminance detail: the material colour carries the skin hue
            // (human tan, orc green), so no race of character bakes in a beige base.
            Color32 baseColor = new Color32(202, 198, 190, 255);
            Color32 shade = new Color32(148, 142, 134, 255);
            Color32 glow = new Color32(232, 228, 218, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float mottle = Fbm(u, v, 10, 3, seed);
                    float pores = Fbm(u, v, 64, 2, seed + 5);

                    Color c = Color.Lerp(baseColor, shade, Mathf.Clamp01((0.5f - mottle) * 1.2f));
                    c = Color.Lerp(c, glow, Mathf.Clamp01((mottle - 0.6f) * 1.4f) * 0.6f);
                    c *= Mathf.Lerp(0.94f, 1.03f, pores);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = pores;
                }
            }
        }

        static void PaintTile(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 mortar = new Color32(52, 44, 40, 255);
            Color32 baseColor = new Color32(132, 74, 56, 255);
            Color32 light = new Color32(176, 116, 84, 255);

            for (int i = 0; i < alb.Length; i++) { alb[i] = mortar; h[i] = 0f; }

            var rng = new System.Random(seed);
            const int cols = 8;
            const int rows = 6;
            float tw = size / (float)cols;
            float th = size / (float)rows;

            for (int r = 0; r < rows; r++)
            {
                float shift = (r % 2) * tw * 0.5f;
                for (int c = -1; c <= cols; c++)
                {
                    float x = c * tw + shift + 2f;
                    float y = r * th + 2f;
                    Color32 tile = Lerp32(baseColor, light, (float)rng.NextDouble() * 0.6f);
                    tile = Lerp32(tile, new Color32(88, 52, 44, 255), (float)rng.NextDouble() * 0.35f);

                    FillRect(alb, h, x, y, tw - 4f, th - 4f, tile, 0.9f, size);

                    for (int i = 0; i < 10; i++)
                    {
                        Dot(alb, h, x + (float)rng.NextDouble() * (tw - 4f), y + (float)rng.NextDouble() * (th - 4f),
                            1f + (float)rng.NextDouble() * 2f, Lerp32(tile, mortar, 0.4f), 0.6f, size);
                    }
                }
            }
        }

        static void PaintPlaster(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 baseColor = new Color32(214, 200, 172, 255);
            Color32 light = new Color32(236, 226, 204, 255);
            Color32 dark = new Color32(160, 142, 116, 255);
            Color32 damp = new Color32(126, 118, 100, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    float trowel = Fbm(u, v, 7, 4, seed);
                    float sweep = Mathf.Repeat(u * 9f + trowel * 3f + v * 1.6f, 1f);
                    float stroke = Mathf.Pow(Mathf.Sin(sweep * Mathf.PI) * 0.5f + 0.5f, 3f);
                    float grit = Fbm(u, v, 72, 2, seed + 9);

                    Color c = Color.Lerp(baseColor, light, Mathf.Clamp01((trowel - 0.42f) * 1.6f) * 0.7f);
                    c = Color.Lerp(c, dark, Mathf.Clamp01((0.45f - trowel) * 1.5f) * 0.6f);
                    c = Color.Lerp(c, light, stroke * 0.14f);
                    c *= Mathf.Lerp(0.9f, 1.05f, grit);

                    // Ground-floor rising damp and splash-back darkening.
                    float v01 = 1f - v;
                    float dampness = Mathf.Clamp01((v01 - 0.72f) / 0.28f);
                    float tide = dampness * (0.55f + 0.45f * Mathf.Clamp01(Fbm(u * 2f, v01 * 1f, 6, 3, seed + 4)));
                    c = Color.Lerp(c, damp, tide * 0.55f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = stroke * 0.35f + trowel * 0.4f + grit * 0.25f + dampness * 0.1f;
                }
            }

            var rng = new System.Random(seed);
            Color32 crack = new Color32(104, 94, 80, 255);
            for (int c = 0; c < size / 48; c++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float px = x, py = y;
                for (int s = 0; s < 8; s++)
                {
                    float len = 6f + (float)rng.NextDouble() * 16f;
                    angle += (float)(rng.NextDouble() - 0.5) * 1.1f;
                    float nx = px + Mathf.Cos(angle) * len;
                    float ny = py + Mathf.Sin(angle) * len;
                    Stroke(alb, h, px, py, nx, ny, 0.9f, crack, 0.05f, size);
                    px = nx;
                    py = ny;
                }
            }

            for (int p = 0; p < size / 2; p++)
            {
                Dot(alb, h, (float)rng.NextDouble() * size, (float)rng.NextDouble() * size,
                    0.7f + (float)rng.NextDouble() * 1.6f,
                    Lerp32(dark, baseColor, (float)rng.NextDouble()), 0.2f, size);
            }
        }

        static void PaintWater(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 deep = new Color32(46, 96, 112, 255);
            Color32 mid = new Color32(92, 158, 168, 255);
            Color32 crest = new Color32(206, 232, 234, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    float drift = Fbm(u * 2f, v * 2f, 5, 3, seed);
                    float rippleA = Mathf.Sin((v * 22f + drift * 5f + u * 2f) * Mathf.PI);
                    float rippleB = Mathf.Sin((u * 15f - v * 4f + drift * 6f) * Mathf.PI * 0.5f);
                    float caustic = Mathf.Pow(Mathf.Clamp01(0.5f - Mathf.Abs(rippleA * 0.5f + rippleB * 0.5f)), 2.4f);
                    float chop = Fbm(u, v, 34, 3, seed + 7);

                    Color c = Color.Lerp(deep, mid, Mathf.Clamp01(chop * 1.3f));
                    c = Color.Lerp(c, crest, caustic * 0.7f);

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = caustic * 0.9f + chop * 0.35f;
                }
            }

            var rng = new System.Random(seed);
            for (int f = 0; f < size / 8; f++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                Stroke(alb, h, x, y, x + (float)(rng.NextDouble() - 0.5) * 22f, y + (float)(rng.NextDouble() - 0.5) * 8f,
                    1.2f, crest, 0.95f, size);
            }
        }

        static void PaintEmber(Color32[] alb, float[] h, int size, int seed)
        {
            Color32 char2 = new Color32(28, 24, 22, 255);
            Color32 coal = new Color32(58, 48, 42, 255);
            Color32 hot = new Color32(255, 166, 58, 255);
            Color32 white = new Color32(255, 226, 164, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    float cells = Fbm(u, v, 9, 3, seed);
                    float fissure = Mathf.Clamp01(1f - Mathf.Abs(cells - 0.5f) * 7f);
                    float heat = Mathf.Clamp01(Fbm(u * 1.5f, v * 1.5f, 4, 3, seed + 3) * 1.5f - 0.35f);
                    float ash = Fbm(u, v, 48, 2, seed + 11);

                    Color c = Color.Lerp(char2, coal, Mathf.Clamp01(ash * 1.4f));
                    c = Color.Lerp(c, hot, Mathf.Clamp01(fissure * heat * 1.6f));
                    c = Color.Lerp(c, white, Mathf.Clamp01((fissure - 0.6f) * heat * 2.2f));

                    int i = y * size + x;
                    alb[i] = To32(c);
                    h[i] = cells * 0.8f + ash * 0.2f;
                }
            }

            var rng = new System.Random(seed);
            for (int g = 0; g < size / 3; g++)
            {
                float x = (float)rng.NextDouble() * size;
                float y = (float)rng.NextDouble() * size;
                Color32 mote = rng.NextDouble() < 0.3 ? white : hot;
                Dot(alb, h, x, y, 0.8f + (float)rng.NextDouble() * 2f, mote, 0.55f, size);
            }
        }

        /// <summary>Alpha-cutout scatter card: blades keep their own alpha so the mesh stays see-through.</summary>
        static void PaintFoliage(Color32[] alb, float[] h, int size, int seed)
        {
            for (int i = 0; i < alb.Length; i++)
            {
                alb[i] = new Color32(92, 118, 60, 0);
                h[i] = 0f;
            }

            Color32 baseColor = new Color32(92, 118, 60, 255);
            Color32 light = new Color32(154, 176, 92, 255);
            Color32 dark = new Color32(44, 66, 34, 255);

            var rng = new System.Random(seed);
            for (int b = 0; b < 54; b++)
            {
                float bx = size * (0.05f + (float)rng.NextDouble() * 0.9f);
                float by = size * (0.9f + (float)rng.NextDouble() * 0.08f);
                float len = size * (0.34f + (float)rng.NextDouble() * 0.5f);
                float lean = (float)(rng.NextDouble() - 0.5) * len * 0.8f;
                float width = size * (0.009f + (float)rng.NextDouble() * 0.014f);

                Color32 tint = Lerp32(baseColor, rng.NextDouble() < 0.35f ? dark : light, (float)rng.NextDouble() * 0.85f);

                int steps = Mathf.CeilToInt(len);
                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    Blade(alb, h, bx + lean * t * t, by - len * t, Mathf.Max(0.55f, width * (1f - t * t)), tint, size);
                }
            }
        }

        static void Blade(Color32[] alb, float[] h, float cx, float cy, float halfWidth, Color32 color, int size)
        {
            int r = Mathf.CeilToInt(halfWidth);
            int py = Mathf.RoundToInt(cy);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    float a = Mathf.Clamp01(1f - Mathf.Abs(dx) / (halfWidth + 0.6f));
                    if (dy != 0) a *= 0.6f;
                    if (a <= 0.03f) continue;

                    int i = Index(Mathf.RoundToInt(cx) + dx, py + dy, size);
                    Color32 dst = alb[i];
                    if (a * 255f <= dst.a) continue;

                    alb[i] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(dst.r, color.r, a)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(dst.g, color.g, a)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(dst.b, color.b, a)),
                        (byte)Mathf.Max(dst.a, Mathf.RoundToInt(a * 255f)));
                    h[i] = Mathf.Max(h[i], a);
                }
            }
        }

        // ------------------------------------------------------------------ drawing helpers

        static void FillRect(Color32[] alb, float[] h, float x, float y, float w, float hh, Color32 color, float heightValue, int size)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(x + w));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(y + hh));

            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    int i = py * size + px;
                    alb[i] = color;
                    h[i] = heightValue;
                }
            }
        }

        static void Dot(Color32[] alb, float[] h, float cx, float cy, float radius, Color32 color, float heightValue, int size)
        {
            int r = Mathf.CeilToInt(radius);
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (d > 1f) continue;
                    float a = Mathf.SmoothStep(1f, 0.55f, d);
                    Splat(alb, h, (int)(cx + dx), (int)(cy + dy), color, a, Mathf.Lerp(h[Index((int)(cx + dx), (int)(cy + dy), size)], heightValue, a), size);
                }
            }
        }

        static void Ring(Color32[] alb, float[] h, float cx, float cy, float radius, Color32 color, float heightValue, int size)
        {
            int steps = Mathf.CeilToInt(radius * 6f);
            for (int s = 0; s < steps; s++)
            {
                float a = s / (float)steps * Mathf.PI * 2f;
                Dot(alb, h, cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, 1.1f, color, heightValue, size);
            }
        }

        static void Stroke(Color32[] alb, float[] h, float x0, float y0, float x1, float y1, float width, Color32 color, float heightValue, int size)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            int steps = Mathf.CeilToInt(Mathf.Max(1f, Mathf.Sqrt(dx * dx + dy * dy)));

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                float taper = Mathf.Sin(t * Mathf.PI) * 0.5f + 0.5f;
                Dot(alb, h, x0 + dx * t, y0 + dy * t, Mathf.Max(0.8f, width * taper), color, heightValue, size);
            }
        }

        static void Splat(Color32[] alb, float[] h, int x, int y, Color32 color, float alpha, float heightValue, int size)
        {
            if (alpha <= 0.001f) return;
            int i = Index(x, y, size);
            Color32 dst = alb[i];
            alb[i] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.r, color.r, alpha)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.g, color.g, alpha)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(dst.b, color.b, alpha)),
                255);
            h[i] = Mathf.Max(h[i], heightValue);
        }

        static int Index(int x, int y, int size)
        {
            int px = ((x % size) + size) % size;
            int py = ((y % size) + size) % size;
            return py * size + px;
        }

        static Color32[] NormalFromHeight(float[] h, int size, float strength)
        {
            var normals = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float l = h[Index(x - 1, y, size)];
                    float r = h[Index(x + 1, y, size)];
                    float d = h[Index(x, y - 1, size)];
                    float u = h[Index(x, y + 1, size)];

                    Vector3 n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    normals[y * size + x] = new Color32(
                        (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255),
                        255);
                }
            }
            return normals;
        }

        static Color32 To32(Color c)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255), 0, 255),
                255);
        }

        static Color32 Lerp32(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                255);
        }

        // ------------------------------------------------------------------ noise

        static int Mod(int x, int m)
        {
            return ((x % m) + m) % m;
        }

        static float Hash(int x, int y, int seed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xffffffu) / 16777215f;
        }

        static float Noise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = x - xi;
            float yf = y - yi;
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);

            int x0 = Mod(xi, period);
            int x1 = Mod(xi + 1, period);
            int y0 = Mod(yi, period);
            int y1 = Mod(yi + 1, period);

            return Mathf.Lerp(
                Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), u),
                Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), u),
                v);
        }

        public static float Fbm(float u, float v, int frequency, int octaves, int seed)
        {
            float sum = 0f;
            float weight = 0f;
            float amp = 1f;
            int f = Mathf.Max(1, frequency);

            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Noise(u * f, v * f, f, seed + o * 131);
                weight += amp;
                amp *= 0.5f;
                f *= 2;
            }

            return sum / weight;
        }
    }
}
