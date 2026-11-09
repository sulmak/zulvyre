using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public static class WorldModels
    {
        static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();

        static readonly Dictionary<string, float> Heights = new Dictionary<string, float>
        {
            { "hero", 1.8f }, { "orc", 1.95f }, { "vampire", 1.9f }, { "wolf", 1.15f }, { "skeleton", 1.8f },
            { "house", 4.9f }, { "longhouse", 4.4f }, { "spire", 11.5f }, { "watchtower", 7.6f },
            { "pine", 8.4f }, { "oak", 7.6f }, { "rock", 1.5f }, { "well", 3.3f }, { "gate", 5.2f },
            { "pillar", 4.5f }, { "lanternpost", 3.3f }, { "fence", 1.3f }, { "crate", 0.9f }
        };

        public static GameObject Spawn(string id, Transform parent, Vector3 position, float yaw = 0f, float height = -1f)
        {
            GameObject prefab = Load(id);
            if (prefab == null) return Fallback(id, parent, position, yaw);

            var go = Object.Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), parent);
            go.name = id;

            float target = height > 0f ? height : (Heights.TryGetValue(id, out float h) ? h : 0f);
            if (target > 0f) Normalize(go, target);

            var renderers = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
                renderers[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            }
            Skin(go, ScaleFor(id, target), IsCharacter(id) ? 0.15f : 0.45f);
            return go;
        }

        static readonly string[] HumanClips = { "idle", "walk", "run", "attack", "hurt", "die" };
        static readonly Dictionary<string, float> HumanGroundShift = new Dictionary<string, float>();

        public static GameObject SpawnHuman(string kind, Transform parent, Vector3 position, float yaw)
        {
            var prefab = Resources.Load<GameObject>("Art3D/Humans/" + kind + "_idle");
            if (prefab == null) return Spawn("hero", parent, position, yaw, 1.8f);

            // The Maya export carries the hips offset on the armature root, so the
            // figure hangs below its transform. "go" is the game-facing pivot (feet),
            // "visual" is the model pushed back up onto that pivot.
            var go = new GameObject(kind);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var visual = Object.Instantiate(prefab);
            visual.name = "visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition += Vector3.up * GroundOffset(kind, visual, position.y);

            var anim = visual.GetComponent<Animation>();
            if (anim == null) anim = visual.AddComponent<Animation>();
            for (int i = 0; i < HumanClips.Length; i++)
            {
                string clipName = HumanClips[i];
                var clip = Resources.Load<AnimationClip>("Art3D/Humans/" + kind + "_" + clipName);
                if (clip == null) continue;
                clip.wrapMode = clipName == "die" ? WrapMode.ClampForever
                    : clipName == "attack" || clipName == "hurt" ? WrapMode.Once : WrapMode.Loop;
                anim.AddClip(clip, clipName);
            }
            anim.playAutomatically = false;

            var renderers = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
                renderers[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            }

            Skin(go, 1.6f, 0.15f);
            go.AddComponent<HumanAnimator>().Bind(anim);
            return go;
        }

        static float GroundOffset(string kind, GameObject visual, float groundY)
        {
            float cached;
            if (HumanGroundShift.TryGetValue(kind, out cached)) return cached;

            var renderers = visual.GetComponentsInChildren<Renderer>();
            float shift = 0f;
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                shift = groundY - bounds.min.y;
                if (float.IsNaN(shift) || float.IsInfinity(shift) || Mathf.Abs(shift) > 3f) shift = 0f;
            }
            HumanGroundShift[kind] = shift;
            return shift;
        }

        static GameObject Load(string id)
        {
            if (Cache.TryGetValue(id, out GameObject cached)) return cached;
            var prefab = Resources.Load<GameObject>("Art3D/" + id);
            Cache[id] = prefab;
            return prefab;
        }

        static void Normalize(GameObject go, float targetHeight)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float current = bounds.size.y;
            if (current < 0.0005f) return;

            float factor = targetHeight / current;
            if (Mathf.Abs(factor - 1f) < 0.015f) return;
            go.transform.localScale *= factor;
        }

        public static void Tint(GameObject go, Color color, params string[] tokens)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderers.Length; r++)
            {
                var materials = renderers[r].materials;
                bool changed = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == null) continue;
                    string name = materials[m].name.ToLowerInvariant();
                    bool match = tokens.Length == 0;
                    for (int t = 0; !match && t < tokens.Length; t++) match = name.Contains(tokens[t]);
                    if (!match) continue;
                    // Textured surfaces already carry their own painted hue, so the requested
                    // colour is applied as a lift rather than a straight multiply.
                    Color applied = color;
                    if (materials[m].mainTexture != null) applied = Color.Lerp(color, Color.white, 0.35f);
                    materials[m].color = applied;
                    changed = true;
                }
                if (changed) renderers[r].materials = materials;
            }
        }

        static readonly Dictionary<string, Material> Skinned = new Dictionary<string, Material>();

        static readonly HashSet<string> CharacterIds = new HashSet<string> { "hero", "orc", "vampire", "wolf", "skeleton" };

        public static bool IsCharacter(string id)
        {
            return id != null && CharacterIds.Contains(id);
        }

        /// <summary>
        /// Replaces an imported model's flat coloured materials with generated surface materials,
        /// keyed off the artist's material names. The instance keeps the original name so
        /// {@link Tint} token matching still works afterwards.
        /// </summary>
        public static void Skin(GameObject go, float tileScale, float lift = 0.45f)
        {
            if (go == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderers.Length; r++)
            {
                var materials = renderers[r].sharedMaterials;
                bool changed = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    if (materials[m] == null) continue;
                    string original = materials[m].name.ToLowerInvariant();
                    SurfaceAssets.Kind kind;
                    float detail;
                    if (!TryMap(original, out kind, out detail)) continue;

                    string key = original + "|" + kind + "|" + detail + "|" + tileScale + "|" + lift;
                    Material surface;
                    if (!Skinned.TryGetValue(key, out surface) || surface == null)
                    {
                        // Props are lifted so their painted textures keep detail; characters are
                        // lifted far less so palette colours (orc green, vampire black) survive.
                        float amount = lift;
                        if (original == "fur" && lift < 0.3f) amount = 0.5f;
                        Color tint = Color.Lerp(materials[m].color, Color.white, amount);
                        surface = Object.Instantiate(SurfaceAssets.MaterialFor(kind, tint, detail * tileScale));
                        surface.name = original;
                        Skinned[key] = surface;
                    }

                    materials[m] = surface;
                    changed = true;
                }
                if (changed) renderers[r].sharedMaterials = materials;
            }
        }

        static bool TryMap(string materialName, out SurfaceAssets.Kind kind, out float detail)
        {
            detail = 1f;
            kind = default(SurfaceAssets.Kind);

            if (materialName.Contains("darkleaf")) { kind = SurfaceAssets.Kind.LeafPine; detail = 2.2f; return true; }
            if (materialName.Contains("leaf")) { kind = SurfaceAssets.Kind.Leaf; detail = 2.2f; return true; }
            if (materialName.Contains("darkstone")) { kind = SurfaceAssets.Kind.StoneDark; detail = 1.4f; return true; }
            if (materialName.Contains("stone")) { kind = SurfaceAssets.Kind.Stone; detail = 1.4f; return true; }
            if (materialName.Contains("plaster")) { kind = SurfaceAssets.Kind.Plaster; detail = 1.3f; return true; }
            if (materialName.Contains("roof")) { kind = SurfaceAssets.Kind.RoofTile; detail = 1.6f; return true; }
            if (materialName.Contains("thatch")) { kind = SurfaceAssets.Kind.Thatch; detail = 1.5f; return true; }
            if (materialName.Contains("wood")) { kind = SurfaceAssets.Kind.Wood; detail = 1.8f; return true; }
            if (materialName.Contains("bark")) { kind = SurfaceAssets.Kind.Bark; detail = 2.4f; return true; }
            if (materialName.Contains("orcskin")) { kind = SurfaceAssets.Kind.Skin; detail = 2.6f; return true; }
            if (materialName.Contains("fur")) { kind = SurfaceAssets.Kind.Fur; detail = 2.4f; return true; }
            if (materialName.Contains("skin")) { kind = SurfaceAssets.Kind.Skin; detail = 3f; return true; }
            if (materialName.Contains("bone")) { kind = SurfaceAssets.Kind.Bone; detail = 2f; return true; }
            if (materialName.Contains("metal")) { kind = SurfaceAssets.Kind.Metal; detail = 2f; return true; }
            if (materialName.Contains("ember")) { kind = SurfaceAssets.Kind.Ember; detail = 2f; return true; }
            if (materialName.Contains("leather")) { kind = SurfaceAssets.Kind.Leather; detail = 2.4f; return true; }
            if (materialName.Contains("vampcloth")) { kind = SurfaceAssets.Kind.Cloth; detail = 2.6f; return true; }
            if (materialName.Contains("hood") || materialName.Contains("cloth")
                || materialName.Contains("accent") || materialName.Contains("blood"))
            {
                kind = SurfaceAssets.Kind.Cloth;
                detail = 2.6f;
                return true;
            }
            if (materialName.Contains("hair")) { kind = SurfaceAssets.Kind.Fur; detail = 3f; return true; }
            if (materialName.Contains("grass")) { kind = SurfaceAssets.Kind.Grass; detail = 1.2f; return true; }
            if (materialName.Contains("dirt")) { kind = SurfaceAssets.Kind.Dirt; detail = 1.4f; return true; }

            return false;
        }

        static float ScaleFor(string id, float height)
        {
            if (height <= 0.01f) return 1.4f;
            if (id == "spire" || id == "watchtower") return 3.6f;
            if (id == "pine" || id == "oak") return 1.6f;
            return Mathf.Clamp(height / 2.4f, 1f, 2.6f);
        }

        public static Material Lit(Color color, float smoothness = 0.06f, float metallic = 0f)
        {
            var material = new Material(SurfaceAssets.SurfaceShader());
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        static GameObject Fallback(string id, Transform parent, Vector3 position, float yaw)
        {
            var root = new GameObject(id);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Color color = new Color(0.55f, 0.47f, 0.36f);
            if (id == "hero") color = new Color(0.42f, 0.52f, 0.35f);
            else if (id == "orc") color = new Color(0.35f, 0.43f, 0.24f);
            else if (id == "vampire") color = new Color(0.22f, 0.18f, 0.26f);
            else if (id == "wolf") color = new Color(0.2f, 0.26f, 0.2f);
            else if (id == "skeleton") color = new Color(0.82f, 0.79f, 0.68f);
            else if (id == "pine" || id == "oak") color = new Color(0.18f, 0.3f, 0.16f);

            var body = GameObject.CreatePrimitive(id == "pine" ? PrimitiveType.Cylinder : PrimitiveType.Capsule);
            body.name = "body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, id == "pine" ? 4f : 0.9f, 0f);
            body.transform.localScale = id == "pine" ? new Vector3(0.5f, 4f, 0.5f) : new Vector3(0.7f, 0.9f, 0.7f);
            var renderer = body.GetComponent<Renderer>();
            var material = Lit(color);
            renderer.material = material;
            return root;
        }
    }

    public class WorldFx : MonoBehaviour
    {
        public static void Burst(WorldScene scene, Vector3 position, Color color, int count, float speed, float size = 0.16f)
        {
            var root = new GameObject("Burst");
            root.transform.SetParent(scene.transform, false);
            root.transform.position = position;

            var material = WorldModels.Lit(color, 0.2f);
            for (int i = 0; i < count; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "shard";
                shard.transform.SetParent(root.transform, false);
                shard.transform.localPosition = Random.insideUnitSphere * 0.2f;
                shard.transform.localScale = Vector3.one * Random.Range(size * 0.5f, size);
                shard.transform.rotation = Random.rotation;
                shard.GetComponent<Renderer>().material = material;
                var body = shard.AddComponent<WorldShard>();
                body.Velocity = (Random.onUnitSphere + Vector3.up * 1.1f).normalized * Random.Range(speed * 0.4f, speed);
                body.Life = Random.Range(0.35f, 0.7f);
            }
            Destroy(root, 1.2f);
        }
    }

    public class WorldShard : MonoBehaviour
    {
        public Vector3 Velocity;
        public float Life = 0.5f;
        float age;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Velocity += Vector3.down * 9f * dt;
            transform.position += Velocity * dt;
            float k = 1f - Mathf.Clamp01(age / Life);
            transform.localScale = Vector3.one * Mathf.Max(0.001f, transform.localScale.x * (1f - 2.5f * dt));
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                var color = renderer.material.color;
                color.a = k;
                renderer.material.color = color;
            }
            if (age > Life) Destroy(gameObject);
        }
    }
}
