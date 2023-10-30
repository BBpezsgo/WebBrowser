using ProgrammingLanguage.Css;
using static SDL2.SDL;
using static SDL2.SDL_ttf;

namespace WebBrowser
{
    internal readonly struct Renderer : IDisposable
    {
        readonly IntPtr Handle;
        readonly List<TextInstance> TextInstances;
        readonly List<FontInstance> FontInstances;

        public Renderer(IntPtr handle)
        {
            Handle = handle;
            TextInstances = new List<TextInstance>();
            FontInstances = new List<FontInstance>();
        }

        TextInstance? GetTextInstance(string text, int fontSize, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (TextInstance textInstance in TextInstances)
            {
                if (!string.Equals(textInstance.Text, text)) continue;
                if (textInstance.FontSize != fontSize) continue;
                if (textInstance.Color != color) continue;
                return textInstance;
            }

            FontInstance fontInstance = GetFontInstance(fontSize);

            TextInstance newInstance = new(Handle, fontInstance.Font, fontSize, text, color);
            TextInstances.Add(newInstance);
            return newInstance;
        }

        TextInstance? GetTextInstance(string text, int fontSize)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (TextInstance textInstance in TextInstances)
            {
                if (!string.Equals(textInstance.Text, text)) continue;
                if (textInstance.FontSize != fontSize) continue;
                return textInstance;
            }

            FontInstance fontInstance = GetFontInstance(fontSize);

            TextInstance newInstance = new(Handle, fontInstance.Font, fontSize, text, Color.White);
            TextInstances.Add(newInstance);
            return newInstance;
        }

        FontInstance GetFontInstance(int fontSize)
        {
            foreach (FontInstance fontInstance in FontInstances)
            {
                if (fontInstance.FontSize != fontSize) continue;
                return fontInstance;
            }

            FontInstance newInstance = FontInstance.Create("C:\\Users\\bazsi\\source\\repos\\WebBrowser\\Font.ttf", fontSize);
            FontInstances.Add(newInstance);
            return newInstance;
        }

        public void Dispose()
        {
            Utils.DisposeAndClear(TextInstances);
            Utils.DisposeAndClear(FontInstances);
        }

        public void DrawText(RectInt rect, string? text, int fontSize, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            TextInstance? textInstance = GetTextInstance(text, fontSize, color);

            if (textInstance == null) return;

            SDL_Rect dest = new()
            {
                x = rect.X,
                y = rect.Y,
                w = textInstance.Width,
                h = textInstance.Height,
            };

            SDL_RenderCopy(Handle, textInstance.Texture, IntPtr.Zero, ref dest);
        }

        public readonly void FillRect(RectInt rect, Color color)
        {
            SDL_Rect _rect = rect;

            SDL_SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);

            SDL_RenderDrawRect(Handle, ref _rect);
        }

        public readonly void OutlineRect(RectInt rect, Color color)
        {
            SDL_SetRenderDrawColor(Handle, color.R, color.G, color.B, color.A);

            SDL_Point[] points = new SDL_Point[]
            {
                rect.TopLeft,
                rect.TopRight,
                rect.BottomRight,
                rect.BottomLeft,
            };

            SDL_RenderDrawLines(Handle, points, points.Length);

        }

        public Vector2Int MeasureText(string text, int fontSize)
        {
            TextInstance? textInstance = GetTextInstance(text, fontSize);
            if (textInstance == null) return Vector2Int.Zero;
            return new Vector2Int(textInstance.Width, textInstance.Height);
        }
    }
}
