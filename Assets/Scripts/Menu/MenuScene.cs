using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class MenuScene : MonoBehaviour
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
        int selected;
        int hovered = -1;
        float fade = 1f;
        bool confirmQuit;
        float emberTime;

        public void Enter()
        {
            game = ZulvyreGame.Instance;
            selected = game != null ? game.SelectedIndex : 0;
            hovered = -1;
            fade = 1f;
            confirmQuit = false;
            EnsureEmbers();
        }

        void EnsureEmbers()
        {
            if (embers.Count > 0) return;
            var rng = new System.Random(707);
            for (int i = 0; i < 46; i++)
            {
                embers.Add(new Ember
                {
                    X = (float)rng.NextDouble() * 1600f,
                    Y = (float)rng.NextDouble() * 900f,
                    Speed = 7f + (float)rng.NextDouble() * 26f,
                    Size = 2f + (float)rng.NextDouble() * 4.5f,
                    Alpha = 0.16f + (float)rng.NextDouble() * 0.45f,
                    Phase = (float)rng.NextDouble() * Mathf.PI * 2f
                });
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fade = Mathf.MoveTowards(fade, 0f, dt * 1.8f);
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

            if (confirmQuit)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) confirmQuit = false;
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) game.QuitToDesktop();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                confirmQuit = true;
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Select(selected - 1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Select(selected + 1);
            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) BeginAdventure();
            if (Input.GetKeyDown(KeyCode.Q)) BeginQuickDuel();
        }

        void Select(int index)
        {
            int count = HeroRoster.Heroes.Length;
            int next = ((index % count) + count) % count;
            if (next == selected) return;
            selected = next;
            if (game != null) game.SetHero(selected);
            if (ZulvyreGame.Instance != null && ZulvyreGame.Instance.Audio != null)
            {
                ZulvyreGame.Instance.Audio.PlayClick(0.35f);
            }
        }

        void BeginAdventure()
        {
            if (game == null) return;
            game.SetHero(selected);
            game.EnterWorld(game.SelectedHero);
        }

        void BeginQuickDuel()
        {
            if (game == null) return;
            game.SetHero(selected);
            game.EnterDuel(game.SelectedHero, false);
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            ImGuiTheme.Begin();

            ImGuiTheme.Backdrop(ImageAssets.BackgroundTexture("village"), new Color(0.36f, 0.38f, 0.34f, 1f));
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0.015f, 0.025f, 0.02f, 0.56f));
            DrawEmbers();
            ImGuiTheme.Vignette(0.92f);
            DrawFrame();

            ImGuiTheme.SpacedText(new Rect(84f, 40f, 760f, 84f), "ZULVYRE", 62, ImGuiTheme.GoldBright, 8f, TextAnchor.MiddleLeft);
            ImGuiTheme.SpacedText(new Rect(88f, 122f, 700f, 24f), "A VILLAGE OF EMBER AND OATH", 13, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.76f), 5.2f, TextAnchor.MiddleLeft);
            ImGuiTheme.HLine(88f, 306f, 154f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.5f), 1f);

            ImGuiTheme.SpacedText(new Rect(900f, 50f, 606f, 20f), "UNITY EDITION", 11, ImGuiTheme.Gold, 3.2f, TextAnchor.MiddleRight);
            ImGuiTheme.Text(new Rect(740f, 76f, 766f, 22f), "Powered by Md. Atikur Rahman Mridha", 14, ImGuiTheme.Parchment, TextAnchor.MiddleRight);
            ImGuiTheme.Text(new Rect(740f, 100f, 766f, 20f), "A 3D open world of villages, wilds and crypts", 11, ImGuiTheme.Muted, TextAnchor.MiddleRight);

            var back = new Rect(1180f, 126f, 326f, 22f);
            bool backHover = back.Contains(Event.current.mousePosition);
            ImGuiTheme.Text(back, game.IsGuest ? "<  RETURN TO CAMP" : "<  RETURN TO " + game.PlayerName.ToUpperInvariant(), 10,
                backHover ? ImGuiTheme.GoldBright : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f), TextAnchor.MiddleRight);
            if (!confirmQuit && ImGuiTheme.Clicked(back)) game.EnterLanding();

            ImGuiTheme.SpacedText(new Rect(340f, 178f, 920f, 26f), "CHOOSE YOUR ADVENTURER", 17, ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.9f), 6f, TextAnchor.MiddleCenter);
            ImGuiTheme.HLine(430f, 610f, 219f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.28f));
            ImGuiTheme.HLine(990f, 1170f, 219f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.28f));

            for (int i = 0; i < HeroRoster.Heroes.Length; i++)
            {
                var baseRect = new Rect(215f + i * 400f, 240f, 370f, 468f);
                var hero = HeroRoster.Heroes[i];
                bool isSelected = i == selected;
                if (isSelected) baseRect.y -= 8f;
                bool isHovered = !confirmQuit && baseRect.Contains(Event.current.mousePosition);
                hovered = isHovered ? i : hovered;
                DrawHeroCard(hero, i, baseRect, isSelected, isHovered);
            }

            bool pressed = false;
            if (!confirmQuit)
            {
                if (ImGuiTheme.Button(new Rect(450f, 748f, 360f, 56f), "BEGIN THE ADVENTURE", true))
                {
                    pressed = true;
                    BeginAdventure();
                }
                if (!pressed && ImGuiTheme.Button(new Rect(826f, 748f, 208f, 56f), "QUICK DUEL"))
                {
                    pressed = true;
                    BeginQuickDuel();
                }
                if (!pressed && ImGuiTheme.Button(new Rect(1050f, 748f, 100f, 56f), "QUIT"))
                {
                    confirmQuit = true;
                }
            }

            ImGuiTheme.Text(new Rect(84f, 820f, 400f, 20f), "M  mute    ·    Alt+Enter  fullscreen", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.72f));
            ImGuiTheme.SpacedText(new Rect(360f, 820f, 880f, 20f), "WASD MOVE   E INTERACT   SPACE STRIKE   F HEAVY   SHIFT DODGE   Q ART", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.82f), 1.6f, TextAnchor.MiddleCenter, false);
            ImGuiTheme.Text(new Rect(1320f, 820f, 196f, 20f), "v1.4  ·  Unity 6", 10, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.6f), TextAnchor.MiddleRight);

            if (confirmQuit) DrawQuitPrompt();
            if (fade > 0.001f) ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, fade * 0.9f));

            ImGuiTheme.End();
        }

        void DrawFrame()
        {
            var frame = new Rect(42f, 30f, 1516f, 838f);
            ImGuiTheme.Panel(frame, new Color(0.02f, 0.035f, 0.027f, 0.12f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.22f));
            const float length = 26f;
            Color bright = ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.7f);
            ImGuiTheme.Line(new Vector2(frame.xMin, frame.yMin + length), new Vector2(frame.xMin, frame.yMin), bright);
            ImGuiTheme.Line(new Vector2(frame.xMin, frame.yMin), new Vector2(frame.xMin + length, frame.yMin), bright);
            ImGuiTheme.Line(new Vector2(frame.xMax - length, frame.yMin), new Vector2(frame.xMax, frame.yMin), bright);
            ImGuiTheme.Line(new Vector2(frame.xMax, frame.yMin), new Vector2(frame.xMax, frame.yMin + length), bright);
            ImGuiTheme.Line(new Vector2(frame.xMin, frame.yMax - length), new Vector2(frame.xMin, frame.yMax), bright);
            ImGuiTheme.Line(new Vector2(frame.xMin, frame.yMax), new Vector2(frame.xMin + length, frame.yMax), bright);
            ImGuiTheme.Line(new Vector2(frame.xMax - length, frame.yMax), new Vector2(frame.xMax, frame.yMax), bright);
            ImGuiTheme.Line(new Vector2(frame.xMax, frame.yMax), new Vector2(frame.xMax, frame.yMax - length), bright);
            ImGuiTheme.HLine(84f, 1516f, 164f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.14f));
        }

        void DrawEmbers()
        {
            for (int i = 0; i < embers.Count; i++)
            {
                var ember = embers[i];
                float sway = Mathf.Sin(emberTime * 0.55f + ember.Phase) * 16f;
                float flicker = 0.62f + 0.38f * Mathf.Sin(emberTime * 1.7f + ember.Phase * 2f);
                Color color = i % 3 == 0 ? ImGuiTheme.Ember : ImGuiTheme.Gold;
                color.a = ember.Alpha * flicker;
                var previous = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(new Rect(ember.X + sway, ember.Y, ember.Size, ember.Size), ImageAssets.Dot, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }
        }

        void DrawHeroCard(HeroDef hero, int index, Rect rect, bool isSelected, bool isHovered)
        {
            Color border = isSelected
                ? ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.92f)
                : ImGuiTheme.WithAlpha(ImGuiTheme.Gold, isHovered ? 0.65f : 0.26f);
            Color fill = isSelected
                ? new Color(0.045f, 0.075f, 0.055f, 0.92f)
                : new Color(0.03f, 0.05f, 0.038f, 0.84f);

            if (isSelected)
            {
                ImGuiTheme.Fill(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height), ImGuiTheme.WithAlpha(hero.Color, 0.09f));
            }

            ImGuiTheme.Panel(rect, fill, border, isSelected ? 2f : 1f, isSelected);

            var portrait = new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 210f);
            var glowRect = new Rect(portrait.center.x - 120f, portrait.center.y - 70f + 34f, 240f, 240f);
            var previous = GUI.color;
            GUI.color = ImGuiTheme.WithAlpha(hero.Accent, isSelected ? 0.22f : 0.1f);
            GUI.DrawTexture(glowRect, ImageAssets.Dot, ScaleMode.StretchToFill, true);
            GUI.color = previous;

            ImGuiTheme.TextureFit(portrait, ImageAssets.CharacterTexture(hero.SpriteName), Color.white, true);
            ImGuiTheme.HLine(rect.x + 28f, rect.xMax - 28f, rect.y + 234f, ImGuiTheme.WithAlpha(hero.Accent, isSelected ? 0.65f : 0.25f), 1f);
            if (isSelected) ImGuiTheme.Fill(new Rect(rect.x + 28f, rect.y + 234f, 34f, 2f), hero.Accent);

            ImGuiTheme.Text(new Rect(rect.x + 26f, rect.y + 242f, rect.width - 52f, 36f), hero.Name, 28, isSelected ? ImGuiTheme.GoldBright : ImGuiTheme.Parchment, TextAnchor.MiddleLeft, true);
            ImGuiTheme.SpacedText(new Rect(rect.x + 27f, rect.y + 280f, rect.width - 54f, 18f), hero.Title.ToUpperInvariant(), 10, ImGuiTheme.WithAlpha(hero.Accent, 0.95f), 2.2f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Label(new Rect(rect.x + 26f, rect.y + 302f, rect.width - 52f, 52f), hero.Blurb, ImGuiTheme.WrapStyle(12), ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.72f), TextAnchor.UpperLeft, false);

            DrawStatRow(new Rect(rect.x + 26f, rect.y + 358f, rect.width - 52f, 14f), "BLADE", hero.Blade, hero.Accent);
            DrawStatRow(new Rect(rect.x + 26f, rect.y + 378f, rect.width - 52f, 14f), "GUARD", hero.Guard, hero.Accent);
            DrawStatRow(new Rect(rect.x + 26f, rect.y + 398f, rect.width - 52f, 14f), "SPIRIT", hero.Spirit, hero.Accent);

            ImGuiTheme.Text(new Rect(rect.x + 26f, rect.y + 422f, rect.width - 52f, 20f), "ART  ·  " + hero.AbilityName, 12, ImGuiTheme.Gold, TextAnchor.MiddleLeft, true);
            ImGuiTheme.Label(new Rect(rect.x + 26f, rect.y + 442f, rect.width - 52f, 24f), hero.AbilityBlurb, ImGuiTheme.WrapStyle(10), ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.92f), TextAnchor.UpperLeft, false);

            // IMGUI hands the click to the first control under the mouse, so the card hit
            // area must not exist while the leave prompt covers it.
            if (!confirmQuit && GUI.Button(rect, GUIContent.none, GUIStyle.none) && index != selected)
            {
                Select(index);
            }
        }

        void DrawStatRow(Rect rect, string label, int value, Color color)
        {
            ImGuiTheme.SpacedText(new Rect(rect.x, rect.y, 84f, rect.height), label, 9, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.85f), 1.4f, TextAnchor.MiddleLeft, false);
            ImGuiTheme.Pips(new Rect(rect.x + 84f, rect.y + 4f, 126f, 6f), value, 5, color);
        }

        void DrawQuitPrompt()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, 0.72f));
            var panel = new Rect(570f, 326f, 460f, 238f);
            ImGuiTheme.Panel(panel, new Color(0.03f, 0.05f, 0.039f, 0.98f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.65f), 1f, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 30f, panel.width - 60f, 30f), "LEAVE ZULVYRE?", 17, ImGuiTheme.GoldBright, 4f, TextAnchor.MiddleCenter);
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 76f, panel.width - 60f, 24f), "The lanterns will wait by the river.", 13, ImGuiTheme.Parchment, TextAnchor.MiddleCenter);
            if (ImGuiTheme.Button(new Rect(panel.x + 44f, panel.y + 152f, 170f, 50f), "STAY"))
            {
                confirmQuit = false;
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 246f, panel.y + 152f, 170f, 50f), "QUIT TO DESKTOP"))
            {
                game.QuitToDesktop();
            }
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 206f, panel.width - 60f, 18f), "ENTER  quit    ·    ESC  stay", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.75f), TextAnchor.MiddleCenter);
        }
    }
}
