using System.Collections;
using System.IO;
using UnityEngine;

namespace Zulvyre
{
    public class CaptureTool : MonoBehaviour
    {
        string directory;
        bool finishDemo;
        bool rotationCheck;
        bool clickCheck;
        ZulvyreGame game;

        public static void MaybeStart(string[] args)
        {
            string directory = null;
            bool tour = false;
            bool finish = false;
            bool rotationCheck = false;
            bool clickCheck = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-tour")
                {
                    tour = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) directory = args[i + 1];
                }
                if (args[i] == "-finish") finish = true;
                if (args[i] == "-rotcheck")
                {
                    tour = true;
                    rotationCheck = true;
                }
                if (args[i] == "-clickcheck")
                {
                    tour = true;
                    clickCheck = true;
                }
            }

            if (!tour) return;

            var go = new GameObject("CaptureTool");
            DontDestroyOnLoad(go);
            var tool = go.AddComponent<CaptureTool>();
            tool.game = ZulvyreGame.Instance;
            tool.directory = Path.GetFullPath(string.IsNullOrEmpty(directory)
                ? Path.Combine(Application.persistentDataPath, "Tour")
                : directory);
            Directory.CreateDirectory(tool.directory);
            tool.finishDemo = finish;
            tool.rotationCheck = rotationCheck;
            tool.clickCheck = clickCheck;
            tool.StartCoroutine(tool.RunTour());
        }

        IEnumerator RunTour()
        {
            yield return new WaitForSecondsRealtime(0.9f);

            if (rotationCheck)
            {
                yield return RotationCheck();
                yield break;
            }

            if (clickCheck)
            {
                yield return ClickCheck();
                yield break;
            }

            if (game != null && game.State == GameState.World)
            {
                yield return WorldTour();
                yield break;
            }

            if (game != null)
            {
                game.SignOut();
                game.EnterLogin();
                yield return new WaitForSecondsRealtime(1.1f);
                yield return Shot("00-login");

                game.SignIn("Wayfarer", true);
                game.EnterLanding();
                yield return new WaitForSecondsRealtime(1.1f);
                yield return Shot("00b-landing");

                game.EnterMenu();
                yield return new WaitForSecondsRealtime(0.8f);
            }

            yield return Shot("01-menu");

            if (game == null) yield break;

            game.EnterVillage(game.SelectedHero);
            yield return new WaitForSecondsRealtime(1.4f);
            yield return Shot("02-village");

            game.EnterDuel(game.SelectedHero, false);
            yield return new WaitForSecondsRealtime(1.1f);
            yield return Shot("03-duel-intro");

            EnablePilot();
            yield return new WaitForSecondsRealtime(2.4f);
            yield return Shot("04-duel-fight");

            yield return new WaitForSecondsRealtime(2.8f);
            yield return Shot("05-duel-clash");

            if (finishDemo)
            {
                yield return new WaitForSecondsRealtime(1.4f);
                ForceFinish();
                yield return new WaitForSecondsRealtime(2.2f);
                yield return Shot("06-duel-result");
            }

            yield return new WaitForSecondsRealtime(0.5f);
            Debug.Log("[Zulvyre] Tour complete: " + directory);
            Application.Quit();
        }

        IEnumerator RotationCheck()
        {
            // Reproduces the world -> menu -> duel path that used to leave the 2D camera
            // pitched/yawed and render the arena black.
            game.EnterWorld(game.SelectedHero);
            yield return new WaitForSecondsRealtime(2.4f);
            yield return Shot("rot-01-world");

            game.EnterMenu();
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("rot-02-menu");

            game.EnterDuel(game.SelectedHero, false);
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("rot-03-duel-intro");

            yield return new WaitForSecondsRealtime(2.6f);
            yield return Shot("rot-04-duel-fight");

            Debug.Log("[Zulvyre] Rotation check complete: " + directory);
            Application.Quit();
        }

        IEnumerator ClickCheck()
        {
            game.EnterVillage(game.SelectedHero);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("click-01-village");

            var villages = Object.FindObjectsByType<VillageScene>(FindObjectsSortMode.None);
            if (villages.Length == 0)
            {
                Debug.LogWarning("[Zulvyre] Click check could not find the village.");
                Application.Quit();
                yield break;
            }

            Vector2 before = villages[0].DebugPlayerPosition;
            villages[0].DebugWalkTo(new Vector2(0.3f, 1.0f));
            yield return new WaitForSecondsRealtime(1.6f);
            Vector2 after = villages[0].DebugPlayerPosition;
            Debug.Log("[Zulvyre] Click move " + before + " -> " + after + " distance=" + Vector2.Distance(before, after).ToString("0.00"));
            yield return Shot("click-02-village-moved");

            villages[0].DebugTalkTo("Vesper");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("click-03-dialogue");

            Application.Quit();
        }

        IEnumerator WorldTour()
        {
            var world = game.World;
            if (world == null)
            {
                yield break;
            }

            world.TourMode = true;
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("world-01-alderwick");

            world.DebugTeleport(new Vector3(0f, 0f, 18f));
            world.DebugSetCamera(180f, 22f, 9f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("world-02-greywood");

            world.Player.God = true;
            world.DebugTeleport(new Vector3(13f, 0f, 27f));
            world.DebugSetCamera(180f, 20f, 8f);
            world.TourMode = false;
            yield return new WaitForSecondsRealtime(1.3f);
            world.DebugFaceNearest();
            world.DebugStrike(true);
            yield return new WaitForSecondsRealtime(0.25f);
            world.DebugStrike(false);
            yield return new WaitForSecondsRealtime(0.45f);
            yield return Shot("world-03-combat");

            world.TourMode = true;
            world.DebugFaceNearest();
            for (int i = 0; i < 3; i++)
            {
                world.DebugKillNearest();
                yield return new WaitForSecondsRealtime(0.3f);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("world-04-quest");

            world.DebugTeleport(new Vector3(-72f, 0f, -32f));
            world.DebugSetCamera(180f, 26f, 12f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("world-05-durnmar");

            world.DebugTeleport(new Vector3(72f, 0f, -34f));
            world.DebugSetCamera(180f, 26f, 12f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("world-06-nocturne");

            world.DebugTeleport(new Vector3(0f, 0f, -78f));
            world.DebugSetCamera(180f, 18f, 9f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("world-07-crypt");

            world.DebugSetLevel(100);
            world.DebugCompleteQuest();
            world.DebugToggleMap(true);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("world-08-map-undying");
            world.DebugToggleMap(false);

            world.DebugEnterDungeon();
            world.DebugSetCamera(180f, 20f, 6f);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("world-09-dungeon");

            Debug.Log("[Zulvyre] World tour complete: " + directory);
            Application.Quit();
        }

        void EnablePilot()
        {
            DuelFighter player = null;
            DuelFighter rival = null;
            var fighters = Object.FindObjectsByType<DuelFighter>(FindObjectsSortMode.None);
            for (int i = 0; i < fighters.Length; i++)
            {
                if (fighters[i].IsPlayer) player = fighters[i];
                else rival = fighters[i];
            }

            if (player == null || rival == null)
            {
                Debug.LogWarning("[Zulvyre] Tour could not find duelists.");
                return;
            }

            player.AutoPilot = true;
            var pilot = player.gameObject.AddComponent<RivalBrain>();
            pilot.Initialize(player, rival);
        }

        void ForceFinish()
        {
            DuelFighter player = null;
            DuelFighter rival = null;
            var fighters = Object.FindObjectsByType<DuelFighter>(FindObjectsSortMode.None);
            for (int i = 0; i < fighters.Length; i++)
            {
                if (fighters[i].IsPlayer) player = fighters[i];
                else rival = fighters[i];
            }

            if (player == null || rival == null || rival.Dead) return;
            rival.ForceDefeat();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(directory, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[Zulvyre] Captured " + path);
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }
}
