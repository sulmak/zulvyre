using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class FxSystem : MonoBehaviour
    {
        class Particle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Gravity;
            public float Age;
            public float Duration;
            public Color Color;
            public float StartScale;
            public float EndScale;
        }

        class PulseRing
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public float From;
            public float To;
            public Color Color;
            public bool Ground;
        }

        class SlashArc
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public Color Color;
            public bool FacingRight;
            public Vector3 Origin;
            public float Scale;
        }

        class FadeCopy
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
            public Color Color;
        }

        class NumberText
        {
            public Vector3 World;
            public string Text;
            public Color Color;
            public float Age;
            public float Duration;
            public float Size;
            public float Rise;
        }

        readonly List<Particle> particles = new List<Particle>();
        readonly List<PulseRing> rings = new List<PulseRing>();
        readonly List<SlashArc> arcs = new List<SlashArc>();
        readonly List<FadeCopy> copies = new List<FadeCopy>();
        readonly List<NumberText> numbers = new List<NumberText>();

        Camera cam;
        Sprite arcSprite;
        float shakeAmount;
        float shakeTimer;
        float shakeDuration;
        Coroutine freezeRoutine;

        public Vector2 ShakeOffset { get; private set; }

        public void Initialize(Camera camera)
        {
            cam = camera;
            arcSprite = BuildArcSprite();
        }

        public void Clear()
        {
            for (int i = 0; i < particles.Count; i++) if (particles[i].Transform != null) Destroy(particles[i].Transform.gameObject);
            for (int i = 0; i < rings.Count; i++) if (rings[i].Transform != null) Destroy(rings[i].Transform.gameObject);
            for (int i = 0; i < arcs.Count; i++) if (arcs[i].Transform != null) Destroy(arcs[i].Transform.gameObject);
            for (int i = 0; i < copies.Count; i++) if (copies[i].Go != null) Destroy(copies[i].Go);
            particles.Clear();
            rings.Clear();
            arcs.Clear();
            copies.Clear();
            numbers.Clear();
            ShakeOffset = Vector2.zero;
            shakeTimer = 0f;
            if (freezeRoutine != null)
            {
                StopCoroutine(freezeRoutine);
                freezeRoutine = null;
            }
            Time.timeScale = 1f;
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                particle.Age += dt;
                if (particle.Age >= particle.Duration || particle.Transform == null)
                {
                    if (particle.Transform != null) Destroy(particle.Transform.gameObject);
                    particles.RemoveAt(i);
                    continue;
                }
                float k = particle.Age / particle.Duration;
                particle.Velocity.y -= particle.Gravity * dt;
                particle.Transform.position += (Vector3)(particle.Velocity * dt);
                float scale = Mathf.Lerp(particle.StartScale, particle.EndScale, k);
                particle.Transform.localScale = new Vector3(scale, scale, 1f);
                Color color = particle.Color;
                color.a *= 1f - k * k;
                particle.Renderer.color = color;
            }

            for (int i = rings.Count - 1; i >= 0; i--)
            {
                var ring = rings[i];
                ring.Age += dt;
                if (ring.Age >= ring.Duration || ring.Transform == null)
                {
                    if (ring.Transform != null) Destroy(ring.Transform.gameObject);
                    rings.RemoveAt(i);
                    continue;
                }
                float k = ring.Age / ring.Duration;
                float diameter = Mathf.Lerp(ring.From, ring.To, 1f - Mathf.Pow(1f - k, 2.2f));
                ring.Transform.localScale = ring.Ground
                    ? new Vector3(diameter, diameter, 1f)
                    : new Vector3(diameter, diameter * 0.42f, 1f);
                Color color = ring.Color;
                color.a *= 1f - k;
                ring.Renderer.color = color;
            }

            for (int i = arcs.Count - 1; i >= 0; i--)
            {
                var arc = arcs[i];
                arc.Age += dt;
                if (arc.Age >= arc.Duration || arc.Transform == null)
                {
                    if (arc.Transform != null) Destroy(arc.Transform.gameObject);
                    arcs.RemoveAt(i);
                    continue;
                }
                float k = arc.Age / arc.Duration;
                arc.Transform.localScale = new Vector3((arc.FacingRight ? 1f : -1f) * (1.5f + k * 0.34f) * arc.Scale, (1.16f + k * 0.22f) * arc.Scale, 1f);
                Color color = arc.Color;
                color.a *= 1f - k * k;
                arc.Renderer.color = color;
            }

            for (int i = copies.Count - 1; i >= 0; i--)
            {
                var copy = copies[i];
                copy.Age += dt;
                if (copy.Age >= copy.Duration || copy.Go == null)
                {
                    if (copy.Go != null) Destroy(copy.Go);
                    copies.RemoveAt(i);
                    continue;
                }
                float k = copy.Age / copy.Duration;
                Color color = copy.Color;
                color.a *= (1f - k) * 0.5f;
                copy.Renderer.color = color;
            }

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                numbers[i].Age += dt;
                if (numbers[i].Age >= numbers[i].Duration) numbers.RemoveAt(i);
            }

            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                ShakeOffset = Random.insideUnitCircle * shakeAmount * k * k;
            }
            else
            {
                ShakeOffset = Vector2.zero;
                shakeAmount = 0f;
            }
        }

        public void Shake(float amount, float duration)
        {
            shakeAmount = Mathf.Max(shakeAmount, amount);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeTimer = Mathf.Max(shakeTimer, duration);
        }

        public void Freeze(float duration)
        {
            if (freezeRoutine != null) StopCoroutine(freezeRoutine);
            freezeRoutine = StartCoroutine(FreezeRoutine(duration));
        }

        IEnumerator FreezeRoutine(float duration)
        {
            Time.timeScale = 0.07f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            freezeRoutine = null;
        }

        public void Sparks(Vector3 position, Color color, int count = 12, float power = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(1.5f, 5.5f) * power;
                SpawnParticle(position + (Vector3)(Random.insideUnitCircle * 0.18f), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    color, Random.Range(0.22f, 0.5f), 4.5f, Random.Range(0.22f, 0.42f), 0.02f, 5200);
            }
        }

        public void Dust(Vector3 position, Color color, int count = 4, float power = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new Vector2(Random.Range(-0.7f, 0.7f), Random.Range(0.2f, 1f)) * power;
                SpawnParticle(position + new Vector3(Random.Range(-0.25f, 0.25f), 0.02f, 0f), velocity,
                    color, Random.Range(0.35f, 0.7f), -0.4f, Random.Range(0.14f, 0.26f), 0.05f, 450);
            }
        }

        public void Trail(Vector3 position, Color color, float scale = 0.22f)
        {
            SpawnParticle(position, Random.insideUnitCircle * 0.4f, color, Random.Range(0.2f, 0.4f), -0.5f, scale, 0.02f, 5000);
        }

        void SpawnParticle(Vector3 position, Vector2 velocity, Color color, float duration, float gravity, float startScale, float endScale, int sortingOrder)
        {
            var go = new GameObject("fx-particle");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ImageAssets.DotSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(startScale, startScale, 1f);
            particles.Add(new Particle
            {
                Transform = go.transform,
                Renderer = renderer,
                Velocity = velocity,
                Gravity = gravity,
                Duration = duration,
                Color = color,
                StartScale = startScale,
                EndScale = endScale
            });
        }

        public void Ring(Vector3 position, Color color, float fromDiameter, float toDiameter, float duration, int sortingOrder = 4800, bool flattened = true, bool ground = false)
        {
            var go = new GameObject("fx-ring");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            if (ground) go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ImageAssets.RingSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(fromDiameter, ground ? fromDiameter : flattened ? fromDiameter * 0.42f : fromDiameter, 1f);
            rings.Add(new PulseRing
            {
                Transform = go.transform,
                Renderer = renderer,
                Duration = duration,
                From = fromDiameter,
                To = toDiameter,
                Color = color,
                Ground = ground
            });
        }

        public void Slash(Vector3 origin, int facing, Color color, float scale = 1f)
        {
            var go = new GameObject("fx-slash");
            go.transform.SetParent(transform, false);
            go.transform.position = origin + new Vector3(facing * 0.62f, 0.98f, 0f);
            go.transform.localScale = new Vector3(facing * 1.5f * scale, 1.16f * scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = arcSprite;
            color.a *= 0.82f;
            renderer.color = color;
            renderer.sortingOrder = 5300;
            arcs.Add(new SlashArc
            {
                Transform = go.transform,
                Renderer = renderer,
                Duration = 0.22f,
                Color = color,
                FacingRight = facing > 0,
                Origin = origin,
                Scale = scale
            });
            Sparks(origin + new Vector3(facing * 0.85f, 1.05f, 0f), color, 7, 0.8f);
        }

        public void Afterimage(SpriteRenderer source, Color color, float duration = 0.28f)
        {
            if (source == null || source.sprite == null) return;
            var go = new GameObject("fx-afterimage");
            go.transform.SetParent(transform, false);
            go.transform.position = source.transform.position;
            go.transform.localScale = source.transform.lossyScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.flipX = source.flipX;
            renderer.sortingOrder = source.sortingOrder - 1;
            renderer.color = color;
            copies.Add(new FadeCopy
            {
                Go = go,
                Renderer = renderer,
                Duration = duration,
                Color = color
            });
        }

        public void Text(Vector3 worldPosition, string text, Color color, float size = 30f, float rise = 1.1f, float duration = 0.9f)
        {
            numbers.Add(new NumberText
            {
                World = worldPosition,
                Text = text,
                Color = color,
                Size = size,
                Rise = rise,
                Duration = duration
            });
        }

        public void DrawNumbers()
        {
            if (cam == null) return;
            for (int i = 0; i < numbers.Count; i++)
            {
                var number = numbers[i];
                float k = Mathf.Clamp01(number.Age / number.Duration);
                Vector3 world = number.World + new Vector3(0f, number.Rise * k, 0f);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f) continue;
                Vector2 virtualPoint = ImGuiTheme.ScreenToVirtual(new Vector2(screen.x, Screen.height - screen.y));
                Color color = number.Color;
                color.a = 1f - Mathf.Clamp01((k - 0.6f) / 0.4f);
                float pop = 1f + Mathf.Sin(Mathf.Clamp01(k * 4f) * Mathf.PI * 0.5f) * 0.12f;
                var rect = new Rect(virtualPoint.x - 140f, virtualPoint.y - 26f, 280f, 52f);
                ImGuiTheme.Label(rect, number.Text, ImGuiTheme.StyleFor(Mathf.RoundToInt(number.Size * pop), true), color, TextAnchor.MiddleCenter);
            }
        }

        Sprite BuildArcSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "fx-arc";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float angular = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(44f, 72f, Mathf.Abs(angle)));
                    float radial = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.55f, radius));
                    radial *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 0.96f, radius));
                    float a = Mathf.Clamp01(radial * angular);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return ImageAssets.MakeSprite(texture, new Vector2(0.5f, 0.5f), 128f, "fx-arc");
        }
    }
}
