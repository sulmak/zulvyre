using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class VillageScene : MonoBehaviour
    {
        class NpcEntry
        {
            public string Name;
            public string Title;
            public string Sprite;
            public float Height;
            public Vector2 Position;
            public string[] Lines;
            public bool IsRival;
            public bool IsElder;
            public bool IsSmith;
            public bool IsWayfarer;
            public Transform Root;
            public SpriteRenderer Renderer;
            public SpriteRenderer Shadow;
            public SpriteRenderer Aura;
            public float Phase;
        }

        class Firefly
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 Origin;
            public float Phase;
            public float Speed;
            public float BaseAlpha;
        }

        class Glow
        {
            public SpriteRenderer Renderer;
            public float BaseAlpha;
            public float Phase;
        }

        static readonly Rect[] Houses =
        {
            new Rect(-5.95f, 3.0f, 1.7f, 1.55f),
            new Rect(-4.3f, 3.1f, 0.95f, 0.62f),
            new Rect(-2.4f, 1.65f, 1.8f, 1.45f),
            new Rect(1.95f, 2.3f, 1.65f, 1.4f),
            new Rect(3.05f, 0.7f, 2.45f, 1.85f),
            new Rect(4.6f, 2.6f, 3.05f, 1.85f),
            new Rect(-8.35f, 1.15f, 2.35f, 2.0f),
            new Rect(-3.7f, -3.05f, 2.4f, 1.6f),
            new Rect(-0.6f, -4.1f, 2.45f, 1.6f),
            new Rect(2.2f, -2.3f, 1.4f, 0.9f)
        };

        const float RiverLow = -1.08f;
        const float RiverHigh = 1.52f;
        const float BridgeLeft = -0.34f;
        const float BridgeRight = 0.92f;
        const float PlayerRadius = 0.22f;

        const float NavCell = 0.25f;
        const int NavWidth = 68;
        const int NavHeight = 37;
        static readonly Vector2 NavOrigin = new Vector2(-8.4f, -4.6f);

        ZulvyreGame game;
        HeroDef hero;
        GameObject worldRoot;
        Transform player;
        Transform playerVisual;
        SpriteRenderer playerRenderer;
        SpriteRenderer playerShadow;
        Vector2 playerPosition;
        int facing = 1;
        float walkPhase;
        float stepTimer;
        float locationTimer;
        float toastTimer;
        string toastText;
        bool paused;
        bool dialogueOpen;
        NpcEntry dialogueNpc;
        NpcEntry nearbyNpc;
        int dialogueLine;
        float typeProgress;
        float markerPulse;
        Transform markerRoot;
        SpriteRenderer markerRune;
        SpriteRenderer markerRing;

        readonly List<NpcEntry> npcs = new List<NpcEntry>();
        readonly List<Firefly> fireflies = new List<Firefly>();
        readonly List<Glow> glows = new List<Glow>();

        bool spokenElder;
        bool spokenFriend;

        NpcEntry pendingNpc;
        float moveTimeout;
        List<Vector2> movePath;
        int pathIndex;
        bool[] navGrid;

        public void Enter(HeroDef selectedHero)
        {
            game = ZulvyreGame.Instance;
            hero = selectedHero;

            if (worldRoot != null) Destroy(worldRoot);
            npcs.Clear();
            fireflies.Clear();
            glows.Clear();

            spokenElder = false;
            spokenFriend = false;
            paused = false;
            dialogueOpen = false;
            dialogueNpc = null;
            nearbyNpc = null;
            dialogueLine = 0;
            typeProgress = 0f;
            movePath = null;
            pathIndex = 0;
            pendingNpc = null;
            moveTimeout = 0f;
            locationTimer = 5.2f;
            toastTimer = game != null && game.DuelWon ? 4f : 0f;
            toastText = game != null && game.DuelWon ? "The mask is broken. Alderwick is at peace." : string.Empty;
            walkPhase = 0f;
            stepTimer = 0f;

            worldRoot = new GameObject("VillageWorld");
            worldRoot.transform.SetParent(transform, false);

            CreateBackground();
            CreatePlayer();
            CreateNpcs();
            CreateMarker();
            CreateLights();

            if (game != null)
            {
                game.ConfigureCamera(4.15f, CameraTarget());
            }
        }

        void CreateBackground()
        {
            var renderer = NewSprite("Village Backdrop", ImageAssets.BackgroundSprite("village"), worldRoot.transform,
                new Vector3(0f, 0f, 0f), -2000, Color.white);
            renderer.transform.localScale = Vector3.one;

            var warm = new Color(1f, 0.93f, 0.8f, 0.06f);
            NewSprite("Warm Wash", ImageAssets.DotSprite, worldRoot.transform, new Vector3(0f, 0f, 0f), -1990, warm,
                new Vector2(26f, 16f));
        }

        void CreatePlayer()
        {
            playerPosition = new Vector2(1.35f, -1.95f);
            facing = 1;

            var root = new GameObject("Hero");
            root.transform.SetParent(worldRoot.transform, false);
            root.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
            player = root.transform;

            var visual = NewSprite("Hero Visual", ImageAssets.CharacterSprite(hero.SpriteName, 2.08f), player,
                Vector3.zero, 0, Color.white);
            playerRenderer = visual;
            playerVisual = visual.transform;
            playerVisual.localPosition = Vector3.zero;

            playerShadow = NewSprite("Hero Shadow", ImageAssets.ShadowSprite, player, new Vector3(0f, 0.025f, 0f), 0,
                new Color(0f, 0f, 0f, 0.55f), new Vector2(1.55f, 0.9f));

            UpdatePlayerSorting();
        }

        void CreateNpcs()
        {
            var other = HeroRoster.OtherThan(hero, 1);

            npcs.Add(MakeNpc("Elder Maren", "Keeper of Alderwick", "elder", 1.95f, new Vector2(1.62f, 2.12f),
                game != null && game.DuelWon
                    ? new[]
                    {
                        "You are still standing. The lanterns burn a little brighter for it.",
                        "Vesper left the mask on the dock. A strange farewell, but the village will take the quiet.",
                        "Rest now, wayfarer. Alderwick remembers."
                    }
                    : new[]
                    {
                        "You came at last. The lanterns have been restless since the masked one arrived.",
                        "Vesper waits by the south dock, blade wrapped in cloth. No one has lasted a minute.",
                        "Learn the rhythm of the circle: strike, breathe, then step away. The river teaches the same lesson.",
                        "When you are ready, speak to Vesper. The duel is yours to begin."
                    }, true, false, false));

            npcs.Add(MakeNpc("Harl", "Village Smith", "smith", 2.14f, new Vector2(6.15f, 2.0f),
                new[]
                {
                    "Heavy steel wants a deep breath. Wind up before the swing, or you will be the one on the ground.",
                    "Your stamina is a purse, friend. Spend it, then give it a breath to fill again.",
                    "That masked one moves like smoke. Make the circle small and do not chase."
                }, false, true, false));

            npcs.Add(MakeNpc(other.Name, "Fellow Wayfarer", other.SpriteName, 1.98f, new Vector2(-4.25f, -2.2f),
                new[]
                {
                    "I crossed the bridge this morning. The masked duelist watched me the whole way.",
                    "Press Shift to slip a strike, Q to wake your art. Vesper will not expect a traveller who knows when to move.",
                    "If your breath runs short, back away. The circle is patient, even if the crowd is not."
                }, false, false, true));

            if (game == null || !game.DuelWon)
            {
                var rival = MakeNpc("Vesper", "The Masked Duelist", "rival", 2.18f, new Vector2(2.72f, -2.42f),
                    new[]
                    {
                        "So the village found another champion.",
                        "I am Vesper. When the lanterns dim, we settle this in the circle by the water.",
                        "Come. Let us see what the road taught you."
                    }, false, false, false);
                rival.IsRival = true;
                rival.Aura = NewSprite("Rival Aura", ImageAssets.DotSprite, rival.Root, new Vector3(0f, 1.15f, 0f), 0,
                    new Color(0.45f, 0.18f, 0.32f, 0.34f), new Vector2(2.6f, 3.0f));
                npcs.Add(rival);
            }
        }

        NpcEntry MakeNpc(string name, string title, string sprite, float height, Vector2 position, string[] lines,
            bool elder, bool smith, bool wayfarer)
        {
            var root = new GameObject(name);
            root.transform.SetParent(worldRoot.transform, false);
            root.transform.position = new Vector3(position.x, position.y, 0f);

            var visual = NewSprite(name + " Visual", ImageAssets.CharacterSprite(sprite, height), root.transform, Vector3.zero, 0, Color.white);
            var shadow = NewSprite(name + " Shadow", ImageAssets.ShadowSprite, root.transform, new Vector3(0f, 0.025f, 0f), 0,
                new Color(0f, 0f, 0f, 0.5f), new Vector2(1.5f, 0.85f));

            return new NpcEntry
            {
                Name = name,
                Title = title,
                Sprite = sprite,
                Height = height,
                Position = position,
                Lines = lines,
                IsElder = elder,
                IsSmith = smith,
                IsWayfarer = wayfarer,
                Root = root.transform,
                Renderer = visual,
                Shadow = shadow,
                Phase = Random.Range(0f, Mathf.PI * 2f)
            };
        }

        void CreateMarker()
        {
            markerRoot = new GameObject("Duel Sigil").transform;
            markerRoot.SetParent(worldRoot.transform, false);
            markerRoot.position = new Vector3(2.72f, -2.42f, 0f);

            markerRune = NewSprite("Sigil", ImageAssets.RuneSprite, markerRoot, new Vector3(0f, 0.03f, 0f), -80,
                new Color(0.82f, 0.65f, 0.35f, 0.55f), new Vector2(2.3f, 1.15f));
            markerRing = NewSprite("Sigil Ring", ImageAssets.RingSprite, markerRoot, new Vector3(0f, 0.035f, 0f), -79,
                new Color(1f, 0.8f, 0.45f, 0.45f), new Vector2(1.65f, 0.55f));

            if (game != null && game.DuelWon)
            {
                markerRoot.gameObject.SetActive(false);
            }
        }

        void CreateLights()
        {
            AddGlow(new Vector2(6.5f, 2.85f), new Color(1f, 0.62f, 0.26f), 4.0f, 0.16f, 0.2f);
            AddGlow(new Vector2(2.7f, 2.95f), new Color(1f, 0.68f, 0.32f), 3.0f, 0.13f, 1.4f);
            AddGlow(new Vector2(-1.35f, 2.45f), new Color(1f, 0.66f, 0.3f), 2.7f, 0.12f, 2.7f);
            AddGlow(new Vector2(-2.3f, -2.0f), new Color(1f, 0.63f, 0.28f), 3.0f, 0.14f, 3.9f);
            AddGlow(new Vector2(3.0f, -1.85f), new Color(1f, 0.7f, 0.35f), 2.6f, 0.13f, 4.8f);
            AddGlow(new Vector2(0.35f, -0.85f), new Color(1f, 0.72f, 0.4f), 2.4f, 0.1f, 5.9f);
            AddGlow(new Vector2(-7.0f, 1.9f), new Color(1f, 0.62f, 0.28f), 3.1f, 0.13f, 0.9f);
            AddGlow(new Vector2(4.6f, -0.2f), new Color(0.55f, 0.82f, 0.95f), 2.8f, 0.09f, 2.2f);

            var rng = new System.Random(1129);
            for (int i = 0; i < 30; i++)
            {
                var position = new Vector3(-7.2f + (float)rng.NextDouble() * 14.4f, -4.1f + (float)rng.NextDouble() * 8.1f, 0f);
                if (position.y < RiverHigh && position.y > RiverLow) position.y += 2.6f;
                var renderer = NewSprite("Firefly", ImageAssets.DotSprite, worldRoot.transform, position, 1350,
                    new Color(1f, 0.86f, 0.48f, 0.5f), new Vector2(0.09f, 0.09f));
                fireflies.Add(new Firefly
                {
                    Transform = renderer.transform,
                    Renderer = renderer,
                    Origin = position,
                    Phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                    Speed = 0.4f + (float)rng.NextDouble() * 0.7f,
                    BaseAlpha = 0.25f + (float)rng.NextDouble() * 0.45f
                });
            }
        }

        void AddGlow(Vector2 position, Color color, float size, float alpha, float phase)
        {
            var renderer = NewSprite("Lantern Glow", ImageAssets.DotSprite, worldRoot.transform,
                new Vector3(position.x, position.y + 0.7f, 0f), -120, new Color(color.r, color.g, color.b, alpha),
                new Vector2(size, size * 0.9f));
            glows.Add(new Glow { Renderer = renderer, BaseAlpha = alpha, Phase = phase });
        }

        SpriteRenderer NewSprite(string name, Sprite sprite, Transform parent, Vector3 localPosition, int order, Color color, Vector2? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (scale.HasValue && scale.Value != Vector2.zero)
            {
                go.transform.localScale = new Vector3(scale.Value.x, scale.Value.y, 1f);
            }
            return renderer;
        }

        void Update()
        {
            if (game == null) game = ZulvyreGame.Instance;
            if (game == null || worldRoot == null) return;

            float dt = Time.deltaTime;
            if (locationTimer > 0f) locationTimer = Mathf.Max(0f, locationTimer - dt);
            if (toastTimer > 0f) toastTimer = Mathf.Max(0f, toastTimer - dt);

            if (dialogueOpen)
            {
                UpdateDialogue(dt);
                UpdateWorldAmbience(dt);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                paused = !paused;
                return;
            }
            if (paused)
            {
                return;
            }

            UpdateMovement(dt);
            UpdateNearbyNpc();

            if (pendingNpc != null)
            {
                if (Vector2.Distance(playerPosition, pendingNpc.Position) <= 2.0f)
                {
                    var npc = pendingNpc;
                    pendingNpc = null;
                    movePath = null;
                    OpenDialogue(npc);
                    return;
                }
                else if (movePath == null)
                {
                    pendingNpc = null;
                }
            }

            UpdateWorldAmbience(dt);

            if (nearbyNpc != null && Input.GetKeyDown(KeyCode.E))
            {
                OpenDialogue(nearbyNpc);
            }
        }

        void LateUpdate()
        {
            if (game == null || worldRoot == null) return;
            var target = CameraTarget() + (Vector3)game.Fx.ShakeOffset;
            var camera = game.GameCamera;
            camera.transform.position = Vector3.Lerp(camera.transform.position, target, 1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        Vector3 CameraTarget()
        {
            float halfHeight = game != null ? game.GameCamera.orthographicSize : 4.15f;
            float halfWidth = halfHeight * (game != null ? game.GameCamera.aspect : 16f / 9f);
            float maxX = Mathf.Max(0f, 8.96f - halfWidth);
            float maxY = Mathf.Max(0f, 5.12f - halfHeight);
            float x = Mathf.Clamp(playerPosition.x, -maxX, maxX);
            float y = Mathf.Clamp(playerPosition.y + 0.68f, -maxY, maxY);
            return new Vector3(x, y, -10f);
        }

        void UpdateMovement(float dt)
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

            bool keyboard = input.sqrMagnitude > 0.001f;
            if (keyboard)
            {
                movePath = null;
                pendingNpc = null;
            }
            else if (movePath != null)
            {
                FollowPath(ref input, dt);
            }

            bool moving = input.sqrMagnitude > 0.001f;
            if (moving)
            {
                input.Normalize();
                bool running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float speed = running ? 4.7f : 3.1f;
                TryMove(input * speed * dt);

                if (Mathf.Abs(input.x) > 0.05f) facing = input.x > 0f ? 1 : -1;
                walkPhase += dt * (running ? 13.5f : 9.5f);
                float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.06f;
                float sway = Mathf.Sin(walkPhase) * 3.2f;
                playerVisual.localPosition = new Vector3(0f, bob, 0f);
                playerVisual.localRotation = Quaternion.Euler(0f, 0f, sway * (running ? 1.2f : 0.8f));
                playerVisual.localScale = new Vector3(1.01f, 0.985f + bob * 0.3f, 1f);
                playerRenderer.flipX = facing < 0;

                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    stepTimer = running ? 0.23f : 0.31f;
                    if (game.Audio != null) game.Audio.PlayStep();
                    game.Fx.Dust(new Vector3(playerPosition.x, playerPosition.y + 0.02f, 0f),
                        new Color(0.36f, 0.31f, 0.22f, 0.36f), 2, running ? 0.85f : 0.55f);
                }
            }
            else
            {
                walkPhase += dt * 2f;
                playerVisual.localPosition = new Vector3(0f, Mathf.Sin(walkPhase) * 0.012f, 0f);
                playerVisual.localRotation = Quaternion.identity;
                playerVisual.localScale = new Vector3(1f, 1f + Mathf.Sin(walkPhase) * 0.004f, 1f);
            }

            player.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
            UpdatePlayerSorting();
        }

        void TryMove(Vector2 delta)
        {
            Vector2 candidate = new Vector2(playerPosition.x + delta.x, playerPosition.y);
            if (!Blocked(candidate)) playerPosition.x = candidate.x;

            candidate = new Vector2(playerPosition.x, playerPosition.y + delta.y);
            if (!Blocked(candidate)) playerPosition.y = candidate.y;
        }

        bool Blocked(Vector2 point)
        {
            if (point.x < -8.4f + PlayerRadius || point.x > 8.4f - PlayerRadius) return true;
            if (point.y < -4.6f + PlayerRadius || point.y > 4.5f - PlayerRadius) return true;

            if (point.y > RiverLow - PlayerRadius && point.y < RiverHigh + PlayerRadius)
            {
                if (point.x < BridgeLeft + PlayerRadius || point.x > BridgeRight - PlayerRadius) return true;
            }

            for (int i = 0; i < Houses.Length; i++)
            {
                Rect house = Houses[i];
                if (point.x > house.xMin - PlayerRadius && point.x < house.xMax + PlayerRadius &&
                    point.y > house.yMin - PlayerRadius && point.y < house.yMax + PlayerRadius)
                {
                    return true;
                }
            }

            Vector2 well = new Vector2(0.62f, 1.95f);
            if ((point - well).sqrMagnitude < 0.68f * 0.68f) return true;

            return false;
        }

        void UpdatePlayerSorting()
        {
            int order = 1000 - Mathf.RoundToInt(playerPosition.y * 20f);
            if (playerRenderer != null) playerRenderer.sortingOrder = order;
            if (playerShadow != null) playerShadow.sortingOrder = order - 10;
        }

        void UpdateWorldAmbience(float dt)
        {
            float time = Time.time;
            markerPulse += dt;

            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.Root == null) continue;
                float bob = Mathf.Sin(time * 1.35f + npc.Phase) * 0.018f;
                if (npc.Renderer != null)
                {
                    npc.Renderer.transform.localPosition = new Vector3(0f, bob, 0f);
                    npc.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.8f + npc.Phase) * 0.6f);
                }
                int order = 1000 - Mathf.RoundToInt(npc.Root.position.y * 20f);
                npc.Renderer.sortingOrder = order;
                npc.Shadow.sortingOrder = order - 10;
                if (npc.Aura != null) npc.Aura.sortingOrder = order - 5;
            }

            for (int i = 0; i < fireflies.Count; i++)
            {
                var firefly = fireflies[i];
                float t = time * firefly.Speed + firefly.Phase;
                Vector3 offset = new Vector3(Mathf.Sin(t) * 0.5f, Mathf.Cos(t * 0.8f) * 0.28f, 0f);
                firefly.Transform.position = firefly.Origin + offset;
                Color color = firefly.Renderer.color;
                color.a = firefly.BaseAlpha * (0.55f + 0.45f * Mathf.Sin(t * 2.1f));
                firefly.Renderer.color = color;
            }

            for (int i = 0; i < glows.Count; i++)
            {
                var glow = glows[i];
                Color color = glow.Renderer.color;
                color.a = glow.BaseAlpha * (0.8f + 0.2f * Mathf.Sin(time * 2.2f + glow.Phase));
                glow.Renderer.color = color;
            }

            if (markerRoot != null && markerRoot.gameObject.activeSelf)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(markerPulse * 2.1f);
                markerRune.color = new Color(0.82f, 0.65f, 0.35f, 0.32f + pulse * 0.3f);
                markerRing.color = new Color(1f, 0.8f, 0.45f, 0.2f + pulse * 0.3f);
                markerRing.transform.localScale = new Vector3(1.65f + pulse * 0.14f, 0.55f + pulse * 0.05f, 1f);
            }
        }

        void UpdateNearbyNpc()
        {
            nearbyNpc = null;
            float best = 2.05f;
            for (int i = 0; i < npcs.Count; i++)
            {
                float distance = Vector2.Distance(playerPosition, npcs[i].Position);
                if (distance < best)
                {
                    best = distance;
                    nearbyNpc = npcs[i];
                }
            }
        }

        void OpenDialogue(NpcEntry npc)
        {
            dialogueNpc = npc;
            dialogueOpen = true;
            dialogueLine = 0;
            typeProgress = 0f;
            paused = false;
            movePath = null;
            pendingNpc = null;
            if (game.Audio != null) game.Audio.PlayClick(0.4f);
        }

        void HandleClick()
        {
            if (paused || dialogueOpen || game == null || game.GameCamera == null) return;
            Event current = Event.current;
            if (current == null || current.type != EventType.MouseUp || current.button != 0) return;

            Vector2 mouse = current.mousePosition;
            if (new Rect(52f, 46f, 366f, 190f).Contains(mouse)) return;
            if (new Rect(1186f, 46f, 362f, 94f).Contains(mouse)) return;

            Vector2 screen = ImGuiTheme.VirtualToScreen(mouse);
            Vector3 world = game.GameCamera.ScreenToWorldPoint(
                new Vector3(screen.x, Screen.height - screen.y, -game.GameCamera.transform.position.z));

            NpcEntry clickedNpc = null;
            float best = 1.05f;
            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                Vector2 body = new Vector2(npc.Position.x, npc.Position.y + npc.Height * 0.45f);
                float distance = Vector2.Distance(new Vector2(world.x, world.y), body);
                if (distance < best)
                {
                    best = distance;
                    clickedNpc = npc;
                }
            }

            if (clickedNpc != null)
            {
                if (Vector2.Distance(playerPosition, clickedNpc.Position) <= 2.0f)
                {
                    OpenDialogue(clickedNpc);
                }
                else
                {
                    Vector2 approach = playerPosition - clickedNpc.Position;
                    if (approach.sqrMagnitude < 0.001f) approach = Vector2.down;
                    approach.Normalize();

                    // The straight approach can sit in the river when the NPC is on the
                    // far bank, so fall back to the cardinals before giving up.
                    Vector2[] candidates =
                    {
                        clickedNpc.Position + approach * 1.6f,
                        clickedNpc.Position + new Vector2(1.6f, 0f),
                        clickedNpc.Position + new Vector2(-1.6f, 0f),
                        clickedNpc.Position + new Vector2(0f, 1.6f),
                        clickedNpc.Position + new Vector2(0f, -1.6f)
                    };
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        if (!WalkTo(candidates[i])) continue;
                        pendingNpc = clickedNpc;
                        return;
                    }
                    return;
                }
                return;
            }

            WalkTo(new Vector2(Mathf.Clamp(world.x, -8.2f, 8.2f), Mathf.Clamp(world.y, -4.4f, 4.3f)));
        }

        bool WalkTo(Vector2 point)
        {
            if (Blocked(point)) return false;
            movePath = FindPath(playerPosition, point);
            if (movePath == null) return false;
            pathIndex = 0;
            moveTimeout = 9f;
            return true;
        }

        void FollowPath(ref Vector2 input, float dt)
        {
            moveTimeout -= dt;
            if (moveTimeout <= 0f || movePath == null || pathIndex >= movePath.Count)
            {
                movePath = null;
                return;
            }

            while (pathIndex < movePath.Count - 1 && ClearLine(playerPosition, movePath[pathIndex + 1]))
            {
                pathIndex++;
            }

            Vector2 delta = movePath[pathIndex] - playerPosition;
            float distance = delta.magnitude;
            if (distance < 0.1f)
            {
                pathIndex++;
                if (pathIndex >= movePath.Count)
                {
                    movePath = null;
                    return;
                }
                delta = movePath[pathIndex] - playerPosition;
                distance = delta.magnitude;
            }
            if (distance > 0.001f) input = delta / distance;
        }

        void BuildNavigation()
        {
            navGrid = new bool[NavWidth * NavHeight];
            for (int y = 0; y < NavHeight; y++)
            {
                for (int x = 0; x < NavWidth; x++)
                {
                    navGrid[y * NavWidth + x] = !Blocked(CellCenter(x, y));
                }
            }
        }

        static Vector2 CellCenter(int x, int y)
        {
            return NavOrigin + new Vector2((x + 0.5f) * NavCell, (y + 0.5f) * NavCell);
        }

        static int CellX(float x)
        {
            return Mathf.Clamp(Mathf.FloorToInt((x - NavOrigin.x) / NavCell), 0, NavWidth - 1);
        }

        static int CellY(float y)
        {
            return Mathf.Clamp(Mathf.FloorToInt((y - NavOrigin.y) / NavCell), 0, NavHeight - 1);
        }

        static bool InGrid(int x, int y)
        {
            return x >= 0 && x < NavWidth && y >= 0 && y < NavHeight;
        }

        bool ClearLine(Vector2 from, Vector2 to)
        {
            float distance = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.15f));
            for (int i = 1; i < steps; i++)
            {
                if (Blocked(Vector2.Lerp(from, to, (float)i / steps))) return false;
            }
            return true;
        }

        List<Vector2> FindPath(Vector2 from, Vector2 to)
        {
            if (navGrid == null) BuildNavigation();
            int startX = CellX(from.x);
            int startY = CellY(from.y);
            int goalX = CellX(to.x);
            int goalY = CellY(to.y);
            if (!NearestFree(ref startX, ref startY)) return null;
            if (!NearestFree(ref goalX, ref goalY)) return null;

            int count = NavWidth * NavHeight;
            var cost = new float[count];
            var came = new int[count];
            var closed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                cost[i] = float.MaxValue;
                came[i] = -1;
            }

            int start = startY * NavWidth + startX;
            int goal = goalY * NavWidth + goalX;
            var open = new List<int> { start };
            cost[start] = 0f;

            int[] dxs = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dys = { 0, 0, 1, -1, 1, -1, 1, -1 };

            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int node = open[i];
                    float score = cost[node] + Heuristic(node, goalX, goalY);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                int current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal) break;
                if (closed[current]) continue;
                closed[current] = true;

                int cx = current % NavWidth;
                int cy = current / NavWidth;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + dxs[k];
                    int ny = cy + dys[k];
                    if (!InGrid(nx, ny)) continue;
                    int next = ny * NavWidth + nx;
                    if (!navGrid[next] || closed[next]) continue;
                    if (k >= 4 && (!navGrid[cy * NavWidth + nx] || !navGrid[ny * NavWidth + cx])) continue;

                    float step = k >= 4 ? 1.41421356f : 1f;
                    float nextCost = cost[current] + step;
                    if (nextCost < cost[next])
                    {
                        cost[next] = nextCost;
                        came[next] = current;
                        open.Add(next);
                    }
                }
            }

            var points = new List<Vector2>();
            if (goal != start)
            {
                int node = goal;
                int guard = 0;
                while (node != start && node != -1 && guard++ < count)
                {
                    points.Add(CellCenter(node % NavWidth, node / NavWidth));
                    node = came[node];
                }
                if (node == -1) return null;
                points.Reverse();
            }
            points.Add(to);
            return points;
        }

        bool NearestFree(ref int cx, ref int cy)
        {
            if (InGrid(cx, cy) && navGrid[cy * NavWidth + cx]) return true;
            for (int radius = 1; radius <= 6; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (InGrid(nx, ny) && navGrid[ny * NavWidth + nx])
                        {
                            cx = nx;
                            cy = ny;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static float Heuristic(int node, int goalX, int goalY)
        {
            int dx = Mathf.Abs(node % NavWidth - goalX);
            int dy = Mathf.Abs(node / NavWidth - goalY);
            return Mathf.Max(dx, dy) + 0.41421356f * Mathf.Min(dx, dy);
        }

        public Vector2 DebugPlayerPosition { get { return playerPosition; } }

        public void DebugWalkTo(Vector2 point)
        {
            WalkTo(point);
        }

        public void DebugTalkTo(string name)
        {
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].Name != name) continue;
                OpenDialogue(npcs[i]);
                return;
            }
        }

        void UpdateDialogue(float dt)
        {
            if (dialogueNpc == null)
            {
                dialogueOpen = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDialogue(false);
                return;
            }

            string line = CurrentLine();
            if (typeProgress < line.Length)
            {
                typeProgress = Mathf.Min(line.Length, typeProgress + dt * 52f);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                {
                    typeProgress = line.Length;
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                ContinueDialogue();
            }
        }

        string CurrentLine()
        {
            if (dialogueNpc == null || dialogueNpc.Lines.Length == 0) return string.Empty;
            return dialogueNpc.Lines[Mathf.Clamp(dialogueLine, 0, dialogueNpc.Lines.Length - 1)];
        }

        bool IsLastLine()
        {
            return dialogueNpc != null && dialogueLine >= dialogueNpc.Lines.Length - 1;
        }

        void ContinueDialogue()
        {
            if (dialogueNpc == null) return;
            string line = CurrentLine();
            if (typeProgress < line.Length)
            {
                typeProgress = line.Length;
                return;
            }

            if (!IsLastLine())
            {
                dialogueLine++;
                typeProgress = 0f;
                if (game.Audio != null) game.Audio.PlayClick(0.3f);
                return;
            }

            if (dialogueNpc.IsRival)
            {
                StartDuel();
            }
            else
            {
                CloseDialogue(true);
            }
        }

        void CloseDialogue(bool reward)
        {
            if (reward && dialogueNpc != null)
            {
                bool changed = false;
                if (dialogueNpc.IsElder && !spokenElder)
                {
                    spokenElder = true;
                    changed = true;
                }
                if ((dialogueNpc.IsSmith || dialogueNpc.IsWayfarer) && !spokenFriend)
                {
                    spokenFriend = true;
                    changed = true;
                }
                if (changed) ShowToast("Quest updated  ·  The Masked Duelist");
            }

            dialogueOpen = false;
            dialogueNpc = null;
            dialogueLine = 0;
            typeProgress = 0f;
        }

        void StartDuel()
        {
            if (game == null) return;
            dialogueOpen = false;
            dialogueNpc = null;
            game.EnterDuel(hero, true);
        }

        void ShowToast(string text)
        {
            toastText = text;
            toastTimer = 3.4f;
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            if (game == null || worldRoot == null) return;

            ImGuiTheme.Begin();
            DrawNameplates();
            game.Fx.DrawNumbers();
            ImGuiTheme.Vignette(0.5f);
            DrawQuestPanel();
            DrawHeroPanel();
            DrawLocationBanner();
            DrawToast();

            if (!paused && !dialogueOpen) DrawInteractionPrompt();
            if (!paused) DrawFooter();

            if (dialogueOpen) DrawDialogue();
            if (paused) DrawPause();

            HandleClick();

            ImGuiTheme.End();
        }

        void DrawNameplates()
        {
            var camera = game.GameCamera;
            for (int i = 0; i < npcs.Count; i++)
            {
                var npc = npcs[i];
                if (npc.Root == null) continue;
                float distance = Vector2.Distance(playerPosition, npc.Position);
                if (distance > 7f) continue;

                Vector3 world = npc.Root.position + new Vector3(0f, npc.Height + 0.18f, 0f);
                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z < 0f) continue;
                Vector2 point = ImGuiTheme.ScreenToVirtual(new Vector2(screen.x, Screen.height - screen.y));
                if (point.x < -200f || point.x > 1800f || point.y < -40f || point.y > 940f) continue;

                float alpha = Mathf.Clamp01((7f - distance) / 3f);
                bool near = npc == nearbyNpc;
                Color color = npc.IsRival ? new Color(0.95f, 0.55f, 0.58f, alpha) : new Color(0.9f, 0.84f, 0.68f, alpha);
                if (near) color = ImGuiTheme.GoldBright;

                ImGuiTheme.Text(new Rect(point.x - 160f, point.y - 12f, 320f, 24f), npc.Name, 11, color, TextAnchor.MiddleCenter, true);
                if (near)
                {
                    ImGuiTheme.Fill(new Rect(point.x - 4f, point.y - 22f, 8f, 8f), ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.95f));
                }
            }
        }

        void DrawQuestPanel()
        {
            var panel = new Rect(52f, 46f, 366f, 190f);
            ImGuiTheme.Panel(panel, new Color(0.025f, 0.045f, 0.034f, 0.86f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.3f), 1f, true);
            ImGuiTheme.Fill(new Rect(panel.x, panel.y, 3f, panel.height), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.7f));

            string title = game.DuelWon ? "ALDERWICK AT PEACE" : "THE MASKED DUELIST";
            ImGuiTheme.SpacedText(new Rect(panel.x + 22f, panel.y + 16f, panel.width - 44f, 20f), title, 11, ImGuiTheme.Gold, 2.4f, TextAnchor.MiddleLeft);
            ImGuiTheme.HLine(panel.x + 22f, panel.xMax - 22f, panel.y + 44f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.22f));

            if (game.DuelWon)
            {
                DrawObjective(panel.x + 22f, panel.y + 60f, true, "Vesper has yielded");
                DrawObjective(panel.x + 22f, panel.y + 88f, true, "Speak with Elder Maren");
                DrawObjective(panel.x + 22f, panel.y + 116f, false, "The lanterns burn easy tonight");
                ImGuiTheme.Text(new Rect(panel.x + 22f, panel.yMax - 28f, panel.width - 44f, 16f), "Alderwick remembers your name.", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f));
            }
            else
            {
                DrawObjective(panel.x + 22f, panel.y + 60f, spokenElder, "Speak with Elder Maren");
                DrawObjective(panel.x + 22f, panel.y + 88f, spokenFriend, "Trade words with a wayfarer");
                DrawObjective(panel.x + 22f, panel.y + 116f, false, "Challenge Vesper at the south dock");
                ImGuiTheme.Text(new Rect(panel.x + 22f, panel.yMax - 28f, panel.width - 44f, 16f), "Reward  ·  Alderwick's gratitude", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f));
            }
        }

        void DrawObjective(float x, float y, bool complete, string text)
        {
            if (complete)
            {
                ImGuiTheme.Fill(new Rect(x, y + 2f, 10f, 10f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.85f));
                ImGuiTheme.Fill(new Rect(x + 3f, y + 5f, 4f, 4f), ImGuiTheme.Ink);
            }
            else
            {
                ImGuiTheme.Panel(new Rect(x, y + 2f, 10f, 10f), Color.clear, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.7f));
            }
            Color color = complete ? ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.62f) : ImGuiTheme.Parchment;
            ImGuiTheme.Text(new Rect(x + 20f, y - 2f, 300f, 20f), text, 12, color);
        }

        void DrawHeroPanel()
        {
            var panel = new Rect(1186f, 46f, 362f, 94f);
            ImGuiTheme.Panel(panel, new Color(0.025f, 0.045f, 0.034f, 0.84f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.26f));
            ImGuiTheme.Fill(new Rect(panel.x, panel.y, panel.width, 2f), ImGuiTheme.WithAlpha(hero.Accent, 0.8f));

            var glow = new Rect(panel.x + 18f, panel.y + 24f, 46f, 46f);
            var previous = GUI.color;
            GUI.color = ImGuiTheme.WithAlpha(hero.Color, 0.85f);
            GUI.DrawTexture(glow, ImageAssets.Dot, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            ImGuiTheme.Text(glow, hero.Name.Substring(0, 1), 22, ImGuiTheme.GoldBright, TextAnchor.MiddleCenter, true);

            ImGuiTheme.Text(new Rect(panel.x + 76f, panel.y + 12f, 260f, 24f), hero.Name, 19, ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 77f, panel.y + 38f, 260f, 16f), hero.Title.ToUpperInvariant(), 9, ImGuiTheme.WithAlpha(hero.Accent, 0.9f), 1.6f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(panel.x + 76f, panel.y + 58f, 40f, 14f), "VIT", 8, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f));
            ImGuiTheme.Bar(new Rect(panel.x + 106f, panel.y + 59f, 232f, 6f), 1f, new Color(0.72f, 0.28f, 0.24f), new Color(0.16f, 0.08f, 0.07f));
            ImGuiTheme.Text(new Rect(panel.x + 76f, panel.y + 69f, 40f, 14f), "STA", 8, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f));
            ImGuiTheme.Bar(new Rect(panel.x + 106f, panel.y + 70f, 232f, 4f), 1f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.8f), new Color(0.14f, 0.12f, 0.07f));
        }

        void DrawLocationBanner()
        {
            if (locationTimer <= 0f) return;
            float alpha = Mathf.Clamp01(locationTimer / 1.2f) * Mathf.Clamp01((5.2f - locationTimer) / 0.8f);
            ImGuiTheme.SpacedText(new Rect(500f, 72f, 600f, 44f), "ALDERWICK", 34, new Color(0.96f, 0.85f, 0.63f, alpha), 7f);
            ImGuiTheme.Text(new Rect(500f, 116f, 600f, 20f), "Riverside village  ·  Dusk", 12, new Color(0.85f, 0.82f, 0.7f, alpha * 0.9f), TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(690f, 910f, 148f, new Color(0.82f, 0.65f, 0.35f, alpha * 0.35f));
        }

        void DrawToast()
        {
            if (toastTimer <= 0f || string.IsNullOrEmpty(toastText)) return;
            float alpha = Mathf.Clamp01(toastTimer / 0.6f) * Mathf.Clamp01((3.4f - toastTimer) / 0.4f);
            var panel = new Rect(560f, 184f, 480f, 38f);
            ImGuiTheme.Panel(panel, new Color(0.05f, 0.08f, 0.06f, 0.9f * alpha), new Color(0.82f, 0.65f, 0.35f, 0.5f * alpha));
            ImGuiTheme.Text(panel, toastText, 12, new Color(0.95f, 0.87f, 0.68f, alpha), TextAnchor.MiddleCenter);
        }

        void DrawInteractionPrompt()
        {
            if (nearbyNpc == null) return;
            string verb = nearbyNpc.IsRival ? "Challenge " + nearbyNpc.Name : "Speak with " + nearbyNpc.Name;
            var panel = new Rect(590f, 782f, 420f, 44f);
            bool hover = panel.Contains(Event.current.mousePosition);
            ImGuiTheme.Panel(panel, new Color(0.03f, 0.05f, 0.038f, 0.9f),
                ImGuiTheme.WithAlpha(ImGuiTheme.Gold, hover ? 0.8f : 0.45f));
            ImGuiTheme.Fill(new Rect(panel.x + 14f, panel.y + 13f, 18f, 18f), ImGuiTheme.WithAlpha(hover ? ImGuiTheme.GoldBright : ImGuiTheme.Gold, 0.9f));
            ImGuiTheme.Text(new Rect(panel.x + 14f, panel.y + 12f, 18f, 18f), "E", 11, ImGuiTheme.Ink, TextAnchor.MiddleCenter, false);
            ImGuiTheme.Text(new Rect(panel.x + 42f, panel.y, panel.width - 52f, panel.height), verb + "   ·   click", 13,
                hover ? ImGuiTheme.GoldBright : ImGuiTheme.Parchment);
            if (ImGuiTheme.Clicked(panel))
            {
                OpenDialogue(nearbyNpc);
            }
        }

        void DrawFooter()
        {
            ImGuiTheme.SpacedText(new Rect(940f, 844f, 612f, 18f), "WASD OR CLICK TO MOVE   ·   SHIFT RUN   ·   E / CLICK INTERACT   ·   ESC PAUSE", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.78f), 1.2f, TextAnchor.MiddleRight, false);
        }

        void DrawDialogue()
        {
            if (dialogueNpc == null) return;
            var panel = new Rect(200f, 624f, 1200f, 226f);
            ImGuiTheme.Panel(panel, new Color(0.022f, 0.04f, 0.03f, 0.97f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.6f), 1f, true);

            var portrait = new Rect(panel.x + 20f, panel.y + 20f, 152f, 186f);
            ImGuiTheme.Panel(portrait, new Color(0.05f, 0.09f, 0.07f, 0.95f), ImGuiTheme.WithAlpha(dialogueNpc.IsRival ? ImGuiTheme.Blood : ImGuiTheme.Gold, 0.28f));
            GUI.BeginGroup(portrait);
            ImGuiTheme.TextureFit(new Rect(0f, -6f, portrait.width, 310f), ImageAssets.CharacterTexture(dialogueNpc.Sprite), Color.white);
            ImGuiTheme.Fill(new Rect(0f, portrait.height - 30f, portrait.width, 30f), new Color(0.02f, 0.04f, 0.03f, 0.6f));
            GUI.EndGroup();

            Color nameColor = dialogueNpc.IsRival ? new Color(1f, 0.68f, 0.68f) : ImGuiTheme.GoldBright;
            ImGuiTheme.Text(new Rect(panel.x + 196f, panel.y + 16f, 600f, 32f), dialogueNpc.Name, 24, nameColor, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 198f, panel.y + 50f, 600f, 18f), dialogueNpc.Title.ToUpperInvariant(), 10, ImGuiTheme.WithAlpha(dialogueNpc.IsRival ? ImGuiTheme.Blood : ImGuiTheme.Gold, 0.9f), 2.2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.HLine(panel.x + 196f, panel.xMax - 28f, panel.y + 74f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.2f));

            string line = CurrentLine();
            int visible = Mathf.Clamp(Mathf.FloorToInt(typeProgress), 0, line.Length);
            string shown = line.Substring(0, visible);
            ImGuiTheme.Label(new Rect(panel.x + 196f, panel.y + 88f, 840f, 92f), shown, ImGuiTheme.WrapStyle(16), ImGuiTheme.Parchment, TextAnchor.UpperLeft, false);

            bool complete = visible >= line.Length;
            if (!complete || !IsLastLine())
            {
                string hint = complete ? "[E] or click  ·  Continue" : "[E] or click  ·  Reveal";
                ImGuiTheme.Text(new Rect(panel.xMax - 280f, panel.yMax - 38f, 252f, 22f), hint, 11, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.8f), TextAnchor.MiddleRight);
            }

            if (complete && IsLastLine())
            {
                if (dialogueNpc.IsRival)
                {
                    if (ImGuiTheme.Button(new Rect(panel.xMax - 330f, panel.yMax - 54f, 120f, 36f), "NOT YET"))
                    {
                        CloseDialogue(false);
                    }
                    if (ImGuiTheme.Button(new Rect(panel.xMax - 196f, panel.yMax - 54f, 168f, 36f), "FACE VESPER", true))
                    {
                        StartDuel();
                    }
                }
                else
                {
                    if (ImGuiTheme.Button(new Rect(panel.xMax - 196f, panel.yMax - 54f, 168f, 36f), "FAREWELL", false))
                    {
                        CloseDialogue(true);
                    }
                }
            }

            var actions = new Rect(panel.xMax - 340f, panel.yMax - 64f, 330f, 54f);
            if (!actions.Contains(Event.current.mousePosition) && ImGuiTheme.Clicked(panel))
            {
                ContinueDialogue();
            }
        }

        void DrawPause()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, 0.66f));
            var panel = new Rect(540f, 236f, 520f, 428f);
            ImGuiTheme.Panel(panel, new Color(0.025f, 0.045f, 0.034f, 0.98f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.6f), 1f, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 28f, panel.width - 60f, 40f), "PAUSED", 26, ImGuiTheme.GoldBright, 6f);
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 76f, panel.width - 60f, 22f), "Alderwick waits by the water.", 13, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.85f), TextAnchor.MiddleCenter);

            ImGuiTheme.HLine(panel.x + 50f, panel.xMax - 50f, panel.y + 112f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.2f));
            ImGuiTheme.Label(new Rect(panel.x + 50f, panel.y + 126f, panel.width - 100f, 62f),
                "WASD / arrows move   ·   click ground or a villager\nShift to run   ·   E or click to speak\nEsc to resume", ImGuiTheme.WrapStyle(12), ImGuiTheme.Muted, TextAnchor.UpperCenter, false);
            ImGuiTheme.HLine(panel.x + 50f, panel.xMax - 50f, panel.y + 196f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.2f));

            if (ImGuiTheme.Button(new Rect(panel.x + 40f, panel.y + 216f, panel.width - 80f, 54f), "RESUME", true))
            {
                paused = false;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 40f, panel.y + 286f, 208f, 48f), "MAIN MENU"))
            {
                paused = false;
                game.EnterMenu();
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 272f, panel.y + 286f, 208f, 48f), "QUIT"))
            {
                game.QuitToDesktop();
            }

            ImGuiTheme.Text(new Rect(panel.x + 40f, panel.yMax - 48f, panel.width - 80f, 20f), "M  mute    ·    Alt+Enter  fullscreen", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.7f), TextAnchor.MiddleCenter);
        }
    }
}
