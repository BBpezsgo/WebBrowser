using System.Diagnostics;

namespace WebBrowser
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public struct RectInt
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public RectInt(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectInt(Vector2Int position, Vector2Int size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        public int Top
        {
            readonly get => Y;
            set => Y = value;
        }
        public int Left
        {
            readonly get => X;
            set => X = value;
        }
        public int Bottom
        {
            readonly get => Y + Height;
            set => Y = value - Height;
        }
        public int Right
        {
            readonly get => X + Width;
            set => X = value - Width;
        }

        public readonly Vector2Int Size => new(Width, Height);

        public Vector2Int Position
        {
            readonly get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public readonly Vector2Int TopLeft => new(Left, Top);
        public readonly Vector2Int TopRight => new(Right, Top);
        public readonly Vector2Int BottomLeft => new(Left, Bottom);
        public readonly Vector2Int BottomRight => new(Right, Bottom);

        public static implicit operator SDL2.SDL.SDL_Rect(RectInt rect) => new()
        {
            x = rect.X,
            y = rect.Y,
            w = rect.Width,
            h = rect.Height,
        };

        public static implicit operator RectInt(SDL2.SDL.SDL_Rect rect) => new()
        {
            X = rect.x,
            Y = rect.y,
            Width = rect.w,
            Height = rect.h,
        };

        public override readonly string ToString() => $"({X}, {Y}, {Width}, {Height})";
        readonly string GetDebuggerDisplay() => ToString();
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public struct Rect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float Top
        {
            readonly get => Y;
            set => Y = value;
        }
        public float Left
        {
            readonly get => X;
            set => X = value;
        }
        public float Bottom
        {
            readonly get => Y + Height;
            set => Y = value - Height;
        }
        public float Right
        {
            readonly get => X + Width;
            set => X = value - Width;
        }

        public readonly Vector2 Size => new(Width, Height);

        public Vector2 Position
        {
            readonly get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public override readonly string ToString() => $"({X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {Height.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        readonly string GetDebuggerDisplay() => ToString();
    }
}
