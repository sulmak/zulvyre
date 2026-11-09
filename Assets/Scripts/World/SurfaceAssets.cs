using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    /// <summary>
    /// Runtime-generated, seamlessly tiling surface sets (albedo + derived normals) so the
    /// procedural world reads as built from real materials instead of flat painted primitives.
    /// </summary>
    public static partial class SurfaceAssets
    {
        public enum Kind
        {
            Grass,
            GrassDry,
            Dirt,
            Gravel,
            Stone,
            StoneDark,
            Plaster,
            Wood,
            WoodDark,
            Thatch,
            Bark,
            Leaf,
            LeafPine,
            Metal,
            Cloth,
            Leather,
            Bone,
            Fur,
            Skin,
            RoofTile,
            Water,
            Ember,
            Foliage
        }

        enum Mode { Opaque, Cutout, Transparent, Emissive }

        class Surface
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        static readonly Dictionary<Kind, Surface> Cache = new Dictionary<Kind, Surface>();
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static Shader cachedSurface;
        static Shader cachedWater;

        /// <summary>
        /// Builds strip unused shaders, and the project ships no material assets, so built-ins
        /// like "Standard" may not exist in the player. The custom shaders live in Resources.
        /// </summary>
        public static Shader SurfaceShader()
        {
            if (cachedSurface == null)
            {
                cachedSurface = Resources.Load<Shader>("ZulvyreSurface");
                if (cachedSurface == null) cachedSurface = Shader.Find("Standard");
                if (cachedSurface == null) cachedSurface = Shader.Find("Hidden/InternalErrorShader");
            }
            return cachedSurface;
        }

        public static Shader WaterShader()
        {
            if (cachedWater == null)
            {
                cachedWater = Resources.Load<Shader>("ZulvyreWater");
                if (cachedWater == null) cachedWater = SurfaceShader();
            }
            return cachedWater;
        }

        public static Texture2D Albedo(Kind kind) { return Get(kind).Albedo; }

        public static Material MaterialFor(Kind kind)
        {
            return Build(kind, Color.white, Vector2.one, -1f, -1f, Mode.Opaque, 0f, Color.black);
        }

        public static Material MaterialFor(Kind kind, Color tint, float scale, float smoothness = -1f, float metallic = -1f)
        {
            return Build(kind, tint, new Vector2(scale, scale), smoothness, metallic, Mode.Opaque, 0f, Color.black);
        }

        public static Material MaterialFor(Kind kind, Color tint, Vector2 scale, float smoothness = -1f, float metallic = -1f)
        {
            return Build(kind, tint, scale, smoothness, metallic, Mode.Opaque, 0f, Color.black);
        }

        /// <summary>Alpha-tested scatter (grass tufts, leaves) that still writes depth and casts shadows.</summary>
        public static Material CutoutFor(Kind kind, Color tint, Vector2 scale, float cutoff = 0.42f)
        {
            return Build(kind, tint, scale, 0.04f, 0f, Mode.Cutout, cutoff, Color.black);
        }

        public static Material TransparentFor(Kind kind, Color tint, Vector2 scale, float alpha, float smoothness = -1f)
        {
            return Build(kind, tint, scale, smoothness, 0f, Mode.Transparent, alpha, Color.black);
        }

        public static Material EmissiveFor(Kind kind, Color emission, Vector2 scale)
        {
            return Build(kind, Color.black, scale, 0f, 0f, Mode.Emissive, 0f, emission);
        }

        static Material Build(Kind kind, Color tint, Vector2 scale, float smoothness, float metallic, Mode mode, float param, Color emission)
        {
            string key = string.Join("|", kind.ToString(), ColorToString(tint), scale.x.ToString("0.###"), scale.y.ToString("0.###"),
                smoothness.ToString("0.###"), metallic.ToString("0.###"), mode.ToString(), param.ToString("0.###"), ColorToString(emission));
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var surface = Get(kind);
            var material = new Material(mode == Mode.Transparent ? WaterShader() : SurfaceShader());
            material.name = "surface-" + kind + "-" + mode;
            material.mainTexture = surface.Albedo;
            material.color = tint;
            material.SetTextureScale("_MainTex", scale);
            material.SetFloat("_Glossiness", smoothness >= 0f ? smoothness : DefaultSmoothness(kind));
            material.SetFloat("_Metallic", metallic >= 0f ? metallic : DefaultMetallic(kind));

            if (surface.Normal != null)
            {
                material.SetTexture("_BumpMap", surface.Normal);
                material.SetFloat("_BumpScale", 1f);
                material.SetTextureScale("_BumpMap", scale);
            }

            switch (mode)
            {
                case Mode.Cutout:
                    material.SetFloat("_Cutoff", param);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = 2450;
                    break;
                case Mode.Transparent:
                    Color faded = tint;
                    faded.a = param;
                    material.color = faded;
                    material.renderQueue = 3000;
                    break;
                case Mode.Emissive:
                    material.SetColor("_EmissionColor", emission);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.renderQueue = 100;
                    break;
            }

            Materials[key] = material;
            return material;
        }

        static float DefaultSmoothness(Kind kind)
        {
            switch (kind)
            {
                case Kind.Metal: return 0.72f;
                case Kind.Skin: return 0.34f;
                case Kind.Leather: return 0.24f;
                case Kind.RoofTile: return 0.3f;
                case Kind.Water: return 0.86f;
                case Kind.Ember: return 0.55f;
                case Kind.Stone:
                case Kind.StoneDark:
                case Kind.Plaster: return 0.12f;
                default: return 0.05f;
            }
        }

        static float DefaultMetallic(Kind kind)
        {
            return kind == Kind.Metal ? 0.82f : 0f;
        }

        static string ColorToString(Color c)
        {
            return Mathf.RoundToInt(c.r * 255) + "," + Mathf.RoundToInt(c.g * 255) + "," + Mathf.RoundToInt(c.b * 255)
                + "," + Mathf.RoundToInt(c.a * 255);
        }

        static Surface Get(Kind kind)
        {
            if (Cache.TryGetValue(kind, out var cached) && cached != null) return cached;

            int size = SizeFor(kind);
            var albedo = new Color32[size * size];
            var height = new float[size * size];

            switch (kind)
            {
                case Kind.Grass: PaintGrass(albedo, height, size, new Color32(74, 96, 52, 255), new Color32(46, 68, 36, 255), new Color32(112, 130, 68, 255), 11); break;
                case Kind.GrassDry: PaintGrass(albedo, height, size, new Color32(112, 104, 58, 255), new Color32(80, 74, 42, 255), new Color32(140, 126, 70, 255), 23); break;
                case Kind.Dirt: PaintDirt(albedo, height, size, 31); break;
                case Kind.Gravel: PaintDirt(albedo, height, size, 57); break;
                case Kind.Stone: PaintStone(albedo, height, size, new Color32(146, 142, 132, 255), 41); break;
                case Kind.StoneDark: PaintStone(albedo, height, size, new Color32(86, 88, 96, 255), 71); break;
                case Kind.Plaster: PaintPlaster(albedo, height, size, 43); break;
                case Kind.Wood: PaintWood(albedo, height, size, new Color32(126, 92, 58, 255), 13); break;
                case Kind.WoodDark: PaintWood(albedo, height, size, new Color32(78, 56, 38, 255), 29); break;
                case Kind.Thatch: PaintThatch(albedo, height, size, 47); break;
                case Kind.Bark: PaintBark(albedo, height, size, 61); break;
                case Kind.Leaf: PaintLeaf(albedo, height, size, new Color32(58, 92, 44, 255), new Color32(96, 126, 58, 255), 83); break;
                case Kind.LeafPine: PaintLeaf(albedo, height, size, new Color32(34, 62, 44, 255), new Color32(58, 88, 56, 255), 97); break;
                case Kind.Metal: PaintMetal(albedo, height, size, 101); break;
                case Kind.Cloth: PaintCloth(albedo, height, size, 107); break;
                case Kind.Leather: PaintCloth(albedo, height, size, 113); break;
                case Kind.Bone: PaintBone(albedo, height, size, 127); break;
                case Kind.Fur: PaintFur(albedo, height, size, 131); break;
                case Kind.Skin: PaintSkin(albedo, height, size, 137); break;
                case Kind.RoofTile: PaintTile(albedo, height, size, 149); break;
                case Kind.Water: PaintWater(albedo, height, size, 151); break;
                case Kind.Ember: PaintEmber(albedo, height, size, 157); break;
                case Kind.Foliage: PaintFoliage(albedo, height, size, 163); break;
            }

            var albedoTex = Upload(albedo, size, "albedo-" + kind, kind != Kind.Foliage);
            var normalTex = Upload(NormalFromHeight(height, size, NormalStrength(kind)), size, "normal-" + kind, false);
            if (kind == Kind.Foliage)
            {
                // A scatter card is a single sprite, not a tiling surface.
                albedoTex.wrapMode = TextureWrapMode.Clamp;
                normalTex.wrapMode = TextureWrapMode.Clamp;
            }

            var surface = new Surface { Albedo = albedoTex, Normal = normalTex };
            Cache[kind] = surface;
            return surface;
        }

        static int SizeFor(Kind kind)
        {
            switch (kind)
            {
                case Kind.Grass:
                case Kind.GrassDry:
                case Kind.Dirt:
                case Kind.Gravel:
                case Kind.Stone:
                case Kind.StoneDark:
                case Kind.Plaster:
                case Kind.RoofTile:
                case Kind.Water:
                case Kind.Foliage:
                    return 512;
                default:
                    return 256;
            }
        }

        static float NormalStrength(Kind kind)
        {
            switch (kind)
            {
                case Kind.Bark:
                case Kind.Thatch:
                case Kind.Stone:
                case Kind.StoneDark:
                case Kind.RoofTile:
                case Kind.Gravel:
                case Kind.Dirt:
                case Kind.Water:
                    return 2.4f;
                case Kind.Metal:
                case Kind.Skin:
                case Kind.Cloth:
                case Kind.Foliage:
                    return 0.5f;
                default:
                    return 1.3f;
            }
        }

        static Texture2D Upload(Color32[] pixels, int size, string name, bool mipmaps)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.SetPixels32(pixels);
            texture.Apply(mipmaps, false);
            return texture;
        }
    }
}
