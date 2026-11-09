using System.Collections.Generic;
using UnityEngine;

namespace Zulvyre
{
    public static class ImGuiTheme
    {
        public const float RefWidth = 1600f;
        public const float RefHeight = 900f;

        public static readonly Color Ink = Hex("0b110d");
        public static readonly Color Parchment = Hex("e9ddc2");
        public static readonly Color Muted = Hex("a6a891");
        public static readonly Color Gold = Hex("d3a75f");
        public static readonly Color GoldBright = Hex("f4d79e");
        public static readonly Color Ember = Hex("d1663c");
        public static readonly Color Blood = Hex("c95546");
        public static readonly Color Moss = Hex("7fa06a");
        public static readonly Color Night = new Color(0.02f, 0.035f, 0.026f, 0.88f);

        public static AudioBank Audio;
        public static float LastScale { get; private set; } = 1f;
        public static Vector2 LastOffset { get; private set; }

        public static GUIStyle DisplayStyle;
        public static GUIStyle HeadingStyle;
        public static GUIStyle BodyStyle;
        public static GUIStyle SmallStyle;
        public static GUIStyle TinyStyle;
        public static GUIStyle MonoStyle;

        static Font displayFont;
        static Font bodyFont;
        static Font monoFont;
        static bool ready;
        static readonly Dictionary<int, GUIStyle> LabelStyles = new Dictionary<int, GUIStyle>();
        static readonly Dictionary<int, GUIStyle> SerifStyles = new Dictionary<int, GUIStyle>();
        static readonly Dictionary<int, GUIStyle> WrapStyles = new Dictionary<int, GUIStyle>();
        static readonly Dictionary<int, GUIStyle> FieldStyles = new Dictionary<int, GUIStyle>();
        static Texture2D white;

        static Texture2D White
        {
            get
            {
                if (white == null) white = ImageAssets.White;
                return white;
            }
        }

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out var color) ? color : Color.white;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static void Begin()
        {
            EnsureReady();

            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            scale = Mathf.Max(0.1f, scale);
            LastScale = scale;
            LastOffset = new Vector2((Screen.width - RefWidth * scale) * 0.5f, (Screen.height - RefHeight * scale) * 0.5f);

            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.black;
            float offsetX = LastOffset.x;
            float offsetY = LastOffset.y;
            if (offsetY > 0.5f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, offsetY + 1f), White);
                GUI.DrawTexture(new Rect(0f, Screen.height - offsetY - 1f, Screen.width, offsetY + 1f), White);
            }
            if (offsetX > 0.5f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, offsetX + 1f, Screen.height), White);
                GUI.DrawTexture(new Rect(Screen.width - offsetX - 1f, 0f, offsetX + 1f, Screen.height), White);
            }
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        public static void End()
        {
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }

        public static Vector2 ScreenToVirtual(Vector2 screenPoint)
        {
            return (screenPoint - LastOffset) / Mathf.Max(0.001f, LastScale);
        }

        public static Vector2 VirtualToScreen(Vector2 virtualPoint)
        {
            return virtualPoint * LastScale + LastOffset;
        }

        // Left-button release over an area that is not a real GUI.Button. Call this after
        // Buttons are drawn: GUI.Button marks the event used, so its clicks never reach here.
        public static bool Clicked(Rect rect)
        {
            Event current = Event.current;
            return current != null && current.type == EventType.MouseUp && current.button == 0 && rect.Contains(current.mousePosition);
        }

        static void EnsureReady()
        {
            if (ready) return;

            displayFont = LoadFont(new[] { "Georgia", "Palatino Linotype", "Book Antiqua", "Times New Roman" }, 44);
            bodyFont = LoadFont(new[] { "Segoe UI", "Trebuchet MS", "Verdana", "Arial" }, 18);
            monoFont = LoadFont(new[] { "Consolas", "Cascadia Mono", "Courier New" }, 16);

            DisplayStyle = MakeStyle(displayFont, 54, TextAnchor.MiddleLeft, false);
            HeadingStyle = MakeStyle(displayFont, 30, TextAnchor.MiddleLeft, false);
            BodyStyle = MakeStyle(bodyFont, 17, TextAnchor.UpperLeft, true);
            SmallStyle = MakeStyle(bodyFont, 13, TextAnchor.MiddleLeft, false);
            TinyStyle = MakeStyle(bodyFont, 11, TextAnchor.MiddleLeft, false);
            MonoStyle = MakeStyle(monoFont, 14, TextAnchor.MiddleLeft, false);
            ready = true;
        }

        static GUIStyle MakeStyle(Font font, int size, TextAnchor anchor, bool wrap)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = font != null ? font : GUI.skin.font,
                fontSize = size,
                alignment = anchor,
                wordWrap = wrap,
                richText = true,
                clipping = TextClipping.Overflow
            };
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            return style;
        }

        static Font LoadFont(string[] names, int size)
        {
            try
            {
                var font = Font.CreateDynamicFontFromOSFont(names, size);
                if (font != null) return font;
            }
            catch (System.Exception)
            {
                // Fall through to the built-in font.
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static GUIStyle StyleFor(int size, bool serif)
        {
            var cache = serif ? SerifStyles : LabelStyles;
            int key = Mathf.Clamp(size, 8, 96);
            if (cache.TryGetValue(key, out var style)) return style;
            style = new GUIStyle(GUI.skin.label)
            {
                font = serif ? (displayFont != null ? displayFont : GUI.skin.font) : (bodyFont != null ? bodyFont : GUI.skin.font),
                fontSize = key,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                richText = true,
                clipping = TextClipping.Overflow
            };
            style.normal.textColor = Color.white;
            cache[key] = style;
            return style;
        }

        public static GUIStyle WrapStyle(int size)
        {
            int key = Mathf.Clamp(size, 8, 96);
            if (WrapStyles.TryGetValue(key, out var style)) return style;
            style = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont != null ? bodyFont : GUI.skin.font,
                fontSize = key,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = Color.white;
            WrapStyles[key] = style;
            return style;
        }

        public static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, White);
            GUI.color = previous;
        }

        /// <summary>Frameless text entry style so panels can draw their own field border.</summary>
        public static GUIStyle FieldStyle(int size = 17)
        {
            EnsureReady();
            int key = Mathf.Clamp(size, 8, 48);
            if (FieldStyles.TryGetValue(key, out var style)) return style;

            style = new GUIStyle(GUI.skin.textField)
            {
                font = bodyFont != null ? bodyFont : GUI.skin.font,
                fontSize = key,
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                padding = new RectOffset(0, 0, 4, 0),
                border = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = null;
            style.hover.background = null;
            style.focused.background = null;
            style.active.background = null;
            style.normal.textColor = Parchment;
            style.hover.textColor = Parchment;
            style.focused.textColor = GoldBright;
            style.active.textColor = GoldBright;
            FieldStyles[key] = style;
            return style;
        }

        public static void Line(Vector2 from, Vector2 to, Color color, float thickness = 1f)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f) return;
            var previous = GUI.matrix;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, from);
            Fill(new Rect(from.x, from.y - thickness * 0.5f, delta.magnitude, thickness), color);
            GUI.matrix = previous;
        }

        public static void HLine(float x1, float x2, float y, Color color, float thickness = 1f)
        {
            Fill(new Rect(x1, y - thickness * 0.5f, x2 - x1, thickness), color);
        }

        public static void VLine(float x, float y1, float y2, Color color, float thickness = 1f)
        {
            Fill(new Rect(x - thickness * 0.5f, y1, thickness, y2 - y1), color);
        }

        public static void Panel(Rect rect, Color fill, Color? border = null, float borderWidth = 1f, bool cornerAccents = false)
        {
            Fill(rect, fill);
            if (border.HasValue)
            {
                Color borderColor = border.Value;
                HLine(rect.xMin, rect.xMax, rect.yMin, borderColor, borderWidth);
                HLine(rect.xMin, rect.xMax, rect.yMax, borderColor, borderWidth);
                VLine(rect.xMin, rect.yMin, rect.yMax, borderColor, borderWidth);
                VLine(rect.xMax, rect.yMin, rect.yMax, borderColor, borderWidth);
                if (cornerAccents)
                {
                    Color bright = WithAlpha(GoldBright, Mathf.Max(0.5f, borderColor.a));
                    float length = 16f;
                    Line(new Vector2(rect.xMin, rect.yMin + length), new Vector2(rect.xMin, rect.yMin), bright, 2f);
                    Line(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin + length, rect.yMin), bright, 2f);
                    Line(new Vector2(rect.xMax - length, rect.yMax), new Vector2(rect.xMax, rect.yMax), bright, 2f);
                    Line(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMax - length), bright, 2f);
                }
            }
        }

        public static void Backdrop(Texture2D texture, Color tint)
        {
            Fill(new Rect(0f, 0f, RefWidth, RefHeight), Color.black);
            if (texture == null) return;
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(0f, 0f, RefWidth, RefHeight), texture, ScaleMode.ScaleAndCrop, true);
            GUI.color = previous;
        }

        public static void Vignette(float alpha = 1f)
        {
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, RefWidth, RefHeight), ImageAssets.Vignette, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        public static void Label(Rect rect, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool shadow = true)
        {
            TextAnchor previousAnchor = style.alignment;
            Color previousContent = GUI.contentColor;
            style.alignment = anchor;

            if (shadow)
            {
                GUI.contentColor = new Color(0f, 0f, 0f, color.a * 0.6f);
                GUI.Label(new Rect(rect.x + 1.5f, rect.y + 2f, rect.width, rect.height), text, style);
            }

            GUI.contentColor = color;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContent;
            style.alignment = previousAnchor;
        }

        public static void Text(Rect rect, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool serif = false, bool shadow = true)
        {
            Label(rect, text, StyleFor(size, serif), color, anchor, shadow);
        }

        public static void SpacedText(Rect rect, string text, int size, Color color, float spacing, TextAnchor anchor = TextAnchor.MiddleCenter, bool serif = true)
        {
            GUIStyle style = StyleFor(size, serif);
            TextAnchor previous = style.alignment;
            Color previousContent = GUI.contentColor;
            style.alignment = TextAnchor.MiddleLeft;

            float total = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                total += style.CalcSize(new GUIContent(text[i].ToString())).x + (i < text.Length - 1 ? spacing : 0f);
            }

            float x = rect.x;
            if (anchor == TextAnchor.MiddleCenter) x = rect.x + (rect.width - total) * 0.5f;
            else if (anchor == TextAnchor.MiddleRight) x = rect.xMax - total;

            GUI.contentColor = color;
            for (int i = 0; i < text.Length; i++)
            {
                string glyph = text[i].ToString();
                float width = style.CalcSize(new GUIContent(glyph)).x;
                GUI.Label(new Rect(x, rect.y, width + 4f, rect.height), glyph, style);
                x += width + spacing;
            }

            GUI.contentColor = previousContent;
            style.alignment = previous;
        }

        public static bool Button(Rect rect, string text, bool primary = false, bool enabled = true, int fontSize = 16)
        {
            bool hover = enabled && rect.Contains(Event.current.mousePosition);
            Color fill = primary
                ? WithAlpha(Gold, hover ? 0.98f : 0.88f)
                : new Color(0.035f, 0.06f, 0.045f, hover ? 0.95f : 0.78f);
            Color border = primary ? GoldBright : WithAlpha(Gold, hover ? 0.95f : 0.42f);
            if (!enabled)
            {
                fill = new Color(0.05f, 0.06f, 0.05f, 0.6f);
                border = new Color(1f, 1f, 1f, 0.12f);
            }

            Panel(rect, fill, border, 1f, hover);
            if (hover)
            {
                Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 2f), WithAlpha(Color.white, 0.12f));
            }

            Color textColor = !enabled
                ? Muted
                : primary
                    ? Ink
                    : (hover ? GoldBright : Parchment);
            Label(rect, text, StyleFor(fontSize, false), textColor, TextAnchor.MiddleCenter, !primary);

            if (enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (Audio != null) Audio.PlayClick();
                return true;
            }
            return false;
        }

        public static void Bar(Rect rect, float fraction, Color fillColor, Color backgroundColor, bool bordered = true)
        {
            fraction = Mathf.Clamp01(fraction);
            Fill(rect, backgroundColor);
            if (fraction > 0f)
            {
                var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
                Fill(fillRect, fillColor);
                Fill(new Rect(fillRect.x, fillRect.y, fillRect.width, Mathf.Max(1f, fillRect.height * 0.22f)), WithAlpha(Color.white, 0.18f));
            }
            if (bordered)
            {
                Color line = new Color(0f, 0f, 0f, 0.55f);
                HLine(rect.xMin, rect.xMax, rect.yMin, line);
                HLine(rect.xMin, rect.xMax, rect.yMax, line);
            }
        }

        public static void Pips(Rect rect, int value, int maximum, Color color)
        {
            float gap = 5f;
            float width = (rect.width - gap * (maximum - 1)) / maximum;
            for (int i = 0; i < maximum; i++)
            {
                var pip = new Rect(rect.x + i * (width + gap), rect.y, width, rect.height);
                Fill(pip, i < value ? color : WithAlpha(color, 0.16f));
            }
        }

        public static void TextureFit(Rect bounds, Texture texture, Color tint, bool alignBottom = false)
        {
            if (texture == null) return;
            float aspect = texture.width / (float)texture.height;
            float width = bounds.width;
            float height = width / aspect;
            if (height > bounds.height)
            {
                height = bounds.height;
                width = height * aspect;
            }
            float x = bounds.x + (bounds.width - width) * 0.5f;
            float y = alignBottom ? bounds.yMax - height : bounds.y + (bounds.height - height) * 0.5f;
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(x, y, width, height), texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
    }
}
