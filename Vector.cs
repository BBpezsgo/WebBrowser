using System.Diagnostics;

namespace WebBrowser
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public struct Vector2
    {
        public float X;
        public float Y;

        public static Vector2 Zero => new(0, 0);

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public readonly Vector2Int RoundToInt() => new(Utils.RoundToInt(X), Utils.RoundToInt(Y));

        public static Vector2 operator +(Vector2 a, Vector2 b)
            => new(a.X + b.X, a.Y + b.Y);

        public static Vector2 operator -(Vector2 a, Vector2 b)
            => new(a.X - b.X, a.Y - b.Y);

        public static Vector2 operator *(Vector2 a, float b)
            => new(a.X * b, a.Y * b);

        public static Vector2 operator /(Vector2 a, float b)
            => new(a.X / b, a.Y / b);

        public override readonly string ToString() => $"({X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {Y.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        readonly string GetDebuggerDisplay() => ToString();
    }

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public struct Vector2Int
    {
        public int X;
        public int Y;

        public static Vector2Int Zero => new(0, 0);

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly Vector2 ToFloat() => new(X, Y);

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
            => new(a.X + b.X, a.Y + b.Y);

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
            => new(a.X - b.X, a.Y - b.Y);

        public static Vector2Int operator *(Vector2Int a, int b)
            => new(a.X * b, a.Y * b);

        public static Vector2Int operator /(Vector2Int a, int b)
            => new(a.X / b, a.Y / b);

        public static implicit operator SDL2.SDL.SDL_Point(Vector2Int v) => new()
        {
            x = v.X,
            y = v.Y,
        };

        public override readonly string ToString() => $"({X}, {Y})";
        readonly string GetDebuggerDisplay() => ToString();
    }
}
