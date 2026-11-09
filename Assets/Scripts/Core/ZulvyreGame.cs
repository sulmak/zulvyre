using UnityEngine;

namespace Zulvyre
{
    public enum GameState
    {
        Login,
        Landing,
        Menu,
        Village,
        Duel,
        World
    }

    public class ZulvyreGame : MonoBehaviour
    {
        public static ZulvyreGame Instance { get; private set; }

        public Camera GameCamera { get; private set; }
        public AudioBank Audio { get; private set; }
        public FxSystem Fx { get; private set; }
        public GameState State { get; private set; }
        public HeroDef SelectedHero { get; private set; }
        public int SelectedIndex { get; private set; }
        public bool DuelWon { get; set; }

        public WorldScene World { get; private set; }

        public string PlayerName { get; private set; } = "Wayfarer";
        public bool IsGuest { get; private set; } = true;

        MenuScene menu;
        VillageScene village;
        DuelScene duel;
        LoginScene login;
        LandingScene landing;
        WorldScene world;
        GameObject menuRoot;
        GameObject villageRoot;
        GameObject duelRoot;
        GameObject loginRoot;
        GameObject landingRoot;
        GameObject worldRoot;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // A 900px client plus its title bar does not fit a 900px desktop, which
            // buries the bottom UI row behind the taskbar.
            int fitHeight = Mathf.Min(900, Display.main.systemHeight - 80);
            if (!Screen.fullScreen && Screen.height > fitHeight)
            {
                Screen.SetResolution(1600, fitHeight, FullScreenMode.Windowed);
            }

            CreateCamera();

            Audio = gameObject.AddComponent<AudioBank>();
            Audio.Initialize();

            Fx = gameObject.AddComponent<FxSystem>();
            Fx.Initialize(GameCamera);

            ImGuiTheme.Audio = Audio;

            SelectedIndex = Mathf.Clamp(PlayerPrefs.GetInt("zulvyre.hero", 0), 0, HeroRoster.Heroes.Length - 1);
            SelectedHero = HeroRoster.ByIndex(SelectedIndex);

            menuRoot = new GameObject("MenuScene");
            menuRoot.transform.SetParent(transform, false);
            menu = menuRoot.AddComponent<MenuScene>();

            loginRoot = new GameObject("LoginScene");
            loginRoot.transform.SetParent(transform, false);
            login = loginRoot.AddComponent<LoginScene>();

            landingRoot = new GameObject("LandingScene");
            landingRoot.transform.SetParent(transform, false);
            landing = landingRoot.AddComponent<LandingScene>();

            villageRoot = new GameObject("VillageScene");
            villageRoot.transform.SetParent(transform, false);
            village = villageRoot.AddComponent<VillageScene>();

            duelRoot = new GameObject("DuelScene");
            duelRoot.transform.SetParent(transform, false);
            duel = duelRoot.AddComponent<DuelScene>();

            worldRoot = new GameObject("WorldScene");
            worldRoot.transform.SetParent(transform, false);
            world = worldRoot.AddComponent<WorldScene>();
            World = world;

            menuRoot.SetActive(false);
            villageRoot.SetActive(false);
            duelRoot.SetActive(false);
            loginRoot.SetActive(false);
            landingRoot.SetActive(false);
            worldRoot.SetActive(false);
        }

        void Start()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            bool quickDuel = false;
            bool villageDemo = false;
            bool worldDemo = false;
            bool landingDemo = false;
            bool menuDemo = false;
            bool devEntry = false;
            foreach (string arg in args)
            {
                if (arg == "-quickduel") quickDuel = true;
                if (arg == "-village") villageDemo = true;
                if (arg == "-world") worldDemo = true;
                if (arg == "-landing") landingDemo = true;
                if (arg == "-menu") menuDemo = true;
                if (arg == "-tour" || arg == "-clickcheck" || arg == "-rotcheck") devEntry = true;
            }

            if (quickDuel) EnterDuel(SelectedHero, false);
            else if (villageDemo) EnterVillage(SelectedHero);
            else if (worldDemo) EnterWorld(SelectedHero);
            else if (menuDemo) EnterMenu();
            else if (landingDemo)
            {
                string last = AccountStore.LastAccount;
                SignIn(string.IsNullOrEmpty(last) ? "Wayfarer" : last, string.IsNullOrEmpty(last));
                EnterLanding();
            }
            else if (devEntry) EnterMenu();
            else EnterLogin();

            CaptureTool.MaybeStart(args);
        }

        void Update()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (Input.GetKeyDown(KeyCode.M) && (State != GameState.World || ctrl)) Audio.ToggleMute();

            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (alt && Input.GetKeyDown(KeyCode.Return))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
        }

        void CreateCamera()
        {
            var cameraObject = new GameObject("Zulvyre Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            GameCamera = cameraObject.AddComponent<Camera>();
            GameCamera.orthographic = true;
            GameCamera.orthographicSize = 5.12f;
            GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.backgroundColor = ImGuiTheme.Ink;
            GameCamera.transform.position = new Vector3(0f, 0f, -10f);
            GameCamera.nearClipPlane = 0.1f;
            GameCamera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
        }

        public void SetHero(int index)
        {
            SelectedIndex = Mathf.Clamp(index, 0, HeroRoster.Heroes.Length - 1);
            SelectedHero = HeroRoster.ByIndex(SelectedIndex);
            PlayerPrefs.SetInt("zulvyre.hero", SelectedIndex);
            if (!IsGuest && !string.IsNullOrEmpty(PlayerName)) AccountStore.SetHero(PlayerName, SelectedIndex);
        }

        public void ConfigureCamera(float size, Vector3 position)
        {
            GameCamera.orthographic = true;
            GameCamera.orthographicSize = size;
            GameCamera.transform.position = new Vector3(position.x, position.y, -10f);
            // The world scene leaves the camera pitched/yawed; without this reset the
            // 2D scenes render from a camera facing away from the sprite plane (black).
            GameCamera.transform.rotation = Quaternion.identity;
        }

        public void ConfigureCameraPerspective(float fieldOfView, Vector3 position, Vector3 lookAt)
        {
            GameCamera.orthographic = false;
            GameCamera.fieldOfView = fieldOfView;
            GameCamera.nearClipPlane = 0.2f;
            GameCamera.farClipPlane = WorldStage.SkyRadius + 80f;
            GameCamera.transform.position = position;
            GameCamera.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
        }

        void DeactivateAll()
        {
            menuRoot.SetActive(false);
            villageRoot.SetActive(false);
            duelRoot.SetActive(false);
            loginRoot.SetActive(false);
            landingRoot.SetActive(false);
            worldRoot.SetActive(false);
            Fx.Clear();
            Time.timeScale = 1f;
        }

        public void SignIn(string name, bool guest)
        {
            PlayerName = string.IsNullOrWhiteSpace(name) ? "Wayfarer" : name.Trim();
            IsGuest = guest;
        }

        public void SignOut()
        {
            PlayerName = "Wayfarer";
            IsGuest = true;
        }

        public void EnterLogin()
        {
            DeactivateAll();
            State = GameState.Login;
            loginRoot.SetActive(true);
            ConfigureCamera(5.12f, Vector3.zero);
            Audio.PlayMenuMusic();
            login.Enter();
        }

        public void EnterLanding()
        {
            DeactivateAll();
            State = GameState.Landing;
            landingRoot.SetActive(true);
            ConfigureCamera(5.12f, Vector3.zero);
            Audio.PlayMenuMusic();
            landing.Enter();
        }

        public void EnterMenu()
        {
            DeactivateAll();
            State = GameState.Menu;
            menuRoot.SetActive(true);
            ConfigureCamera(5.12f, Vector3.zero);
            Audio.PlayMenuMusic();
            menu.Enter();
        }

        public void EnterVillage(HeroDef hero)
        {
            SelectedHero = hero;
            SelectedIndex = HeroRoster.IndexOf(hero);
            DeactivateAll();
            State = GameState.Village;
            villageRoot.SetActive(true);
            Audio.PlayMenuMusic();
            Audio.PlayAmbientVillage();
            village.Enter(hero);
        }

        public void EnterDuel(HeroDef hero, bool fromVillage)
        {
            SelectedHero = hero;
            SelectedIndex = HeroRoster.IndexOf(hero);
            DeactivateAll();
            State = GameState.Duel;
            duelRoot.SetActive(true);
            Audio.PlayDuelMusic();
            duel.Enter(hero, fromVillage);
        }

        public void EnterWorld(HeroDef hero)
        {
            SelectedHero = hero;
            SelectedIndex = HeroRoster.IndexOf(hero);
            DeactivateAll();
            State = GameState.World;
            worldRoot.SetActive(true);
            Audio.PlayMenuMusic();
            world.Enter(hero);
        }

        public void QuitToDesktop()
        {
            PlayerPrefs.Save();
            Debug.Log("[quit] Application.Quit requested");
            Application.Quit();
            StartCoroutine(ForceQuitFallback());
        }

        System.Collections.IEnumerator ForceQuitFallback()
        {
            // Application.Quit is deferred and has been observed to be swallowed on
            // some Windows players; if the process is still here, end it directly.
            yield return new WaitForSecondsRealtime(1.5f);
            Debug.Log("[quit] Application.Quit did not exit; killing process");
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }
    }

    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Launch()
        {
            if (Object.FindAnyObjectByType<ZulvyreGame>() != null) return;
            var gameObject = new GameObject("Zulvyre");
            gameObject.AddComponent<ZulvyreGame>();
        }
    }
}
