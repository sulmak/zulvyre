using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class LandingScene : MonoBehaviour
    {
        class Ember
        {
            public float X;
            public float Y;
            public float Speed;
            public float Size;
            public float Alpha;
            public float Phase;
        }

        readonly List<Ember> embers = new List<Ember>();
        ZulvyreGame game;
        float fade = 1f;
        float emberTime;

        public void Enter()
        {
            game = ZulvyreGame.Instance;
            fade = 1f;
            EnsureEmbers();
        }

        void EnsureEmbers()
        {
            if (embers.Count > 0) return;
            var rng = new System.Random(913);
            for (int i = 0; i < 34; i++)
            {
                embers.Add(new Ember
                {
                    X = (float)rng.NextDouble() * 1600f,
                    Y = (float)rng.NextDouble() * 900f,
                    Speed = 5f + (float)rng.NextDouble() * 20f,
                    Size = 2f + (float)rng.NextDouble() * 4f,
                    Alpha = 0.1f + (float)rng.NextDouble() * 0.34f,
                    Phase = (float)rng.NextDouble() * Mathf.PI * 2f
                });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fade = Mathf.MoveTowards(fade, 0f, dt * 1.7f);
            emberTime += dt;

            for (int i = 0; i < embers.Count; i++)
            {
                var ember = embers[i];
                ember.Y -= ember.Speed * dt;
                if (ember.Y < -20f)
                {
                    ember.Y = 920f;
                    ember.X = Random.Range(0f, 1600f);
                }
            }

            if (game == null) game = ZulvyreGame.Instance;
            if (game == null) return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                BeginAdventure();
            }
            if (Input.GetKeyDown(KeyCode.Q)) QuickDuel();
            if (Input.GetKeyDown(KeyCode.C)) Continue();
            if (Input.GetKeyDown(KeyCode.Escape)) LogOut();
        }

        void Continue()
        {
            if (game == null) return;
            game.EnterMenu();
        }

        void BeginAdventure()
        {
            if (game == null) return;
            game.EnterWorld(game.SelectedHero);
        }

        void QuickDuel()
        {
            if (game == null) return;
            game.EnterDuel(game.SelectedHero, false);
        }

        void LogOut()
        {
            if (game == null) return;
            game.SignOut();
            game.EnterLogin();
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            ImGuiTheme.Begin();

            ImGuiTheme.Backdrop(ImageAssets.BackgroundTexture("arena"), new Color(0.32f, 0.3f, 0.34f, 1f));
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0.012f, 0.02f, 0.024f, 0.72f));
            DrawEmbers();
            ImGuiTheme.Vignette(0.95f);

            DrawSidebar();
            DrawWelcome();

            if (fade > 0.001f)
            {
                ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, fade * 0.9f));
            }

            ImGuiTheme.End();
        }

        void DrawSidebar()
        {
            var rail = new Rect(42f, 30f, 424f, 838f);
            ImGuiTheme.Panel(rail, new Color(0.016f, 0.028f, 0.021f, 0.93f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.3f), 1f, true);

            ImGuiTheme.SpacedText(new Rect(70f, 52f, 340f, 44f), "ZULVYRE", 34, ImGuiTheme.GoldBright, 5f, TextAnchor.MiddleLeft);
            ImGuiTheme.SpacedText(new Rect(72f, 92f, 360f, 16f), "A VILLAGE OF EMBER AND OATH", 8, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.72f), 2.6f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.HLine(70f, 438f, 120f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.4f));

            DrawIdentityCard(new Rect(66f, 138f, 376f, 92f));

            ImGuiTheme.SpacedText(new Rect(70f, 258f, 360f, 16f), "THE ROAD AHEAD", 8, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.85f), 2.6f, TextAnchor.MiddleLeft, false);

            if (ImGuiTheme.Button(new Rect(66f, 284f, 376f, 52f), "CONTINUE ADVENTURE", true, true, 13)) BeginAdventure();
            if (ImGuiTheme.Button(new Rect(66f, 346f, 376f, 52f), "QUICK DUEL", false, true, 13)) QuickDuel();
            if (ImGuiTheme.Button(new Rect(66f, 408f, 376f, 52f), "CHANGE ADVENTURER", false, true, 13)) Continue();

            ImGuiTheme.SpacedText(new Rect(70f, 482f, 360f, 16f), "ACCOUNT", 8, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.85f), 2.6f, TextAnchor.MiddleLeft, false);
            if (ImGuiTheme.Button(new Rect(66f, 508f, 376f, 48f), game.IsGuest ? "LOG OUT OF GUEST" : "LOG OUT", false, true, 12)) LogOut();

            ImGuiTheme.HLine(70f, 438f, 600f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.16f));

            ImGuiTheme.Text(new Rect(70f, 620f, 360f, 20f), game.IsGuest
                    ? "Guest visit · the ledger keeps no record"
                    : "Sworn " + AccountStore.CreatedFor(game.PlayerName),
                10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f));

            ImGuiTheme.Text(new Rect(70f, 790f, 380f, 18f), "ENTER  adventure    ·    Q  duel    ·    C  adventurer", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.66f));
            ImGuiTheme.Text(new Rect(70f, 812f, 380f, 18f), "Powered by Md. Atikur Rahman Mridha", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.54f));
            ImGuiTheme.Text(new Rect(70f, 834f, 380f, 18f), "v1.4  ·  Unity 6", 9,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.5f));
        }

        void DrawIdentityCard(Rect rect)
        {
            ImGuiTheme.Panel(rect, new Color(0.028f, 0.046f, 0.035f, 0.92f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.26f));

            var avatar = new Rect(rect.x + 14f, rect.y + 14f, 64f, 64f);
            Color accent = game.SelectedHero != null ? game.SelectedHero.Accent : ImGuiTheme.Gold;
            ImGuiTheme.Panel(avatar, ImGuiTheme.WithAlpha(accent, 0.22f), ImGuiTheme.WithAlpha(accent, 0.7f));
            string initial = string.IsNullOrEmpty(game.PlayerName) ? "?" : game.PlayerName.Substring(0, 1).ToUpperInvariant();
            ImGuiTheme.Text(avatar, initial, 28, ImGuiTheme.GoldBright, TextAnchor.MiddleCenter, true);

            ImGuiTheme.Text(new Rect(rect.x + 92f, rect.y + 14f, rect.width - 108f, 26f), game.PlayerName, 19,
                ImGuiTheme.GoldBright, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(rect.x + 94f, rect.y + 42f, rect.width - 110f, 14f),
                game.IsGuest ? "GUEST VISIT" : "SIGNED IN", 8,
                ImGuiTheme.WithAlpha(game.IsGuest ? ImGuiTheme.Ember : ImGuiTheme.Moss, 0.95f), 2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(rect.x + 94f, rect.y + 60f, rect.width - 110f, 16f),
                game.SelectedHero != null ? game.SelectedHero.Name + " · " + game.SelectedHero.Title : "No hero chosen",
                10, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.74f));
        }

        void DrawWelcome()
        {
            var hero = game.SelectedHero;

            ImGuiTheme.SpacedText(new Rect(516f, 118f, 1000f, 20f), "WELCOME BACK", 12, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.95f), 6.4f, TextAnchor.MiddleLeft);
            ImGuiTheme.SpacedText(new Rect(516f, 144f, 1040f, 70f), game.PlayerName.ToUpperInvariant(), 50, ImGuiTheme.GoldBright, 5f, TextAnchor.MiddleLeft);
            ImGuiTheme.HLine(518f, 838f, 228f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.42f));

            string status = game.IsGuest
                ? "Playing as a guest. The ledger will not remember this visit."
                : "Sworn in " + AccountStore.CreatedFor(game.PlayerName) + ". The lanterns are lit and the road is open.";
            ImGuiTheme.Text(new Rect(518f, 242f, 980f, 24f), status, 12, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.75f));

            DrawHeroCard(new Rect(516f, 288f, 1040f, 356f), hero);

            DrawLedger(new Rect(516f, 664f, 1040f, 128f));
        }

        void DrawHeroCard(Rect rect, HeroDef hero)
        {
            if (hero == null) return;
            ImGuiTheme.Panel(rect, new Color(0.026f, 0.042f, 0.032f, 0.9f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.42f), 1f, true);

            var portrait = new Rect(rect.x + 26f, rect.y + 22f, 200f, 312f);
            var glowRect = new Rect(portrait.center.x - 120f, portrait.center.y - 100f, 240f, 240f);
            var previous = GUI.color;
            GUI.color = ImGuiTheme.WithAlpha(hero.Accent, 0.18f);
            GUI.DrawTexture(glowRect, ImageAssets.Dot, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            ImGuiTheme.TextureFit(portrait, ImageAssets.CharacterTexture(hero.SpriteName), Color.white, true);

            float x = rect.x + 260f;
            float width = rect.width - 296f;
            ImGuiTheme.SpacedText(new Rect(x, rect.y + 30f, width, 18f), "CHOSEN ADVENTURER", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.9f), 2.6f, TextAnchor.MiddleLeft);
            ImGuiTheme.Text(new Rect(x, rect.y + 52f, width, 44f), hero.Name, 34, ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(x, rect.y + 100f, width, 18f), hero.Title.ToUpperInvariant(), 10, ImGuiTheme.WithAlpha(hero.Accent, 0.95f), 2.2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Label(new Rect(x, rect.y + 128f, width, 58f), hero.Blurb, ImGuiTheme.WrapStyle(12), ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.72f), TextAnchor.UpperLeft, false);

            float statY = rect.y + 196f;
            DrawStatRow(new Rect(x, statY, width, 14f), "BLADE", hero.Blade, hero.Accent);
            DrawStatRow(new Rect(x, statY + 22f, width, 14f), "GUARD", hero.Guard, hero.Accent);
            DrawStatRow(new Rect(x, statY + 44f, width, 14f), "SPIRIT", hero.Spirit, hero.Accent);

            ImGuiTheme.Text(new Rect(x, rect.y + 272f, width, 20f), "ART  ·  " + hero.AbilityName, 12, ImGuiTheme.Gold, TextAnchor.MiddleLeft, true);
            ImGuiTheme.Label(new Rect(x, rect.y + 294f, width, 36f), hero.AbilityBlurb, ImGuiTheme.WrapStyle(10), ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.92f), TextAnchor.UpperLeft, false);
        }

        void DrawLedger(Rect rect)
        {
            ImGuiTheme.Panel(rect, new Color(0.02f, 0.034f, 0.026f, 0.8f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.2f));

            float third = rect.width / 3f;
            ImGuiTheme.SpacedText(new Rect(rect.x + 26f, rect.y + 18f, third - 40f, 14f), "LEDGER", 8,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(rect.x + 26f, rect.y + 36f, third - 40f, 26f), game.IsGuest ? "Visitor" : "Keeper", 16,
                ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);

            ImGuiTheme.SpacedText(new Rect(rect.x + third + 20f, rect.y + 18f, third - 40f, 14f), "CHOSEN HERO", 8,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(rect.x + third + 20f, rect.y + 36f, third - 40f, 26f),
                game.SelectedHero != null ? game.SelectedHero.Name : "—", 16, ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);

            ImGuiTheme.SpacedText(new Rect(rect.x + third * 2f + 20f, rect.y + 18f, third - 40f, 14f), "SWORN ON", 8,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.9f), 2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Text(new Rect(rect.x + third * 2f + 20f, rect.y + 36f, third - 40f, 26f),
                game.IsGuest ? "Not recorded" : AccountStore.CreatedFor(game.PlayerName), 16,
                ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);

            ImGuiTheme.Text(new Rect(rect.x + 26f, rect.y + 82f, rect.width - 52f, 24f),
                "Your name, your hero and your oath travel with this account on this machine.", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.72f));
        }

        void DrawStatRow(Rect rect, string label, int value, Color color)
        {
            ImGuiTheme.SpacedText(new Rect(rect.x, rect.y, 84f, rect.height), label, 9, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.85f), 1.4f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Pips(new Rect(rect.x + 84f, rect.y + 4f, 126f, 6f), value, 5, color);
        }

        void DrawEmbers()
        {
            for (int i = 0; i < embers.Count; i++)
            {
                var ember = embers[i];
                float sway = Mathf.Sin(emberTime * 0.5f + ember.Phase) * 12f;
                float flicker = 0.6f + 0.4f * Mathf.Sin(emberTime * 1.5f + ember.Phase * 2f);
                Color color = i % 3 == 0 ? ImGuiTheme.Ember : ImGuiTheme.Gold;
                color.a = ember.Alpha * flicker;
                var previous = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(new Rect(ember.X + sway, ember.Y, ember.Size, ember.Size), ImageAssets.Dot, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }
        }
    }
}
