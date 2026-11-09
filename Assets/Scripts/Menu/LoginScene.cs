using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public class LoginScene : MonoBehaviour
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

        enum Mode
        {
            Login,
            Create
        }

        readonly List<Ember> embers = new List<Ember>();
        ZulvyreGame game;
        Mode mode = Mode.Login;
        string account = string.Empty;
        string password = string.Empty;
        string notice = string.Empty;
        bool noticeError;
        bool confirmQuit;
        float fade = 1f;
        float emberTime;
        int focusField;
        bool requestFocus;

        public void Enter()
        {
            game = ZulvyreGame.Instance;
            mode = Mode.Login;
            account = AccountStore.LastAccount;
            password = string.Empty;
            notice = string.IsNullOrEmpty(account) ? string.Empty : "Welcome back. Your account is filled in.";
            noticeError = false;
            confirmQuit = false;
            fade = 1f;
            focusField = string.IsNullOrEmpty(account) ? 0 : 1;
            requestFocus = true;
            EnsureEmbers();
        }

        void EnsureEmbers()
        {
            if (embers.Count > 0) return;
            var rng = new System.Random(491);
            for (int i = 0; i < 40; i++)
            {
                embers.Add(new Ember
                {
                    X = (float)rng.NextDouble() * 1600f,
                    Y = (float)rng.NextDouble() * 900f,
                    Speed = 6f + (float)rng.NextDouble() * 22f,
                    Size = 2f + (float)rng.NextDouble() * 4f,
                    Alpha = 0.12f + (float)rng.NextDouble() * 0.38f,
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

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                focusField = focusField == 0 ? 1 : 0;
                requestFocus = true;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Submit();
        }

        void Submit()
        {
            string error;
            if (mode == Mode.Login)
            {
                string name = AccountStore.Verify(account, password, out error);
                if (name == null)
                {
                    Fail(error);
                    return;
                }
                game.SignIn(name, false);
                game.SetHero(AccountStore.HeroFor(name, game.SelectedIndex));
                game.EnterLanding();
                return;
            }

            string created = AccountStore.Create(account, password, game.SelectedIndex, out error);
            if (created == null)
            {
                Fail(error);
                return;
            }
            game.SignIn(created, false);
            game.EnterLanding();
        }

        void Fail(string error)
        {
            notice = error;
            noticeError = true;
            if (game != null && game.Audio != null) game.Audio.PlayClick(0.3f);
        }

        void PlayGuest()
        {
            game.SignIn("Wayfarer", true);
            game.EnterLanding();
        }

        void OnGUI()
        {
            if (game == null) game = ZulvyreGame.Instance;
            ImGuiTheme.Begin();

            ImGuiTheme.Backdrop(ImageAssets.BackgroundTexture("village"), new Color(0.3f, 0.32f, 0.27f, 1f));
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0.012f, 0.022f, 0.017f, 0.66f));
            DrawEmbers();
            ImGuiTheme.Vignette(0.94f);
            DrawFrame();

            DrawTitle();
            DrawPanel();
            DrawFooter();

            if (confirmQuit) DrawQuitPrompt();
            if (fade > 0.001f)
            {
                ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, fade * 0.92f));
            }

            requestFocus = false;
            ImGuiTheme.End();
        }

        void DrawFrame()
        {
            var frame = new Rect(42f, 30f, 1516f, 838f);
            ImGuiTheme.Panel(frame, new Color(0.02f, 0.035f, 0.027f, 0.16f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.22f));
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
        }

        void DrawTitle()
        {
            ImGuiTheme.SpacedText(new Rect(96f, 132f, 640f, 84f), "ZULVYRE", 60, ImGuiTheme.GoldBright, 7.5f, TextAnchor.MiddleLeft);
            ImGuiTheme.SpacedText(new Rect(100f, 212f, 620f, 24f), "A VILLAGE OF EMBER AND OATH", 12, ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.78f), 4.6f, TextAnchor.MiddleLeft);
            ImGuiTheme.HLine(100f, 252f, 254f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.5f), 1f);

            ImGuiTheme.Label(new Rect(100f, 296f, 380f, 88f),
                "Sign the ledger before you cross the bridge. The lantern duel keeps its own book, and it never forgets a name.",
                ImGuiTheme.WrapStyle(14), ImGuiTheme.WithAlpha(ImGuiTheme.Parchment, 0.7f), TextAnchor.UpperLeft, false);

            ImGuiTheme.SpacedText(new Rect(100f, 430f, 600f, 20f), "ACCOUNTS ARE KEPT ON THIS MACHINE", 9, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.8f), 2.2f, TextAnchor.MiddleLeft);
            ImGuiTheme.HLine(100f, 250f, 470f, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.22f));

            ImGuiTheme.SpacedText(new Rect(880f, 88f, 560f, 20f), "UNITY EDITION", 10, ImGuiTheme.Gold, 3f, TextAnchor.MiddleRight);
            ImGuiTheme.Text(new Rect(720f, 110f, 720f, 22f), "Powered by Md. Atikur Rahman Mridha", 13, ImGuiTheme.Parchment, TextAnchor.MiddleRight);
        }

        void DrawPanel()
        {
            var panel = new Rect(800f, 190f, 640f, 590f);
            ImGuiTheme.Panel(panel, new Color(0.028f, 0.045f, 0.034f, 0.93f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.45f), 1f, true);

            var loginTab = new Rect(panel.x + 34f, panel.y + 30f, 276f, 46f);
            var createTab = new Rect(panel.x + 330f, panel.y + 30f, 276f, 46f);
            if (ImGuiTheme.Button(loginTab, "LOG IN", mode == Mode.Login, true, 13))
            {
                if (mode != Mode.Login) notice = string.Empty;
                mode = Mode.Login;
                focusField = 1;
                requestFocus = true;
            }
            if (ImGuiTheme.Button(createTab, "CREATE ACCOUNT", mode == Mode.Create, true, 13))
            {
                if (mode != Mode.Create) notice = "Pick a name and a password of four or more characters.";
                mode = Mode.Create;
                focusField = 1;
                requestFocus = true;
            }

            DrawField(panel.x + 34f, panel.y + 118f, 572f, "ACCOUNT", ref account, 16, false, 0);
            DrawField(panel.x + 34f, panel.y + 210f, 572f, "PASSWORD", ref password, 24, true, 1);

            Color noticeColor = noticeError ? ImGuiTheme.Blood : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.95f);
            ImGuiTheme.Label(new Rect(panel.x + 34f, panel.y + 306f, 572f, 40f), notice, ImGuiTheme.WrapStyle(11), noticeColor, TextAnchor.UpperLeft, false);

            string primary = mode == Mode.Login ? "ENTER THE VILLAGE" : "SWEAR THE OATH";
            if (ImGuiTheme.Button(new Rect(panel.x + 34f, panel.y + 356f, 572f, 58f), primary))
            {
                Submit();
            }
            if (ImGuiTheme.Button(new Rect(panel.x + 34f, panel.y + 430f, 572f, 48f), "PLAY AS GUEST"))
            {
                PlayGuest();
            }

            ImGuiTheme.Text(new Rect(panel.x + 34f, panel.y + 498f, 572f, 22f),
                "ENTER submit    ·    TAB next field    ·    ESC quit", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.7f), TextAnchor.MiddleCenter);

            if (ImGuiTheme.Clicked(new Rect(panel.x + 190f, panel.y + 528f, 260f, 26f)))
            {
                confirmQuit = true;
            }
            bool hovering = new Rect(panel.x + 190f, panel.y + 528f, 260f, 26f).Contains(Event.current.mousePosition);
            ImGuiTheme.Text(new Rect(panel.x + 190f, panel.y + 528f, 260f, 26f), "QUIT TO DESKTOP", 10,
                hovering ? ImGuiTheme.GoldBright : ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.62f), TextAnchor.MiddleCenter);
        }

        void DrawField(float x, float y, float width, string label, ref string value, int maxLength, bool mask, int index)
        {
            ImGuiTheme.SpacedText(new Rect(x, y - 24f, width, 18f), label, 9, ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.9f), 2.4f, TextAnchor.MiddleLeft, false);

            var box = new Rect(x, y, width, 52f);
            bool focused = focusField == index;
            bool hover = box.Contains(Event.current.mousePosition);
            ImGuiTheme.Fill(box, new Color(0.015f, 0.026f, 0.02f, 0.92f));
            ImGuiTheme.Panel(box, new Color(0f, 0f, 0f, 0f),
                focused ? ImGuiTheme.WithAlpha(ImGuiTheme.GoldBright, 0.95f) : ImGuiTheme.WithAlpha(ImGuiTheme.Gold, hover ? 0.6f : 0.3f), 1f);
            if (focused) ImGuiTheme.Fill(new Rect(box.x, box.yMax - 2f, box.width, 2f), ImGuiTheme.GoldBright);

            if (box.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
            {
                focusField = index;
                requestFocus = true;
            }

            var inner = new Rect(box.x + 14f, box.y + 8f, box.width - 28f, box.height - 16f);
            if (index == 0)
            {
                GUI.SetNextControlName("zv-login-account");
                if (requestFocus && focusField == 0) GUI.FocusControl("zv-login-account");
                value = GUI.TextField(inner, value, maxLength, ImGuiTheme.FieldStyle(17));
            }
            else
            {
                GUI.SetNextControlName("zv-login-password");
                if (requestFocus && focusField == 1) GUI.FocusControl("zv-login-password");
                value = GUI.PasswordField(inner, value, '*', maxLength, ImGuiTheme.FieldStyle(18));
            }

            if (!string.IsNullOrEmpty(value))
            {
                var clear = new Rect(box.xMax - 34f, box.y + 14f, 24f, 24f);
                if (ImGuiTheme.Clicked(clear))
                {
                    value = string.Empty;
                    focusField = index;
                    requestFocus = true;
                }
                ImGuiTheme.Text(clear, "×", 14, ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.75f), TextAnchor.MiddleCenter, false);
            }
        }

        void DrawFooter()
        {
            ImGuiTheme.Text(new Rect(96f, 806f, 500f, 20f), "Your account also carries your chosen hero.", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.66f));
            ImGuiTheme.Text(new Rect(1260f, 806f, 250f, 20f), "v1.4  ·  Unity 6", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.6f), TextAnchor.MiddleRight);
        }

        void DrawEmbers()
        {
            for (int i = 0; i < embers.Count; i++)
            {
                var ember = embers[i];
                float sway = Mathf.Sin(emberTime * 0.5f + ember.Phase) * 14f;
                float flicker = 0.6f + 0.4f * Mathf.Sin(emberTime * 1.6f + ember.Phase * 2f);
                Color color = i % 3 == 0 ? ImGuiTheme.Ember : ImGuiTheme.Gold;
                color.a = ember.Alpha * flicker;
                var previous = GUI.color;
                GUI.color = color;
                GUI.DrawTexture(new Rect(ember.X + sway, ember.Y, ember.Size, ember.Size), ImageAssets.Dot, ScaleMode.StretchToFill, true);
                GUI.color = previous;
            }
        }

        void DrawQuitPrompt()
        {
            ImGuiTheme.Fill(new Rect(0f, 0f, ImGuiTheme.RefWidth, ImGuiTheme.RefHeight), new Color(0f, 0f, 0f, 0.72f));
            var panel = new Rect(570f, 326f, 460f, 238f);
            ImGuiTheme.Panel(panel, new Color(0.03f, 0.05f, 0.039f, 0.98f), ImGuiTheme.WithAlpha(ImGuiTheme.Gold, 0.65f), 1f, true);
            ImGuiTheme.SpacedText(new Rect(panel.x + 30f, panel.y + 30f, panel.width - 60f, 30f), "LEAVE ZULVYRE?", 17, ImGuiTheme.GoldBright, 4f, TextAnchor.MiddleCenter);
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 76f, panel.width - 60f, 24f), "The ledger stays open.", 13, ImGuiTheme.Parchment, TextAnchor.MiddleCenter);
            if (ImGuiTheme.Button(new Rect(panel.x + 44f, panel.y + 152f, 170f, 50f), "STAY")) confirmQuit = false;
            if (ImGuiTheme.Button(new Rect(panel.x + 246f, panel.y + 152f, 170f, 50f), "QUIT TO DESKTOP")) game.QuitToDesktop();
            ImGuiTheme.Text(new Rect(panel.x + 30f, panel.y + 206f, panel.width - 60f, 18f), "ENTER  quit    ·    ESC  stay", 10,
                ImGuiTheme.WithAlpha(ImGuiTheme.Muted, 0.75f), TextAnchor.MiddleCenter);
        }
    }
}
