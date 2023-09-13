using System.Drawing;

namespace WebBrowser
{
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

        public int yMin
        {
            readonly get => Y;
            set => Y = value;
        }
        public int xMin
        {
            readonly get => X;
            set => X = value;
        }
        public int yMax
        {
            readonly get => Y + Height;
            set => Y = value - Height;
        }
        public int xMax
        {
            readonly get => X + Width;
            set => X = value - Width;
        }

        public readonly Vector2Int size => new(Width, Height);

        public Vector2Int Position
        {
            readonly get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public readonly Vector2Int TopLeft => new(xMin, yMin);
        public readonly Vector2Int TopRight => new(xMax, yMin);
        public readonly Vector2Int BottomLeft => new(xMin, yMax);
        public readonly Vector2Int BottomRight => new(xMax, yMax);

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
    }

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

        public float yMin
        {
            readonly get => Y;
            set => Y = value;
        }
        public float xMin
        {
            readonly get => X;
            set => X = value;
        }
        public float yMax
        {
            readonly get => Y + Height;
            set => Y = value - Height;
        }
        public float xMax
        {
            readonly get => X + Width;
            set => X = value - Width;
        }

        public readonly Vector2 size => new(Width, Height);

        public Vector2 Position
        {
            readonly get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }
    }
}
