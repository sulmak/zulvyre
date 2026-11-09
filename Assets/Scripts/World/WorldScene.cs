using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class WorldScene : MonoBehaviour
    {
        const float DungeonZ = -400f;
        static readonly Vector3 AlderwickCenter = new Vector3(0f, 0f, 58f);
        static readonly Vector3 DurnmarCenter = new Vector3(-72f, 0f, -58f);
        static readonly Vector3 NocturneCenter = new Vector3(72f, 0f, -60f);
        static readonly Vector3 CryptGate = new Vector3(0f, 0f, -100f);
        static readonly Vector3 GateExit = new Vector3(0f, 0f, -94f);
        static readonly Vector3 DungeonEntry = new Vector3(0f, 0f, -391f);
        static readonly Vector3 DungeonExit = new Vector3(0f, 0f, -385f);
        static readonly Vector3 VillageSpawn = new Vector3(2f, 0f, 67f);
        static readonly Vector3 ElderPosition = new Vector3(2f, 0f, 57f);

        static readonly int[] TitleLevels = { 1, 3, 5, 8, 12, 16, 20, 25, 30, 38, 46, 55, 65, 75, 85, 92, 100 };
        static readonly string[] TitleNames =
        {
            "Wanderer", "Roadhand", "Wayfarer", "Ranger of the Greywood", "Flamebearer", "Oathkeeper",
            "Duskbane", "Wyrmbound", "Starborne", "Crowned of Cinders", "Deathless", "Mythforged",
            "Worldbreaker", "Lantern Eternal", "Voidwarden", "Dragon of the Deep", "Undying Supreme Lord"
        };

        static readonly Color HpColor = new Color(0.80f, 0.34f, 0.26f);
        static readonly Color StaminaColor = new Color(0.56f, 0.72f, 0.45f);
        static readonly Color XpColor = new Color(0.82f, 0.66f, 0.32f);
        static readonly Color DuskColor = new Color(0.43f, 0.34f, 0.33f);

        enum Stage { Wolves, Orcs, Grok, Vampires, Velyra, Crypt, Warden, Ascend, Complete }

        class FloatingText
        {
            public Vector3 Position;
            public string Text;
            public Color Color;
            public float Age;
            public float Life;
            public int Size;
        }

        ZulvyreGame game;
        HeroDef hero;
        Transform worldRoot;
        Camera cam;
        Light sun;
        Transform elder;
        Transform sky;
        Material skyMaterial;
        ScreenGrade grade;
        WorldStage.Mood mood;
        WorldStage.Mood moodTarget;
        readonly List<WorldEnemy> enemies = new List<WorldEnemy>();
        readonly List<FloatingText> floats = new List<FloatingText>();
        readonly List<Flicker> flickers = new List<Flicker>();

        class Flicker
        {
            public Light Lamp;
            public float Base;
            public float Phase;
            public float Rate;
        }

        Color savedBackground;
        CameraClearFlags savedClearFlags;
        bool savedOrtho;
        bool savedFog;
        Color savedFogColor;
        float savedFogStart;
        float savedFogEnd;
        Color savedAmbient;
        UnityEngine.Rendering.AmbientMode savedAmbientMode;

        public WorldPlayer Player { get; private set; }
        public AudioBank Audio => game != null ? game.Audio : null;
        public bool Paused { get; private set; }
        public bool MapOpen { get; private set; }
        public bool TourMode;
        public bool InDungeon { get; private set; }
        public bool AllowRespawn => Player != null && Player.Dead && deathTimer > 0.8f;
        public float ArtCooldownTotal => 14f;

        public float CameraYaw = 180f;
        public float CameraPitch = 15f;
        public float CameraDistance = 6.6f;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpNext { get; private set; } = 100;
        public string Title { get; private set; } = "Wanderer";
        public float MaxHp => hero == null ? 100f : hero.MaxHp + (Level - 1) * 10f;
        public float PlayerDamage => hero == null ? 7f : 7f * hero.Power * (1f + 0.09f * (Level - 1));

        Stage stage = Stage.Wolves;
        int questHave;
        int questNeed;
        string questTitle = "ASH ON THE WIND";
        string questObjective = "Slay wolves in the Greywood";
        bool grokDead;
        bool velyraDead;
        bool wardenDead;
        bool entered;
        bool exiting;

        string region = "";
        float regionTimer;
        string toast;
        float toastTimer;
        float damageFlash;
        float shake;
        float deathTimer;
        float targetTimer;
        WorldEnemy target;

        public void Enter(HeroDef selectedHero)
        {
            game = ZulvyreGame.Instance;
            hero = selectedHero;
            cam = game.GameCamera;
            entered = true;
            exiting = false;

            savedBackground = cam.backgroundColor;
            savedClearFlags = cam.clearFlags;
            savedOrtho = cam.orthographic;
            savedFog = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogStart = RenderSettings.fogStartDistance;
            savedFogEnd = RenderSettings.fogEndDistance;
            savedAmbient = RenderSettings.ambientLight;
            savedAmbientMode = RenderSettings.ambientMode;

            Level = 1;
            Xp = 0;
            XpNext = 100;
            Title = TitleFor(Level);
            stage = Stage.Wolves;
            grokDead = velyraDead = wardenDead = false;
            Paused = false;
            MapOpen = false;
            InDungeon = false;
            CameraYaw = 180f;
            CameraPitch = 15f;
            CameraDistance = 6.6f;
            damageFlash = 0f;
            shake = 0f;
            deathTimer = 0f;
            target = null;
            targetTimer = 0f;
            BeginStage(Stage.Wolves, false);
            BuildWorld();
            ConfigureCameraForWorld();
            game.Audio.PlayMenuMusic();
            game.Audio.PlayAmbientVillage();
            Toast("The road south is yours. Speak with Elder Maren.", 4f);
            region = "";
            regionTimer = 0f;
        }

        void ConfigureCameraForWorld()
        {
            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = WorldStage.SkyRadius + 80f;
            cam.transform.position = new Vector3(0f, 6f, 74f);
            cam.transform.rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            if (grade == null)
            {
                grade = cam.GetComponent<ScreenGrade>();
                if (grade == null) grade = cam.gameObject.AddComponent<ScreenGrade>();
            }
            grade.enabled = true;
            WorldStage.Apply(mood, cam, sun, skyMaterial);
        }

        void OnDisable()
        {
            if (!entered) return;
            entered = false;

            if (cam != null)
            {
                cam.orthographic = savedOrtho;
                cam.backgroundColor = savedBackground;
                cam.clearFlags = savedClearFlags;
                if (cam.orthographic) cam.orthographicSize = 5.12f;
            }
            if (grade != null) grade.enabled = false;
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogStartDistance = savedFogStart;
            RenderSettings.fogEndDistance = savedFogEnd;
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientLight = savedAmbient;

            if (worldRoot != null) Destroy(worldRoot.gameObject);
            worldRoot = null;
            Player = null;
            enemies.Clear();
            floats.Clear();
            flickers.Clear();
        }

        void AnimateFlicker()
        {
            float t = Time.time;
            for (int i = 0; i < flickers.Count; i++)
            {
                var f = flickers[i];
                if (f.Lamp == null) continue;
                float w = Mathf.Sin(t * f.Rate + f.Phase) * 0.6f + Mathf.Sin(t * f.Rate * 2.7f + f.Phase * 1.9f) * 0.4f;
                f.Lamp.intensity = Mathf.Max(0.08f, f.Base * (0.88f + w * 0.17f));
            }
        }

        void Update()
        {
            if (game == null || exiting) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (MapOpen) MapOpen = false;
                else if (Player != null && !Player.Dead)
                {
                    Paused = !Paused;
                    if (Audio != null) Audio.PlayClick(0.4f);
                }
            }

            if (Input.GetKeyDown(KeyCode.M) && !Paused && Player != null && !Player.Dead)
            {
                MapOpen = !MapOpen;
                if (Audio != null) Audio.PlayClick(0.35f);
            }

            if (Paused || MapOpen || Player == null) return;

            AnimateFlicker();

            deathTimer = Mathf.Max(0f, deathTimer + (Player.Dead ? Time.deltaTime : 0f));
            damageFlash = Mathf.Max(0f, damageFlash - Time.deltaTime * 2.4f);
            toastTimer = Mathf.Max(0f, toastTimer - Time.deltaTime);
            regionTimer = Mathf.Max(0f, regionTimer - Time.deltaTime);
            targetTimer = Mathf.Max(0f, targetTimer - Time.deltaTime);
            shake = Mathf.Max(0f, shake - Time.deltaTime * 1.6f);

            if (!TourMode && !Player.Dead)
            {
                if (Input.GetMouseButton(1))
                {
                    CameraYaw += Input.GetAxis("Mouse X") * 3.6f;
                    CameraPitch = Mathf.Clamp(CameraPitch - Input.GetAxis("Mouse Y") * 2.6f, 5f, 46f);
                }
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f) CameraDistance = Mathf.Clamp(CameraDistance - scroll * 0.7f, 3.6f, 11f);
            }

            UpdateRegion();
            SelectMood();
            BlendMood();

            if (stage == Stage.Ascend && Level >= 20) CompleteAscension();
            if (Player.transform.position.y < -12f) RespawnPlayer();
        }

        void LateUpdate()
        {
            if (cam == null || Player == null) return;

            Vector3 forward = Quaternion.Euler(CameraPitch, CameraYaw, 0f) * Vector3.forward;
            Vector3 targetPoint = Player.transform.position + Vector3.up * 1.55f;
            Vector3 desired = targetPoint - forward * CameraDistance;

            Vector3 jitter = shake > 0.001f ? Random.insideUnitSphere * shake * 0.45f : Vector3.zero;

            Vector3 pull = desired - targetPoint;
            float pullLength = pull.magnitude;
            if (pullLength > 0.01f)
            {
                RaycastHit hit;
                if (Physics.SphereCast(targetPoint, 0.28f, pull / pullLength, out hit, pullLength,
                        ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(Player.transform))
                {
                    float safe = Mathf.Clamp(hit.distance - 0.2f, 1.1f, pullLength);
                    desired = targetPoint + pull / pullLength * safe;
                }
            }

            float k = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            cam.transform.position = Vector3.Lerp(cam.transform.position, desired + jitter, k);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(targetPoint - cam.transform.position), k);

            if (sky != null) sky.position = cam.transform.position;
        }

        // ------------------------------------------------------------------ world

        void BuildWorld()
        {
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            enemies.Clear();
            floats.Clear();
            flickers.Clear();

            var root = new GameObject("World3D").transform;
            root.SetParent(transform, false);
            worldRoot = root;

            BuildLighting(root);
            BuildGround(root);
            BuildAlderwick(root);
            BuildForest(root);
            BuildDurnmar(root);
            BuildNocturne(root);
            BuildCrypt(root);
            BuildDungeon(root);
            SpawnEnemies(root);
            SpawnPlayer(root);
        }

        void BuildLighting(Transform root)
        {
            var sunObject = new GameObject("Sun");
            sunObject.transform.SetParent(root, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -36f, 0f);
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = WorldStage.Greywood.SunColor;
            sun.intensity = WorldStage.Greywood.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = WorldStage.Greywood.ShadowStrength;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.09f;
            RenderSettings.sun = sun;

            QualitySettings.shadowDistance = 170f;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.skinWeights = SkinWeights.TwoBones;

            Vector3 towardSun = -sunObject.transform.forward;
            float sunAzimuth = Mathf.Atan2(towardSun.x, towardSun.z) * Mathf.Rad2Deg;
            sky = WorldStage.BuildSky(root, sunAzimuth + 180f, out skyMaterial).transform;

            mood = moodTarget = WorldStage.Greywood;
            WorldStage.Apply(mood, cam, sun, skyMaterial);
        }

        void BuildGround(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Grasslands";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, 0f, -20f);
            ground.transform.localScale = new Vector3(150f, 1f, 150f);
            ground.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Grass,
                Color.white, new Vector2(166f, 166f));

            Patch(root, new Vector3(-40f, 0.02f, 20f), new Vector2(60f, 55f), SurfaceAssets.Kind.Grass,
                new Color(0.86f, 0.94f, 0.78f));
            Patch(root, new Vector3(48f, 0.02f, 0f), new Vector2(55f, 40f), SurfaceAssets.Kind.Grass,
                new Color(0.92f, 1f, 0.84f));
            Patch(root, new Vector3(0f, 0.02f, -60f), new Vector2(70f, 60f), SurfaceAssets.Kind.GrassDry,
                new Color(0.94f, 0.92f, 0.8f));

            Patch(root, AlderwickCenter, new Vector2(46f, 40f), SurfaceAssets.Kind.Grass,
                new Color(0.92f, 0.98f, 0.82f), 0.03f);
            Patch(root, new Vector3(0f, 0f, 57f), new Vector2(27f, 18f), SurfaceAssets.Kind.Dirt,
                new Color(1f, 0.94f, 0.8f), 0.045f);
            Patch(root, DurnmarCenter, new Vector2(42f, 36f), SurfaceAssets.Kind.GrassDry,
                new Color(0.82f, 0.76f, 0.6f), 0.03f);
            Patch(root, NocturneCenter, new Vector2(42f, 36f), SurfaceAssets.Kind.StoneDark,
                new Color(0.86f, 0.86f, 0.95f), 0.03f);
            Patch(root, new Vector3(0f, 0f, CryptGate.z + 2f), new Vector2(26f, 22f), SurfaceAssets.Kind.Stone,
                new Color(0.9f, 0.9f, 0.94f), 0.03f);

            Road(root, AlderwickCenter + new Vector3(0f, 0f, -16f), new Vector3(0f, 0f, -32f), 9f);
            Road(root, new Vector3(0f, 0f, -30f), CryptGate, 8f);
            Road(root, new Vector3(0f, 0f, -24f), DurnmarCenter + new Vector3(0f, 0f, 16f), 6.5f);
            Road(root, new Vector3(0f, 0f, -24f), NocturneCenter + new Vector3(0f, 0f, 17f), 6.5f);
            Road(root, DurnmarCenter + new Vector3(0f, 0f, 16f), DurnmarCenter, 8f);
            Road(root, NocturneCenter + new Vector3(0f, 0f, 17f), NocturneCenter, 8f);

            var pond = GameObject.CreatePrimitive(PrimitiveType.Plane);
            pond.name = "Mill Pond";
            pond.transform.SetParent(root, false);
            pond.transform.position = new Vector3(27f, 0.05f, 40f);
            pond.transform.localScale = new Vector3(2.4f, 1f, 1.5f);
            pond.GetComponent<Renderer>().material = SurfaceAssets.TransparentFor(SurfaceAssets.Kind.Water,
                new Color(0.42f, 0.72f, 0.76f), new Vector2(2.6f, 2.2f), 0.86f, 0.78f);
            Destroy(pond.GetComponent<Collider>());

            Random.InitState(31);
            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                WorldModels.Spawn("rock", root, new Vector3(27f + Mathf.Cos(angle) * 13f, 0f, 40f + Mathf.Sin(angle) * 9f), Random.Range(0f, 360f), Random.Range(1f, 1.8f));
            }

            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f;
                float radius = 215f;
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Distant Hill";
                hill.transform.SetParent(root, false);
                hill.transform.position = new Vector3(Mathf.Cos(angle) * radius, -24f, -20f + Mathf.Sin(angle) * radius);
                hill.transform.localScale = new Vector3(150f, 68f + (i % 3) * 22f, 150f);
                hill.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.GrassDry,
                    new Color(0.46f, 0.53f, 0.44f), new Vector2(12f, 8f));
                Destroy(hill.GetComponent<Collider>());
            }

            Boundary(root, new Vector3(0f, 0f, 88f), new Vector3(240f, 40f, 2f));
            Boundary(root, new Vector3(0f, 0f, -122f), new Vector3(240f, 40f, 2f));
            Boundary(root, new Vector3(-118f, 0f, -20f), new Vector3(2f, 40f, 240f));
            Boundary(root, new Vector3(118f, 0f, -20f), new Vector3(2f, 40f, 240f));
        }

        void BuildAlderwick(Transform root)
        {
            var village = new GameObject("Alderwick").transform;
            village.SetParent(root, false);

            Spawn("house", village, new Vector3(-13f, 0f, 66f), 18f);
            Spawn("house", village, new Vector3(12f, 0f, 68f), -22f);
            Spawn("house", village, new Vector3(-6f, 0f, 51f), 6f);
            Spawn("house", village, new Vector3(25f, 0f, 71f), -14f);
            Spawn("house", village, new Vector3(-24f, 0f, 70f), 21f);
            Spawn("watchtower", village, new Vector3(18f, 0f, 55f), 0f);
            Spawn("well", village, new Vector3(-8f, 0f, 64f), 0f);
            Spawn("crate", village, new Vector3(8f, 0f, 51.5f), 20f);
            Spawn("crate", village, new Vector3(9f, 0f, 50.4f), -12f);
            Spawn("crate", village, new Vector3(-10f, 0f, 61f), 5f);

            Spawn("lanternpost", village, new Vector3(6.5f, 0f, 48f), 0f);
            Spawn("lanternpost", village, new Vector3(-10f, 0f, 71f), 180f);
            Spawn("lanternpost", village, new Vector3(6.5f, 0f, 72f), 0f);

            for (int i = 0; i < 5; i++)
            {
                Spawn("fence", village, new Vector3(-19f, 0f, 52f + i * 2.3f), 90f, 1.3f);
                Spawn("fence", village, new Vector3(23f, 0f, 64f + i * 2.3f), 90f, 1.3f);
            }

            // market row and street dressing
            Stall(village, new Vector3(5.8f, 0f, 57.2f), 196f, new Color(0.86f, 0.68f, 0.42f));
            Stall(village, new Vector3(-11.6f, 0f, 56.4f), 158f, new Color(0.58f, 0.66f, 0.48f));
            Barrel(village, new Vector3(9.6f, 0f, 54.2f));
            Barrel(village, new Vector3(8.7f, 0f, 53.4f), 34f, 0.88f);
            Barrel(village, new Vector3(-9.4f, 0f, 54.2f), 12f, 1.05f);
            Cart(village, new Vector3(-2.4f, 0f, 44.6f), 14f);
            Bench(village, new Vector3(-4.4f, 0f, 63.6f), 90f);
            Bench(village, new Vector3(-11.6f, 0f, 63.6f), -90f);
            Signpost(village, new Vector3(4.6f, 0f, 70.8f), 12f);
            Planter(village, new Vector3(-15.8f, 0f, 61.4f), 0f);
            Planter(village, new Vector3(14.4f, 0f, 64.2f), 28f);
            Planter(village, new Vector3(-16.2f, 0f, 66.4f), 90f);

            // Outer ring of homes and the south gate give the village a full silhouette
            // when the third-person camera is low behind the player.
            Spawn("house", village, new Vector3(-31f, 0f, 63f), 88f);
            Spawn("house", village, new Vector3(31f, 0f, 62f), -92f);
            Spawn("house", village, new Vector3(0f, 0f, 84f), 180f);
            Spawn("longhouse", village, new Vector3(-27f, 0f, 76f), 118f);
            Spawn("watchtower", village, new Vector3(-23f, 0f, 47f), 0f);
            Spawn("gate", village, new Vector3(0f, 0f, 41.5f), 0f);

            for (int i = 0; i < 6; i++)
            {
                Spawn("fence", village, new Vector3(25f + i * 2.3f, 0f, 55.4f), 0f, 1.3f);
                Spawn("fence", village, new Vector3(-35f + i * 2.3f, 0f, 57.2f), 0f, 1.3f);
            }

            Spawn("lanternpost", village, new Vector3(-6.5f, 0f, 47f), 0f);
            Spawn("lanternpost", village, new Vector3(6.5f, 0f, 60f), 180f);
            Spawn("lanternpost", village, new Vector3(-6.5f, 0f, 66f), 0f);
            Spawn("lanternpost", village, new Vector3(26f, 0f, 66f), 180f);

            Bench(village, new Vector3(8.8f, 0f, 56.2f), 90f);
            Bench(village, new Vector3(-13.6f, 0f, 58.4f), -90f);
            Barrel(village, new Vector3(12.4f, 0f, 58.6f), 24f, 1.02f);
            Barrel(village, new Vector3(-4.4f, 0f, 52.6f), 8f, 0.94f);
            Cart(village, new Vector3(1.2f, 0f, 38.6f), 6f);
            Planter(village, new Vector3(-4.6f, 0f, 72.4f), 0f);
            Planter(village, new Vector3(10.6f, 0f, 71.8f), 40f);
            Signpost(village, new Vector3(-6.8f, 0f, 40.6f), 8f);

            var shadeA = Spawn("oak", village, new Vector3(-31f, 0f, 73f), 24f, 5.6f);
            WorldModels.Tint(shadeA, new Color(0.42f, 0.52f, 0.34f));
            var shadeB = Spawn("pine", village, new Vector3(29f, 0f, 76f), 0f, 6.8f);
            WorldModels.Tint(shadeB, new Color(0.36f, 0.46f, 0.32f));

            var elderGo = WorldModels.SpawnHuman("woman", village, ElderPosition, 0f);
            elder = elderGo.transform;
            WorldModels.Tint(elderGo, new Color(0.42f, 0.44f, 0.38f), "cloth");
            WorldModels.Tint(elderGo, new Color(0.86f, 0.72f, 0.38f), "accent");
            WorldModels.Tint(elderGo, new Color(0.62f, 0.60f, 0.56f), "hair");

            AddBlocker(root, new Vector3(-13f, 0f, 66f), new Vector3(5.4f, 3f, 4.4f), 18f);
            AddBlocker(root, new Vector3(12f, 0f, 68f), new Vector3(5.4f, 3f, 4.4f), -22f);
            AddBlocker(root, new Vector3(-6f, 0f, 51f), new Vector3(5.4f, 3f, 4.4f), 6f);
            AddBlocker(root, new Vector3(25f, 0f, 71f), new Vector3(5.4f, 3f, 4.4f), -14f);
            AddBlocker(root, new Vector3(-24f, 0f, 70f), new Vector3(5.4f, 3f, 4.4f), 21f);
            AddBlocker(root, new Vector3(18f, 0f, 55f), new Vector3(3.6f, 5f, 3.6f), 0f);
            AddBlocker(root, new Vector3(-8f, 0f, 64f), new Vector3(2.8f, 1.2f, 2.8f), 0f);
            AddBlocker(root, new Vector3(-31f, 0f, 63f), new Vector3(5.4f, 3f, 4.4f), 88f);
            AddBlocker(root, new Vector3(31f, 0f, 62f), new Vector3(5.4f, 3f, 4.4f), -92f);
            AddBlocker(root, new Vector3(0f, 0f, 84f), new Vector3(5.4f, 3f, 4.4f), 180f);
            AddBlocker(root, new Vector3(-27f, 0f, 76f), new Vector3(6.4f, 3f, 4f), 118f);
            AddBlocker(root, new Vector3(-23f, 0f, 47f), new Vector3(3.6f, 5f, 3.6f), 0f);

            Light(village, new Vector3(6.5f, 2.9f, 48f), new Color(1f, 0.68f, 0.36f), 8f, 2.1f);
            Light(village, new Vector3(-10f, 2.9f, 71f), new Color(1f, 0.68f, 0.36f), 8f, 1.8f);
            Light(village, new Vector3(6.5f, 2.9f, 72f), new Color(1f, 0.68f, 0.36f), 8f, 1.8f);
            Light(village, new Vector3(5.8f, 2.4f, 57.2f), new Color(1f, 0.72f, 0.4f), 6f, 1.2f);
            Light(village, new Vector3(-6.5f, 2.9f, 47f), new Color(1f, 0.68f, 0.36f), 7f, 1.7f);
            Light(village, new Vector3(6.5f, 2.9f, 60f), new Color(1f, 0.68f, 0.36f), 7f, 1.7f);
            Light(village, new Vector3(-6.5f, 2.9f, 66f), new Color(1f, 0.68f, 0.36f), 7f, 1.7f);
            Light(village, new Vector3(26f, 2.9f, 66f), new Color(1f, 0.68f, 0.36f), 7f, 1.6f);
        }

        void BuildForest(Transform root)
        {
            var forest = new GameObject("Greywood").transform;
            forest.SetParent(root, false);
            Random.InitState(77);

            int trees = 0;
            for (int i = 0; i < 3000 && trees < 250; i++)
            {
                float x = Random.Range(-96f, 96f);
                float z = Random.Range(-50f, 50f);
                if (NearVillage(x, z, 34f)) continue;
                if (Mathf.Abs(x) < 18f && z < 22f && z > -62f) continue;

                bool pine = Random.value < 0.64f;
                Spawn(pine ? "pine" : "oak", forest, new Vector3(x, 0f, z), Random.Range(0f, 360f),
                    pine ? Random.Range(7.5f, 13.5f) : Random.Range(6.4f, 10.5f));
                AddBlocker(root, new Vector3(x, 0f, z), new Vector3(0.9f, 4f, 0.9f), 0f);
                trees++;
            }

            // Undergrowth: saplings and shrubs fill the gaps between trunks so the
            // forest floor reads as dense rather than a lawn with poles.
            int shrubs = 0;
            for (int i = 0; i < 1800 && shrubs < 150; i++)
            {
                float x = Random.Range(-94f, 94f);
                float z = Random.Range(-48f, 48f);
                if (NearVillage(x, z, 30f)) continue;
                if (Mathf.Abs(x) < 18f && z < 40f && z > -62f) continue;

                bool sapling = Random.value < 0.42f;
                float height = sapling ? Random.Range(2.2f, 3.8f) : Random.Range(1.6f, 2.8f);
                var shrub = Spawn(sapling ? "pine" : "oak", forest, new Vector3(x, 0f, z), Random.Range(0f, 360f), height);
                WorldModels.Tint(shrub, new Color(0.38f, 0.5f, 0.32f));
                shrubs++;
            }

            for (int i = 0; i < 36; i++)
            {
                float x = Random.Range(-96f, 96f);
                float z = Random.Range(-50f, 50f);
                if (NearVillage(x, z, 30f)) continue;
                if (Mathf.Abs(x) < 18f && z < 40f && z > -62f) continue;
                Spawn("rock", forest, new Vector3(x, 0f, z), Random.Range(0f, 360f), Random.Range(1f, 2.2f));
            }
        }

        void BuildDurnmar(Transform root)
        {
            var village = new GameObject("Durnmar").transform;
            village.SetParent(root, false);

            Spawn("longhouse", village, DurnmarCenter + new Vector3(-11f, 0f, 8f), 12f);
            Spawn("longhouse", village, DurnmarCenter + new Vector3(10f, 0f, 6f), -18f);
            Spawn("longhouse", village, DurnmarCenter + new Vector3(0f, 0f, -10f), 0f);
            Spawn("watchtower", village, DurnmarCenter + new Vector3(16f, 0f, -6f), 0f);

            AddBlocker(root, DurnmarCenter + new Vector3(-11f, 0f, 8f), new Vector3(6.4f, 3f, 4f), 12f);
            AddBlocker(root, DurnmarCenter + new Vector3(10f, 0f, 6f), new Vector3(6.4f, 3f, 4f), -18f);
            AddBlocker(root, DurnmarCenter + new Vector3(0f, 0f, -10f), new Vector3(6.4f, 3f, 4f), 0f);

            for (int i = 0; i < 4; i++)
            {
                Spawn("crate", village, DurnmarCenter + new Vector3(-4f + i * 2.4f, 0f, 2.5f), i * 30f);
                Spawn("fence", village, DurnmarCenter + new Vector3(-17f + i * 2.4f, 0f, -16f), 0f);
            }

            Totem(village, DurnmarCenter + new Vector3(-4.6f, 0f, 14f), 0f);
            Totem(village, DurnmarCenter + new Vector3(4.6f, 0f, 14f), 0f);
            Barrel(village, DurnmarCenter + new Vector3(6.8f, 0f, 1.4f), 0f, 1.12f);
            Barrel(village, DurnmarCenter + new Vector3(7.8f, 0f, 2.3f), 40f, 0.92f);
            Barrel(village, DurnmarCenter + new Vector3(-7.2f, 0f, 2.1f), 18f, 1.02f);

            Vector3 rack = DurnmarCenter + new Vector3(-2.4f, 0f, -6.4f);
            Prop(village, PrimitiveType.Cube, "Weapon Rack", rack + Vector3.up * 1.15f, new Vector3(2.3f, 0.12f, 0.12f), WoodMaterial());
            for (int i = 0; i < 2; i++)
            {
                Prop(village, PrimitiveType.Cylinder, "Rack Post", rack + new Vector3(i == 0 ? -1.05f : 1.05f, 0.58f, 0f),
                    new Vector3(0.12f, 0.58f, 0.12f), DarkWoodMaterial());
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 spear = rack + new Vector3(-0.7f + i * 0.7f, 0f, 0.14f);
                Prop(village, PrimitiveType.Cylinder, "Spear Shaft", spear + Vector3.up * 0.92f,
                    new Vector3(0.05f, 0.92f, 0.05f), WoodMaterial());
                Prop(village, PrimitiveType.Cylinder, "Spear Tip", spear + Vector3.up * 1.93f,
                    new Vector3(0.075f, 0.22f, 0.075f),
                    SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Bone, new Color(0.95f, 0.92f, 0.82f), new Vector2(1.2f, 1.2f)));
            }

            Vector3 hide = DurnmarCenter + new Vector3(12.5f, 0f, 4.6f);
            Prop(village, PrimitiveType.Cylinder, "Hide Pole", hide + Vector3.up * 1.15f, new Vector3(0.1f, 1.15f, 0.1f), DarkWoodMaterial());
            Prop(village, PrimitiveType.Cube, "Stretched Hide", hide + Vector3.up * 1.5f, new Vector3(1.5f, 1.05f, 0.04f),
                SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Fur, new Color(0.74f, 0.58f, 0.46f), new Vector2(1.6f, 1.4f)));

            var fire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fire.name = "Campfire";
            fire.transform.SetParent(village, false);
            fire.transform.position = DurnmarCenter + new Vector3(0f, 0.15f, 3f);
            fire.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            fire.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Ember,
                Color.white, new Vector2(2.4f, 1.2f), 0.4f);
            Destroy(fire.GetComponent<Collider>());

            Light(village, DurnmarCenter + new Vector3(0f, 2.2f, 3f), new Color(1f, 0.55f, 0.22f), 14f, 3.2f);

            Spawn("longhouse", village, DurnmarCenter + new Vector3(22f, 0f, 15f), 205f, 4.4f);
        }

        void BuildNocturne(Transform root)
        {
            var village = new GameObject("Nocturne").transform;
            village.SetParent(root, false);

            var spireA = Spawn("spire", village, NocturneCenter + new Vector3(-12f, 0f, 9f), 20f);
            var spireB = Spawn("spire", village, NocturneCenter + new Vector3(11f, 0f, 7f), -14f);
            var spireC = Spawn("spire", village, NocturneCenter + new Vector3(0f, 0f, -12f), 0f);
            var spireTint = new Color(0.34f, 0.28f, 0.41f);
            WorldModels.Tint(spireA, spireTint);
            WorldModels.Tint(spireB, spireTint);
            WorldModels.Tint(spireC, spireTint);

            AddBlocker(root, NocturneCenter + new Vector3(-12f, 0f, 9f), new Vector3(5f, 6f, 5f), 20f);
            AddBlocker(root, NocturneCenter + new Vector3(11f, 0f, 7f), new Vector3(5f, 6f, 5f), -14f);
            AddBlocker(root, NocturneCenter + new Vector3(0f, 0f, -12f), new Vector3(5f, 6f, 5f), 0f);

            Random.InitState(303);
            for (int i = 0; i < 5; i++)
            {
                float angle = i / 5f * Mathf.PI * 2f;
                var tree = Spawn("oak", village, NocturneCenter + new Vector3(Mathf.Cos(angle) * 18f, 0f, Mathf.Sin(angle) * 15f), Random.Range(0f, 360f), Random.Range(5.5f, 7.5f));
                WorldModels.Tint(tree, new Color(0.26f, 0.25f, 0.30f));
            }

            Spawn("pillar", village, NocturneCenter + new Vector3(-4f, 0f, 1f), 0f);
            Spawn("pillar", village, NocturneCenter + new Vector3(4f, 0f, 1f), 0f);
            Spawn("lanternpost", village, NocturneCenter + new Vector3(-8f, 0f, -3f), 90f);
            Spawn("lanternpost", village, NocturneCenter + new Vector3(8f, 0f, -3f), -90f);

            for (int i = 0; i < 5; i++)
            {
                Gravestone(village, NocturneCenter + new Vector3(-10.4f + i * 2.7f, 0f, -6.5f),
                    i % 2 == 0 ? 6f : -5f, i % 3 == 0 ? 1.15f : 0.95f);
            }
            for (int i = 0; i < 3; i++)
            {
                Spawn("fence", village, NocturneCenter + new Vector3(-16f + i * 2.3f, 0f, 12.4f), 0f);
                Spawn("fence", village, NocturneCenter + new Vector3(10f + i * 2.3f, 0f, 12.4f), 0f);
            }

            Brazier(village, NocturneCenter + new Vector3(-6.8f, 0f, 5.2f));
            Brazier(village, NocturneCenter + new Vector3(6.8f, 0f, 5.2f));

            Light(village, NocturneCenter + new Vector3(-4f, 3.4f, 1f), new Color(0.72f, 0.42f, 0.95f), 12f, 2.2f);
            Light(village, NocturneCenter + new Vector3(4f, 3.4f, 1f), new Color(0.72f, 0.42f, 0.95f), 12f, 2.2f);
            Light(village, NocturneCenter + new Vector3(-6.8f, 1.6f, 5.2f), new Color(0.62f, 0.4f, 0.95f), 8f, 1.6f);
            Light(village, NocturneCenter + new Vector3(6.8f, 1.6f, 5.2f), new Color(0.62f, 0.4f, 0.95f), 8f, 1.6f);
        }

        void BuildCrypt(Transform root)
        {
            var crypt = new GameObject("Sunken Crypt").transform;
            crypt.SetParent(root, false);

            Spawn("gate", crypt, CryptGate, 0f, 5.4f);
            Spawn("pillar", crypt, new Vector3(-5.5f, 0f, -103f), 0f);
            Spawn("pillar", crypt, new Vector3(5.5f, 0f, -103f), 0f);
            Spawn("rock", crypt, new Vector3(-9f, 0f, -96f), 20f, 2.2f);
            Spawn("rock", crypt, new Vector3(9f, 0f, -95f), 75f, 1.8f);

            for (int i = 0; i < 4; i++)
            {
                Gravestone(crypt, new Vector3(-15f + i * 3.4f, 0f, -85f - (i % 2) * 1.6f), i * 9f, 0.86f + (i % 2) * 0.22f);
            }

            AddBlocker(root, new Vector3(-1.95f, 0f, CryptGate.z), new Vector3(1.1f, 4.5f, 1.2f), 0f);
            AddBlocker(root, new Vector3(1.95f, 0f, CryptGate.z), new Vector3(1.1f, 4.5f, 1.2f), 0f);

            Light(crypt, new Vector3(-5.5f, 3.4f, -97f), new Color(1f, 0.72f, 0.46f), 13f, 1.45f);
            Light(crypt, new Vector3(5.5f, 3.4f, -97f), new Color(1f, 0.72f, 0.46f), 13f, 1.45f);
            Brazier(crypt, new Vector3(-5.5f, 0f, -97f));
            Brazier(crypt, new Vector3(5.5f, 0f, -97f));
        }

        void BuildDungeon(Transform root)
        {
            var dungeon = new GameObject("Dungeon").transform;
            dungeon.SetParent(root, false);
            Vector3 origin = new Vector3(0f, 0f, DungeonZ);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Crypt Floor";
            floor.transform.SetParent(dungeon, false);
            // Sit just above the ground plane; at exactly 0 the grass texture z-fights through.
            floor.transform.position = origin + new Vector3(0f, -0.19f, 0f);
            floor.transform.localScale = new Vector3(46f, 0.5f, 38f);
            floor.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.StoneDark,
                new Color(0.78f, 0.78f, 0.86f), new Vector2(11.5f, 9.5f));

            var wallMaterial = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.StoneDark,
                new Color(0.82f, 0.82f, 0.9f), new Vector2(5.5f, 2f));
            Wall(dungeon, origin + new Vector3(0f, 4f, 18.5f), new Vector3(46f, 8f, 1f), wallMaterial);
            Wall(dungeon, origin + new Vector3(0f, 4f, -18.5f), new Vector3(46f, 8f, 1f), wallMaterial);
            Wall(dungeon, origin + new Vector3(-22.5f, 4f, 0f), new Vector3(1f, 8f, 38f), wallMaterial);
            Wall(dungeon, origin + new Vector3(22.5f, 4f, 0f), new Vector3(1f, 8f, 38f), wallMaterial);

            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -9f : 9f;
                float z = i < 2 ? 4f : -12f;
                Spawn("pillar", dungeon, origin + new Vector3(x, 0f, z), 0f, 4.5f);
                AddBlocker(root, origin + new Vector3(x, 0f, z), new Vector3(1.4f, 4f, 1.4f), 0f);
            }

            Light(dungeon, origin + new Vector3(-9f, 4.3f, 4f), new Color(1f, 0.74f, 0.48f), 12f, 1.35f);
            Light(dungeon, origin + new Vector3(9f, 4.3f, -12f), new Color(1f, 0.74f, 0.48f), 12f, 1.35f);
            Light(dungeon, origin + new Vector3(-5f, 3.6f, 12f), new Color(1f, 0.76f, 0.52f), 12f, 1.1f);
            Light(dungeon, origin + new Vector3(5f, 3.6f, 12f), new Color(1f, 0.76f, 0.52f), 12f, 1.1f);
            Light(dungeon, origin + new Vector3(0f, 4.2f, -4f), new Color(1f, 0.74f, 0.5f), 16f, 1.2f);
            Brazier(dungeon, origin + new Vector3(-5f, 0f, 12f));
            Brazier(dungeon, origin + new Vector3(5f, 0f, 12f));
            Brazier(dungeon, origin + new Vector3(0f, 0f, -4f));

            var seal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seal.name = "Warden Seal";
            seal.transform.SetParent(dungeon, false);
            seal.transform.position = origin + new Vector3(0f, 0.03f, -14f);
            seal.transform.localScale = new Vector3(8f, 0.03f, 8f);
            seal.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Stone,
                new Color(0.72f, 0.58f, 0.8f), new Vector2(4f, 3f), 0.5f);
            Destroy(seal.GetComponent<Collider>());

            Spawn("pillar", dungeon, origin + new Vector3(-4.5f, 0f, -17f), 0f, 3.4f);
            Spawn("pillar", dungeon, origin + new Vector3(4.5f, 0f, -17f), 0f, 3.4f);

            Vector3[] rubble =
            {
                origin + new Vector3(-19f, 0f, 10f), origin + new Vector3(18f, 0f, 14f),
                origin + new Vector3(-20f, 0f, -8f), origin + new Vector3(19f, 0f, -10f),
                origin + new Vector3(-16f, 0f, -16f), origin + new Vector3(16f, 0f, 6f)
            };
            float[] rubbleSizes = { 1.5f, 1.1f, 1.8f, 1.3f, 1.6f, 1f };
            for (int i = 0; i < rubble.Length; i++)
                Spawn("rock", dungeon, rubble[i], i * 47f, rubbleSizes[i]);

            var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "Exit Portal";
            portal.transform.SetParent(dungeon, false);
            portal.transform.position = DungeonExit + Vector3.up * 0.08f;
            portal.transform.localScale = new Vector3(3.2f, 0.06f, 3.2f);
            portal.GetComponent<Renderer>().material = SurfaceAssets.EmissiveFor(SurfaceAssets.Kind.Ember,
                new Color(1.6f, 0.85f, 0.32f), new Vector2(2f, 2f));
            Destroy(portal.GetComponent<Collider>());
            Light(dungeon, DungeonExit + Vector3.up * 1.5f, new Color(1f, 0.62f, 0.3f), 10f, 2f);
        }

        void SpawnEnemies(Transform root)
        {
            Vector3[] wolves =
            {
                new Vector3(-16f, 0f, 16f), new Vector3(13f, 0f, 20f), new Vector3(-30f, 0f, 4f),
                new Vector3(26f, 0f, 2f), new Vector3(-9f, 0f, -12f), new Vector3(9f, 0f, -22f)
            };
            for (int i = 0; i < wolves.Length; i++) SpawnEnemy(root, "wolf", wolves[i]);

            Vector3[] orcs =
            {
                DurnmarCenter + new Vector3(-5f, 0f, 11f), DurnmarCenter + new Vector3(7f, 0f, 11f),
                DurnmarCenter + new Vector3(-14f, 0f, -2f), DurnmarCenter + new Vector3(14f, 0f, -4f),
                DurnmarCenter + new Vector3(-6f, 0f, -16f), DurnmarCenter + new Vector3(6f, 0f, -16f)
            };
            for (int i = 0; i < orcs.Length; i++) SpawnEnemy(root, "orc", orcs[i]);
            SpawnEnemy(root, "chief", DurnmarCenter + new Vector3(1f, 0f, 5f), null, new Color(0.58f, 0.24f, 0.18f));

            Vector3[] vampires =
            {
                NocturneCenter + new Vector3(-7f, 0f, 12f), NocturneCenter + new Vector3(8f, 0f, 11f),
                NocturneCenter + new Vector3(-14f, 0f, -3f), NocturneCenter + new Vector3(14f, 0f, -4f),
                NocturneCenter + new Vector3(0f, 0f, -2f)
            };
            for (int i = 0; i < vampires.Length; i++) SpawnEnemy(root, "vampire", vampires[i]);
            SpawnEnemy(root, "countess", NocturneCenter + new Vector3(0f, 0f, 5f), null, new Color(0.52f, 0.2f, 0.5f));

            Vector3 origin = new Vector3(0f, 0f, DungeonZ);
            Vector3[] skeletons =
            {
                origin + new Vector3(-13f, 0f, 2f), origin + new Vector3(13f, 0f, -4f),
                origin + new Vector3(-7f, 0f, -13f), origin + new Vector3(8f, 0f, -9f)
            };
            for (int i = 0; i < skeletons.Length; i++) SpawnEnemy(root, "skeleton", skeletons[i]);
            SpawnEnemy(root, "warden", origin + new Vector3(0f, 0f, -14f), null, new Color(0.34f, 0.62f, 0.52f));

            Color[] villagerColors =
            {
                new Color(0.45f, 0.36f, 0.24f), new Color(0.32f, 0.30f, 0.42f),
                new Color(0.46f, 0.30f, 0.26f), new Color(0.30f, 0.40f, 0.34f),
                new Color(0.52f, 0.42f, 0.22f), new Color(0.34f, 0.36f, 0.46f),
                new Color(0.40f, 0.34f, 0.44f), new Color(0.48f, 0.40f, 0.30f),
                new Color(0.28f, 0.38f, 0.40f), new Color(0.50f, 0.34f, 0.30f)
            };
            string[] names = { "Hanna", "Bram", "Sable", "Orin", "Pip", "Tess", "Mira", "Tomas", "Wren", "Nell" };
            string[] bodies = { "woman", "man", "woman", "man", "child", "child", "woman", "man", "child", "woman" };
            Vector3[] spots =
            {
                new Vector3(-16f, 0f, 58f), new Vector3(-2f, 0f, 62f), new Vector3(10f, 0f, 60f),
                new Vector3(-15f, 0f, 48f), new Vector3(4f, 0f, 61f), new Vector3(-13f, 0f, 60f),
                new Vector3(18f, 0f, 64f), new Vector3(-20f, 0f, 66f), new Vector3(8f, 0f, 72f),
                new Vector3(-6f, 0f, 47f)
            };
            for (int i = 0; i < names.Length; i++)
            {
                SpawnEnemy(root, "villager", spots[i], names[i], villagerColors[i], bodies[i]);
            }
        }

        WorldEnemy SpawnEnemy(Transform root, string kind, Vector3 position, string display = null,
            Color? tint = null, string body = null)
        {
            var go = new GameObject(kind + " enemy");
            go.transform.SetParent(root, false);
            var enemy = go.AddComponent<WorldEnemy>();
            enemy.Setup(this, kind, position, display, tint, body);
            enemies.Add(enemy);
            return enemy;
        }

        void SpawnPlayer(Transform root)
        {
            var go = new GameObject("World Player");
            go.transform.SetParent(root, false);
            go.transform.position = VillageSpawn;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.slopeLimit = 55f;

            Player = go.AddComponent<WorldPlayer>();
            Player.Initialize(this, hero);
        }

        // ------------------------------------------------------------------ combat

        public void PlayerAttack(WorldPlayer player, bool heavy)
        {
            Vector3 forward = player.transform.forward;
            float range = heavy ? 2.9f : 2.35f;
            float multiplier = heavy ? 2.15f : 1f;
            int hits = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.Dead || enemy.Friendly) continue;
                Vector3 delta = enemy.transform.position - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude > range + 0.6f) continue;
                if (Vector3.Angle(forward, delta) > 85f) continue;
                enemy.TakeDamage(PlayerDamage * multiplier * Random.Range(0.9f, 1.1f), player.transform.position, heavy);
                hits++;
            }

            if (Audio != null) Audio.PlaySwing(heavy);
            if (hits > 0)
            {
                if (Audio != null) Audio.PlayHit(heavy);
                Shake(heavy ? 0.16f : 0.07f);
            }
            WorldFx.Burst(this, player.transform.position + forward * 1.3f + Vector3.up * 1.05f,
                heavy ? new Color(1f, 0.78f, 0.44f) : new Color(0.96f, 0.9f, 0.74f), heavy ? 9 : 5, heavy ? 4.5f : 3.2f, 0.08f);
        }

        public void PlayerArt(WorldPlayer player)
        {
            switch (hero.Ability)
            {
                case AbilityKind.Grace:
                    player.Heal(MaxHp * 0.32f);
                    player.GrantHaste(6f);
                    if (Audio != null) Audio.PlayHeal();
                    Toast("WAYFARER'S GRACE — mended and swift", 2.4f);
                    WorldFx.Burst(this, player.transform.position + Vector3.up * 1.1f, new Color(0.62f, 0.95f, 0.62f), 22, 4f);
                    break;
                case AbilityKind.Bulwark:
                    player.GrantShield(6f);
                    if (Audio != null) Audio.PlayShield();
                    Toast("BULWARK OATH — damage halved", 2.4f);
                    WorldFx.Burst(this, player.transform.position + Vector3.up * 1.1f, new Color(1f, 0.82f, 0.42f), 20, 3.5f);
                    break;
                case AbilityKind.Emberlash:
                    int burns = 0;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var enemy = enemies[i];
                        if (enemy == null || enemy.Dead || enemy.Friendly) continue;
                        Vector3 delta = enemy.transform.position - player.transform.position;
                        delta.y = 0f;
                        if (delta.magnitude > 6.2f) continue;
                        enemy.TakeDamage(PlayerDamage * 2.3f, player.transform.position, true);
                        burns++;
                    }
                    if (Audio != null) Audio.PlayBolt();
                    Shake(0.2f);
                    Toast("EMBERLASH — cinders answer", 2.4f);
                    WorldFx.Burst(this, player.transform.position + player.transform.forward * 2f + Vector3.up * 1.1f,
                        new Color(1f, 0.55f, 0.2f), 26, 6f, 0.12f);
                    break;
            }
        }

        public void EnemyStrike(WorldEnemy enemy, WorldPlayer player)
        {
            if (player == null || player.Dead) return;
            if (Audio != null) Audio.PlaySwing(enemy.Boss);
            player.TakeDamage(enemy.Damage, enemy.transform.position);
            WorldFx.Burst(this, player.transform.position + Vector3.up * 0.9f, new Color(0.95f, 0.42f, 0.32f), 7, 3.2f, 0.09f);
            if (player.Shielded)
            {
                if (Audio != null) Audio.PlayShield();
            }
            else if (Audio != null) Audio.PlayHit(false);
            Shake(0.08f);
        }

        public void OnPlayerDamaged(float amount, bool blocked)
        {
            damageFlash = Mathf.Min(0.55f, damageFlash + 0.3f);
            Shake(0.14f);
            AddFloat(Player.transform.position + Vector3.up * 2f,
                (blocked ? "BLOCK " : "-") + Mathf.RoundToInt(amount), blocked ? new Color(0.7f, 0.88f, 1f) : new Color(1f, 0.42f, 0.36f), 16);
        }

        public void OnPlayerDied()
        {
            deathTimer = 0f;
            if (Audio != null) Audio.PlayDefeat();
            Toast("You have fallen. The lanterns wait.", 3f);
        }

        public void RespawnPlayer()
        {
            if (Player == null) return;
            InDungeon = false;
            SetDungeonVisuals(false);
            Player.Teleport(VillageSpawn);
            Player.ResetVitals();
            CameraYaw = 180f;
            deathTimer = 0f;
            damageFlash = 0f;
            if (Audio != null) Audio.PlayHeal();
            Toast("You wake beneath the lanterns of Alderwick.", 3.5f);
        }

        public void OnEnemyDamaged(WorldEnemy enemy, float amount, bool heavy)
        {
            AddFloat(enemy.transform.position + Vector3.up * (enemy.Boss ? 2.8f : 2.2f), "-" + Mathf.RoundToInt(amount),
                heavy ? new Color(1f, 0.78f, 0.38f) : new Color(1f, 0.86f, 0.72f), heavy ? 18 : 14);
            WorldFx.Burst(this, enemy.transform.position + Vector3.up * 1.1f, new Color(1f, 0.72f, 0.4f), heavy ? 9 : 5, 3.5f, 0.09f);
            target = enemy;
            targetTimer = 4f;
        }

        public void OnEnemyKilled(WorldEnemy enemy)
        {
            AddXp(enemy.Xp);

            switch (enemy.Kind)
            {
                case "wolf":
                    if (stage == Stage.Wolves)
                    {
                        questHave++;
                        if (questHave >= questNeed) AdvanceStage();
                    }
                    break;
                case "orc":
                    if (stage == Stage.Orcs)
                    {
                        questHave++;
                        if (questHave >= questNeed) AdvanceStage();
                    }
                    break;
                case "chief":
                    grokDead = true;
                    if (stage == Stage.Grok) AdvanceStage();
                    else if (stage == Stage.Orcs && questHave >= questNeed) AdvanceStage();
                    break;
                case "vampire":
                    if (stage == Stage.Vampires)
                    {
                        questHave++;
                        if (questHave >= questNeed) AdvanceStage();
                    }
                    break;
                case "countess":
                    velyraDead = true;
                    if (stage == Stage.Velyra) AdvanceStage();
                    break;
                case "warden":
                    wardenDead = true;
                    if (stage == Stage.Warden) AdvanceStage();
                    break;
            }

            if (enemy.Boss)
            {
                Shake(0.4f);
                Toast(enemy.Kind == "warden" ? "THE UNDYING WARDEN FALLS" : enemy.Display.ToUpperInvariant() + " FALLS", 3.2f);
                if (Audio != null) Audio.PlayVictory();
            }
        }

        // ------------------------------------------------------------------ progression

        void AddXp(float amount)
        {
            if (Level >= 100) return;
            Xp += Mathf.RoundToInt(amount);
            while (Xp >= XpNext && Level < 100)
            {
                Xp -= XpNext;
                Level++;
                XpNext = 55 + Level * 45;
                string newTitle = TitleFor(Level);
                bool titleUp = newTitle != Title;
                Title = newTitle;
                if (Player != null) Player.FullHeal();
                Toast("LEVEL " + Level + " — " + Title.ToUpperInvariant(), 3.2f);
                WorldFx.Burst(this, Player.transform.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.45f), 24, 5f, 0.11f);
                if (Audio != null) Audio.PlayVictory();
                if (titleUp) AddFloat(Player.transform.position + Vector3.up * 2.4f, Title, new Color(1f, 0.86f, 0.5f), 20);
            }
        }

        void BeginStage(Stage next, bool announce = true)
        {
            stage = next;
            questHave = 0;
            switch (next)
            {
                case Stage.Wolves:
                    questTitle = "ASH ON THE WIND";
                    questObjective = "Slay greywood wolves";
                    questNeed = 3;
                    break;
                case Stage.Orcs:
                    questTitle = "THE DURNMAR THREAT";
                    questObjective = "Break the orc warband";
                    questNeed = 6;
                    break;
                case Stage.Grok:
                    questTitle = "THE DURNMAR THREAT";
                    questObjective = "Slay Warchief Grok in Durnmar";
                    questNeed = 1;
                    break;
                case Stage.Vampires:
                    questTitle = "NOCTURNE'S HUNGER";
                    questObjective = "Put the bloodsworn to rest";
                    questNeed = 5;
                    break;
                case Stage.Velyra:
                    questTitle = "NOCTURNE'S HUNGER";
                    questObjective = "Destroy Countess Velyra";
                    questNeed = 1;
                    break;
                case Stage.Crypt:
                    questTitle = "THE SUNKEN CRYPT";
                    questObjective = "Enter the crypt below the old gate";
                    questNeed = 1;
                    break;
                case Stage.Warden:
                    questTitle = "THE SUNKEN CRYPT";
                    questObjective = "Defeat the Undying Warden";
                    questNeed = 1;
                    break;
                case Stage.Ascend:
                    questTitle = "THE UNDYING PATH";
                    questObjective = "Reach level 20 and claim Duskbane";
                    questNeed = 20;
                    break;
                case Stage.Complete:
                    questTitle = "THE UNDYING PATH";
                    questObjective = "Titles climb to 100 — Undying Supreme Lord";
                    questNeed = 100;
                    break;
            }

            if (announce && next != Stage.Complete)
            {
                Toast("NEW QUEST — " + questTitle, 3f);
                if (Audio != null) Audio.PlayClick(0.5f);
            }
        }

        void AdvanceStage()
        {
            switch (stage)
            {
                case Stage.Wolves:
                    AddXp(120f);
                    BeginStage(Stage.Orcs);
                    break;
                case Stage.Orcs:
                    AddXp(220f);
                    BeginStage(grokDead ? Stage.Vampires : Stage.Grok);
                    break;
                case Stage.Grok:
                    AddXp(350f);
                    BeginStage(Stage.Vampires);
                    break;
                case Stage.Vampires:
                    AddXp(280f);
                    BeginStage(velyraDead ? Stage.Crypt : Stage.Velyra);
                    break;
                case Stage.Velyra:
                    AddXp(450f);
                    BeginStage(Stage.Crypt);
                    break;
                case Stage.Warden:
                    AddXp(900f);
                    BeginStage(Stage.Ascend);
                    break;
            }
        }

        void CompleteAscension()
        {
            AddXp(1500f);
            BeginStage(Stage.Complete);
            Toast("THE WORLD REMEMBERS YOUR NAME", 4f);
            if (Audio != null) Audio.PlayVictory();
        }

        public static string TitleFor(int level)
        {
            string title = TitleNames[0];
            for (int i = 0; i < TitleLevels.Length; i++)
            {
                if (level >= TitleLevels[i]) title = TitleNames[i];
            }
            return title;
        }

        // ------------------------------------------------------------------ interaction

        public void Interact(WorldPlayer player)
        {
            if (Paused || MapOpen || player.Dead) return;

            if (InDungeon)
            {
                if (Vector3.Distance(player.transform.position, DungeonExit) < 3.6f) ExitDungeon();
                return;
            }

            if (Vector3.Distance(player.transform.position, ElderPosition) < 3.4f)
            {
                TalkToElder();
                return;
            }

            if (Vector3.Distance(player.transform.position, CryptGate) < 4.4f) EnterDungeon();
        }

        void TalkToElder()
        {
            if (Player == null) return;
            string line;
            switch (stage)
            {
                case Stage.Wolves:
                    line = "Wolves have tasted the flock. Thin the Greywood, and the road will remember you.";
                    break;
                case Stage.Orcs:
                case Stage.Grok:
                    line = "Durnmar's drums carry on the wind. Break the warband before it breaks us.";
                    break;
                case Stage.Vampires:
                case Stage.Velyra:
                    line = "Nocturne does not sleep. Neither do its hungers. End them.";
                    break;
                case Stage.Crypt:
                case Stage.Warden:
                    line = "The old gate opens on older bones. The Warden has not yielded in a thousand years.";
                    break;
                case Stage.Ascend:
                    line = "You carry the lantern now. Grow until the title finds you.";
                    break;
                default:
                    line = "The river sings your name, Undying. Rest is earned, never given.";
                    break;
            }

            Player.Heal(MaxHp * 0.3f);
            Toast(line, 4.5f);
            if (Audio != null) Audio.PlayHeal();
        }

        void EnterDungeon()
        {
            InDungeon = true;
            SetDungeonVisuals(true);
            Player.Teleport(DungeonEntry + Vector3.up * 0.05f);
            CameraYaw = 180f;
            CameraPitch = 22f;
            if (stage == Stage.Crypt) BeginStage(Stage.Warden);
            Toast("THE SUNKEN CRYPT — the dark breathes", 3f);
            if (Audio != null) Audio.PlayAmbientVillage();
        }

        void ExitDungeon()
        {
            InDungeon = false;
            SetDungeonVisuals(false);
            Player.Teleport(GateExit + Vector3.up * 0.05f);
            CameraYaw = 0f;
            Toast("You climb back into the last light.", 2.6f);
        }

        void SetDungeonVisuals(bool inside)
        {
            if (sky != null) sky.gameObject.SetActive(!inside);
            SelectMood();
            if (inside) mood = moodTarget;
            WorldStage.Apply(mood, cam, sun, skyMaterial);
        }

        void SelectMood()
        {
            if (InDungeon) { moodTarget = WorldStage.Crypt; return; }
            if (region == null) { moodTarget = WorldStage.Greywood; return; }
            if (region.StartsWith("DURNMAR")) moodTarget = WorldStage.Durnmar;
            else if (region.StartsWith("NOCTURNE")) moodTarget = WorldStage.Nocturne;
            else if (region.StartsWith("ALDERWICK")) moodTarget = WorldStage.Alderwick;
            else moodTarget = WorldStage.Greywood;
        }

        void BlendMood()
        {
            float k = 1f - Mathf.Exp(-0.85f * Time.unscaledDeltaTime);
            mood = WorldStage.Lerp(mood, moodTarget, k);
            WorldStage.Apply(mood, cam, sun, skyMaterial);
        }

        void UpdateRegion()
        {
            string next;
            if (InDungeon) next = "THE SUNKEN CRYPT";
            else if (Vector3.Distance(Player.transform.position, DurnmarCenter) < 42f) next = "DURNMAR — ORC VILLAGE";
            else if (Vector3.Distance(Player.transform.position, NocturneCenter) < 42f) next = "NOCTURNE — VAMPIRE VILLAGE";
            else if (Vector3.Distance(Player.transform.position, AlderwickCenter) < 36f) next = "ALDERWICK";
            else next = "THE GREYWOOD";

            if (next != region)
            {
                region = next;
                regionTimer = 3.2f;
            }
        }

        // ------------------------------------------------------------------ helpers

        void Toast(string message, float duration)
        {
            if (string.IsNullOrEmpty(message)) return;
            toast = message;
            toastTimer = duration;
        }

        void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        void AddFloat(Vector3 world, string text, Color color, int size)
        {
            floats.Add(new FloatingText { Position = world, Text = text, Color = color, Age = 0f, Life = 1.2f, Size = size });
        }

        GameObject Spawn(string id, Transform parent, Vector3 position, float yaw = 0f, float height = -1f)
        {
            return WorldModels.Spawn(id, parent, position, yaw, height);
        }

        static bool NearVillage(float x, float z, float radius)
        {
            float r2 = radius * radius;
            float dx = x - AlderwickCenter.x;
            float dz = z - AlderwickCenter.z;
            if (dx * dx + dz * dz < r2) return true;
            dx = x - DurnmarCenter.x;
            dz = z - DurnmarCenter.z;
            if (dx * dx + dz * dz < r2) return true;
            dx = x - NocturneCenter.x;
            dz = z - NocturneCenter.z;
            return dx * dx + dz * dz < r2;
        }

        void Patch(Transform parent, Vector3 position, Vector2 size, SurfaceAssets.Kind kind, Color tint, float y = 0.02f, float tile = 7f)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground Patch";
            plane.transform.SetParent(parent, false);
            plane.transform.position = new Vector3(position.x, y, position.z);
            plane.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);
            plane.GetComponent<Renderer>().material =
                SurfaceAssets.MaterialFor(kind, tint, new Vector2(size.x / tile, size.y / tile));
            Destroy(plane.GetComponent<Collider>());
        }

        void Road(Transform parent, Vector3 from, Vector3 to, float width)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.1f) return;
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Road";
            plane.transform.SetParent(parent, false);
            plane.transform.position = new Vector3((from.x + to.x) * 0.5f, 0.035f, (from.z + to.z) * 0.5f);
            plane.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            plane.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);
            plane.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Gravel,
                new Color(0.98f, 0.9f, 0.78f), new Vector2(width / 3.2f, length / 3.2f));
            Destroy(plane.GetComponent<Collider>());
        }

        void AddBlocker(Transform parent, Vector3 position, Vector3 size, float yaw)
        {
            var go = new GameObject("Blocker");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y + size.y * 0.5f, position.z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
        }

        void Boundary(Transform parent, Vector3 position, Vector3 size)
        {
            AddBlocker(parent, position, size, 0f);
        }

        void Wall(Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Crypt Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().material = material;
        }

        void Light(Transform parent, Vector3 position, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            flickers.Add(new Flicker
            {
                Lamp = light,
                Base = intensity,
                Phase = Random.Range(0f, 6.283f),
                Rate = Random.Range(5.5f, 11f)
            });
        }

        void Brazier(Transform parent, Vector3 position)
        {
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Brazier";
            bowl.transform.SetParent(parent, false);
            bowl.transform.position = position + Vector3.up * 0.45f;
            bowl.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            bowl.GetComponent<Renderer>().material = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Metal,
                new Color(0.82f, 0.76f, 0.68f), new Vector2(1.6f, 1f), 0.6f, 0.75f);
            Destroy(bowl.GetComponent<Collider>());

            var coals = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coals.name = "Brazier Coals";
            coals.transform.SetParent(parent, false);
            coals.transform.position = position + Vector3.up * 0.86f;
            coals.transform.localScale = new Vector3(0.44f, 0.06f, 0.44f);
            coals.GetComponent<Renderer>().material = SurfaceAssets.EmissiveFor(SurfaceAssets.Kind.Ember,
                new Color(1.5f, 0.62f, 0.16f), new Vector2(1.4f, 1.4f));
            Destroy(coals.GetComponent<Collider>());

            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(parent, false);
            flame.transform.position = position + Vector3.up * 0.92f;
            flame.transform.localScale = new Vector3(0.3f, 0.46f, 0.3f);
            flame.GetComponent<Renderer>().material = SurfaceAssets.EmissiveFor(SurfaceAssets.Kind.Ember,
                new Color(2.1f, 0.9f, 0.3f), new Vector2(1.2f, 1.2f));
            Destroy(flame.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------ set dressing

        static Material WoodMaterial() =>
            SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Wood, new Color(0.94f, 0.86f, 0.72f), new Vector2(1.6f, 1.1f));

        static Material DarkWoodMaterial() =>
            SurfaceAssets.MaterialFor(SurfaceAssets.Kind.WoodDark, new Color(0.9f, 0.86f, 0.8f), new Vector2(1.8f, 1.2f));

        static Material IronMaterial() =>
            SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Metal, new Color(0.62f, 0.62f, 0.66f), new Vector2(1.4f, 1f), 0.5f, 0.72f);

        static Material StoneMaterial() =>
            SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Stone, new Color(0.92f, 0.9f, 0.86f), new Vector2(1.4f, 1.1f));

        static Material DarkStoneMaterial() =>
            SurfaceAssets.MaterialFor(SurfaceAssets.Kind.StoneDark, new Color(0.9f, 0.9f, 0.96f), new Vector2(1.5f, 1.2f));

        GameObject Prop(Transform parent, PrimitiveType shape, string name, Vector3 position, Vector3 scale, Material material, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = material;
            Destroy(go.GetComponent<Collider>());
            return go;
        }

        void Barrel(Transform parent, Vector3 at, float yaw = 0f, float size = 1f)
        {
            var wood = WoodMaterial();
            var iron = IronMaterial();
            var body = Prop(parent, PrimitiveType.Cylinder, "Barrel", at + Vector3.up * (0.42f * size),
                new Vector3(0.54f * size, 0.42f * size, 0.54f * size), wood, yaw);
            for (int i = 0; i < 2; i++)
            {
                var hoop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hoop.name = "Barrel Hoop";
                hoop.transform.SetParent(body.transform, false);
                hoop.transform.localPosition = new Vector3(0f, i == 0 ? -0.3f : 0.3f, 0f);
                hoop.transform.localScale = new Vector3(1.05f, 0.07f, 1.05f);
                hoop.GetComponent<Renderer>().material = iron;
                Destroy(hoop.GetComponent<Collider>());
            }
        }

        void Bench(Transform parent, Vector3 at, float yaw)
        {
            var wood = WoodMaterial();
            var dark = DarkWoodMaterial();
            Prop(parent, PrimitiveType.Cube, "Bench", at + Vector3.up * 0.42f, new Vector3(1.7f, 0.1f, 0.44f), wood, yaw);
            Prop(parent, PrimitiveType.Cube, "Bench Back", at + Vector3.up * 0.78f + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.19f),
                new Vector3(1.7f, 0.38f, 0.08f), wood, yaw);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.68f : 0.68f;
                Prop(parent, PrimitiveType.Cube, "Bench Leg", at + Vector3.up * 0.2f + Quaternion.Euler(0f, yaw, 0f) * new Vector3(x, 0f, 0f),
                    new Vector3(0.12f, 0.4f, 0.4f), dark, yaw);
            }
        }

        void Stall(Transform parent, Vector3 at, float yaw, Color awning)
        {
            var wood = WoodMaterial();
            var dark = DarkWoodMaterial();
            var cloth = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Cloth, awning, new Vector2(2.4f, 1.4f));
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = turn * new Vector3(i % 2 == 0 ? -1.05f : 1.05f, 0f, i < 2 ? -0.7f : 0.7f);
                Prop(parent, PrimitiveType.Cylinder, "Stall Post", at + corner + Vector3.up * 1.15f,
                    new Vector3(0.11f, 1.15f, 0.11f), dark, yaw);
            }
            Prop(parent, PrimitiveType.Cube, "Stall Counter", at + turn * new Vector3(0f, 0f, 0.42f) + Vector3.up * 0.82f,
                new Vector3(2.3f, 0.12f, 0.62f), wood, yaw);
            Prop(parent, PrimitiveType.Cube, "Stall Awning", at + Vector3.up * 2.3f, new Vector3(2.7f, 0.08f, 1.9f), cloth, yaw);
            Prop(parent, PrimitiveType.Cube, "Stall Valance", at + turn * new Vector3(0f, 0f, 0.94f) + Vector3.up * 2.14f,
                new Vector3(2.7f, 0.28f, 0.05f), cloth, yaw);
            Prop(parent, PrimitiveType.Cube, "Stall Crate", at + turn * new Vector3(-0.62f, 0f, -0.36f) + Vector3.up * 0.26f,
                new Vector3(0.62f, 0.52f, 0.55f), wood, yaw + 8f);
            Prop(parent, PrimitiveType.Sphere, "Stall Apple", at + turn * new Vector3(0.58f, 0f, 0.42f) + Vector3.up * 0.94f,
                new Vector3(0.16f, 0.16f, 0.16f),
                SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Leaf, new Color(0.95f, 0.55f, 0.4f), new Vector2(1f, 1f)), yaw);
        }

        void Cart(Transform parent, Vector3 at, float yaw)
        {
            var wood = WoodMaterial();
            var dark = DarkWoodMaterial();
            var iron = IronMaterial();
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            Prop(parent, PrimitiveType.Cube, "Cart Bed", at + Vector3.up * 0.62f, new Vector3(2.4f, 0.18f, 1.3f), wood, yaw);
            Prop(parent, PrimitiveType.Cube, "Cart Side", at + Vector3.up * 0.86f + turn * new Vector3(0f, 0f, -0.6f),
                new Vector3(2.4f, 0.36f, 0.1f), dark, yaw);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.95f : 0.95f;
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Cart Wheel";
                wheel.transform.SetParent(parent, false);
                wheel.transform.position = at + turn * new Vector3(x, 0f, 0.62f) + Vector3.up * 0.48f;
                wheel.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
                wheel.transform.localScale = new Vector3(0.48f, 0.07f, 0.48f);
                wheel.GetComponent<Renderer>().material = dark;
                Destroy(wheel.GetComponent<Collider>());
                Prop(parent, PrimitiveType.Cylinder, "Cart Hub", at + turn * new Vector3(x, 0f, 0.62f) + Vector3.up * 0.48f,
                    new Vector3(0.12f, 0.1f, 0.12f), iron, yaw);
            }
            Prop(parent, PrimitiveType.Cube, "Cart Shaft", at + turn * new Vector3(-1.55f, 0f, 0f) + Vector3.up * 0.5f,
                new Vector3(1.1f, 0.1f, 0.1f), wood, yaw);
        }

        void Signpost(Transform parent, Vector3 at, float yaw)
        {
            var wood = WoodMaterial();
            var dark = DarkWoodMaterial();
            Prop(parent, PrimitiveType.Cylinder, "Sign Post", at + Vector3.up * 1.1f, new Vector3(0.12f, 1.1f, 0.12f), dark, yaw);
            Prop(parent, PrimitiveType.Cube, "Sign Board", at + Vector3.up * 1.8f + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.42f, 0f, 0f),
                new Vector3(0.9f, 0.26f, 0.07f), wood, yaw);
            Prop(parent, PrimitiveType.Cube, "Sign Board", at + Vector3.up * 1.45f + Quaternion.Euler(0f, yaw + 180f, 0f) * new Vector3(0.34f, 0f, 0f),
                new Vector3(0.7f, 0.22f, 0.07f), wood, yaw);
        }

        void Planter(Transform parent, Vector3 at, float yaw)
        {
            var wood = WoodMaterial();
            Prop(parent, PrimitiveType.Cube, "Planter", at + Vector3.up * 0.24f, new Vector3(1.6f, 0.48f, 0.7f), wood, yaw);
            Prop(parent, PrimitiveType.Cube, "Planter Soil", at + Vector3.up * 0.47f, new Vector3(1.42f, 0.08f, 0.54f),
                SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Dirt, new Color(0.85f, 0.8f, 0.74f), new Vector2(1.2f, 1f)), yaw);
            var leaf = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Leaf, new Color(0.9f, 1f, 0.82f), new Vector2(1.2f, 1.2f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 spot = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.54f + i * 0.36f, 0f, (i % 2 == 0 ? -0.12f : 0.12f)) + Vector3.up * 0.62f;
                Prop(parent, PrimitiveType.Sphere, "Shrub", spot, new Vector3(0.42f, 0.36f, 0.42f), leaf, yaw + i * 40f);
            }
        }

        void Gravestone(Transform parent, Vector3 at, float yaw, float size = 1f)
        {
            var stone = DarkStoneMaterial();
            Prop(parent, PrimitiveType.Cube, "Headstone", at + Vector3.up * 0.55f * size,
                new Vector3(0.62f * size, 1.1f * size, 0.16f * size), stone, yaw);
            Prop(parent, PrimitiveType.Sphere, "Headstone Crown", at + Vector3.up * 1.1f * size,
                new Vector3(0.31f * size, 0.2f * size, 0.16f * size), stone, yaw);
            Prop(parent, PrimitiveType.Cube, "Grave Mound", at + Vector3.up * 0.09f,
                new Vector3(0.78f * size, 0.18f, 1.5f * size),
                SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Dirt, new Color(0.7f, 0.7f, 0.76f), new Vector2(1.4f, 1.2f)), yaw);
        }

        void Totem(Transform parent, Vector3 at, float yaw)
        {
            var dark = DarkWoodMaterial();
            var bone = SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Bone, new Color(0.95f, 0.92f, 0.82f), new Vector2(1.4f, 1.2f));
            Prop(parent, PrimitiveType.Cylinder, "Totem Post", at + Vector3.up * 1.1f, new Vector3(0.2f, 1.1f, 0.2f), dark, yaw);
            Prop(parent, PrimitiveType.Sphere, "Totem Skull", at + Vector3.up * 2.3f, new Vector3(0.42f, 0.5f, 0.44f), bone, yaw);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.16f : 0.16f;
                var tusk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tusk.name = "Totem Horn";
                tusk.transform.SetParent(parent, false);
                tusk.transform.position = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(x, 0f, 0f) + Vector3.up * 2.4f;
                tusk.transform.rotation = Quaternion.Euler(0f, yaw, i == 0 ? 24f : -24f);
                tusk.transform.localScale = new Vector3(0.07f, 0.3f, 0.07f);
                tusk.GetComponent<Renderer>().material = bone;
                Destroy(tusk.GetComponent<Collider>());
            }
            Prop(parent, PrimitiveType.Cube, "Totem Hide", at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.16f, 0f, 0f) + Vector3.up * 1.5f,
                new Vector3(0.5f, 0.9f, 0.05f),
                SurfaceAssets.MaterialFor(SurfaceAssets.Kind.Fur, new Color(0.8f, 0.62f, 0.5f), new Vector2(1.4f, 1.4f)), yaw);
        }


        // ------------------------------------------------------------------ ui

        bool Project(Vector3 world, out Vector2 point)
        {
            point = Vector2.zero;
            if (cam == null) return false;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f) return false;
            point = new Vector2(screen.x / Screen.width * 1600f, (1f - screen.y / Screen.height) * 900f);
            return true;
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            if (game == null || exiting || Player == null || cam == null) return;

            ImGuiTheme.Begin();
            ImGuiTheme.Vignette(0.5f);

            if (damageFlash > 0.001f)
            {
                ImGuiTheme.Fill(new Rect(0f, 0f, 1600f, 900f), new Color(0.62f, 0.06f, 0.05f, damageFlash * 0.42f));
            }

            DrawFloatingTexts();
            DrawEnemyBars();
            DrawHud();
            DrawActionBar();
            DrawQuestPanel();
            DrawRegionBanner();
            DrawTargetBar();
            DrawToast();
            DrawPrompt();
            DrawHints();
            DrawWorldLabels();

            if (MapOpen) DrawMap();
            if (Paused) DrawPause();
            if (exiting)
            {
                ImGuiTheme.End();
                return;
            }
            if (Player != null && Player.Dead) DrawDeath();

            HandleWorldClick();

            ImGuiTheme.End();
        }

        void DrawFloatingTexts()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = floats.Count - 1; i >= 0; i--)
            {
                var text = floats[i];
                text.Age += dt;
                if (text.Age >= text.Life)
                {
                    floats.RemoveAt(i);
                    continue;
                }

                Vector3 world = text.Position + Vector3.up * (text.Age * 0.7f);
                if (!Project(world, out Vector2 point)) continue;

                float alpha = 1f - Mathf.Clamp01((text.Age - text.Life * 0.55f) / (text.Life * 0.45f));
                var color = text.Color;
                color.a = alpha;
                var rect = new Rect(point.x - 80f, point.y - 14f, 160f, 28f);
                ImGuiTheme.Text(rect, text.Text, text.Size, color, TextAnchor.MiddleCenter, true);
            }
        }

        void DrawEnemyBars()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.Dead || enemy.Friendly) continue;
                float distance = Vector3.Distance(Player.transform.position, enemy.transform.position);
                float limit = enemy.Boss ? 32f : 18f;
                if (distance > limit) continue;
                if (!enemy.Boss && distance > 11f && enemy.HpFraction >= 0.999f) continue;
                if (!Project(enemy.transform.position + Vector3.up * (enemy.Boss ? 2.9f : 2.3f), out Vector2 point)) continue;

                float width = enemy.Boss ? 130f : 92f;
                var back = new Rect(point.x - width * 0.5f, point.y - 6f, width, enemy.Boss ? 9f : 6f);
                ImGuiTheme.Fill(back, new Color(0f, 0f, 0f, 0.62f));
                ImGuiTheme.Fill(new Rect(back.x, back.y, back.width * enemy.HpFraction, back.height),
                    enemy.Boss ? new Color(0.78f, 0.26f, 0.24f) : new Color(0.72f, 0.32f, 0.28f));

                if (enemy.Boss || distance < 9f)
                {
                    ImGuiTheme.Text(new Rect(point.x - 110f, point.y - 24f, 220f, 16f), enemy.Display,
                        enemy.Boss ? 11 : 9, enemy.Boss ? ImGuiTheme.GoldBright : new Color(0.9f, 0.85f, 0.75f), TextAnchor.MiddleCenter, true);
                }
            }
        }

        void DrawHud()
        {
            var panel = new Rect(48f, 690f, 470f, 168f);
            ImGuiTheme.Panel(panel, new Color(0.024f, 0.036f, 0.028f, 0.9f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.4f), 1f, true);

            ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 12f, 280f, 28f), hero.Name.ToUpperInvariant(), 20, ImGuiTheme.GoldBright, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 24f, panel.y + 38f, 300f, 16f), "LEVEL " + Level + "  ·  " + Title.ToUpperInvariant(), 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.85f), 1.4f, TextAnchor.MiddleLeft, false);

            float hpFraction = Mathf.Clamp01(Player.Hp / MaxHp);
            ImGuiTheme.Bar(new Rect(panel.x + 22f, panel.y + 62f, 426f, 14f), hpFraction, HpColor, new Color(0f, 0f, 0f, 0.55f));
            ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 60f, 426f, 16f), Mathf.CeilToInt(Player.Hp) + " / " + Mathf.CeilToInt(MaxHp), 9,
                ImGuiTheme.Parchment, TextAnchor.MiddleRight, false);

            ImGuiTheme.Bar(new Rect(panel.x + 22f, panel.y + 84f, 426f, 9f), Mathf.Clamp01(Player.Stamina / 100f), StaminaColor, new Color(0f, 0f, 0f, 0.55f));
            ImGuiTheme.Bar(new Rect(panel.x + 22f, panel.y + 102f, 426f, 7f), Level >= 100 ? 1f : Mathf.Clamp01((float)Xp / Mathf.Max(1, XpNext)), XpColor, new Color(0f, 0f, 0f, 0.55f));

            string artStatus = Player.ArtCooldown <= 0.01f ? "READY" : Player.ArtCooldown.ToString("0.0") + "s";
            ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 120f, 300f, 20f), "Q  ·  " + hero.AbilityName.ToUpperInvariant(), 10, ImGuiTheme.Gold, TextAnchor.MiddleLeft, true);
            ImGuiTheme.Text(new Rect(panel.x + 300f, panel.y + 120f, 148f, 20f), artStatus, 10,
                Player.ArtCooldown <= 0.01f ? ImGuiTheme.Moss : ImGuiTheme.Muted, TextAnchor.MiddleRight, false);
            ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 140f, 426f, 18f), Level >= 100 ? "MAX LEVEL" : Xp + " / " + XpNext + " XP", 8,
                new Color(0.88f, 0.82f, 0.68f), TextAnchor.MiddleRight, false);
            if (Player.Shielded)
            {
                ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 140f, 300f, 18f), "BULWARK ACTIVE", 9, new Color(0.7f, 0.92f, 1f), TextAnchor.MiddleLeft, false);
            }
            else if (Player.Hasted)
            {
                ImGuiTheme.Text(new Rect(panel.x + 22f, panel.y + 140f, 300f, 18f), "SWIFT", 9, new Color(0.68f, 0.95f, 0.72f), TextAnchor.MiddleLeft, false);
            }
        }

        void DrawQuestPanel()
        {
            var panel = new Rect(1120f, 44f, 432f, 164f);
            ImGuiTheme.Panel(panel, new Color(0.024f, 0.036f, 0.028f, 0.9f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.38f));
            ImGuiTheme.SpacedText(new Rect(panel.x + 24f, panel.y + 14f, 300f, 16f), "QUEST", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.9f), 2.4f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(panel.x + 24f, panel.y + 34f, panel.width - 48f, 24f), questTitle, 17, ImGuiTheme.GoldBright, TextAnchor.MiddleLeft, true);
            ImGuiTheme.Label(new Rect(panel.x + 24f, panel.y + 62f, panel.width - 48f, 40f), questObjective, ImGuiTheme.WrapStyle(13), ImGuiTheme.Parchment, TextAnchor.UpperLeft, false);

            if (stage == Stage.Ascend)
            {
                ImGuiTheme.Bar(new Rect(panel.x + 24f, panel.y + 108f, panel.width - 48f, 10f), Mathf.Clamp01((float)Level / 20f), ImGuiTheme.Gold, new Color(0f, 0f, 0f, 0.5f));
                ImGuiTheme.Text(new Rect(panel.x + 24f, panel.y + 122f, panel.width - 48f, 18f), "LEVEL " + Level + " / 20", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.8f), TextAnchor.MiddleRight, false);
            }
            else if (stage != Stage.Complete)
            {
                ImGuiTheme.Bar(new Rect(panel.x + 24f, panel.y + 108f, panel.width - 48f, 10f), questNeed <= 0 ? 0f : Mathf.Clamp01((float)questHave / questNeed), ImGuiTheme.Gold, new Color(0f, 0f, 0f, 0.5f));
                ImGuiTheme.Text(new Rect(panel.x + 24f, panel.y + 122f, panel.width - 48f, 18f), questHave + " / " + questNeed, 9, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.8f), TextAnchor.MiddleRight, false);
            }
            else
            {
                ImGuiTheme.Text(new Rect(panel.x + 24f, panel.y + 108f, panel.width - 48f, 20f), "ENDGAME — TITLES TO LEVEL 100", 9, ImGuiTheme.Muted, TextAnchor.MiddleLeft, false);
            }

            ImGuiTheme.Text(new Rect(panel.x + 24f, panel.y + 140f, panel.width - 48f, 18f), "NEXT TITLE  ·  " + NextTitle(), 8,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), TextAnchor.MiddleLeft, false);
        }

        string NextTitle()
        {
            for (int i = 0; i < TitleLevels.Length; i++)
            {
                if (TitleLevels[i] > Level) return TitleNames[i] + "  (Lv " + TitleLevels[i] + ")";
            }
            return "Undying Supreme Lord";
        }

        void DrawRegionBanner()
        {
            if (regionTimer <= 0f || string.IsNullOrEmpty(region)) return;
            float alpha = Mathf.Clamp01(regionTimer / 0.6f) * Mathf.Clamp01((3.2f - regionTimer) / 0.5f + 1f);
            alpha = Mathf.Clamp01(alpha);
            var color = new Color(0.96f, 0.85f, 0.62f, alpha);
            ImGuiTheme.SpacedText(new Rect(500f, 44f, 600f, 28f), region, 16, color, 4f, TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(700f, 900f, 76f, new Color(0.85f, 0.68f, 0.38f, alpha * 0.35f));
        }

        void DrawTargetBar()
        {
            if (target == null || target.Dead || targetTimer <= 0f) return;
            var panel = new Rect(600f, 96f, 400f, 50f);
            ImGuiTheme.Panel(panel, new Color(0.03f, 0.02f, 0.02f, 0.78f), ImGuiTheme.WithAlpha(ImGuiTheme.Blood, 0.5f));
            ImGuiTheme.Text(new Rect(panel.x + 14f, panel.y + 6f, panel.width - 28f, 20f), target.Display, 13, new Color(1f, 0.82f, 0.72f), TextAnchor.MiddleLeft, true);
            ImGuiTheme.Bar(new Rect(panel.x + 14f, panel.y + 30f, panel.width - 28f, 8f), target.HpFraction,
                new Color(0.78f, 0.26f, 0.24f), new Color(0f, 0f, 0f, 0.6f));
        }

        void DrawToast()
        {
            if (toastTimer <= 0f || string.IsNullOrEmpty(toast)) return;
            float alpha = Mathf.Clamp01(toastTimer / 0.5f) * Mathf.Clamp01((5f - toastTimer) / 0.4f + 0.4f);
            alpha = Mathf.Clamp01(alpha);
            var color = new Color(0.95f, 0.88f, 0.7f, alpha);
            var rect = new Rect(560f, 742f, 480f, 40f);
            ImGuiTheme.Panel(rect, new Color(0.05f, 0.06f, 0.05f, 0.86f * alpha), new Color(0.82f, 0.65f, 0.35f, 0.45f * alpha));
            ImGuiTheme.Label(new Rect(rect.x + 16f, rect.y, rect.width - 32f, rect.height), toast, ImGuiTheme.WrapStyle(12), color, TextAnchor.MiddleCenter, false);
        }

        string PromptText()
        {
            if (InDungeon)
            {
                if (Vector3.Distance(Player.transform.position, DungeonExit) < 3.6f) return "Leave the Sunken Crypt";
                return null;
            }
            if (Vector3.Distance(Player.transform.position, ElderPosition) < 3.4f) return "Speak with Elder Maren";
            if (Vector3.Distance(Player.transform.position, CryptGate) < 4.4f) return "Enter the Sunken Crypt";
            return null;
        }

        void DrawPrompt()
        {
            string prompt = PromptText();
            if (string.IsNullOrEmpty(prompt) || Paused || MapOpen || Player.Dead) return;
            var panel = new Rect(590f, 806f, 420f, 44f);
            bool hover = panel.Contains(Event.current.mousePosition);
            ImGuiTheme.Panel(panel, new Color(0.03f, 0.05f, 0.038f, 0.9f),
                ImGuiTheme.WithAlpha(ImGuiTheme.Gold, hover ? 0.8f : 0.45f));
            ImGuiTheme.Fill(new Rect(panel.x + 14f, panel.y + 13f, 18f, 18f), ImGuiTheme.WithAlpha(hover ? ImGuiTheme.GoldBright : ImGuiTheme.Gold, 0.9f));
            ImGuiTheme.Text(new Rect(panel.x + 14f, panel.y + 12f, 18f, 18f), "E", 11, ImGuiTheme.Ink, TextAnchor.MiddleCenter, false);
            ImGuiTheme.Text(new Rect(panel.x + 42f, panel.y, panel.width - 52f, panel.height), prompt + "   ·   click", 13,
                hover ? ImGuiTheme.GoldBright : ImGuiTheme.Parchment);
            if (ImGuiTheme.Clicked(panel)) Interact(Player);
        }

        void DrawActionBar()
        {
            if (Paused || MapOpen || Player == null || Player.Dead) return;
            if (ImGuiTheme.Button(new Rect(48f, 634f, 148f, 46f), "STRIKE", true, true, 12)) Player.CommandLightAttack();
            if (ImGuiTheme.Button(new Rect(204f, 634f, 148f, 46f), "HEAVY", false, true, 12)) Player.CommandHeavyAttack();
            if (ImGuiTheme.Button(new Rect(360f, 634f, 148f, 46f), "ART", false, true, 12)) Player.CommandArt();
        }

        void HandleWorldClick()
        {
            if (Paused || MapOpen || Player == null || Player.Dead) return;
            Event current = Event.current;
            if (current == null || current.type != EventType.MouseUp || current.button != 0) return;

            Vector2 mouse = current.mousePosition;
            if (mouse.y < 40f || mouse.y > 620f) return;
            if (new Rect(1120f, 44f, 432f, 164f).Contains(mouse)) return;
            Player.CommandLightAttack();
        }

        void DrawHints()
        {
            ImGuiTheme.SpacedText(new Rect(848f, 858f, 704f, 18f),
                "WASD MOVE   SHIFT ROLL   RMB LOOK   WHEEL ZOOM   SPACE / CLICK STRIKE   F HEAVY   Q ART   E INTERACT   M MAP",
                8, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.72f), 1.1f, TextAnchor.MiddleRight, false);
        }

        void DrawWorldLabels()
        {
            if (elder != null)
            {
                float distance = Vector3.Distance(Player.transform.position, elder.position);
                if (distance < 26f && Project(elder.position + Vector3.up * 2.5f, out Vector2 point))
                {
                    float alpha = Mathf.Clamp01(1f - (distance - 16f) / 10f);
                    ImGuiTheme.Text(new Rect(point.x - 90f, point.y - 12f, 180f, 20f), "ELDER MAREN", 10,
                        new Color(0.96f, 0.85f, 0.55f, alpha), TextAnchor.MiddleCenter, true);
                    if (stage != Stage.Complete)
                    {
                        ImGuiTheme.Text(new Rect(point.x - 8f, point.y - 34f, 16f, 18f), "!", 13,
                            new Color(1f, 0.88f, 0.5f, alpha), TextAnchor.MiddleCenter, true);
                    }
                }
            }

            if (!InDungeon && Project(CryptGate + Vector3.up * 6.4f, out Vector2 gatePoint))
            {
                float distance = Vector3.Distance(Player.transform.position, CryptGate);
                if (distance < 55f)
                {
                    float alpha = Mathf.Clamp01(1f - (distance - 35f) / 20f);
                    ImGuiTheme.Text(new Rect(gatePoint.x - 120f, gatePoint.y - 10f, 240f, 20f), "THE SUNKEN CRYPT", 10,
                        new Color(0.92f, 0.82f, 0.6f, alpha), TextAnchor.MiddleCenter, true);
                }
            }
        }

        void DrawMap()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, 1600f, 900f), new Color(0f, 0f, 0f, 0.62f));
            var panel = new Rect(230f, 62f, 1140f, 776f);
            ImGuiTheme.Panel(panel, new Color(0.04f, 0.045f, 0.038f, 0.97f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.5f), 1f, true);

            ImGuiTheme.SpacedText(new Rect(panel.x + 36f, panel.y + 20f, 600f, 26f), "ZULVYRE — WORLD MAP", 17, ImGuiTheme.GoldBright, 3.4f, TextAnchor.MiddleLeft);
            ImGuiTheme.Text(new Rect(panel.x + 36f, panel.y + 50f, 600f, 18f), "The Greywood and its three powers", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.85f), TextAnchor.MiddleLeft, false);
            ImGuiTheme.SpacedText(new Rect(panel.xMax - 340f, panel.y + 26f, 300f, 20f), "M  CLOSE", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f), 2f, TextAnchor.MiddleRight, false);

            var map = new Rect(panel.x + 36f, panel.y + 92f, 780f, 610f);
            ImGuiTheme.Fill(map, new Color(0.10f, 0.12f, 0.095f, 1f));
            ImGuiTheme.Panel(map, new Color(0f, 0f, 0f, 0f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.25f));

            Vector2 MapPoint(Vector3 world)
            {
                float x = Mathf.InverseLerp(-125f, 125f, world.x);
                float y = Mathf.InverseLerp(-132f, 100f, world.z);
                return new Vector2(map.x + x * map.width, map.y + (1f - y) * map.height);
            }

            var alderwick = MapPoint(AlderwickCenter);
            var durnmar = MapPoint(DurnmarCenter);
            var nocturne = MapPoint(NocturneCenter);
            var gate = MapPoint(CryptGate);
            var crossroads = MapPoint(new Vector3(0f, 0f, -24f));

            ImGuiTheme.Fill(new Rect(map.x + map.width * 0.12f, map.y + map.height * 0.30f, map.width * 0.76f, map.height * 0.54f), new Color(0.14f, 0.20f, 0.13f, 0.55f));
            ImGuiTheme.Text(new Rect(map.x + map.width * 0.30f, map.y + map.height * 0.44f, 300f, 24f), "THE GREYWOOD", 14,
                new Color(0.62f, 0.72f, 0.55f, 0.55f), TextAnchor.MiddleLeft, true);

            var road = new Color(0.78f, 0.66f, 0.42f, 0.75f);
            ImGuiTheme.Line(alderwick, crossroads, road, 2f);
            ImGuiTheme.Line(crossroads, durnmar, road, 2f);
            ImGuiTheme.Line(crossroads, nocturne, road, 2f);
            ImGuiTheme.Line(crossroads, gate, road, 2f);

            Landmark(alderwick, "ALDERWICK", "Human village", ImGuiTheme.GoldBright);
            Landmark(durnmar, "DURNMAR", "Orc village", new Color(1f, 0.6f, 0.42f));
            Landmark(nocturne, "NOCTURNE", "Vampire village", new Color(0.85f, 0.6f, 1f));
            Landmark(gate, "SUNKEN CRYPT", "Dungeon", new Color(0.78f, 0.82f, 0.9f));

            Vector2 playerPoint = MapPoint(Player.transform.position);
            ImGuiTheme.Fill(new Rect(playerPoint.x - 5f, playerPoint.y - 5f, 10f, 10f), ImGuiTheme.GoldBright);
            Vector3 facing = Player.transform.forward;
            ImGuiTheme.Line(playerPoint, playerPoint + new Vector2(facing.x, -facing.z) * 18f, ImGuiTheme.GoldBright, 2f);

            var side = new Rect(map.xMax + 30f, map.y, 254f, map.height);
            ImGuiTheme.SpacedText(new Rect(side.x, side.y, side.width, 18f), "TITLE LADDER", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.9f), 2f, TextAnchor.MiddleLeft, false);
            float lineY = side.y + 30f;
            for (int i = 0; i < TitleLevels.Length; i++)
            {
                bool reached = Level >= TitleLevels[i];
                ImGuiTheme.Text(new Rect(side.x, lineY, 54f, 20f), "LV " + TitleLevels[i], 9,
                    reached ? ImGuiTheme.GoldBright : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.75f), TextAnchor.MiddleLeft, false);
                ImGuiTheme.Text(new Rect(side.x + 58f, lineY, side.width - 58f, 20f), TitleNames[i], 9,
                    reached ? ImGuiTheme.Parchment : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.7f), TextAnchor.MiddleLeft, false);
                lineY += 22f;
            }

            ImGuiTheme.Text(new Rect(side.x, side.yMax - 60f, side.width, 18f), "LEVEL " + Level + "  ·  " + Title, 9,
                ImGuiTheme.Gold, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(side.x, side.yMax - 42f, side.width, 18f), "WASD move   M or click outside to close", 8, ImGuiTheme.Muted, TextAnchor.MiddleLeft, false);

            if (!panel.Contains(Event.current.mousePosition) && ImGuiTheme.Clicked(new Rect(0f, 0f, 1600f, 900f)))
            {
                MapOpen = false;
            }
        }

        void Landmark(Vector2 point, string name, string kind, Color color)
        {
            ImGuiTheme.Fill(new Rect(point.x - 5f, point.y - 5f, 10f, 10f), color);
            ImGuiTheme.Text(new Rect(point.x + 12f, point.y - 22f, 220f, 18f), name, 10, color, TextAnchor.MiddleLeft, true);
            ImGuiTheme.Text(new Rect(point.x + 12f, point.y - 6f, 220f, 16f), kind, 8, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.85f), TextAnchor.MiddleLeft, false);
        }

        void DrawPause()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, 1600f, 900f), new Color(0f, 0f, 0f, 0.66f));
            var panel = new Rect(540f, 252f, 520f, 396f);
            ImGuiTheme.Panel(panel, new Color(0.025f, 0.035f, 0.03f, 0.98f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.6f), 1f, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 34f, panel.width - 60f, 30f), "THE ROAD WAITS", 18, ImGuiTheme.GoldBright, 4f, TextAnchor.MiddleCenter);
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 78f, panel.width - 60f, 22f), "Resume when you are ready.", 12, ImGuiTheme.Parchment, TextAnchor.MiddleCenter);

            if (ImGuiTheme.Button(new Rect(panel.x + 120f, panel.y + 150f, 280f, 54f), "RESUME", true))
            {
                Paused = false;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 120f, panel.y + 222f, 280f, 48f), "RETURN TO CAMP"))
            {
                exiting = true;
                game.EnterLanding();
                return;
            }
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 300f, panel.width - 60f, 18f), "ESC  RESUME", 8, ImGuiTheme.Muted, 2f, TextAnchor.MiddleCenter, false);
        }

        void DrawDeath()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, 1600f, 900f), new Color(0.12f, 0f, 0f, 0.55f));
            var panel = new Rect(540f, 272f, 520f, 356f);
            ImGuiTheme.Panel(panel, new Color(0.05f, 0.02f, 0.02f, 0.97f), ImGuiTheme.WithAlpha(ImGuiTheme.Blood, 0.7f), 1f, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 40f, panel.width - 60f, 34f), "YOU HAVE FALLEN", 22, new Color(1f, 0.72f, 0.68f), 4f, TextAnchor.MiddleCenter);
            ImGuiTheme.Text(new Rect(panel.x + 40f, panel.y + 92f, panel.width - 80f, 44f), "The lanterns of Alderwick will take you back.", 12, ImGuiTheme.Parchment, TextAnchor.MiddleCenter);
            if (ImGuiTheme.Button(new Rect(panel.x + 120f, panel.y + 170f, 280f, 56f), AllowRespawn ? "RISE AGAIN  ·  R" : "RISE AGAIN", true, AllowRespawn))
            {
                RespawnPlayer();
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 120f, panel.y + 244f, 280f, 44f), "RETURN TO CAMP"))
            {
                exiting = true;
                game.EnterLanding();
                return;
            }
        }

        // ------------------------------------------------------------------ tour

        public void DebugTeleport(Vector3 position)
        {
            if (Player != null) Player.Teleport(position);
        }

        public void DebugFullHeal()
        {
            if (Player != null)
            {
                Player.ResetVitals();
                Player.FullHeal();
            }
        }

        public void DebugSetCamera(float yaw, float pitch = 15f, float distance = 6.6f)
        {
            CameraYaw = yaw;
            CameraPitch = pitch;
            CameraDistance = distance;
        }

        public void DebugToggleMap(bool open)
        {
            MapOpen = open;
        }

        public void DebugEnterDungeon()
        {
            if (Player != null) EnterDungeon();
        }

        public void DebugSetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, 100);
            Xp = 0;
            XpNext = 55 + Level * 45;
            Title = TitleFor(Level);
            if (Player != null) Player.FullHeal();
        }

        public void DebugStrike(bool heavy)
        {
            if (Player != null) PlayerAttack(Player, heavy);
        }

        public void DebugCompleteQuest()
        {
            BeginStage(Stage.Complete, false);
        }

        public void DebugFaceNearest()
        {
            if (Player == null) return;
            WorldEnemy best = NearestHostile(40f);
            if (best == null) return;
            Vector3 direction = best.transform.position - Player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) Player.transform.rotation = Quaternion.LookRotation(direction);
        }

        public bool DebugKillNearest(float maxDistance = 45f)
        {
            WorldEnemy best = NearestHostile(maxDistance);
            if (best == null) return false;
            best.ForceKill();
            return true;
        }

        WorldEnemy NearestHostile(float maxDistance)
        {
            if (Player == null) return null;
            WorldEnemy best = null;
            float bestDistance = maxDistance;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.Dead || enemy.Friendly) continue;
                float distance = Vector3.Distance(Player.transform.position, enemy.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = enemy;
            }
            return best;
        }

        public Vector3 DebugDungeonEntry => DungeonEntry;
    }
}
