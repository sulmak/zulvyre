using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class DuelScene : MonoBehaviour
    {
        const float IntroDuration = 2.6f;
        const float DuelDuration = 90f;
        // The fight plane is world X/Z; the game-space Y axis is arena depth.
        const float ArenaMinX = -5.6f;
        const float ArenaMaxX = 5.6f;
        const float ArenaMinY = -1.15f;
        const float ArenaMaxY = 1.15f;
        const float ChestHeight = 1.2f;
        const float CameraFov = 36f;
        static readonly Vector3 CameraHome = new Vector3(0f, 4.3f, -11.2f);
        static readonly Vector3 CameraLook = new Vector3(0f, 1.2f, 0.35f);

        static readonly Color HpFill = new Color(0.8f, 0.34f, 0.26f, 1f);
        static readonly Color HpChunk = new Color(0.96f, 0.83f, 0.61f, 0.55f);
        static readonly Color StaminaFill = new Color(0.56f, 0.72f, 0.45f, 1f);
        static readonly Color RivalFill = new Color(0.68f, 0.3f, 0.4f, 1f);
        static readonly Color RivalStamina = new Color(0.62f, 0.55f, 0.7f, 1f);

        class Projectile
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public DuelFighter Caster;
            public DuelFighter Target;
            public Vector3 Start;
            public Vector3 End;
            public float Age;
            public float Duration;
            public float Damage;
            public float Burn;
            public float TrailTimer;
        }

        class Mote
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 Origin;
            public float Phase;
            public float Speed;
            public float Radius;
            public float BaseAlpha;
        }

        public bool CombatActive { get; private set; }

        ZulvyreGame game;
        HeroDef hero;
        bool fromVillage;
        Transform worldRoot;
        DuelFighter player;
        DuelFighter rival;
        SpriteRenderer rune;
        Light sun;
        Material skyMaterial;
        readonly List<Projectile> projectiles = new List<Projectile>();
        readonly List<Mote> motes = new List<Mote>();

        float introTimer;
        float finishBanner;
        float timeLeft;
        float resultDelay = -1f;
        float damageFlash;
        float emberTime;
        float fade = 1f;
        float playerHpLag = 1f;
        float rivalHpLag = 1f;
        string finishText = string.Empty;
        Color finishColor = Color.white;
        bool roundOver;
        bool resultVictory;
        bool resultDraw;
        bool paused;

        public void Enter(HeroDef selectedHero, bool fromVillage)
        {
            game = ZulvyreGame.Instance;
            hero = selectedHero;
            this.fromVillage = fromVillage;

            EnsureWorld();
            RebuildFighters();
            ClearProjectiles();

            introTimer = 0f;
            finishBanner = 0f;
            finishText = string.Empty;
            CombatActive = false;
            timeLeft = DuelDuration;
            roundOver = false;
            resultDelay = -1f;
            resultVictory = false;
            resultDraw = false;
            paused = false;
            damageFlash = 0f;
            fade = 1f;
            emberTime = 0f;
            playerHpLag = 1f;
            rivalHpLag = 1f;

            if (game != null)
            {
                game.ConfigureCameraPerspective(CameraFov, CameraHome, CameraLook);
            }
        }

        void EnsureWorld()
        {
            if (worldRoot != null) return;
            var go = new GameObject("Arena World");
            go.transform.SetParent(transform, false);
            worldRoot = go.transform;
            BuildArena();
        }

        void BuildArena()
        {
            var mood = WorldStage.Nocturne;
            mood.Horizon = new Color(0.10f, 0.09f, 0.12f);
            mood.SkyTint = new Color(0.56f, 0.56f, 0.74f);
            mood.FogStart = 26f;
            mood.FogEnd = 130f;

            Material sky;
            var dome = WorldStage.BuildSky(worldRoot, 0f, out sky);
            skyMaterial = sky;

            var sunObject = new GameObject("Moonlight");
            sunObject.transform.SetParent(worldRoot, false);
            sunObject.transform.rotation = Quaternion.Euler(52f, 148f, 0f);
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;

            WorldStage.Apply(mood, game != null ? game.GameCamera : null, sun, skyMaterial);
            if (dome != null) dome.transform.SetParent(worldRoot, true);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Arena Ground";
            ground.transform.SetParent(worldRoot, false);
            ground.transform.localScale = new Vector3(9f, 1f, 9f);
            ground.transform.position = new Vector3(0f, -0.02f, 0f);
            ground.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Dirt, new Color(0.42f, 0.34f, 0.25f), 1.6f);

            var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Arena Platform";
            platform.transform.SetParent(worldRoot, false);
            platform.transform.localScale = new Vector3(13.4f, 0.05f, 13.4f);
            platform.transform.position = new Vector3(0f, 0.02f, 0f);
            platform.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Stone, new Color(0.5f, 0.46f, 0.4f), 1.1f);

            var kerb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            kerb.name = "Arena Kerb";
            kerb.transform.SetParent(worldRoot, false);
            kerb.transform.localScale = new Vector3(14.1f, 0.03f, 14.1f);
            kerb.transform.position = new Vector3(0f, -0.01f, 0f);
            kerb.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.StoneDark, new Color(0.34f, 0.32f, 0.3f), 1.3f);

            var runeObject = new GameObject("Duel Sigil");
            runeObject.transform.SetParent(worldRoot, false);
            runeObject.transform.position = new Vector3(0f, 0.075f, 0f);
            runeObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            runeObject.transform.localScale = new Vector3(5.4f, 5.4f, 1f);
            rune = runeObject.AddComponent<SpriteRenderer>();
            rune.sprite = ImageAssets.RuneSprite;
            rune.color = new Color(1f, 0.86f, 0.6f, 0.5f);
            rune.sortingOrder = 40;

            var rng = new System.Random(313);
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f + 0.2f;
                float radius = 8.9f + (float)rng.NextDouble() * 0.8f;
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                WorldModels.Spawn("pillar", worldRoot, position, angle * Mathf.Rad2Deg + 90f, 4.2f);
            }

            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + i * 90f;
                float radius = 8.2f;
                var position = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 0f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                WorldModels.Spawn("lanternpost", worldRoot, position, angle + 180f);
                AddLanternLight(position + Vector3.up * 2.6f, 6.2f + (i % 2) * 0.8f);
            }

            for (int i = 0; i < 26; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = 11.5f + (float)rng.NextDouble() * 9f;
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                bool pine = rng.NextDouble() < 0.55;
                WorldModels.Spawn(pine ? "pine" : "oak", worldRoot, position, (float)rng.NextDouble() * 360f);
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = 9.6f + (float)rng.NextDouble() * 3.2f;
                var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                WorldModels.Spawn("rock", worldRoot, position, (float)rng.NextDouble() * 360f);
            }

            for (int i = 0; i < 26; i++)
            {
                var moteObject = new GameObject("Firefly");
                moteObject.transform.SetParent(worldRoot, false);
                var renderer = moteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = ImageAssets.DotSprite;
                renderer.sortingOrder = 60;
                float size = 0.05f + (float)rng.NextDouble() * 0.07f;
                moteObject.transform.localScale = new Vector3(size, size, 1f);
                renderer.color = new Color(1f, 0.82f, 0.46f, 0.4f);

                var origin = new Vector3(-7f + (float)rng.NextDouble() * 14f, 0.6f + (float)rng.NextDouble() * 3.4f, -3f + (float)rng.NextDouble() * 7f);
                moteObject.transform.position = origin;
                motes.Add(new Mote
                {
                    Transform = moteObject.transform,
                    Renderer = renderer,
                    Origin = origin,
                    Phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                    Speed = 0.3f + (float)rng.NextDouble() * 0.55f,
                    Radius = 0.16f + (float)rng.NextDouble() * 0.5f,
                    BaseAlpha = 0.18f + (float)rng.NextDouble() * 0.34f
                });
            }
        }

        void AddLanternLight(Vector3 position, float range)
        {
            var lightObject = new GameObject("Lantern Light");
            lightObject.transform.SetParent(worldRoot, false);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.63f, 0.32f);
            light.range = range;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;
        }

        void RebuildFighters()
        {
            if (player != null)
            {
                player.gameObject.SetActive(false);
                Destroy(player.gameObject);
                player = null;
            }
            if (rival != null)
            {
                rival.gameObject.SetActive(false);
                Destroy(rival.gameObject);
                rival = null;
            }

            var playerObject = new GameObject("Duelist " + hero.Name);
            playerObject.transform.SetParent(transform, false);
            player = playerObject.AddComponent<DuelFighter>();
            player.BoundsMin = new Vector2(ArenaMinX, ArenaMinY);
            player.BoundsMax = new Vector2(ArenaMaxX, ArenaMaxY);
            player.Initialize(hero, true, this, new Vector3(-2.6f, 0f, -0.55f));

            var rivalObject = new GameObject("Duelist " + HeroRoster.Rival.Name);
            rivalObject.transform.SetParent(transform, false);
            rival = rivalObject.AddComponent<DuelFighter>();
            rival.BoundsMin = new Vector2(ArenaMinX, ArenaMinY);
            rival.BoundsMax = new Vector2(ArenaMaxX, ArenaMaxY);
            rival.Initialize(HeroRoster.Rival, false, this, new Vector3(2.7f, 0f, 0.55f));

            player.Opponent = rival;
            rival.Opponent = player;

            var brain = rivalObject.AddComponent<RivalBrain>();
            brain.Initialize(rival, player);
        }

        void Update()
        {
            if (game == null) game = ZulvyreGame.Instance;

            float unscaled = Time.unscaledDeltaTime;
            float dt = Time.deltaTime;

            fade = Mathf.MoveTowards(fade, 0f, unscaled * 1.5f);
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, unscaled * 2.6f);
            emberTime += dt;

            UpdateMotes();
            UpdateProjectiles(dt);

            if (rune != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(emberTime * 0.9f);
                rune.color = new Color(1f, 0.9f, 0.68f, 0.4f + pulse * 0.15f);
            }

            if (player != null) playerHpLag = Mathf.MoveTowards(playerHpLag, player.HpFraction, unscaled * 0.55f);
            if (rival != null) rivalHpLag = Mathf.MoveTowards(rivalHpLag, rival.HpFraction, unscaled * 0.55f);

            if (finishBanner > 0f) finishBanner = Mathf.Max(0f, finishBanner - unscaled);

            if (roundOver)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Rematch();
                return;
            }

            if (paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(false);
                return;
            }

            if (resultDelay >= 0f)
            {
                resultDelay -= unscaled;
                if (resultDelay <= 0f) PresentResult();
                return;
            }

            if (!CombatActive)
            {
                introTimer += unscaled;
                if (introTimer >= IntroDuration) StartCombat();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(true);
                return;
            }

            timeLeft = Mathf.Max(0f, timeLeft - dt);
            if (timeLeft <= 0f) ResolveTimeout();
        }

        void LateUpdate()
        {
            if (game == null || game.GameCamera == null) return;
            var camera = game.GameCamera;
            float zoom = Mathf.Clamp01(introTimer / IntroDuration);
            float fov = Mathf.Lerp(CameraFov + 4f, CameraFov, Mathf.SmoothStep(0f, 1f, zoom));
            camera.fieldOfView = fov;
            Vector3 shake = game.Fx.ShakeOffset * 0.6f;
            Vector3 target = CameraHome + new Vector3(shake.x, shake.y, 0f);
            camera.transform.position = Vector3.Lerp(camera.transform.position, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        void UpdateMotes()
        {
            for (int i = 0; i < motes.Count; i++)
            {
                var mote = motes[i];
                if (mote.Transform == null) continue;
                float t = emberTime * mote.Speed + mote.Phase;
                mote.Transform.position = mote.Origin + new Vector3(Mathf.Sin(t) * mote.Radius, Mathf.Cos(t * 0.73f) * mote.Radius * 0.5f, Mathf.Cos(t * 0.61f) * mote.Radius * 0.6f);
                var color = mote.Renderer.color;
                color.a = mote.BaseAlpha * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 1.7f)));
                mote.Renderer.color = color;
            }
        }

        void UpdateProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                if (projectile.Go == null)
                {
                    projectiles.RemoveAt(i);
                    continue;
                }

                projectile.Age += dt;
                if (projectile.Target != null)
                {
                    projectile.End = projectile.Target.transform.position + new Vector3(0f, ChestHeight, 0f);
                }

                float k = Mathf.Clamp01(projectile.Age / projectile.Duration);
                Vector3 position = Vector3.Lerp(projectile.Start, projectile.End, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.55f);
                projectile.Go.transform.position = position;
                float scale = 0.6f + Mathf.Sin(projectile.Age * 42f) * 0.07f;
                projectile.Go.transform.localScale = new Vector3(scale, scale, 1f);

                Color color = projectile.Renderer.color;
                color.a = 0.95f - k * 0.15f;
                projectile.Renderer.color = color;

                projectile.TrailTimer -= dt;
                if (projectile.TrailTimer <= 0f && game != null)
                {
                    projectile.TrailTimer = 0.035f;
                    game.Fx.Trail(position, new Color(1f, 0.6f, 0.26f, 0.62f), 0.2f);
                }

                if (k >= 1f)
                {
                    Impact(projectile);
                    if (projectile.Go != null) Destroy(projectile.Go);
                    if (i < projectiles.Count && ReferenceEquals(projectiles[i], projectile)) projectiles.RemoveAt(i);
                }
            }
        }

        void Impact(Projectile projectile)
        {
            if (game == null) return;
            Vector3 position = projectile.Go != null ? projectile.Go.transform.position : projectile.End;
            game.Fx.Sparks(position, new Color(1f, 0.62f, 0.28f), 18, 1.2f);
            game.Fx.Ring(position, new Color(1f, 0.55f, 0.25f, 0.8f), 0.2f, 2.4f, 0.4f, 5400, false);

            if (projectile.Target == null || projectile.Target.Dead) return;
            projectile.Target.TakeDamage(projectile.Damage, projectile.Caster != null ? projectile.Caster.Position : (Vector2)position, false, projectile.Caster);
            projectile.Target.ApplyBurn(projectile.Burn, 3f);
            game.Fx.Text(position + Vector3.up * 0.5f, "SEARED", new Color(1f, 0.66f, 0.32f), 16f, 0.7f, 0.7f);
        }

        public void CastFirebolt(DuelFighter caster)
        {
            if (caster == null || caster.Opponent == null) return;
            if (game == null) game = ZulvyreGame.Instance;

            var go = new GameObject("Emberlash Bolt");
            go.transform.SetParent(transform, false);
            Vector3 start = caster.transform.position + new Vector3(caster.Facing * 0.4f, ChestHeight + 0.05f, 0f);
            go.transform.position = start;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ImageAssets.DotSprite;
            renderer.color = new Color(1f, 0.68f, 0.32f, 0.95f);
            renderer.sortingOrder = 5400;

            projectiles.Add(new Projectile
            {
                Go = go,
                Renderer = renderer,
                Caster = caster,
                Target = caster.Opponent,
                Start = start,
                End = caster.Opponent.transform.position + new Vector3(0f, ChestHeight, 0f),
                Duration = 0.38f,
                Damage = 16f * caster.Def.Power,
                Burn = 10f * caster.Def.Power
            });

            if (game != null)
            {
                game.Fx.Ring(start, new Color(1f, 0.55f, 0.25f, 0.8f), 0.22f, 1.7f, 0.3f, 5400, false);
            }
        }

        public void NotifyPlayerDamaged()
        {
            damageFlash = 1f;
        }

        public void OnFighterDied(DuelFighter fighter)
        {
            if (roundOver || resultDelay >= 0f) return;

            CombatActive = false;
            resultVictory = fighter == rival;
            resultDraw = false;
            resultDelay = 1.55f;
            finishText = resultVictory ? "VESPER YIELDS" : hero.Name.ToUpperInvariant() + " FALLS";
            finishColor = resultVictory ? ImGuiTheme.GoldBright : ImGuiTheme.Blood;
            finishBanner = 1.6f;
            ClearProjectiles();

            if (game != null)
            {
                game.Fx.Shake(0.22f, 0.55f);
                game.Fx.Ring(fighter.transform.position + new Vector3(0f, 0.08f, 0f), new Color(0.9f, 0.45f, 0.35f, 0.7f), 0.4f, 4.6f, 0.65f, 400, true, true);
            }
        }

        void ResolveTimeout()
        {
            if (roundOver || resultDelay >= 0f) return;
            CombatActive = false;
            float diff = player != null && rival != null ? player.HpFraction - rival.HpFraction : 0f;
            resultDraw = Mathf.Abs(diff) < 0.02f;
            resultVictory = !resultDraw && diff > 0f;
            resultDelay = 1.2f;
            finishText = "TIME";
            finishColor = ImGuiTheme.Parchment;
            finishBanner = 1.35f;
            ClearProjectiles();
        }

        void StartCombat()
        {
            introTimer = IntroDuration;
            CombatActive = true;
            finishText = "FIGHT";
            finishColor = ImGuiTheme.GoldBright;
            finishBanner = 1.2f;

            if (game != null)
            {
                game.Audio.PlayShield();
                game.Fx.Ring(new Vector3(0f, 0.09f, 0f), new Color(1f, 0.82f, 0.5f, 0.75f), 0.6f, 8.8f, 0.75f, 400, true, true);
                game.Fx.Sparks(new Vector3(0f, ChestHeight, 0f), new Color(1f, 0.8f, 0.5f), 16, 0.9f);
            }
        }

        void PresentResult()
        {
            roundOver = true;
            resultDelay = -1f;
            if (game == null) return;

            if (resultVictory)
            {
                game.DuelWon = true;
                game.Audio.PlayVictory();
            }
            else if (!resultDraw)
            {
                game.Audio.PlayDefeat();
            }
            else
            {
                game.Audio.PlayClick();
            }
        }

        void SetPaused(bool value)
        {
            if (roundOver) return;
            paused = value;
            CombatActive = !value && introTimer >= IntroDuration && resultDelay < 0f;
            if (game != null) game.Audio.PlayClick(0.35f);
        }

        void ClearProjectiles()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                if (projectiles[i].Go != null) Destroy(projectiles[i].Go);
            }
            projectiles.Clear();
        }

        void Rematch()
        {
            if (game == null) return;
            game.EnterDuel(hero, fromVillage);
        }

        void LeaveToVillage()
        {
            if (game == null) return;
            game.EnterVillage(hero);
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            if (game == null) return;

            ImGuiTheme.Begin();
            game.Fx.DrawNumbers();
            ImGuiTheme.Vignette(0.62f);

            if (damageFlash > 0.01f)
            {
                ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0.55f, 0.06f, 0.05f, damageFlash * 0.2f));
            }

            DrawHud();
            DrawAbilityPanel();
            DrawActionBar();
            DrawCombo();
            DrawControls();
            DrawFinishBanner();

            if (introTimer < IntroDuration && !roundOver && resultDelay < 0f && !paused) DrawIntro();
            if (paused) DrawPause();
            if (roundOver) DrawResult();

            if (fade > 0.001f)
            {
                ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, fade * 0.92f));
            }

            HandleArenaClick();

            ImGuiTheme.End();
        }

        void DrawHud()
        {
            if (player == null || rival == null) return;
            DrawFighterPanel(56f, player, true);
            DrawTimerPanel();
            DrawFighterPanel(944f, rival, false);
        }

        void DrawFighterPanel(float x, DuelFighter fighter, bool left)
        {
            var panel = new Rect(x, 44f, 600f, 92f);
            ImGuiTheme.Panel(panel, new Color(0.018f, 0.035f, 0.026f, 0.9f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.34f));
            ImGuiTheme.Fill(new Rect(panel.x, panel.y, panel.width, 2f), ImGuiTheme.WithAlpha(fighter.Def.Accent, 0.9f));

            var portrait = new Rect(left ? panel.x + 16f : panel.xMax - 80f, panel.y + 14f, 64f, 64f);
            ImGuiTheme.Fill(portrait, ImGuiTheme.WithAlpha(fighter.Def.Color, 0.42f));
            ImGuiTheme.TextureFit(portrait, ImageAssets.CharacterTexture(fighter.Def.SpriteName), Color.white);
            HLineRect(portrait, ImGuiTheme.WithAlpha(fighter.Def.Accent, 0.6f));

            float nameX = left ? panel.x + 92f : panel.x + 204f;
            ImGuiTheme.Text(new Rect(nameX, panel.y + 6f, 300f, 28f), fighter.Def.Name, 21,
                fighter.Def.Accent, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, true);
            ImGuiTheme.SpacedText(new Rect(nameX, panel.y + 33f, 300f, 15f), fighter.Def.Title.ToUpperInvariant(), 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.6f), 1.8f,
                left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, false);

            float barX = left ? panel.x + 92f : panel.x + 168f;
            var hpBar = new Rect(barX, panel.y + 52f, 340f, 13f);
            DrawDuelBar(hpBar, fighter.HpFraction, left ? playerHpLag : rivalHpLag, left ? HpFill : RivalFill, HpChunk, !left);
            string hpText = Mathf.CeilToInt(fighter.Hp) + " / " + Mathf.RoundToInt(fighter.MaxHp);
            ImGuiTheme.Text(new Rect(left ? panel.x + 440f : panel.x + 20f, panel.y + 41f, 140f, 24f), hpText, 13,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.9f), left ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);

            var staminaBar = new Rect(barX, panel.y + 71f, 340f, 6f);
            DrawDuelBar(staminaBar, fighter.StaminaFraction, fighter.StaminaFraction, left ? StaminaFill : RivalStamina, Color.clear, !left);

            var pip = new Rect(left ? panel.x + 540f : panel.x + 60f, panel.y + 68f, 9f, 9f);
            ImGuiTheme.Fill(pip, fighter.AbilityReady
                ? ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.95f)
                : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.28f));
        }

        void DrawDuelBar(Rect rect, float fraction, float lagFraction, Color fill, Color chunk, bool mirrored)
        {
            ImGuiTheme.Fill(rect, new Color(0.006f, 0.014f, 0.01f, 0.92f));

            float lag = Mathf.Clamp01(lagFraction);
            if (lag > 0f && chunk.a > 0f)
            {
                var lagRect = mirrored
                    ? new Rect(rect.xMax - rect.width * lag, rect.y + 1f, rect.width * lag, rect.height - 2f)
                    : new Rect(rect.x, rect.y + 1f, rect.width * lag, rect.height - 2f);
                ImGuiTheme.Fill(lagRect, chunk);
            }

            float value = Mathf.Clamp01(fraction);
            if (value > 0f)
            {
                var fillRect = mirrored
                    ? new Rect(rect.xMax - rect.width * value, rect.y + 1f, rect.width * value, rect.height - 2f)
                    : new Rect(rect.x, rect.y + 1f, rect.width * value, rect.height - 2f);
                ImGuiTheme.Fill(fillRect, fill);
                ImGuiTheme.Fill(new Rect(fillRect.x, fillRect.y, fillRect.width, 2f), new Color(1f, 1f, 1f, 0.2f));
            }

            ImGuiTheme.HLine(rect.xMin, rect.xMax, rect.yMin, new Color(0f, 0f, 0f, 0.55f));
            ImGuiTheme.HLine(rect.xMin, rect.xMax, rect.yMax, new Color(0f, 0f, 0f, 0.55f));
        }

        void DrawTimerPanel()
        {
            var panel = new Rect(720f, 44f, 160f, 92f);
            ImGuiTheme.Panel(panel, new Color(0.018f, 0.035f, 0.026f, 0.93f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.5f), 1f, true);

            bool urgent = timeLeft <= 15f && CombatActive;
            bool blink = urgent && Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.45f;
            ImGuiTheme.Text(new Rect(panel.x, panel.y + 9f, panel.width, 46f), FormatTime(timeLeft), 32,
                blink ? ImGuiTheme.Ember : ImGuiTheme.GoldBright, TextAnchor.MiddleCenter, true);
            ImGuiTheme.SpacedText(new Rect(panel.x, panel.y + 62f, panel.width, 14f), "FIRST TO YIELD", 8,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 1.4f, TextAnchor.MiddleCenter, false);
        }

        void DrawAbilityPanel()
        {
            if (player == null) return;

            var panel = new Rect(56f, 770f, 352f, 88f);
            bool hover = panel.Contains(Event.current.mousePosition) && player.CanActNow;
            ImGuiTheme.Panel(panel, new Color(0.018f, 0.035f, 0.026f, 0.84f),
                ImGuiTheme.WithAlpha(player.Def.Accent, hover ? 0.85f : 0.38f), 1f, true);

            var chip = new Rect(panel.x + 14f, panel.y + 22f, 52f, 52f);
            ImGuiTheme.Panel(chip, ImGuiTheme.WithAlpha(player.Def.Color, 0.58f),
                ImGuiTheme.WithAlpha(hover ? ImGuiTheme.GoldBright : ImGuiTheme.Gold, hover ? 1f : 0.8f));
            ImGuiTheme.Text(chip, "Q", 26, ImGuiTheme.GoldBright, TextAnchor.MiddleCenter, true);

            ImGuiTheme.Text(new Rect(panel.x + 80f, panel.y + 10f, panel.width - 96f, 24f),
                player.Def.AbilityName, 16, ImGuiTheme.GoldBright, TextAnchor.MiddleLeft, true);

            bool ready = player.AbilityReady;
            string status;
            Color statusColor;
            if (player.FailedTimer > 0f)
            {
                status = player.FailedText;
                statusColor = ImGuiTheme.Ember;
            }
            else if (ready)
            {
                status = "READY  ·  " + Mathf.RoundToInt(player.AbilityCost) + " BREATH";
                statusColor = ImGuiTheme.Moss;
            }
            else
            {
                status = "RECOVERING  ·  " + player.AbilityCooldown.ToString("0.0") + "s";
                statusColor = ImGuiTheme.Muted;
            }
            ImGuiTheme.Text(new Rect(panel.x + 80f, panel.y + 35f, panel.width - 96f, 18f), status, 11, statusColor);

            float fraction = ready ? 1f : Mathf.Clamp01(1f - player.AbilityCooldown / Mathf.Max(0.01f, player.AbilityCooldownTotal));
            ImGuiTheme.Bar(new Rect(panel.x + 80f, panel.y + 64f, panel.width - 104f, 8f), fraction,
                ready ? ImGuiTheme.Gold : ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.55f), new Color(0f, 0f, 0f, 0.5f));

            if (ImGuiTheme.Clicked(panel)) player.TryAbility();
        }

        void DrawActionBar()
        {
            if (player == null) return;
            bool live = CombatActive && !paused && !roundOver && !player.Dead;

            if (ImGuiTheme.Button(new Rect(444f, 800f, 200f, 48f), "STRIKE", true, live, 13)) player.TryLightAttack();
            if (ImGuiTheme.Button(new Rect(660f, 800f, 200f, 48f), "HEAVY", false, live, 13)) player.TryHeavyAttack();
            if (ImGuiTheme.Button(new Rect(876f, 800f, 200f, 48f), "DODGE", false, live, 13)) player.TryDodge(Vector2.zero);
        }

        void HandleArenaClick()
        {
            if (game == null || game.GameCamera == null || player == null || rival == null) return;
            if (paused || roundOver || !CombatActive || player.Dead || fade > 0.25f) return;

            Event current = Event.current;
            if (current == null || current.type != EventType.MouseUp || current.button != 0) return;

            Vector2 mouse = current.mousePosition;
            if (mouse.y < 150f || mouse.y > 780f) return;
            if (new Rect(56f, 770f, 352f, 88f).Contains(mouse)) return;

            Vector2 screen = ImGuiTheme.VirtualToScreen(mouse);
            var ray = game.GameCamera.ScreenPointToRay(new Vector3(screen.x, Screen.height - screen.y, 0f));
            var floor = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (!floor.Raycast(ray, out distance)) return;
            Vector3 world = ray.GetPoint(distance);
            Vector2 ground = new Vector2(world.x, world.z);

            if (!rival.Dead && Vector2.Distance(ground, rival.Position) < 0.85f)
            {
                player.TryLightAttack();
                return;
            }

            player.CommandMove(new Vector2(
                Mathf.Clamp(ground.x, ArenaMinX, ArenaMaxX),
                Mathf.Clamp(ground.y, ArenaMinY, ArenaMaxY)));
        }

        void DrawCombo()
        {
            if (player == null || player.Combo < 2) return;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 9f) * 0.05f;
            ImGuiTheme.Text(new Rect(56f, 714f, 352f, 42f), "COMBO ×" + player.Combo,
                Mathf.RoundToInt(22f * pulse), ImGuiTheme.GoldBright, TextAnchor.MiddleLeft, true);
        }

        void DrawControls()
        {
            ImGuiTheme.SpacedText(new Rect(1096f, 800f, 448f, 18f), "CLICK STRIKE / HEAVY / DODGE   ·   Q ART", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.66f), 1.1f, TextAnchor.MiddleRight, false);
            ImGuiTheme.SpacedText(new Rect(1096f, 822f, 448f, 18f), "WASD OR CLICK TO MOVE   ESC PAUSE   M MUTE", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.6f), 1.1f, TextAnchor.MiddleRight, false);
        }

        void DrawFinishBanner()
        {
            if (finishBanner <= 0f || roundOver) return;
            float alpha = Mathf.Clamp01(finishBanner / 0.4f);
            Color color = ImGuiTheme.WithAlpha(finishColor, alpha);
            ImGuiTheme.SpacedText(new Rect(400f, 282f, 800f, 68f), finishText, 48, color, 10f, TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(560f, 1040f, 351f, ImGuiTheme.WithAlpha(finishColor, alpha * 0.5f));
        }

        void DrawIntro()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0.01f, 0.02f, 0.015f, 0.5f));

            ImGuiTheme.SpacedText(new Rect(300f, 190f, 1000f, 40f), "THE LANTERN DUEL", 26, ImGuiTheme.GoldBright, 8f);
            ImGuiTheme.Text(new Rect(300f, 228f, 1000f, 20f), "BENEATH THE PINES  ·  BY LANTERNLIGHT", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(560f, 1040f, 258f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.35f));

            ImGuiTheme.Text(new Rect(200f, 296f, 480f, 40f), hero.Name, 30, ImGuiTheme.Parchment, TextAnchor.MiddleRight, true);
            ImGuiTheme.SpacedText(new Rect(200f, 338f, 480f, 18f), hero.Title.ToUpperInvariant(), 9,
                ImGuiTheme.WithAlpha(hero.Accent, 0.9f), 1.8f, TextAnchor.MiddleRight, false);
            ImGuiTheme.Text(new Rect(760f, 296f, 40f, 40f), "VS", 20, ImGuiTheme.Gold, TextAnchor.MiddleCenter, true);
            ImGuiTheme.Text(new Rect(820f, 296f, 480f, 40f), HeroRoster.Rival.Name, 30, new Color(0.93f, 0.67f, 0.7f), TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(820f, 338f, 480f, 18f), HeroRoster.Rival.Title.ToUpperInvariant(), 9,
                ImGuiTheme.WithAlpha(HeroRoster.Rival.Accent, 0.95f), 1.8f, TextAnchor.MiddleLeft, false);

            int count = Mathf.Max(1, Mathf.CeilToInt(IntroDuration - introTimer));
            var ringRect = new Rect(700f, 402f, 200f, 200f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.55f, 0.8f);
            GUI.DrawTexture(ringRect, ImageAssets.RingTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            ImGuiTheme.Text(ringRect, count.ToString(), 84, ImGuiTheme.GoldBright, TextAnchor.MiddleCenter, true);

            ImGuiTheme.Text(new Rect(300f, 624f, 1000f, 22f), "First to yield — or the higher guard when the lantern gutters.", 12,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.82f), TextAnchor.MiddleCenter);
            ImGuiTheme.SpacedText(new Rect(300f, 654f, 1000f, 18f), "WASD OR CLICK TO MOVE   ·   CLICK OR KEYS TO FIGHT", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.85f), 1.4f, TextAnchor.MiddleCenter, false);
        }

        void DrawPause()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, 0.7f));
            var panel = new Rect(540f, 226f, 520f, 448f);
            ImGuiTheme.Panel(panel, new Color(0.022f, 0.04f, 0.03f, 0.98f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.55f), 1f, true);

            ImGuiTheme.SpacedText(new Rect(panel.x, panel.y + 28f, panel.width, 34f), "PAUSED", 22, ImGuiTheme.GoldBright, 6f);
            ImGuiTheme.Text(new Rect(panel.x, panel.y + 70f, panel.width, 22f), "The lanterns hold their breath.", 12,
                ImGuiTheme.Parchment, TextAnchor.MiddleCenter);

            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 128f, panel.width - 120f, 46f), "RESUME DUEL", true))
            {
                SetPaused(false);
                return;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 188f, panel.width - 120f, 46f), "RESTART DUEL"))
            {
                Rematch();
                return;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 248f, panel.width - 120f, 46f),
                    fromVillage ? "RETURN TO THE VILLAGE" : "RETURN TO CAMP"))
            {
                if (fromVillage) LeaveToVillage();
                else if (game != null) game.EnterLanding();
                return;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 308f, panel.width - 120f, 46f), "RETURN TO MENU"))
            {
                if (game != null) game.EnterMenu();
                return;
            }

            ImGuiTheme.Text(new Rect(panel.x, panel.y + 378f, panel.width, 20f), "M  mute   ·   Alt+Enter  fullscreen", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.7f), TextAnchor.MiddleCenter);
        }

        void DrawResult()
        {
            if (player == null || rival == null) return;

            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, 0.72f));
            var panel = new Rect(520f, 180f, 560f, 520f);
            Color accent = resultDraw ? ImGuiTheme.Parchment : resultVictory ? ImGuiTheme.Gold : ImGuiTheme.Blood;
            ImGuiTheme.Panel(panel, new Color(0.02f, 0.036f, 0.028f, 0.98f), ImGuiTheme.WithAlpha(accent, 0.7f), 1f, true);
            ImGuiTheme.Fill(new Rect(panel.x, panel.y, panel.width, 3f), accent);

            ImGuiTheme.SpacedText(new Rect(panel.x, panel.y + 24f, panel.width, 20f), "THE LANTERN DUEL", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 3f);
            string title = resultDraw ? "DRAW" : resultVictory ? "VICTORY" : "DEFEAT";
            ImGuiTheme.SpacedText(new Rect(panel.x, panel.y + 50f, panel.width, 62f), title, 46, accent, 8f);

            string subtitle = resultDraw
                ? "Neither blade finds its answer tonight."
                : resultVictory
                    ? "Vesper lowers the mask and yields the circle."
                    : "The mask holds. The circle is Vesper's.";
            ImGuiTheme.Text(new Rect(panel.x + 40f, panel.y + 120f, panel.width - 80f, 24f), subtitle, 12,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.85f), TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(panel.x + 60f, panel.xMax - 60f, panel.y + 160f, ImGuiTheme.WithAlpha(accent, 0.4f));

            DrawStatRow(panel, panel.y + 180f, "DAMAGE DEALT", Mathf.RoundToInt(player.DamageDealt).ToString());
            DrawStatRow(panel, panel.y + 212f, "BEST COMBO", player.BestCombo.ToString());
            DrawStatRow(panel, panel.y + 244f, "TIME REMAINING", FormatTime(timeLeft));

            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 324f, panel.width - 120f, 52f), "REMATCH", true))
            {
                Rematch();
                return;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 60f, panel.y + 390f, 236f, 46f),
                    fromVillage ? "RETURN TO VILLAGE" : "RETURN TO CAMP", false, true, 11))
            {
                if (fromVillage) LeaveToVillage();
                else if (game != null) game.EnterLanding();
                return;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 312f, panel.y + 390f, 188f, 46f), "RETURN TO MENU", false, true, 12))
            {
                if (game != null) game.EnterMenu();
                return;
            }

            ImGuiTheme.Text(new Rect(panel.x, panel.y + 456f, panel.width, 20f), "ENTER  rematch", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.6f), TextAnchor.MiddleCenter);
        }

        void DrawStatRow(Rect panel, float y, string label, string value)
        {
            ImGuiTheme.SpacedText(new Rect(panel.x + 60f, y, 300f, 20f), label, 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 1.8f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(panel.xMax - 220f, y - 4f, 160f, 26f), value, 16,
                ImGuiTheme.Parchment, TextAnchor.MiddleRight, true);
            ImGuiTheme.HLine(panel.x + 60f, panel.xMax - 60f, y + 26f, new Color(1f, 1f, 1f, 0.06f));
        }

        void HLineRect(Rect rect, Color color)
        {
            ImGuiTheme.HLine(rect.xMin, rect.xMax, rect.yMin, color);
            ImGuiTheme.HLine(rect.xMin, rect.xMax, rect.yMax, color);
            ImGuiTheme.VLine(rect.xMin, rect.yMin, rect.yMax, color);
            ImGuiTheme.VLine(rect.xMax, rect.yMin, rect.yMax, color);
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return string.Format("{0}:{1:00}", total / 60, total % 60);
        }
    }
}
